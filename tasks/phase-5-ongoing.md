# Phase 5 — Ongoing / hardening

## Goal
Enforce pricing limits, handle billing lifecycle, meet compliance requirements, and ensure the platform is accessible and linguistically correct.

---

## Tasks

### Pricing enforcement

- [ ] Member limit checks per tier (Starter: 150, Forening: 600, Forening+: unlimited)
- [ ] Graceful degradation when limit is reached: block new self-registrations, show upgrade prompt to admin
- [ ] Feature tier gating (white-label, DGI/DIF export, season management, custom domains)

### Billing lifecycle

- [ ] Upgrade flow: admin initiates upgrade → Stripe Checkout → tier updated
- [ ] Downgrade flow: admin initiates downgrade → scheduled at next billing cycle → tier-gated features disabled
- [ ] Billing management: Stripe billing portal link in admin settings
- [ ] Failed payment handling: email notification, grace period, then feature restriction

### GDPR compliance

- [ ] Member data export (admin can download all data for a member as JSON/CSV)
- [ ] Member deletion: full removal of personal data on request
- [ ] Data retention policy documentation
- [ ] Cookie/consent handling on public-facing pages

### Accessibility

- [ ] WCAG 2.1 AA audit across all member-facing screens
- [ ] Fix identified issues (focus management, contrast, screen reader labels, keyboard navigation)
- [ ] Particular attention to self-registration form and payment pages (highest-traffic, most diverse user age range)

### Danish language QA

- [ ] All UI copy reviewed by a native Danish speaker
- [ ] Error messages, emails, and notifications reviewed
- [ ] Legal text (privacy policy, terms of service) reviewed

### Infrastructure

- [ ] PostgreSQL backup monitoring: daily check that backup job succeeded
- [ ] Monthly restore drill to verify backup integrity
- [ ] Uptime monitoring (e.g. UptimeRobot or self-hosted equivalent, EU-only)
