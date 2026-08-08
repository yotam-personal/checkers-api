#!/usr/bin/env bash
# Leaderboard fidelity, asserted against a real Postgres and a real build of this service.
#
# Every case here corresponds to a defect two independent adversarial reviews found in the
# Firestore-to-Postgres port. They are kept as tests rather than as a changelog because a
# fix with no test that goes red when the fix is removed is not a fix — the same reviews
# demonstrated that by reverting nine earlier "fixes" with the suite still fully green.
#
# Usage: test/fidelity.sh [image]     (default: checkers-api:local)
set -uo pipefail
cd "$(dirname "$0")/.."
IMAGE="${1:-checkers-api:local}"
NET=ckfid-net
PG=ckfid-pg
API=ckfid-api
PORT=18099
B="http://localhost:$PORT/api"
PASS=0; FAIL=0
ok(){ echo "  ok    $1"; PASS=$((PASS+1)); }
bad(){ echo "  FAIL  $1"; FAIL=$((FAIL+1)); }
psql_(){ docker exec $PG psql -U checkers -tAc "$1" 2>/dev/null | tr -d ' '; }

cleanup(){ docker rm -f $PG $API >/dev/null 2>&1; docker network rm $NET >/dev/null 2>&1; }
trap cleanup EXIT
cleanup

docker network create $NET >/dev/null
docker run -d --name $PG --network $NET \
  -e POSTGRES_USER=checkers -e POSTGRES_PASSWORD=localpw -e POSTGRES_DB=checkers \
  postgres:17-alpine >/dev/null
for _ in $(seq 1 60); do docker exec $PG pg_isready -U checkers >/dev/null 2>&1 && break; sleep 1; done
docker run -d --name $API --network $NET -p $PORT:8080 \
  -e PGHOST=$PG -e PGUSER=checkers -e PGPASSWORD=localpw -e PGDATABASE=checkers \
  "$IMAGE" >/dev/null
for _ in $(seq 1 60); do
  [ "$(curl -s -o /dev/null -w '%{http_code}' $B/healthz 2>/dev/null)" = "200" ] && break; sleep 1
done

# The engine replays every submission, so a winning position cannot be invented. These two
# were derived from the engine's own move generator: black to move, computer playing white,
# and the single legal move takes white's last piece.
START='########1###0###################'
AFTER='#####0##########################'

# Submit a win. Echoes "<http_code> <body>".
submit(){
  local name="$1"
  local gid
  gid=$(curl -s -X POST $B/startgame -H 'Content-Type: application/json' \
        -d "{\"turnColor\":\"black\",\"computerColor\":\"white\",\"position\":\"$START\"}" \
        | python3 -c 'import sys,json;print(json.load(sys.stdin).get("id",""))' 2>/dev/null)
  [ -z "$gid" ] && { echo "000 no-game"; return; }
  python3 - "$gid" "$name" "$AFTER" "$B" <<'PY'
import json, sys, urllib.request, urllib.error
gid, name, after, base = sys.argv[1:5]
body = json.dumps({"moveRequest": {"gameId": gid, "position": after}, "winner": {"name": name}}).encode()
req = urllib.request.Request(base + "/addwinner", data=body, headers={"Content-Type": "application/json"})
try:
    with urllib.request.urlopen(req) as r:
        print(r.status, r.read().decode()[:60].replace("\n", " "))
except urllib.error.HTTPError as e:
    print(e.code, e.read().decode()[:60].replace("\n", " "))
PY
}

echo "== the cap holds under concurrency =="
# Two simultaneous wins onto a full board left SIX rows, 25 trials out of 25, because under
# READ COMMITTED neither transaction could see the other's insert and both deleted the same
# victim. getwinners' LIMIT hid it, so the board looked correct while retention had stopped.
for i in $(seq 1 6); do submit "seed$i" >/dev/null; done
OVER=0
for round in 1 2 3 4 5; do
  for i in 1 2 3 4; do submit "burst${round}x${i}" >/dev/null & done
  wait
  R=$(psql_ "select count(*) from winners")
  [ "${R:-99}" -gt 5 ] && OVER=$((OVER+1))
done
[ "$OVER" -eq 0 ] && ok "20 concurrent wins in 5 bursts never put a sixth row on the board" \
  || bad "the board exceeded five rows in $OVER of 5 concurrent bursts"

