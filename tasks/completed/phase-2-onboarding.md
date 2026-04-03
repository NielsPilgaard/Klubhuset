# Phase 2 — Onboarding and growth

## Goal

Make it easy for schools to get their staff onto the platform and complete initial setup with minimal friction.

---

## Tasks

### Staff invitation flow

- [x] Admin pastes a list of email addresses into a text area
- [x] System sends invitation emails to each address with the school name
- [x] Each invite contains a unique registration link valid for 14 days
- [x] Staff member clicks link → registration form → Keycloak account created with appropriate role
- [x] Admin sees invite status (sent / accepted / expired) in dashboard
- [x] Resend expired invites with one click
- [x] Admin can assign role (teacher / aide) at invitation time *(role defaults to Teacher; editable after creation)*

### School setup wizard

- [x] Step 1: School name + slug (if not already set)
- [x] Step 2: Logo upload
- [x] Step 3: Time slot wizard — define default lesson structure (duration, breaks)
- [x] Step 4: Create first classes (e.g. 0.a, 1.a, 2.b)
- [x] Step 5: Create first courses (e.g. dansk, matematik, idræt)
- [x] Step 6: Add rooms (e.g. Lokale 1, Gymnastiksalen)
- [x] Step 7: Invite staff (paste emails)
- [x] Wizard is skippable at each step and resumable from dashboard

### Onboarding UX polish

- [x] Progress indicator showing setup completion (e.g. "4 of 7 steps done")
- [x] Contextual help on each step (plain Danish, no jargon)
- [x] Empty states in dashboard guide admin to complete setup (e.g. "Ingen klasser endnu — opret din første klasse")
