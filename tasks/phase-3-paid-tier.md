# Phase 3 — Billing, file explorer, and reporting

## Goal

Implement self-serve billing via Stripe Checkout, the file explorer, and exportable reporting.

---

## Tasks

### Stripe Checkout billing

- [ ] Stripe product and price configuration for Basis tier (299 kr/month)
- [ ] Signup flow: school completes trial → Stripe Checkout session → subscription created
- [ ] Auto-renew monthly
- [ ] Stripe billing portal link in admin settings (manage subscription, update card, cancel)
- [ ] 14-day free trial: full access, no payment required during trial
- [ ] Trial expiry handling: prompt to subscribe, read-only grace period if not converted

### Billing lifecycle

- [ ] Failed payment handling: email notification, retry, grace period, then feature restriction
- [ ] Cancellation flow: access until end of billing period, then read-only
- [ ] Webhook handler for Stripe events (payment succeeded, payment failed, subscription cancelled)

### File explorer

- [ ] File upload (admin and teacher) → OVHCloud Object Storage
- [ ] Link files to courses
- [ ] Browse files by course
- [ ] File list with name, upload date, uploader, linked course
- [ ] Download file
- [ ] Delete file (admin only)
- [ ] Storage quota enforcement (100 GB for Basis, 1000 GB for Skole+ when available)

### Reporting and exports

- [ ] Course hour summary: hours per course per class (exportable CSV / PDF)
- [ ] Teacher hour summary: total teaching hours per teacher (exportable)
- [ ] Schema export: full weekly schema as CSV or PDF
- [ ] Stats visible on admin dashboard
