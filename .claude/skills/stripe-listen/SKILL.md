---
name: stripe-listen
description: "Trigger a Stripe test-mode webhook event locally. USE THIS SKILL when the user says 'trigger stripe event', 'test stripe webhook', 'stripe trigger', or 'test stripe locally'. The webhook listener itself runs automatically as part of `aspire run` — this skill only fires one-off events against it via scripts/stripe-listen.ps1."
---

# Stripe Listen Skill

The Stripe webhook listener runs automatically inside the Aspire stack (the `stripe-listen` container in `infrastructure/aspire/Skoleoverblikket.AppHost/Program.cs`), forwarding real Stripe test-mode events to the local API at `http://localhost:5000/api/v1/stripe/webhook`. No manual `stripe listen` terminal, no webhook-secret copy-paste — it's live whenever `aspire run` is running.

This skill is only for firing individual test events against that already-running listener.

## What this does

1. Checks Stripe CLI is installed
2. Runs `stripe trigger <event>` (via `scripts/stripe-listen.ps1 -EventType <event>`)

## Steps

1. Confirm the Aspire stack is running (`aspire run`). If not, tell the user to start it first — the `stripe-listen` container won't be up otherwise.
2. Run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/stripe-listen.ps1 -EventType <event-type>
```

3. Check the Aspire dashboard or API logs to confirm the event was received and handled (`SubscriptionService.HandleWebhookAsync`).

## Common trigger commands

```bash
stripe trigger checkout.session.completed
stripe trigger customer.subscription.updated
stripe trigger invoice.payment_failed
stripe trigger invoice.payment_succeeded
stripe trigger customer.subscription.deleted
```

## Troubleshooting

- **400 from webhook endpoint (signature mismatch)**: the `stripe-listen` container's API key doesn't match `Stripe:WebhookSecret` in `appsettings.Development.json`, or the key expired. See "Rotating the Stripe test key" in `docs/STRIPE_LOCAL.md`.
- **`stripe: command not found`**: `winget install Stripe.StripeCLI` (only needed for `stripe trigger` — the listener itself runs in-container and doesn't need the CLI installed on the host).
- **No events received**: Aspire stack isn't running, or the `stripe-listen` container failed to start — check the Aspire dashboard for its logs. A `401 api_key_expired` there means the key needs rotating (see `docs/STRIPE_LOCAL.md`).

See `docs/STRIPE_LOCAL.md` for the full reference.
