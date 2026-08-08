#!/usr/bin/env python3
"""
Submit a winner whose name contains a NUL byte, and print "<status> <body>".

A separate file because bash cannot carry a NUL through a command substitution — it drops
the byte and warns, so the shell version of this test submitted the harmless string
"badname" and passed against code with no NUL handling at all.

The byte matters: Postgres refuses it as an invalid UTF-8 sequence, and that refusal used to
be caught as though the database were unreachable, which stopped the durable game counter
for everybody until the next probe.
"""
import json
import sys
import urllib.error
import urllib.request

base, start, after = sys.argv[1:4]


def post(path, payload):
    req = urllib.request.Request(
        base + path,
        data=json.dumps(payload).encode(),
        headers={"Content-Type": "application/json"},
    )
    try:
        with urllib.request.urlopen(req) as r:
            return r.status, r.read().decode()[:60]
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode()[:60]


_, body = post("/startgame", {"turnColor": "black", "computerColor": "white", "position": start})
gid = json.loads(body)["id"]
code, body = post(
    "/addwinner",
    {"moveRequest": {"gameId": gid, "position": after}, "winner": {"name": "bad\x00name"}},
)
print(code, body.replace("\n", " "))
