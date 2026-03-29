# Phase 2 — Onboarding and growth

## Goal
Make it easy for clubs to get their members onto the platform, whether they are starting fresh or migrating from an existing system.

---

## Tasks

### Invitation flow (primary onboarding)

- [ ] Admin pastes a list of email addresses into a text area
- [ ] System sends branded invite emails (club logo + name) to each address
- [ ] Each invite contains a unique registration link valid for 14 days
- [ ] Member clicks link → pre-filled registration form → account created
- [ ] Admin sees invite status (sent / accepted / expired) in dashboard
- [ ] Resend expired invites with one click

### Holdsport member import

- [ ] Accept Holdsport member export file upload (no CSV knowledge required)
- [ ] Auto-map Holdsport fields to Klubhuset member fields
- [ ] Preview: show mapped data before confirming import
- [ ] Import creates member records and optionally sends invite emails to imported members
- [ ] Handle duplicates gracefully (match on email)

### MinForening member import

- [ ] Same pattern as Holdsport import
- [ ] Accept MinForening export format
- [ ] Auto-map fields, preview, confirm

### Onboarding wizard (new club setup)

- [ ] Step 1: Club name + slug (if not already set)
- [ ] Step 2: Logo upload
- [ ] Step 3: Create first afdeling
- [ ] Step 4: Create first team within that afdeling
- [ ] Step 5: Invite members (paste emails or import file)
- [ ] Wizard is skippable at each step and resumable from dashboard
