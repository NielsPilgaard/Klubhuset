# PRD.md — Skoleoverblikket Product Requirements

## Product overview

Skoleoverblikket is a multi-tenant SaaS schema planner for Danish schools. It is sold B2B to schools, not to end users. The primary market is friskoler and private/independent schools; the secondary market is folkeskoler who want a simpler, self-serve alternative to Docendo and Skoleplan.

## Problem being solved

Danish schools plan their weekly schedules (skemaer) using spreadsheets, whiteboards, or paper. Conflict detection — double-booked teachers, rooms, or aides — is entirely manual. No single tool handles teacher/aide assignment, room allocation, and course-hour tracking in one place at a price point that makes sense for small schools. Existing tools are either expensive (Docendo, Skoleintra), opaque in pricing (Skoleplan), or not available to all school types (Aula is limited to folkeskoler via municipal procurement).

Folkeskoler are not vendor-locked into any single timetable tool. Aula (the national school-home communication platform) is separate from timetable software — folkeskoler choose their timetable tool independently. Skoleoverblikket can serve both school types.

## Target customer

**Primary**: Danish friskole or private/independent school
- 50–500 students
- Small admin staff: skoleleder, skolesekretær, possibly a viceskoleleder
- Low technical sophistication among admin users
- Currently using: Excel, paper, Skoleplan, or Docendo

**Secondary**: Danish folkeskole seeking a simpler, lower-cost timetable tool
- Same size range: 100–600 students
- Admin staff often more IT-literate (municipality IT support available)
- May use UNI•Login for SSO and expect UVM hour reporting
- Currently using: Docendo, Skoleplan, or municipality-provided tools

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

See [docs/adr/](adr/) for full rationale. Summary:

- **API**: ASP.NET Core Web API (C# / .NET 10)
- **ORM**: Entity Framework Core + PostgreSQL (self-hosted)
- **Frontend**: React + Vite + TypeScript + Tailwind CSS
- **Auth**: Keycloak (Docker Compose)
- **Hosting**: OVHCloud VPS + Dokploy + Docker Compose
- **Object storage**: OVHCloud Object Storage (S3-compatible, EU)
- **Email**: Scaleway TEM via SMTP (MailKit)

---

## Competitive context

| Competitor | Strengths                                                   | Weaknesses                                                              |
| ---------- | ----------------------------------------------------------- | ----------------------------------------------------------------------- |
| Skoleplan  | Established, known in friskole market                       | Opaque pricing, no public feature list, no self-serve trial             |
| Docendo    | Feature-rich, integrates into AULA for folkeskoler          | Expensive, complex, targets folkeskoler — overkill for small friskoler  |
| Skoleintra | Solid product, good reputation                              | High price, heavy for small schools, unclear friskole-specific support  |
| Aula       | 2.3M users, government-backed, school-home communication    | Folkeskoler only (KOMBIT/municipality procurement) — friskoler excluded |

### Why Aula is not a competitor

Aula is a **school-home communication platform** (messaging, calendars, news, attendance), not a timetable builder. It has no schedule builder, no conflict detection, and no course/room/teacher allocation. Folkeskoler that need timetable building use a separate tool (typically Docendo) that embeds into Aula as a widget.

More importantly: Aula is procured by KOMBIT on behalf of municipalities. **Friskoler have no access to Aula** — this is a structural governance exclusion (Brugerportalsinitiativet), not a pricing issue. The entire friskole market is unserved by Aula.

Docendo is the closest functional analog — it is a schema builder used by folkeskoler — but it targets a different institution type and requires AULA/STIL integration to deploy.

## Differentiators

1. Transparent pricing — listed on the website, no sales calls required
2. Simple enough for a school secretary to run without training
3. Real-time conflict detection built into the schema builder
4. Clean, modern UI — no bloat, no legacy design
5. EU-only data storage (OVHCloud, Scaleway)
6. 14-day free trial with full access
7. Built specifically for friskoler — no folkeskole/AULA assumptions baked in

## Migration from competitors

The most likely migration paths into Skoleoverblikket:

| Source      | Data available                                         | Migration approach                                                   |
| ----------- | ------------------------------------------------------ | -------------------------------------------------------------------- |
| Skoleplan   | CSV import/export (staff, classes); STIL XML (WS17)    | Provide a CSV import template matching Skoleplan's semicolon format  |
| Docendo     | Per-user iCal export; CSV schedule import              | CSV staff/class import; no bulk schedule export — expect manual work |
| Skoleintra  | Excel schedule import format; JSON/PDF export via SFTP | Excel import template; parse exported data where structured          |
| Spreadsheet | N/A — current state for many schools                   | CSV import template is sufficient                                    |

There is **no national standard timetable exchange format** in Denmark. STIL's SkoleGrunddata (WS17 XML) covers identity and enrollment data only — not schedule structure. Migration from all competitors requires a guided CSV onboarding flow, not an automated sync.

### Recommended migration UX (future feature)

A structured onboarding wizard with CSV upload steps:
1. Import staff list (navn, rolle, email)
2. Import class list (klassenavn, klassetrin)
3. Import course list (fag)
4. Manual schema rebuild (no competitor exports structured timetable data we can reliably parse)

## AULA integration

Aula has a **widget vendor program** (managed by KOMBIT) that lets third-party tools embed widgets into the Aula SPA. Docendo uses this to display schedules inside Aula for folkeskoler.

**This channel is not viable for Skoleoverblikket's core market.** Friskoler have no Aula instance to embed a widget into — the widget distribution model requires a municipal Aula administrator to activate it per institution. There is no friskole pathway.

However, if Skoleoverblikket ever expands to serve folkeskoler or mixed schools, the AULA widget program is the right integration path. KOMBIT controls vendor approval. Documentation: [aulainfo.dk/footer/widgetleverandoer](https://aulainfo.dk/footer/widgetleverandoer).

**For now**: Aula integration is out of scope. The framing to use with customers is: "We don't need Aula — Aula isn't available to your school anyway."

## Future features (not v1)

- UVM reporting (course hour reporting to the ministry) — see [tasks/08-uvm-reporting.md](../tasks/08-uvm-reporting.md)
- Parent/student logins
- Homework upload (linked to courses)
- UVM course hour integration (minimumstimetal tracking against official requirements)
- Parent communication (v2 candidate)
- CSV/Excel migration import wizard (guided onboarding from Skoleplan, Docendo, Skoleintra)
- SFO week plan (staff rota + flexible weekly event grid) — see [tasks/01-SFO-schema.md](../tasks/01-SFO-schema.md)
- UNI•Login SSO (federated login for folkeskole staff, paid add-on module) — see [tasks/07-uni-login.md](../tasks/07-uni-login.md)

## Out of scope

- Student grading
- LMS / learning management
- SMS notifications
- Native mobile app
- Aula widget integration (blocked — friskoler have no Aula instance)
- STIL/WS17 integration (targets folkeskoler, not friskoler)
- Accounting software integration
