# checkers on the cx22 — install as /etc/cron.d/checkers (root:root 0644).
#
# Clear of 03:00-03:15 where MatchStory's `docker builder prune -af` runs, and
# ten minutes after azakot's dump rather than on top of it: two pg_dump | gzip -9
# pipelines at once is both cores of a 2-vCPU box for no reason.
#
# checkers has no refresh job — the whole nightly story here is the dump.
SHELL=/bin/bash
PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin

50 4 * * * root /opt/checkers/deploy/cx22/pg-dump.sh >> /var/log/checkers-pg-dump.log 2>&1
