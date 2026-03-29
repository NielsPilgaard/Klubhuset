# Klubhuset

White-label, multi-tenant SaaS for Danish sports societies (foreninger). Clubs get their own branded space — end users never see the Klubhuset name. Built for volunteer-run, multi-sport clubs with 150–2000 members.

## What it does

- **Membership management** — self-registration, member register, multi-afdeling structure (one afdeling = one sport)
- **Team management** — training schedules with weekly recurrence and exception support, team messaging, self-signup
- **Payments** — free tier uses the club's own MobilePay; paid tiers get platform-mediated payments via Stripe + MobilePay Subscriptions
- **Onboarding** — invitation flow, Holdsport importer, MinForening importer
- **White-labelling** — club logo and name on all member-facing screens; custom domain on Forening+ tier
- **Admin dashboard** — member overview, payment status, DGI/DIF statistics export (paid tiers)

## Tech stack

| Layer | Choice |
|---|---|
| API | ASP.NET Core Web API, C# / .NET 9 |
| ORM | Entity Framework Core |
| Database | PostgreSQL (Docker Compose, self-hosted on OVH VPS) |
| Frontend | React + Vite + TypeScript + Tailwind CSS |
| Auth | Keycloak (Docker Compose) |
| API client | Generated from OpenAPI spec (Kiota / NSwag) |
| Email | Scaleway TEM via SMTP (MailKit) |
| Object storage | OVHCloud Object Storage (S3-compatible, EU) |
| Hosting | OVHCloud VPS + Dokploy + Docker Compose |

## Repository layout

```
/api      ASP.NET Core Web API
/web      React + Vite frontend
/docs     PRD, pricing, decisions, and other documentation
/tasks    Phase-by-phase implementation tasks
```

## Key architecture rules

**Multi-tenancy**: every query that touches member, team, afdeling, or payment data must be scoped to a tenant via EF Core global query filters. Never bypass them. Never trust a URL slug as an authorization signal — always resolve to a `TenantId` at the middleware boundary.

**White-label**: branding (logo, club name, colors) is tenant-specific and applied at the UI layer. No values may be hard-coded.

**Payment tier separation**:
- Free tier — club's own MobilePay Business account; Klubhuset never touches their money
- Forening / Forening+ — platform-mediated via Stripe Connect + MobilePay Subscriptions; transaction fees are charged to the member, not the club

**Simplicity first**: the primary admin user is a non-technical volunteer. Every feature must be operable without training.

## Pricing tiers

| Tier | Members | Monthly |
|---|---|---|
| Gratis | Up to 100 | Free |
| Forening | Up to 500 | ~199 kr |
| Forening+ | Unlimited | ~399 kr |

See [docs/PRICING.md](docs/PRICING.md) for full details.

## Documentation

- [PRD](docs/PRD.md) — product requirements and feature spec
- [Personas](docs/PERSONAS.md) — the real users every design decision is measured against
- [Decisions](docs/DECISIONS.md) — product and architecture decisions
- [Team features](docs/team-features.md) — team management detail
- [Tasks](docs/TASKS.md) — phased implementation plan
- [Pricing](docs/PRICING.md) — tier breakdown
- [Contacts](docs/CONTACTS.md) — key contacts
