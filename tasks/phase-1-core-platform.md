# Phase 1 — Core platform

## Goal

Build the core product: tenant setup, schema planner with conflict detection, staff/class/course/room CRUD, printable schemas, and basic stats.

---

## Tasks

### Tenant routing and slug

- [ ] Tenant slug design and enforcement
  - Admin picks slug at signup (e.g. `vildskud-friskole`)
  - Validation: lowercase letters, digits, hyphens only; 3–40 chars; globally unique
  - Reserved words blocked: `api`, `admin`, `www`, `static`, `health`, `app`, `dashboard`, `login`, `logout`, `signup`
  - Slug is immutable immediately after creation — no self-serve rename; corrections via support
- [ ] Path-based tenant resolution middleware
  - Extracts slug from URL path prefix: `/{slug}/...`
  - Resolves slug → TenantId via cached DB lookup; returns HTTP 404 for unknown slugs
  - Injects TenantId into request context (`ITenantContext`)
  - All downstream services read TenantId from context — never from URL

### Tenant / school setup

- [ ] Tenant creation flow: school admin fills signup form, picks slug, creates admin account via Keycloak
- [ ] School settings page (admin): school name, contact info
- [ ] Logo upload (→ OVHCloud Object Storage)

### Authentication

- [ ] Keycloak integration: admin login, teacher login, aide login
- [ ] Role-based access: `admin`, `teacher`, `aide` roles mapped from Keycloak token claims
- [ ] All API endpoints scoped to authenticated tenant (middleware enforces this)

### Room management

- [ ] CRUD for rooms (lokaler): name, capacity (optional), description
- [ ] Room list view (admin)

### Staff management

- [ ] Staff register CRUD (admin view): name, email, phone, role (teacher/aide/substitute)
- [ ] Staff can be associated with courses they teach

### Class management

- [ ] CRUD for classes (klasser): name (e.g. 2.b, 9.a), description
- [ ] Class list view (admin)

### Course management

- [ ] CRUD for courses (fag): name (e.g. dansk, matematik, idræt), description
- [ ] Courses linked to classes via the schema (not directly — the link is a schema assignment)

### Time slot template

- [ ] School-level default time slot template: lesson duration, breaks, school day start/end
- [ ] Time slot wizard for onboarding (see [schema-features.md](../docs/schema-features.md))
- [ ] Per-class time slot overrides

### Schema builder

- [ ] Weekly grid view per class: rows = time slots, columns = weekdays
- [ ] Cell assignment: select course + teacher + room (+ optional aide) per cell
- [ ] Real-time conflict detection:
  - Teacher double-booking (any clock-time overlap, not just matching slot indices)
  - Room double-booking
  - Aide double-booking
- [ ] Conflict display: highlight conflicting cells, summary panel with conflict details
- [ ] Schema must be conflict-free before it can be marked as complete
- [ ] Copy/duplicate schema from one class to another as a starting point

### Printable schemas

- [ ] Per-class weekly schema (print-friendly, A4)
- [ ] Per-teacher weekly schema (all classes for one teacher)
- [ ] Per-room weekly schema (all classes using one room)
- [ ] Include school name, term, generation date on all printable views

### Stats dashboard

- [ ] Hours per course per class (towards minimumstimetal)
- [ ] Hours per teacher / aide
- [ ] Unassigned slots (classes with empty cells)
- [ ] Summary on admin dashboard

### Staff schedule views

- [ ] Teacher: view own weekly schedule across all classes (read-only)
- [ ] Aide: view own weekly schedule (read-only)
- [ ] Room schedule: view which classes use a room and when (admin + teacher view)

### Admin dashboard

- [ ] School overview: class count, staff count, course count
- [ ] Schema completeness: how many classes have a complete schema
- [ ] Quick links to schema builder, staff list, course list

### Responsive UI validation

- [ ] Schema builder and admin views usable on laptop-size screens (1280px+)
- [ ] Teacher/aide schedule views fully functional on phone (375px+)
- [ ] No feature unusable at any screen size (per responsive-ui ADR)
