# Phase 3 — Paid tier features

## Goal
Implement platform-mediated payments, automated invoicing, white-label UI, and expanded admin capabilities for Forening and Forening+ tiers.

## Prerequisites
- Verify MobilePay Subscriptions API is available for new integrations before starting payment work
- Stripe Connect account and platform setup

---

## Tasks

### Payments

- [ ] Stripe Connect integration (platform-mediated payments)
- [ ] MobilePay Subscriptions integration
- [ ] Transaction fee pass-through: 2.5% + 2 kr added transparently at checkout, charged to member
- [ ] Automated kontingent invoicing: generate and send invoices per season/period
- [ ] Payment status dashboard: who has paid, who hasn't, overdue
- [ ] Automatic payment reminders: configurable schedule, sent by email

### White-label UI

- [ ] Hide Klubhuset branding for Forening+ clubs (logo, colors, name)
- [ ] Apply club logo and name across all member-facing pages
- [ ] Tier gating: only paid tiers get white-label (free tier always shows Klubhuset branding)

### Admin roles

- [ ] Multi-admin role support: formand, kasserer, afdeling admin
- [ ] Role-based permissions: kasserer sees payments, afdeling admin manages their own afdeling only

### Reporting and exports

- [ ] Member list CSV export (admin)
- [ ] DGI/DIF statistics export (aldersgrupperet membertal report, required for kommunal tilskud)
- [ ] Payment report export

### Season management

- [ ] Seasons for teams (start date, end date)
- [ ] Kontingent tied to season
- [ ] Season rollover workflow for admin
