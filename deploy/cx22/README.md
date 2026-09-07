# checkers on the cx22

Raw Docker on `openclaw` (178.104.144.231), sharing the box and the docker
network with MatchStory. Repo lives at `/opt/checkers`; `.env` sits beside the
compose file, mode 600, never committed (see `.env.example`).

- `docker compose -f deploy/cx22/docker-compose.yml up -d` — db, api, web.
- No host ports. The shared Caddy reaches `checkers-web:8080` and `checkers-api:8080`.
- `checkers-api` is a singleton (in-process game state); never scale it.
- `checkers-web` is built from `yotam-personal/yotam-peled-website`.
- Nightly cron (`cron.d` → `/etc/cron.d/checkers`): dump 04:50, to
  `/var/backups/pg/checkers/` and `s3://yotamorbitplatform/raw-dumps/checkers/`.
