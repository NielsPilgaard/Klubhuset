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

### Landing page

- [ ] Fix `AuthProvider` — change `onLoad: 'login-required'` to `'check-sso'` so unauthenticated visitors are not force-redirected to Keycloak (`/web/src/auth/AuthProvider.tsx`)
- [ ] Restructure `App.tsx` routing: add public `/` via `HomeRedirect` component (shows `LandingPage` or redirects to `/dashboard` if already authenticated), add `/login` route, remove index redirect from inside `<Layout>`
- [ ] New `LoginPage.tsx` — calls `keycloak.login()` on mount (drives login flow from "Log ind" links)
- [ ] New `LandingPage.tsx` — full marketing landing page with sections: Nav, Hero, Features, Pricing (`id="priser"`), Trust, CTA footer, Page footer. All text in Danish. Uses existing brand colors and Playfair Display / Lato fonts.
  - Nav: sticky, "Skoleplanen" brand name + "Log ind" (`/login`) + "Prøv gratis" (`/signup`) 
  - Hero: headline "Det enkle skema — bygget til friskoler", primary CTA to `/signup`, trust line ("Intet kreditkort · Opsig når som helst · Data opbevares i EU")
  - Features: 5-card grid — conflict detection, schema builder, staff overview, printable schemas, file management
  - Pricing: dark brand section, single "Basis" card (299 kr/md), checklist, CTA to `/signup`
  - Trust: EU data, transparent pricing, simple from day one
  - CTA footer + page footer
- [ ] Update `SignupPage.tsx` — change "Log ind" link from `href="/"` to `href="/login"`

### Reporting and exports

- [ ] Course hour summary: hours per course per class (exportable CSV / PDF)
- [ ] Teacher hour summary: total teaching hours per teacher (exportable)
- [ ] Schema export: full weekly schema as CSV or PDF
- [ ] Stats visible on admin dashboard
