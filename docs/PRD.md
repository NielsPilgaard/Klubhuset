# PRD.md — Klubhuset Product Requirements

## Product overview

Klubhuset is a white-label, multi-tenant SaaS platform for Danish sports societies. It is sold B2B to clubs (foreninger), not to end users. The typical customer is a volunteer-run, multi-sport club with 150–2000 members.

## Problem being solved

Danish sports societies are drowning in spreadsheets, email threads, and manual bank transfers. Volunteer board members (often non-technical, often older) spend disproportionate time on administration instead of running the club. Existing tools either lack white-labelling (Holdsport) or are complex and expensive to set up (MinForening white label).

## Target customer

- Multi-sport Danish forening (flerstrenget idrætsforening)
- 150–2000 members
- Volunteer-run board: formand, kasserer, sekretær
- Low technical sophistication
- Already affiliated with DGI or DIF
- Currently using: Holdsport, MinForening, WinKAS, or just Excel/paper

## Target end user

Members of the forening. Range from 3-year-old children (parents acting on their behalf) to pensioners. Must be able to self-register and pay without assistance. Primary devices are phones and tablets — see [Design principles](#design-principles).

---

## Design principles

See [docs/PERSONAS.md](PERSONAS.md) for the concrete users these principles are written for.

**Mobile-first**: the primary end users are parents registering children, preteens, and the elderly. All member-facing screens must work fully on a phone. Admin screens must work on tablet and desktop. No feature may require a wide screen to operate.

**No surprises**: UI must be predictable and obvious. Prefer one clear action over multiple options. Never use jargon the user has not seen before without an immediate explanation.

**Simplicity over features**: the admin user is often a 60-year-old volunteer formand with low technical sophistication. Every feature must be operable without training. Prefer fewer options over configurability.

---

## Core features (MVP)

### Membership management

- Member register with basic fields (name, email, phone, birthdate, address)
- Self-registration flow: member visits club URL, fills form, gets confirmed
- Afdeling structure: clubs have multiple afdelinger — one afdeling = one sport (e.g. fodbold, badminton, gymnastik). The term "afdeling" is used throughout the UI and introduced with a short subtitle on first encounter.
- Members can belong to multiple afdelinger

### Team management

See [docs/team-features.md](team-features.md) for full detail.

- Teams (hold) within each afdeling
- Each team has a primary træner and optional extra trænere
- Training schedule: weekly recurrence pattern (day + time + location) with exception support (cancel or reschedule individual sessions)
- Session changes trigger email notifications to all team members
- Team self-signup: members browse available teams and register themselves
- Season management (paid tiers only)
- Team messages: admins can message any team; trænere can message their own teams if the club enables this permission

### Payments

- **Free tier**: club admin configures their MobilePay Betalingslink and QR code in the dashboard. The platform displays it — money never touches Klubhuset. Admin manually marks members as paid. See [free-tier-payment-bypass](decisions/free-tier-payment-bypass.md).
- **Paid tiers**: platform-mediated payments via Stripe + MobilePay Subscriptions. Automated kontingent invoicing, payment status dashboard, automatic reminders for unpaid members. Transaction fee (approx 2.5% + 2 kr) added on top of kontingent and charged to the member, not the club. See [transaction-fees-to-members](decisions/transaction-fees-to-members.md).

### Onboarding / member import

- **Invitation flow** (primary): admin pastes email addresses, system sends branded invite emails, members self-register. Works for all clubs regardless of prior system.
- **Holdsport import**: club downloads their Holdsport member export, uploads to Klubhuset, system maps fields automatically. No CSV knowledge required.
- **MinForening import**: same pattern as Holdsport import.

### Admin dashboard

- Overview: member count, payment status, recent signups
- Paid tiers: reporting, exports, multi-admin roles, DGI/DIF statistics export (required for kommunal tilskud)

### White-labelling

- **Free tier**: club gets a path on klubhuset.dk (e.g. `klubhuset.dk/minforening`). Klubhuset branding visible.
- **Paid tiers**: full white-label. Club uploads logo, sets name. Members see only club branding.
- **Forening+ tier**: custom domain support (manual DNS + automatic SSL via Caddy).

### Tenant routing

Clubs are identified by a short slug chosen at signup (e.g. `minforening`). At MVP, clubs are served at `klubhuset.dk/{slug}`. Slugs are immutable after creation. See [path-based-tenant-routing](decisions/path-based-tenant-routing.md).

---

## Tech stack

See [docs/decisions/](decisions/) for full rationale. Summary:

- **API**: ASP.NET Core Web API (C# 12 / .NET 9)
- **ORM**: Entity Framework Core + PostgreSQL (self-hosted)
- **Frontend**: React + Vite + TypeScript + Tailwind CSS
- **Auth**: Keycloak (Docker Compose)
- **Hosting**: OVHCloud VPS + Dokploy + Docker Compose
- **Object storage**: OVHCloud Object Storage (S3-compatible, EU)

---

## Competitive context

| Competitor | Strengths | Weaknesses |
|---|---|---|
| Holdsport | Free, feature-rich, large user base | Not white-label, per-transaction fees, complex UX |
| MinForening | White-label option | Expensive, slow setup (billed by dev hours) |
| goMember, ForeningsAdministrator, VoresForening | Niche players | Less relevant |

## Differentiators

1. Genuinely invisible platform — end users only see the club's brand
2. Simple enough for a 60-year-old volunteer to run without training
3. Flat monthly pricing — predictable, no per-transaction fees for the club
4. Multi-afdeling structure built in from day one, not bolted on
5. One-step migration from Holdsport
6. Mobile-first — designed for parents and members on phones

## Out of scope (for now)

- Booking systems (courts, halls)
- Webshop / merchandise
- SMS notifications
- Access control / door systems
- Accounting software integration
- Native mobile app
