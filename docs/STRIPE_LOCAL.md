---
title: 'Stripe Local Testing'
description: >-
  How the local Stripe billing webhook flow works under aspire run, and how
  to rotate the local test-mode key when it expires.
status: 'Living'
purpose: How-to for testing Stripe subscription flows locally without touching production billing.
---

# STRIPE_LOCAL.md — Testing Stripe subscription logic locally

This document covers how the local Stripe billing flow works when running `aspire run`, and how to rotate the test-mode key when it expires.

---

## How it works — fully automatic

The Aspire AppHost (`infrastructure/aspire/Skoleoverblikket.AppHost/Program.cs`) runs a `stripe-listen` container alongside the rest of the stack. It runs `stripe listen` non-interactively (authenticated via `STRIPE_API_KEY`, no `stripe login` needed) and forwards every Stripe test-mode webhook event straight to the local API. There is nothing to start manually — as long as `aspire run` is running, webhooks work.

```
Stripe (test mode) / stripe trigger
        │
        ▼
   stripe-listen container (stripe listen --forward-to ...)
        │  forwards with Stripe-Signature header
        ▼
   http://host.docker.internal:5000/api/v1/stripe/webhook
        │
        ▼
   StripeWebhookController → SubscriptionService
```

The API itself (`Stripe:SecretKey` in `appsettings.Development.json`) and the `stripe-listen` container (`stripe-secret-key` Aspire parameter, same value) both use the same committed test-mode secret key, so checkout sessions created locally and the webhooks they trigger are on the same Stripe test account.

---

## Full end-to-end flow via the UI

1. Run `aspire run` from the repo root.
2. Log in as a school admin in the local frontend (http://localhost:5173).
3. Navigate to billing → click "Køb abonnement".
4. The frontend calls `POST /api/v1/billing/checkout` → you are redirected to Stripe Checkout.
5. Use a [Stripe test card](https://docs.stripe.com/testing#cards): `4242 4242 4242 4242`, any future expiry, any CVC.
6. Complete payment → Stripe fires `checkout.session.completed` → the `stripe-listen` container forwards it → `StripeWebhookController` processes it → subscription status in DB becomes `Active`. No manual step in between.

---

## Webhook events the app handles

| Event | Handler effect |
|---|---|
| `checkout.session.completed` | Creates/links subscription record, sets status to `Active`, persists the checked-out `Interval` |
| `customer.subscription.updated` | Syncs status from Stripe (e.g. trial → active, active → past_due); syncs `Interval` if the Price's recurring interval changed — driven by `POST /api/v1/billing/interval` (`SwitchIntervalAsync`), not Stripe Portal, which has plan/interval switching disabled (see `SubscriptionService.CreateBillingPortalSessionAsync`) |
| `customer.subscription.deleted` | Sets status to `Canceled` |
| `invoice.payment_succeeded` | Sets status to `Active`, updates `CurrentPeriodEnd` |
| `invoice.payment_failed` | Sets status to `PastDue` |

---

## Configuration reference

All local Stripe config lives in `api/Skoleoverblikket.Api/appsettings.Development.json`:

```json
"Stripe": {
  "SecretKey": "sk_test_...",
  "BasePriceIdMonthly": "price_...",
  "BasePriceIdYearly": "price_...",
  "WebhookSecret": "whsec_...",
  "ModulePriceIds": { "ParentModule": { "Monthly": "price_...", "Yearly": "price_..." }, ... }
}
```

The same `SecretKey` value is also set as the `stripe-secret-key` Aspire parameter in `Program.cs` (used by the `stripe-listen` container). Keep them in sync — see rotation below.

For production values see `docs/DEPLOYMENT.md`.

---

## Rotating the Stripe test key

**Symptom**: `stripe-listen` container logs `level=fatal msg="Error while authenticating with Stripe: ... api_key_expired"`.

**Cause**: Stripe keys obtained via `stripe login` (CLI OAuth) auto-expire after 60–90 days. Keys created directly in the Stripe Dashboard never expire — use one of those instead so this doesn't recur.

**Fix**:

1. Stripe Dashboard → Developers → API keys → create a new test-mode secret key (or use the existing dashboard-created one, not a CLI-login-derived one).
2. Replace the value in two places:
   - `api/Skoleoverblikket.Api/appsettings.Development.json` → `Stripe:SecretKey`
   - `infrastructure/aspire/Skoleoverblikket.AppHost/Program.cs` → the `stripe-secret-key` parameter's default value
3. Restart `aspire run`. The `stripe-listen` container will print a fresh `whsec_...` on first connect — if it differs from `Stripe:WebhookSecret` in `appsettings.Development.json`, copy it in (see troubleshooting below). In practice the signing secret is deterministic per API key, so this is usually a no-op after the first rotation with a given key.

Do **not** use production Stripe keys in a local environment. Test-mode keys (`sk_test_...`) are safe to commit to this repo's dev config.

---

## Troubleshooting

**400 from the webhook endpoint**
The `Stripe-Signature` header did not validate. `Stripe:WebhookSecret` in `appsettings.Development.json` does not match the secret the `stripe-listen` container is using. Check the container's logs in the Aspire dashboard for the `whsec_...` it printed on startup and copy it in.

**`stripe-listen` container fails with `api_key_expired`**
See "Rotating the Stripe test key" above.

**Webhook event received but subscription not updated**
Check that `school_id` is present in the Stripe event metadata. The webhook handler resolves the tenant by looking up `school_id` on the Stripe customer or checkout session. If it is missing, the handler logs a warning and skips the update.
