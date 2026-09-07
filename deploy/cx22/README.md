# checkers on the shared box

Raw Docker on a single host that also runs another application, whose reverse
proxy owns ports 80/443 and whose docker network we join. This repository is
public, so the host, the network name and the backup destination are not named
here — they live in `deploy/cx22/.env` (mode 600, never committed, see
`.env.example`) and in the private runbook.

- `docker compose -f deploy/cx22/docker-compose.yml up -d` — db, api, web.
- No host ports. The shared front door reaches `checkers-web:8080` and `checkers-api:8080`.
- `checkers-api` is a singleton (in-process game state); never scale it.
- `checkers-web` is built from the website repository, not this one.
- `cron.d` → `/etc/cron.d/checkers`, `logrotate` → `/etc/logrotate.d/checkers`.
- Dumps: `/var/backups/pg/checkers/` plus the off-box copy `pg-dump.sh` makes.
- Front door: `checkers.caddy` is copied into the shared proxy's drop-in
  directory and applied with a `caddy reload` — a config reload, never a
  restart of the co-tenant's proxy.

## Shared box

**This host is shared, and the other application on it is production.** Its
containers, volumes and network are not ours and nothing here may reconfigure
them. In particular:

- **Never run `docker system prune`, `docker volume prune` or
  `docker network prune` on this box.** They are host-wide and do not know which
  project owns what. Anything of the co-tenant's that is momentarily unreferenced
  is gone.
- Remove a scratch container with `docker rm -v <name>` so its anonymous volume
  goes with it. That is the reason a prune ever looks tempting; it is not one.
- Remove this project's volumes **only by name**: `docker volume rm checkers_pgdata`.
- Join the shared network as `external` and never edit its settings. Keep every
  service name globally unique on it — the co-tenant has a service called `db`
  and resolves it by name, and compose registers service names as network
  aliases.
