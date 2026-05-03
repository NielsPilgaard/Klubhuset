# Phase 0 — Foundation

## Goal

Establish the project skeleton, local dev environment, CI, and hosting infrastructure before any feature work begins.

---

## Tasks

### Repo and scaffold

- [x] Scaffold monorepo: `/api` (ASP.NET Core WebAPI, .NET 10) + `/web` (React + Vite + TypeScript + Tailwind)
- [x] Write `.editorconfig` covering C# and TypeScript formatting rules
- [x] Add `.gitignore` covering both stacks

### Local development

- [x] Local dev via .NET Aspire (`aspire run`) — orchestrates PostgreSQL, Keycloak, LocalStack, API, web
- [x] Document local setup steps in `README.md`

### Database

- [x] Configure EF Core with PostgreSQL connection
- [x] Write base `AppDbContext` with tenant scoping global query filter (`HasQueryFilter` on all tenant-scoped entities)
- [x] Write and apply initial migration (empty schema baseline)

### Auth

- [x] Keycloak realm setup: `Skoleoverblikket` realm, API resource server client, web app public client
- [x] Roles: `admin`, `teacher`, `aide`
- [x] Realm export committed to repo — imported automatically via `--import-realm` on startup

### API client codegen

- [x] `openapi-typescript` — API emits spec at build time, web runs `npm run codegen`

### CI

- [x] GitHub Actions workflow: on PR → build API + run tests → build web → lint

### Hosting

- [x] Production `docker-compose.prod.yml` with Traefik labels for Dokploy
- [x] Postgres internal-only (no published port)
- [x] Keycloak `/admin` blocked at Traefik
- [x] API reaches Keycloak internally via `MetadataAddress`; issuer validated against public URL

### Transactional email infrastructure

- [x] MailKit + `IEmailSender` abstraction (Scaleway TEM via SMTP)
- [x] SMTP credentials in user secrets (local) / env vars (production)

---

See [phase-0-vps.md](phase-0-vps.md) for the remaining hosting and go-live tasks.