echo "== the newest accepted winner is the one displayed =="
# created_at defaulted to now(), which is transaction START time, so a slower transaction
# ranked as older than submissions that began after it — and trimmed itself away while
# still returning "Winner added successfully".
for i in $(seq 1 5); do submit "filler$i" >/dev/null; done
LAST=$(submit "the-newest-one")
echo "$LAST" | grep -q '^200' && ok "the last submission was accepted" || bad "the last submission was rejected: $LAST"
curl -s $B/getwinners > /tmp/ckfid-winners.json
grep -q "the-newest-one" /tmp/ckfid-winners.json \
  && ok "the last accepted winner is on the board" \
  || bad "a winner was told it was added and is not on the board"
FIRST=$(python3 -c "import json;d=json.load(open('/tmp/ckfid-winners.json'));print(d[0]['name'] if d else '')")
[ "$FIRST" = "the-newest-one" ] && ok "the newest winner sorts first" || bad "the board leads with '$FIRST'"

# The two assertions above only catch the defect when transactions actually interleave, which
# needs a slow insert to reproduce and did NOT go red when the fix was reverted. These two do,
# deterministically: they skew the timestamp column directly and require the board to ignore
# it. That is the real invariant — position on the leaderboard is arrival order, and no
# wall-clock column gets a vote. It was `now()`, which is transaction START time, so a
# submission that merely ran slower than its neighbours sorted below rows created after it
# and was trimmed away by its own transaction while returning "added successfully".
psql_ "update winners set created_at = '1999-01-01' where id = (select max(id) from winners)" >/dev/null
curl -s $B/getwinners > /tmp/ckfid-skew.json
SKEWED=$(python3 -c "import json;d=json.load(open('/tmp/ckfid-skew.json'));print(d[0]['name'] if d else '')")
[ "$SKEWED" = "the-newest-one" ] \
  && ok "an antique timestamp on the newest row does not move it down the board" \
  || bad "the board re-sorted on created_at and now leads with '$SKEWED'"

# And the trim must not evict it either: with a full board, the newest row is the newest row
# however it is stamped.
submit "one-more" >/dev/null
[ "$(psql_ "select count(*) from winners where name='the-newest-one'")" = "1" ] \
  && ok "the trim keeps the newest rows by arrival, not by timestamp" \
  || bad "a row was trimmed because of its timestamp rather than its position"

echo "== a bad request is not an outage =="
# One NUL byte in a name raised a PostgresException, which was treated as an unreachable
# database: it cleared readiness and stopped the durable counter for the whole probe
# interval, while Postgres was perfectly healthy.
BEFORE=$(psql_ "select value from counters where name='games_started'")
# Built in python, not by the shell. bash silently drops NUL bytes from a command
# substitution, so the shell-built version of this sent the string "badname" and proved
# nothing whatsoever — it passed against code that had no NUL handling at all.
NUL=$(python3 test/submit_nul.py "$B" "$START" "$AFTER")
echo "$NUL" | grep -q '^400' && ok "a name containing a NUL byte is refused with 400" || bad "NUL name -> $NUL"
# A file, not a pipe. `grep -q` exits at the first match, which hands curl a SIGPIPE, and
# under `set -o pipefail` the pipeline then reports failure BECAUSE the pattern matched.
curl -s $B/metrics > /tmp/ckfid-up.txt
grep -q '^checkers_database_up 1' /tmp/ckfid-up.txt \
  && ok "a refused name leaves the database reported up" \
  || bad "a refused name marked the database down"
for i in 1 2 3; do
  curl -s -o /dev/null -X POST $B/startgame -H 'Content-Type: application/json' \
    -d "{\"turnColor\":\"black\",\"computerColor\":\"white\",\"position\":\"$START\"}"
done
sleep 1
AFTERC=$(psql_ "select value from counters where name='games_started'")
[ -n "$AFTERC" ] && [ "$AFTERC" -ge "$((BEFORE + 3))" ] \
  && ok "games started after a refused name are still counted ($BEFORE -> $AFTERC)" \
  || bad "the durable counter stalled after a refused name: $BEFORE -> ${AFTERC:-nothing}"

echo "== names are bounded =="
BIG=$(submit "$(python3 -c 'print("A"*5000)')")
echo "$BIG" | grep -q '^400' && ok "an oversized name is refused with 400" || bad "a 5000-character name -> $BIG"
MAXROW=$(psql_ "select coalesce(max(length(name)),0) from winners")
[ -n "$MAXROW" ] && [ "$MAXROW" -le 64 ] && ok "no stored name exceeds the bound (longest is $MAXROW)" \
  || bad "a stored name is $MAXROW characters"

