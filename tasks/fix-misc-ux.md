# Task: Fix Miscellaneous UX Bugs

Small isolated fixes across school settings, dashboard, and billing.

---

## 1. School settings: add address field

**Location**: School settings page (search for `skole`, `settings`, or `school` in `web/src/pages/`).

Add a text input for the school's address. This is a single free-text field (not structured street/city/zip) for now.

### API

Add `Address` (`string?`, max 500 chars) to the school/tenant settings model. Generate a new EF Core migration. Add it to the settings GET and PUT endpoints.

### Frontend

Add a labeled text input "Adresse" to the school settings form, below the school name. Pre-populate from the saved value. Include in the form submit payload.

---

## 2. Dashboard: "Opret schema" goes to /klasser, should go to new schema flow

**Location**: Dashboard page, "Opret schema" button/card.

Currently clicking "Opret schema" navigates to `/klasser`. It should navigate directly to the new schema creation view — whichever route/modal allows creating a new schema for a class.

Find the dashboard component and the "Opret schema" action. Change the `navigate()` target to the correct route. If the new schema flow requires selecting a class first, navigate to `/klasser` with a query param like `?action=new-schema` and handle that param in ClassesPage to open the new-schema modal automatically.

---

## 3. Subscribe / Billing: "Abonner nu" broken flow

**Symptom**: Clicking "Abonner nu" shows a "Start gratis" step and asks for an email address. This is wrong — we should go straight to Stripe Checkout for a credit card.

**Fix**: 
- Remove the email capture step from the subscription flow entirely.
- The "Abonner nu" button must trigger Stripe Checkout directly. Call the existing `POST /api/v1/billing/checkout` (or equivalent) endpoint which creates a Stripe Checkout session and returns a redirect URL.
- Redirect the user to the Stripe Checkout URL immediately.
- No MobilePay, no manual invoicing, no email collection in this flow.

**Investigation**: Find the billing/subscribe component and the API endpoint. Check if the Stripe Checkout session creation is already implemented on the API side. If the API endpoint exists, the fix is likely frontend-only — remove the intermediate step and call the checkout endpoint directly on button click.

---

## Constraints

- Billing: Stripe Checkout only. No alternative payment flows.
- All settings mutations: tenant-scoped, `TenantId` from `ITenantContext`.
- `ProblemDetails` for all API errors.
- Do not edit existing migration files — generate new ones.
