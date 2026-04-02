# Skoleplanen

SaaS schema planner for Danish friskoler and private/independent schools. Schools build, manage, and print their weekly class schedules — with real-time conflict detection for teachers, rooms, and aides.

## What it does

- **Schema planner** — weekly grid per class, assign course + teacher + room to each time slot
- **Conflict detection** — real-time validation: no double-booked teachers, rooms, or aides
- **Time slot wizard** — school defines default lesson structure (durations, breaks); classes inherit with optional overrides
- **Staff management** — teachers, aides, substitutes; invitation-based onboarding
- **File explorer** — upload files linked to courses for easy reference
- **Stats** — hours per course, hours per teacher/aide, unassigned slots
- **Printable schemas** — per class, per teacher, per room; designed for A4 print
- **Billing** — self-serve via Stripe Checkout, 14-day free trial, auto-renew monthly

## Tech stack

| Layer          | Choice                                              |
| -------------- | --------------------------------------------------- |
| API            | ASP.NET Core Web API, C# / .NET 10                  |
| ORM            | Entity Framework Core                               |
| Database       | PostgreSQL (Docker Compose, self-hosted on OVH VPS) |
| Frontend       | React + Vite + TypeScript + Tailwind CSS            |
| Auth           | Keycloak (Docker Compose)                           |
| API client     | Generated from OpenAPI spec (Kiota / NSwag)         |
| Email          | Scaleway TEM via SMTP (MailKit)                     |
| Object storage | OVHCloud Object Storage (S3-compatible, EU)         |
| Hosting        | OVHCloud VPS + Dokploy + Docker Compose             |

## Repository layout

```
/api      ASP.NET Core Web API
/web      React + Vite frontend
/docs     PRD, pricing, decisions, and other documentation
/tasks    Phase-by-phase implementation tasks
```

## Key architecture rules

**Multi-tenancy**: every query that touches school, staff, class, course, schema, or file data must be scoped to a tenant via EF Core global query filters. Never bypass them. Never trust a URL slug as an authorization signal — always resolve to a `TenantId` at the middleware boundary.

**Responsive UI**: admin UI (schema builder) is laptop-first. Staff schedule views must work on phones and tablets.

**Simplicity first**: the primary admin user is a school secretary with limited time. Every feature must be operable without training.

## Pricing

| Tier            | Storage | Monthly |
| --------------- | ------- | ------- |
| Basis           | 100 GB  | 299 kr  |
| Skole+ (future) | 1000 GB | 499 kr  |

14-day free trial with full access. No per-student pricing.

See [docs/PRICING.md](docs/PRICING.md) for full details.

## Documentation

- [PRD](docs/PRD.md) — product requirements and feature spec
- [Personas](docs/PERSONAS.md) — the real users every design decision is measured against
- [Schema features](docs/schema-features.md) — schema planner detail
- [Decisions](docs/DECISIONS.md) — product and architecture decisions
- [Tasks](docs/TASKS.md) — phased implementation plan
- [Pricing](docs/PRICING.md) — tier breakdown
- [Testing](docs/TESTING.md) — testing strategy
- [Contacts](media/CONTACTS.md) — discovery interview contacts
