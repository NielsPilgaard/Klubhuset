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

- [x] Keycloak realm configured: `skoleplanen-web` public OIDC client, `skoleplanen-api` bearer-only resource server
- [x] Role-based access: `admin`, `teacher`, `aide` roles in Keycloak; mapped to JWT claims; enforced on API endpoints via `[Authorize(Roles = "...")]`
- [x] Frontend Keycloak OIDC login flow (`keycloak-js`, PKCE, auto token refresh)
- [x] All API endpoints require a valid Keycloak-issued JWT; tenant_id claim drives `ITenantContext`

### Room management

- [x] CRUD API for rooms (lokaler): name, capacity, description — `GET/POST/PUT/DELETE /api/v1/rooms`
- [x] Room list view (admin) — `RoomsPage`

### Staff management

- [x] Staff register CRUD API (admin): name, email, phone, role (Teacher/Aide/Substitute) — `GET/POST/PUT/DELETE /api/v1/staff`
- [x] Staff list view (admin) — `StaffPage`
- [ ] Staff invitation email flow (send Keycloak invite link to new staff member)

### Class management

- [x] CRUD API for classes (klasser): name, description — `GET/POST/PUT/DELETE /api/v1/classes`
- [x] Class list view (admin) — `ClassesPage`

### Course management

- [x] CRUD API for courses (fag): name, description — `GET/POST/PUT/DELETE /api/v1/courses`
- [x] Course list view (admin) — `CoursesPage`

### Time slot template

- [x] School-level time slot template API: lesson duration, breaks, school day start/end — `GET/PUT /api/v1/time-slot-template`
- [ ] Time slot wizard for onboarding (see [schema-features.md](../docs/schema-features.md))
- [ ] Per-class time slot overrides

### Schema builder

- [x] CRUD API for schemas per class — `GET/POST/DELETE /api/v1/classes/{classId}/schemas`
- [x] Weekly grid UI — `SchemaBuilderPage`
- [x] Cell assignment: course + teacher + room + optional aide via `PUT /api/v1/classes/{classId}/schemas/{schemaId}/slots`
- [x] Real-time conflict detection (teacher, room, aide double-booking with clock-time overlap)
- [x] Conflict display: returned on every slot upsert via `SlotsAndConflictsDto`
- [x] Schema marked complete blocked when conflicts exist (HTTP 422)
- [ ] Conflict highlight UI in schema builder grid
- [ ] Copy/duplicate schema from one class to another

### Printable schemas

- [ ] Per-class weekly schema (print-friendly, A4)
- [ ] Per-teacher weekly schema (all classes for one teacher)
- [ ] Per-room weekly schema (all classes using one room)
- [ ] Include school name, term, generation date on all printable views

### Stats dashboard

- [x] Hours per course per class — `GET /api/v1/stats/dashboard`
- [x] Hours per teacher / aide
- [x] Unassigned slots count
- [x] Stats displayed on admin dashboard — `DashboardPage`

### Staff schedule views

- [ ] Teacher: view own weekly schedule across all classes (read-only)
- [ ] Aide: view own weekly schedule (read-only)
- [ ] Room schedule: view which classes use a room and when

### Admin dashboard

- [x] School overview: class count, staff count, course count — `DashboardPage`
- [x] Schema completeness indicator
- [x] Quick links to schema builder, staff list, course list via sidebar

### Responsive UI validation

- [ ] Schema builder and admin views usable on laptop-size screens (1280px+)
- [ ] Teacher/aide schedule views fully functional on phone (375px+)
- [ ] No feature unusable at any screen size (per responsive-ui ADR)

### Testing

- [x] Integration test project: tUnit + Testcontainers (real PostgreSQL) + `WebApplicationFactory`
- [x] Test auth handler bypasses Keycloak for integration tests
- [x] CRUD tests: full room lifecycle (create, read, update, delete, 404, validation)
- [x] Tenant isolation tests: data from tenant A is invisible to tenant B; cross-tenant ID lookup returns 404
- [x] Conflict detection tests: teacher double-booking, room double-booking, mark-complete blocked on conflicts
