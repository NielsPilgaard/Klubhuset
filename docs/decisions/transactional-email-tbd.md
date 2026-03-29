# Transactional email provider: Scaleway TEM via SMTP

**Status**: Accepted

## Decision

Use **Scaleway Transactional Email** (SMTP relay) in all environments: development, staging, and production.

Integrate via **MailKit** over SMTP (port 587, STARTTLS). No provider SDK required.

## Context

Klubhuset sends transactional emails on behalf of clubs: membership confirmations, payment receipts, password resets. These emails contain personal member data, so the provider must be EU-hosted. The platform is white-label — no third-party branding may appear in member-facing emails.

Volume at launch is low: likely well under 1,000 emails/month for the first tenants, growing to ~5,000/month at scale. Cost-efficiency and operational simplicity are the primary drivers.

## Evaluation

### Integration approach

All evaluated providers support **SMTP relay** alongside REST APIs. SMTP relay via **MailKit** (NuGet: `MailKit`) is the standard C# approach and requires no provider-specific SDK:

```csharp
using var smtp = new SmtpClient();
await smtp.ConnectAsync("smtp.provider.com", 587, SecureSocketOptions.StartTls);
await smtp.AuthenticateAsync(username, apiKeyOrPassword);
await smtp.SendAsync(message);
await smtp.DisconnectAsync(true);
```

Switching providers is a config change — one connection string swap. No application code changes.

### Scaleway TEM

- **EU data residency**: Yes — French cloud provider (Iliad group), data never leaves the EU.
- **Free tier**: 300 emails/month permanently included.
- **Cost**: €0.25 per 1,000 emails beyond the free 300. At 5,000/month: **~€1.18/month**. At 10,000/month: **~€2.43/month**. No monthly minimum, no fixed fee.
- **Branding**: none — clean white-label output on all plans.
- **SMTP**: `smtp.tem.scaleway.com:587`
- **Caveat**: new accounts have a default rate limit of ~300 emails/hour; raiseable via support request.

### Brevo (rejected)

Considered for dev-only use due to generous free tier. Rejected — having two providers adds config complexity for negligible cost saving. Scaleway's 300 free emails/month is sufficient for development.

### MailerSend (rejected)

Previously considered. As of October 2025, MailerSend eliminated its useful free tier for new accounts. The current free tier caps at 500 emails/month with mandatory branding. The first paid tier is $7/month minimum — more expensive than Scaleway's pay-as-you-go at the same volume with less EU clarity.

### Postal / self-hosted (rejected)

Zero cost but significant deliverability risk (cold OVH VPS IP, no managed reputation) and ops overhead (MariaDB, RabbitMQ, Redis). Not appropriate for a solo developer.

### Postmark / Resend (rejected)

US-only infrastructure. Hard disqualifier under GDPR.

## Rationale

Scaleway TEM is the single provider for all environments. The 300 free emails/month covers development. Production cost is ~€1.18/month at 5,000 emails — effectively free. Pure EU hosting, no branding, no monthly minimum, and SMTP relay means zero provider lock-in.

## Implementation notes

- Abstract sends behind `IEmailSender` — one implementation, provider is injected via config.
- Store SMTP credentials in user secrets locally, environment variables in production.
- Configure SPF, DKIM, and DMARC on the sending domain before any production send.
- All email templates must carry the club's branding (name, logo) — never Klubhuset branding.
- Do not route bulk/marketing sends through this integration. Transactional only.

## Consequences

- No provider lock-in — switching is a config change.
- Near-zero email cost for the first year of production.
- New Scaleway accounts have a default rate limit of ~300 emails/hour; raiseable via support request if needed.
- MailKit adds one NuGet dependency (`MailKit`).
