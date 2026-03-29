# Phase 1 — Core platform (free tier)

## Goal
Build the full free-tier product: tenant setup, member management, afdeling/team structure, basic dashboard, auth, and team communications.

---

## Tasks

### Tenant routing and slug

- [ ] Tenant slug design and enforcement
  - Admin picks slug at signup (e.g. `minforening`)
  - Validation: lowercase letters, digits, hyphens only; 3–40 chars; globally unique
  - Reserved words blocked: `api`, `admin`, `www`, `static`, `health`, `app`, `dashboard`, `login`, `logout`, `signup`
  - Slug is immutable immediately after creation — no self-serve rename; corrections via support
- [ ] Path-based tenant resolution middleware
  - Extracts slug from URL path prefix: `/{slug}/...`
  - Resolves slug → TenantId via cached DB lookup; returns HTTP 404 for unknown slugs
  - Injects TenantId into request context (`ITenantContext`)
  - All downstream services read TenantId from context — never from URL

### Tenant / club setup

- [ ] Tenant creation flow: club fills signup form, picks slug, creates admin account via Keycloak
- [ ] Club branding settings: upload logo (→ OVHCloud Object Storage), set club name
- [ ] Basic club settings page (admin)

### Authentication

- [ ] Keycloak integration: admin login, member login, træner login
- [ ] Role-based access: `admin`, `member`, `traener` roles mapped from Keycloak token claims
- [ ] All API endpoints scoped to authenticated tenant (middleware enforces this)

### Member management

- [ ] Member register CRUD (admin view) — mobile-first layout
- [ ] Self-registration form (public, per-tenant URL `/{slug}/tilmeld`) — mobile-first
  - Fields: name, email, phone, birthdate, address
  - Confirmation email sent after registration (requires email provider from Phase 0)

### Afdeling management

- [ ] Create / edit / delete afdelinger
- [ ] Afdeling = one sport (e.g. fodbold, badminton, gymnastik)
- [ ] Members can belong to multiple afdelinger
- [ ] UI introduces "afdeling" with a short subtitle on first encounter

### Team management

See [docs/team-features.md](../docs/team-features.md) for full feature spec.

- [ ] Create / edit / delete teams (hold) within an afdeling
- [ ] Add primary træner to a team
- [ ] Add extra trænere to a team (optional, multiple)
- [ ] Set weekly recurrence pattern (day(s) of week + time + location)
- [ ] Exception calendar: cancel or reschedule individual sessions
  - Admin or træner (if messaging permission is on) can create exceptions
  - On cancel/reschedule: email notification sent to all team members
- [ ] Team self-signup: members browse available teams and register themselves

### Free tier payment

- [ ] Admin configures club's MobilePay Betalingslink and QR code in dashboard
- [ ] Payment info displayed to members on their profile / payment page
- [ ] Admin manually marks individual members as paid (checkbox in member list)
- [ ] Platform has zero involvement in money movement

### Basic admin dashboard

- [ ] Member count
- [ ] Recent signups list
- [ ] Payment status overview (who is marked paid / unpaid)
- [ ] Mobile-first layout (works on tablet)

### Team messages

- [ ] Admin can send a message to any team → delivered by email to all team members
- [ ] Club setting (admin toggle): allow trænere to send messages to their own teams
- [ ] Træner can send message to their own team(s) if the club setting is enabled
- [ ] Message history visible in admin dashboard
