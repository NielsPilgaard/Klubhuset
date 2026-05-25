---
name: stripe-listen
description: "Start the Stripe CLI webhook listener for local development. USE THIS SKILL when the user says 'start stripe', 'stripe listen', 'stripe webhooks', 'test stripe locally', 'launch stripe cli', or 'stripe webhook listener'. Runs scripts/stripe-listen.ps1 which forwards Stripe events to the local API and prints the webhook secret with instructions."
---

# Stripe Listen Skill

Starts the Stripe CLI webhook listener and forwards events to the local API at `http://localhost:5000/api/v1/stripe/webhook`.

## What this does

1. Checks Stripe CLI is installed
2. Prints instructions: where to copy the webhook secret
3. Runs `stripe listen --forward-to http://localhost:5000/api/v1/stripe/webhook`

## Steps

1. Run the script in a terminal the user can see (not background — it must stay open):

```powershell
powershell -ExecutionPolicy Bypass -File scripts/stripe-listen.ps1
```

2. Tell the user:
   - Watch for the `whsec_...` secret printed on startup
   - Copy it into `api/Skoleoverblikket.Api/appsettings.Development.json` → `Stripe:WebhookSecret`
   - Leave this terminal open
   - Use a second terminal for `stripe trigger <event>`

3. The Aspire stack must be running for the forwarded events to reach the API. If it is not running, start it first with `aspire start`.

## Common trigger commands (second terminal)

```bash
stripe trigger checkout.session.completed
stripe trigger customer.subscription.updated
stripe trigger invoice.payment_failed
stripe trigger invoice.payment_succeeded
stripe trigger customer.subscription.deleted
```

## Troubleshooting

- **400 from webhook endpoint**: `WebhookSecret` in appsettings does not match the CLI secret. Copy the `whsec_...` from the CLI output into the config.
- **`stripe: command not found`**: `winget install Stripe.StripeCLI`
- **No events received**: Aspire stack is not running — run `aspire start` first.

See `docs/STRIPE_LOCAL.md` for the full reference.
