# Phase 5 — Ongoing / hardening

## Goal

Enforce pricing limits, handle billing lifecycle edge cases, meet compliance requirements, and ensure the platform is accessible and linguistically correct.

---

## Tasks

### Pricing enforcement

- [ ] Trial expiry checks: block feature access after 14-day trial if no subscription
- [ ] Storage quota enforcement per tier
- [ ] Feature tier gating when Skole+ tier is introduced

### GDPR compliance

- [ ] Staff data export (admin can download all data for a staff member as JSON/CSV)
- [ ] Staff data deletion: full removal of personal data on request
- [ ] Student data handling policy (when parent/student logins are added — extra sensitivity)
- [ ] Data retention policy documentation
- [ ] Cookie/consent handling on public-facing pages

### Accessibility

- [ ] WCAG 2.1 AA audit across all user-facing screens
- [ ] Fix identified issues (focus management, contrast, screen reader labels, keyboard navigation)
- [ ] Particular attention to schema builder (complex grid interaction) and login flow

### Danish language QA

- [ ] All UI copy reviewed by a native Danish speaker
- [ ] Error messages, emails, and notifications reviewed
- [ ] Legal text (privacy policy, terms of service) reviewed
- [ ] Ensure consistent use of Danish terms (klasse, fag, lokale, lektion) throughout

### Infrastructure

- [ ] PostgreSQL backup monitoring: daily check that backup job succeeded
- [ ] Monthly restore drill to verify backup integrity
- [ ] Uptime monitoring (e.g. UptimeRobot or self-hosted equivalent, EU-only)
- [ ] Log aggregation and error alerting
