# PRD.md — {{PRODUCT_NAME}} Product Requirements

## Product overview

{{PRODUCT_NAME}} is a multi-tenant SaaS schema planner for Danish friskoler and private/independent schools. It is sold B2B to schools, not to end users. The typical customer is a small-to-medium friskole with 50–500 students, run by a small team of administrators and teachers.

## Problem being solved

Danish friskoler and private schools plan their weekly schedules (skemaer) using spreadsheets, whiteboards, or paper. Conflict detection — double-booked teachers, rooms, or aides — is entirely manual. No single tool handles teacher/aide assignment, room allocation, and course-hour tracking in one place at a price point that makes sense for small schools. Existing tools are either expensive (Docendo, Skoleintra), opaque in pricing (Skoleplan), or built for the public school system (Aula).

## Target customer

- Danish friskole or private/independent school (NOT folkeskole, NOT gymnasium)
- 50–500 students
- Small admin staff: skoleleder, skolesekretær, possibly a viceskoleleder
- Low technical sophistication among admin users
- Currently using: Excel, paper, Skoleplan, or Docendo

## Target end user

Staff at the school. Range from school secretaries managing the full schema to substitute teachers who just need to know where to be. Primary devices are laptops (admin) and phones (teachers checking schedules) — see [Design principles](#design-principles).

---

## Design principles

See [docs/PERSONAS.md](PERSONAS.md) for the concrete users these principles are written for.

**Laptop-first for admin, responsive for all**: the primary admin user builds schemas on a laptop. The schema builder must work well on a laptop-sized screen. Teachers and aides view their schedules on phones and tablets. No feature may require a specific screen size to operate — but the schema builder is optimised for laptop use.

**No surprises**: UI must be predictable and obvious. Prefer one clear action over multiple options. Never use jargon the user has not seen before without an immediate explanation.

**Simplicity over features**: the admin user is often a school secretary with limited time and low technical sophistication. Every feature must be operable without training. Prefer fewer options over configurability.

---

## Core features (v1)

### Schema planner

The core product. Each class (klasse) has its own weekly schema (skema). The school defines a default time slot template — lesson durations and breaks — and each class inherits it. Classes can override individual time slots. See [docs/schema-features.md](schema-features.md) for full detail.

- Weekly grid view per class
- Assign course (fag) + teacher (lærer) + room (lokale) to each time slot
- Real-time conflict detection: teacher double-booked, room double-booked, aide double-booked
- Per-class time slot overrides (classes are not forced to align)

### Time slot wizard

On school setup, admin defines the default lesson structure via a guided wizard:
- Default lesson duration (e.g. 45 min)
- Breaks between lessons (optional — some schools have fixed breaks, others don't)
- Generates the school's default weekly time slot grid
- Per-class overrides allowed after initial setup

### Staff management

- Staff register: teachers (lærere), aides (pædagoger), substitutes (vikarer)
- Each staff member has a role, contact info, and assigned courses
- Staff onboarding via invitation flow (email invite → Keycloak account)

### Class and course management

- CRUD for classes (klasser): e.g. 2.b, 5.a, 9.a
- CRUD for courses (fag): e.g. dansk, matematik, idræt
- Courses are linked to classes via the schema

### File explorer

- Upload files (PDFs, documents) to the platform
- Link files to courses for easy reference
- Browse files by course
- Storage: OVHCloud Object Storage (S3-compatible, EU)

### Stats and reporting

- Hours per course per class (towards minimumstimetal)
- Hours per teacher / aide
- Exportable summaries

### Printable schema

- Print-friendly weekly schema views: per class, per teacher, per room
- Schools print and physically post schedules — this must work without the app

### Admin dashboard

- School overview: class count, staff count, schema status
- Quick access to schema builder, staff list, course list

### Payments and billing

- **Stripe Checkout**: self-serve signup, card payment, auto-renew monthly
- **14-day free trial**: full access, no payment required upfront
- **Single tier (v1)**: 299 kr/month — all features included
- Schools are never invoiced manually — everything is self-serve

---

## Tech stack

See [docs/decisions/](decisions/) for full rationale. Summary:

- **API**: ASP.NET Core Web API (C# / .NET 9)
- **ORM**: Entity Framework Core + PostgreSQL (self-hosted)
- **Frontend**: React + Vite + TypeScript + Tailwind CSS
- **Auth**: Keycloak (Docker Compose)
- **Hosting**: OVHCloud VPS + Dokploy + Docker Compose
- **Object storage**: OVHCloud Object Storage (S3-compatible, EU)
- **Email**: Scaleway TEM via SMTP (MailKit)

---

## Competitive context

| Competitor | Strengths | Weaknesses |
|---|---|---|
| Skoleplan | Established, known in friskole market | Pricing not public, unclear feature set |
| Docendo | Feature-rich | Expensive, complex |
| Skoleintra | Solid product, good reputation | High price, heavy for small schools |
| Aula | Large user base, government-backed | Public schools only — not available to friskoler |

## Differentiators

1. Transparent pricing — listed on the website, no sales calls required
2. Simple enough for a school secretary to run without training
3. Real-time conflict detection built into the schema builder
4. Clean, modern UI — no bloat, no legacy design
5. EU-only data storage (OVHCloud, Scaleway)
6. 14-day free trial with full access

## Future features (not v1)

- UVM reporting (course hour reporting to the ministry)
- Parent/student logins
- Homework upload (linked to courses)
- UVM course hour integration (minimumstimetal tracking against official requirements)
- Parent communication (v2 candidate)

## Out of scope

- Student grading
- LMS / learning management
- SMS notifications
- Native mobile app
- Folkeskole / Aula integration
- Accounting software integration
