#!/bin/bash
# Nightly logical backup of the checkers database on the cx22.
#
# 2026-09-07: the cluster this replaced had CNPG's barman plugin writing WAL and
# a nightly base backup to S3. A raw Docker box has neither, so this is the only
# backup checkers has. It is modelled on the orbit service chart's own dump job
# (charts/service/templates/database.yaml): pg_dump --no-owner --no-privileges
# piped through gzip, written to .part and renamed, pruned by age, with stale
# .part files swept so a run that dies mid-dump cannot fill the disk with
# half-written files the age prune never matches.
#
# The off-box copy matters more here than it did on the cluster: this box holds
# the only running copy of the database, so a lost box is a lost database unless
# the dump is somewhere else. Credentials for the bucket are read from a 0600
# file outside the repo; if that file is absent the local dump still happens and
# the script says so rather than failing the whole backup.
# S3_ENV is not in this repo. It is /etc/orbit/s3.env on the box, mode 0600, and
# holds rclone's remote as environment variables:
#
#   RCLONE_CONFIG_ORBIT_TYPE=s3
#   RCLONE_CONFIG_ORBIT_PROVIDER=Other
#   RCLONE_CONFIG_ORBIT_ENDPOINT=https://fsn1.your-objectstorage.com
#   RCLONE_CONFIG_ORBIT_REGION=fsn1
#   RCLONE_CONFIG_ORBIT_ACCESS_KEY_ID=<orbit-s3 HETZNER_S3_ACCESS_KEY>
#   RCLONE_CONFIG_ORBIT_SECRET_ACCESS_KEY=<orbit-s3 HETZNER_S3_SECRET_KEY>
#
# REGION is the line that is easy to leave out and hard to diagnose. Hetzner
# Object Storage answers reads without it and rejects every PUT with
# LocationConstraintConflict, so `rclone ls` succeeds while the backup silently
# never leaves the box. 2026-09-07: cost an hour the first time.
set -euo pipefail
umask 077

APP=checkers
CONTAINER=checkers-db
DB_USER=checkers
DB_NAME=checkers
DEST=/var/backups/pg/${APP}
KEEP_DAYS=7
S3_ENV=/etc/orbit/s3.env
S3_REMOTE=orbit:yotamorbitplatform/raw-dumps/${APP}

mkdir -p "$DEST"

out="$DEST/${DB_NAME}-$(date -u +%Y%m%dT%H%M%SZ).sql.gz"
docker exec -i "$CONTAINER" pg_dump -U "$DB_USER" --no-owner --no-privileges "$DB_NAME" \
  | gzip -9 > "$out.part"
mv "$out.part" "$out"
echo "wrote $out ($(stat -c %s "$out") bytes)"

if [ -r "$S3_ENV" ]; then
  set -a; . "$S3_ENV"; set +a
  rclone copy "$out" "$S3_REMOTE/"
  echo "copied to $S3_REMOTE/$(basename "$out")"
else
  echo "WARNING: $S3_ENV unreadable — local dump only, no off-box copy" >&2
fi

find "$DEST" -name '*.sql.gz' -mtime +${KEEP_DAYS} -delete
find "$DEST" -name '*.part' -mmin +120 -delete
ls -lh "$DEST"
