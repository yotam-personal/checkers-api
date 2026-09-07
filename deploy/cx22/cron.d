# checkers — install as /etc/cron.d/checkers (root:root 0644).
#
# 04:50 is chosen against the box's other tenants, not for its own sake: it is
# clear of the co-tenant's nightly image prune, and ten minutes after the other
# application's dump rather than on top of it, because two
# `pg_dump | gzip -9` pipelines at once is both cores of a 2-vCPU box for no
# reason. Check the private runbook before moving it.
#
# checkers has no refresh job — the whole nightly story here is the dump.
SHELL=/bin/bash
PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin

50 4 * * * root /opt/checkers/deploy/cx22/pg-dump.sh >> /var/log/checkers-pg-dump.log 2>&1
