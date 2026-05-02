# STRIPE_LOCAL.md — Testing Stripe subscription logic locally

This document covers how to test the full Stripe billing flow locally using the Stripe CLI — both manually and when running as an agent.

---

## Prerequisites

1. **Stripe CLI installed** — [https://docs.stripe.com/stripe-cli](https://docs.stripe.com/stripe-cli)
2. **Logged in to Stripe CLI** — `stripe login` (uses the Skoleplanen test account)
3. **Aspire stack running** — `aspire run` from the repo root, or the API already up on port 5000
4. **`appsettings.Development.json` Stripe keys in place** — they are committed and point to the test account

---

## How the webhook pipeline works locally

Stripe cannot reach `localhost` directly. The Stripe CLI acts as a proxy: it listens on Stripe's servers and forwards webhook events to your local API.

```
Stripe Dashboard / stripe trigger
        │
        ▼
   Stripe CLI (stripe listen)
        │  forwards with Stripe-Signature header
        ▼
   http://localhost:5000/api/v1/stripe/webhook
        │
        ▼
   StripeWebhookController → SubscriptionService
```

The webhook secret used by the CLI (`whsec_…`) must match `Stripe:WebhookSecret` in `appsettings.Development.json`. The committed development secret is already paired with the CLI listener.

---

## Step-by-step: running the webhook listener

### 1. Start the listener

```bash
stripe listen --forward-to http://localhost:5000/api/v1/stripe/webhook
```

The CLI will print a webhook signing secret on startup:

```
> Ready! You are using Stripe API Version [2024-xx-xx].
> Your webhook signing secret is whsec_bd68b4fb57148df7...  (same as appsettings.Development.json)
```

Leave this terminal open. Every Stripe event (triggered manually or by completing a Checkout session) is forwarded here.

### 2. Trigger individual events manually

In a second terminal, use `stripe trigger` to fire specific webhook events without going through the UI:

```bash
# Simulate a completed checkout
stripe trigger checkout.session.completed

# Simulate a subscription update
stripe trigger customer.subscription.updated

# Simulate a failed payment
stripe trigger invoice.payment_failed

# Simulate a successful payment
stripe trigger invoice.payment_succeeded

# Simulate a subscription deletion/cancellation
stripe trigger customer.subscription.deleted
```

The API will log the received event. Check the Aspire dashboard or API stdout.

### 3. Full end-to-end flow via the UI

1. Log in as a school admin in the local frontend (http://localhost:5173)
2. Navigate to billing → click "Upgrade" or similar
3. The frontend calls `POST /api/v1/billing/checkout` → you are redirected to Stripe Checkout
4. Use a [Stripe test card](https://docs.stripe.com/testing#cards): `4242 4242 4242 4242`, any future expiry, any CVC
5. Complete payment → Stripe fires `checkout.session.completed` → CLI forwards it → `StripeWebhookController` processes it → subscription status in DB becomes `Active`

---

## Useful `stripe` CLI commands

| Command | Purpose |
|---|---|
| `stripe listen --forward-to <url>` | Proxy webhooks to local endpoint |
| `stripe trigger <event>` | Fire a test event |
| `stripe customers list` | List test customers |
| `stripe subscriptions list` | List test subscriptions |
| `stripe logs tail` | Stream API request logs |
| `stripe open dashboard` | Open Stripe Dashboard in browser |

---

## Webhook events the app handles

| Event | Handler effect |
|---|---|
| `checkout.session.completed` | Creates/links subscription record, sets status to `Active` |
| `customer.subscription.updated` | Syncs status from Stripe (e.g. trial → active, active → past_due) |
| `customer.subscription.deleted` | Sets status to `Canceled` |
| `invoice.payment_succeeded` | Sets status to `Active`, updates `CurrentPeriodEnd` |
| `invoice.payment_failed` | Sets status to `PastDue` |

---

## Configuration reference

All local Stripe config lives in `api/Skoleplanen.Api/appsettings.Development.json`:

```json
"Stripe": {
  "SecretKey": "sk_test_51T...",
  "PriceId": "price_1T...",
  "WebhookSecret": "whsec_bd68..."
}
```

The `WebhookSecret` here must match what `stripe listen` prints. They are kept in sync in the committed dev config — do not change one without the other.

For production values see `docs/DEPLOYMENT.md`.

---

## Agent instructions

When an agent needs to test subscription logic:

1. Verify the Aspire stack is running (`aspire run` or check port 5000 is responding).
2. Start the webhook listener in the background:
   ```bash
   stripe listen --forward-to http://localhost:5000/api/v1/stripe/webhook &
   ```
3. Trigger the specific event under test with `stripe trigger <event>`.
4. Query the database or call `GET /api/v1/billing/subscription` to assert the expected subscription state.
5. Stop the listener when done (`kill %1` or `pkill -f "stripe listen"`).

Do **not** use production Stripe keys in a local environment. The committed `sk_test_…` key is scoped to test mode and safe to use.

---

## Troubleshooting

**400 from the webhook endpoint**
The `Stripe-Signature` header did not validate. This means the `WebhookSecret` in `appsettings.Development.json` does not match the secret printed by `stripe listen`. Copy the secret from the CLI output into `appsettings.Development.json`.

**Webhook event received but subscription not updated**
Check that `school_id` is present in the Stripe event metadata. The webhook handler resolves the tenant by looking up `school_id` on the Stripe customer or checkout session. If it is missing, the handler logs a warning and skips the update.

**`stripe: command not found`**
Install the Stripe CLI: `winget install Stripe.StripeCLI` on Windows or follow [https://docs.stripe.com/stripe-cli#install](https://docs.stripe.com/stripe-cli#install).
