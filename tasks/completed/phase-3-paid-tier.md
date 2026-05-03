# Phase 3 — Billing, file explorer, and reporting

## Goal

Implement self-serve billing via Stripe Checkout, the file explorer, and exportable reporting.

---

## Tasks

### Stripe Checkout billing

- [x] Stripe product and price configuration for Basis tier (299 kr/month)
- [x] Signup flow: school completes trial → Stripe Checkout session → subscription created
- [x] Auto-renew monthly
- [x] Stripe billing portal link in admin settings (manage subscription, update card, cancel)
- [x] 14-day free trial: full access, no payment required during trial
- [x] Trial expiry handling: prompt to subscribe, read-only grace period if not converted
- [x] Thorough tests

### Billing lifecycle

- [x] Failed payment handling: email notification, retry, grace period, then feature restriction
- [x] Cancellation flow: access until end of billing period, then read-only
- [x] Webhook handler for Stripe events (payment succeeded, payment failed, subscription cancelled)

### File explorer

- [x] File upload (admin and teacher) → OVHCloud Object Storage
- [x] Link files to courses
- [x] Browse files by course
- [x] File list with name, upload date, uploader, linked course
- [x] Download file
- [x] Delete file (admin only)
- [x] Storage quota enforcement (100 GB for Basis, 1000 GB for Skole+ when available)

### Landing page

- [x] Fix `AuthProvider` — change `onLoad: 'login-required'` to `'check-sso'` so unauthenticated visitors are not force-redirected to Keycloak (`/web/src/auth/AuthProvider.tsx`)
- [x] Restructure `App.tsx` routing: add public `/` via `HomeRedirect` component (shows `LandingPage` or redirects to `/dashboard` if already authenticated), add `/login` route, remove index redirect from inside `<Layout>`
- [x] New `LoginPage.tsx` — calls `keycloak.login()` on mount (drives login flow from "Log ind" links)
- [x] New `LandingPage.tsx` — full marketing landing page with sections: Nav, Hero, Features, Pricing (`id="priser"`), Trust, CTA footer, Page footer. All text in Danish. Uses existing brand colors and Playfair Display / Lato fonts.
  - Nav: sticky, "Skoleoverblikket" brand name + "Log ind" (`/login`) + "Prøv gratis" (`/signup`)
  - Hero: headline "Det enkle skema — bygget til friskoler", primary CTA to `/signup`, trust line ("Intet kreditkort · Opsig når som helst · Data opbevares i EU")
  - Features: 5-card grid — conflict detection, schema builder, staff overview, printable schemas, file management
  - Pricing: dark brand section, single "Basis" card (299 kr/md), checklist, CTA to `/signup`
  - Trust: EU data, transparent pricing, simple from day one
  - CTA footer + page footer
- [x] Update `SignupPage.tsx` — change "Log ind" link from `href="/"` to `href="/login"`

### Reporting and exports

- [x] Course hour summary: hours per course per class (exportable CSV / PDF)
- [x] Teacher hour summary: total teaching hours per teacher (exportable)
- [x] Schema export: full weekly schema as CSV or PDF
- [x] Stats visible on admin dashboard
