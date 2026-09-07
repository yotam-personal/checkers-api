#!/bin/bash
# Nightly logical backup of the checkers database.
#
# 2026-09-07: the cluster this replaced had CloudNativePG's barman plugin
# writing WAL and a nightly base backup to object storage. A raw Docker box has
# neither, so this is the only backup checkers has. It follows the orbit service
# chart's own dump job: pg_dump --no-owner --no-privileges piped through gzip,
# written to .part and renamed, pruned by age, with stale .part files swept so a
# run that dies mid-dump cannot fill the disk with half-written files that the
# age prune never matches.
#
# The off-box copy matters more here than it did on the cluster: this box holds
# the only running copy of the database, so a lost box is a lost database unless
# the dump is somewhere else. That is why a failed upload is an ERROR and a
# non-zero exit, not a warning — a backup job that exits 0 while the backup
# never leaves the box is worse than no backup job, because it is also a false
# assurance. The local prune still runs on every path (see the EXIT trap), so a
# broken uploader can never also fill the disk.
#
# This repository is public. Nothing here names the host, the bucket, the
# endpoint or the credentials file; all of that arrives through the
# deployment's own .env. The private runbook has the values.
set -euo pipefail
umask 077

APP=checkers
CONTAINER=checkers-db
DB_USER=checkers
DB_NAME=checkers
DEST=/var/backups/pg/${APP}
KEEP_DAYS=7

HERE="$(cd "$(dirname "$(readlink -f "$0")")" && pwd)"

# Deployment-specific settings, from the same 0600 .env the compose file uses:
#   BACKUP_S3_ENV     path to a 0600 file of RCLONE_CONFIG_<REMOTE>_* variables
#   BACKUP_S3_REMOTE  rclone destination, e.g. <remote>:<bucket>/<prefix>/checkers
# Both are documented in .env.example and carried in the private runbook.
if [ -r "$HERE/.env" ]; then
  set -a; . "$HERE/.env"; set +a
fi

mkdir -p "$DEST"

# Runs on EVERY exit path, so a failing upload can never also mean a disk that
# fills with dumps nobody prunes. It does not touch the exit status.
cleanup() {
  find "$DEST" -name '*.sql.gz' -mtime +"${KEEP_DAYS}" -delete || true
  find "$DEST" -name '*.part' -mmin +120 -delete || true
  ls -lh "$DEST" || true
}
trap cleanup EXIT

out="$DEST/${DB_NAME}-$(date -u +%Y%m%dT%H%M%SZ).sql.gz"
docker exec -i "$CONTAINER" pg_dump -U "$DB_USER" --no-owner --no-privileges "$DB_NAME" \
  | gzip -9 > "$out.part"
mv "$out.part" "$out"
echo "wrote $out ($(stat -c %s "$out") bytes)"

# From here on the local dump is safe on disk, so every remaining failure is
# reported and carried to the exit status rather than aborting the run.
rc=0
if [ -z "${BACKUP_S3_ENV:-}" ] || [ -z "${BACKUP_S3_REMOTE:-}" ]; then
  echo "ERROR: BACKUP_S3_ENV / BACKUP_S3_REMOTE unset — no off-box copy made" >&2
  rc=1
elif [ ! -r "$BACKUP_S3_ENV" ]; then
  echo "ERROR: $BACKUP_S3_ENV missing or unreadable — no off-box copy made" >&2
  rc=1
else
  set -a; . "$BACKUP_S3_ENV"; set +a
  if rclone copy "$out" "$BACKUP_S3_REMOTE/"; then
    echo "copied to $BACKUP_S3_REMOTE/$(basename "$out")"
  else
    echo "ERROR: off-box copy of $(basename "$out") failed" >&2
    rc=1
  fi
fi

exit $rc