echo "== a replayed game is the caller's error =="
GID=$(curl -s -X POST $B/startgame -H 'Content-Type: application/json' \
      -d "{\"turnColor\":\"black\",\"computerColor\":\"white\",\"position\":\"$START\"}" \
      | python3 -c 'import sys,json;print(json.load(sys.stdin)["id"])')
curl -s -o /dev/null -X POST $B/addwinner -H 'Content-Type: application/json' \
  -d "{\"moveRequest\":{\"gameId\":\"$GID\",\"position\":\"$AFTER\"},\"winner\":{\"name\":\"replay\"}}"
CODE=$(curl -s -o /dev/null -w '%{http_code}' -X POST $B/addwinner -H 'Content-Type: application/json' \
  -d "{\"moveRequest\":{\"gameId\":\"$GID\",\"position\":\"$AFTER\"},\"winner\":{\"name\":\"replay\"}}")
[ "$CODE" = "400" ] && ok "replaying a consumed game is a 400, not a 500" || bad "a replayed game returned $CODE"
[ "$(psql_ "select count(*) from winners where name='replay'")" = "1" ] \
  && ok "a replayed game stored exactly one row" || bad "a replayed game did not store exactly one row"

echo "== one unreadable row does not take down the board =="
psql_ "insert into winners (name, game_sequence) values ('poison', ARRAY['nonsense'])" >/dev/null
C=$(curl -s -o /dev/null -w '%{http_code}' $B/getwinners)
[ "$C" = "200" ] && ok "the leaderboard still serves with an unreplayable row present" \
  || bad "one unreplayable row returned $C for the whole leaderboard"
psql_ "delete from winners where name='poison'" >/dev/null
# And the column should refuse an empty sequence outright.
psql_ "insert into winners (name, game_sequence) values ('empty', '{}')" >/dev/null
[ "$(psql_ "select count(*) from winners where name='empty'")" = "0" ] \
  && ok "an empty game sequence is refused by the schema" || bad "an empty game sequence was stored"

echo "== hostile names still round-trip =="
# The port must not have narrowed what a name may contain beyond the two cases Postgres
# genuinely cannot store. Hebrew, emoji, quotes and SQL metacharacters all held before.
for n in 'יותם פלד' '😀🏆' "Robert'); DROP TABLE winners;--" 'a,b{c}\d"e'; do
  R=$(submit "$n")
  echo "$R" | grep -q '^200' || { bad "a legitimate name was refused: $n -> $R"; continue; }
  curl -s $B/getwinners > /tmp/ckfid-rt.json
  python3 -c "
import json,sys
d=json.load(open('/tmp/ckfid-rt.json'))
sys.exit(0 if any(w['name']==sys.argv[1] for w in d) else 1)" "$n" \
    && ok "round-trips unchanged: $n" || bad "did not round-trip unchanged: $n"
done
[ "$(psql_ "select count(*) from winners")" = "5" ] && ok "still exactly five rows at the end" \
  || bad "the board holds $(psql_ "select count(*) from winners") rows"

echo "== a drifted schema is not reported healthy =="
docker rm -f $API >/dev/null 2>&1
docker exec $PG psql -U checkers -c "DROP TABLE winners" >/dev/null 2>&1
docker exec $PG psql -U checkers -c "CREATE TABLE winners (id text, whatever int)" >/dev/null 2>&1
docker run -d --name $API --network $NET -p $PORT:8080 \
  -e PGHOST=$PG -e PGUSER=checkers -e PGPASSWORD=localpw -e PGDATABASE=checkers \
  "$IMAGE" >/dev/null
for _ in $(seq 1 40); do
  [ "$(curl -s -o /dev/null -w '%{http_code}' $B/healthz 2>/dev/null)" = "200" ] && break; sleep 1
done
sleep 6
curl -s $B/metrics > /tmp/ckfid-metrics.txt
grep -q '^checkers_database_up 0' /tmp/ckfid-metrics.txt \
  && ok "a table of the wrong shape is reported as a database that is not usable" \
  || bad "the service reported healthy against a schema it cannot write"
grep -q '^checkers_games_all_time' /tmp/ckfid-metrics.txt \
  && bad "the lifetime total is published despite never having been read" \
  || ok "the lifetime total is not published until it has been read"

echo "fidelity: $PASS passed, $FAIL failed"
[ "$FAIL" -eq 0 ]
