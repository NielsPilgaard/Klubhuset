# Phase 2 — Onboarding and growth

## Goal

Make it easy for schools to get their staff onto the platform and complete initial setup with minimal friction.

---

## Tasks

### Staff invitation flow

- [ ] Admin pastes a list of email addresses into a text area
- [ ] System sends invitation emails to each address with the school name
- [ ] Each invite contains a unique registration link valid for 14 days
- [ ] Staff member clicks link → registration form → Keycloak account created with appropriate role
- [ ] Admin sees invite status (sent / accepted / expired) in dashboard
- [ ] Resend expired invites with one click
- [ ] Admin can assign role (teacher / aide) at invitation time

### School setup wizard

- [ ] Step 1: School name + slug (if not already set)
- [ ] Step 2: Logo upload
- [ ] Step 3: Time slot wizard — define default lesson structure (duration, breaks)
- [ ] Step 4: Create first classes (e.g. 0.a, 1.a, 2.b)
- [ ] Step 5: Create first courses (e.g. dansk, matematik, idræt)
- [ ] Step 6: Add rooms (e.g. Lokale 1, Gymnastiksalen)
- [ ] Step 7: Invite staff (paste emails)
- [ ] Wizard is skippable at each step and resumable from dashboard

### Onboarding UX polish

- [ ] Progress indicator showing setup completion (e.g. "4 of 7 steps done")
- [ ] Contextual help on each step (plain Danish, no jargon)
- [ ] Empty states in dashboard guide admin to complete setup (e.g. "Ingen klasser endnu — opret din første klasse")
