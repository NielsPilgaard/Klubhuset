# Phase 0 — Foundation

## Goal

Establish the project skeleton, local dev environment, CI, and hosting infrastructure before any feature work begins.

---

## Tasks

### Repo and scaffold

- [ ] Scaffold monorepo: `/api` (ASP.NET Core WebAPI, .NET 9) + `/web` (React + Vite + TypeScript + Tailwind)
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
- [ ] Self-hosted PostgreSQL backup strategy:
  - `pg_dump` on daily cron → gzip → upload to OVHCloud Object Storage
  - Retain last 30 daily backups
  - Alert on backup failure

### Auth

- [ ] Keycloak realm setup: `{{PRODUCT_NAME}}` realm, API resource server client, web app public client
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
- [ ] Configure Caddy/Traefik for HTTPS on `{{PRODUCT_NAME}}.dk`

### Research spike — transactional email

- [x] Evaluate EU transactional email providers against these criteria:
  - EU data residency (GDPR, sovereignty, trust)
  - C# SDK or clean REST API
  - Free dev tier available
  - Affordable at ~5000 emails/month in production
- [x] Candidates: Brevo, Mailersend, Postal (self-hosted), Infobip EU
- [x] Document decision in ADR (see [transactional-email-tbd](../docs/decisions/transactional-email-tbd.md))
