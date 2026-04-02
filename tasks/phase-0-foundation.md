# Phase 0 — Foundation

## Goal

Establish the project skeleton, local dev environment, CI, and hosting infrastructure before any feature work begins.

---

## Tasks

### Repo and scaffold

- [ ] Scaffold monorepo: `/api` (ASP.NET Core WebAPI, .NET 10) + `/web` (React + Vite + TypeScript + Tailwind)
- [ ] Write `.editorconfig` covering C# and TypeScript formatting rules
- [ ] Add `.gitignore` covering both stacks

### Local development

- [ ] Write `docker-compose.yml` with services: `api`, `web`, `postgres`, `keycloak`, `localstack`
  - `localstack`: S3-compatible local emulation for OVHCloud Object Storage (file explorer needs this)
  - `postgres`: self-hosted, not managed
  - `keycloak`: latest stable, with dev realm pre-configured via realm export
- [ ] Document local setup steps in `README.md`

### Database

- [ ] Configure EF Core with PostgreSQL connection
- [ ] Write base `AppDbContext` with tenant scoping global query filter (`HasQueryFilter` on all tenant-scoped entities)
- [ ] Write and apply initial migration (empty schema baseline)
- [ ] Configure Dokploy built-in database backup:
  - Daily schedule via cron expression in Dokploy dashboard
  - Destination: OVHCloud Object Storage (S3-compatible)
  - Retention: 30 backups
  - Verify backup appears in Dokploy dashboard

### Auth

- [ ] Keycloak realm setup: `Skoleplanen` realm, API resource server client, web app public client
- [ ] Roles: `admin`, `teacher`, `aide`
- [ ] Document realm export process so it can be committed and replayed in dev/staging/prod

### API client codegen

- [ ] Decide between Kiota and NSwag for OpenAPI → TypeScript client generation
- [ ] Set up codegen pipeline: API generates spec at build time → web app runs codegen → typed client committed or generated at build (prefer build/compile time if possible)

### CI

- [ ] GitHub Actions workflow: on PR → build API + run tests → build web → lint

### Hosting

- [ ] Set up OVH VPS
- [ ] Install Dokploy
- [ ] Write production `docker-compose.yml` (or Dokploy config) for: `api`, `web`, `postgres`, `keycloak`, reverse proxy (Caddy or Traefik)
- [ ] Configure Caddy/Traefik for HTTPS on `Skoleplanen.dk`

**Secrets and security checklist when moving from Aspire local → Docker Compose VPS:**

- [ ] Replace hardcoded `KC_BOOTSTRAP_ADMIN_PASSWORD=admin` with a strong generated secret (e.g. `openssl rand -base64 32`). Store in Dokploy environment variables, never in the compose file.
- [ ] Replace hardcoded `postgres-password` with a strong generated secret. Same rule — env var only, not in the file.
- [ ] Switch Keycloak from `start-dev` to `start` (production mode). `start-dev` disables security hardening and is not safe on a public VPS.
- [ ] Set `KC_PROXY=edge` and `KC_HOSTNAME=https://auth.Skoleplanen.dk` so Keycloak trusts the reverse proxy's `X-Forwarded-Proto` header and issues tokens with the correct issuer URL.
- [ ] Ensure the Keycloak admin console (`/admin`) is **not** publicly accessible — block it at the reverse proxy level or bind Keycloak to the internal Docker network only. Expose only `/realms` publicly.
- [ ] Commit the Keycloak realm export JSON to the repo so realm config is reproducible. Import it at container startup via `--import-realm` — removes all manual UI steps on a new VPS.
- [ ] The API validates JWTs against Keycloak's JWKS endpoint. The issuer claim in the token will always be the **public** URL (`https://auth.Skoleplanen.dk`). The API can reach Keycloak internally (`http://keycloak:8080`) for JWKS fetching — configure `MetadataAddress` or pin the JWKS URI explicitly in `appsettings.Production.json` to avoid the browser vs. container URL mismatch.
- [ ] Postgres should be on the internal Docker network only — no published port on the VPS.

### Research spike — transactional email

- [x] Evaluate EU transactional email providers against these criteria:
  - EU data residency (GDPR, sovereignty, trust)
  - C# SDK or clean REST API
  - Free dev tier available
  - Affordable at ~5000 emails/month in production
- [x] Candidates: Brevo, Mailersend, Postal (self-hosted), Infobip EU
- [x] Document decision in ADR (see [transactional-email](../docs/adr/transactional-email.md))

### Transactional email infrastructure

- [ ] Add MailKit NuGet dependency
- [ ] Implement `IEmailSender` abstraction with MailKit SMTP implementation (Scaleway TEM)
- [ ] Store SMTP credentials in user secrets (local) / environment variables (production)
- [ ] Configure SPF, DKIM, and DMARC on the sending domain
- [ ] Verify email sending works in dev (Scaleway 300 free emails/month covers this)
