# Phase 0 — VPS & Go-Live

Remaining tasks before the first production deployment. Everything in [phase-0-foundation.md](phase-0-foundation.md) must be done first.

---

## Tasks

### Hosting

- [ ] Provision OVH VPS (Ubuntu 24.04 LTS recommended)
- [ ] Install Dokploy (`curl -sSL https://dokploy.com/install.sh | sh`)
- [ ] Add `docker-compose.prod.yml` as a Dokploy compose app
- [ ] Set environment variables in Dokploy dashboard:
  - `POSTGRES_PASSWORD` — `openssl rand -base64 32`
  - `KEYCLOAK_ADMIN_PASSWORD` — `openssl rand -base64 32`
  - `SMTP_USERNAME` / `SMTP_PASSWORD` — Scaleway TEM credentials

### Security checklist (before first deploy)

- [ ] Keycloak running in `start` mode (production) — not `start-dev`
- [ ] Keycloak `/admin` not reachable from the public internet (Traefik rule in compose file already blocks it — verify after deploy)
- [ ] `KC_PROXY=edge` and `KC_HOSTNAME=https://auth.skoleplanen.dk` set (already in compose file)
- [ ] DNS: `skoleplanen.dk` and `auth.skoleplanen.dk` pointing to the VPS IP

### Database backup

- [ ] Enable Dokploy built-in database backup for the `postgres` service:
  - Schedule: daily (`0 2 * * *`)
  - Destination: OVHCloud Object Storage (S3-compatible) — add S3 credentials in Dokploy
  - Retention: 30 backups
  - Verify a backup appears in the Dokploy dashboard after first run

### Transactional email

- [ ] Configure SPF record on `skoleplanen.dk` (Scaleway TEM provides the value)
- [ ] Configure DKIM (add DNS TXT record from Scaleway TEM dashboard)
- [ ] Configure DMARC (`_dmarc.skoleplanen.dk TXT "v=DMARC1; p=quarantine; rua=mailto:postmaster@skoleplanen.dk"`)
- [ ] Send a test email and confirm delivery + no spam classification
