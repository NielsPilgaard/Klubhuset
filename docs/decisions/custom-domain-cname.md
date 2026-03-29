# Custom domain via CNAME, self-serve with guided flow (Forening+)

**Status**: Accepted

## Decision

Custom domain (Forening+ only) is self-serve. The club enters their desired domain in the dashboard, receives a single DNS instruction (add a CNAME record pointing to `clubs.klubhuset.dk`), and the platform provisions SSL automatically.

## Reason

A single CNAME change is feasible in any registrar (GoDaddy, One.com, Domeneshop) — it is one form field, not a zone file. Automatic cert provisioning eliminates manual SSL work.

## Implementation options

- **Caddy** (self-hosted, recommended for MVP): on-demand TLS auto-provisions Let's Encrypt certs for arbitrary hostnames on first request.
- **Cloudflare for SaaS**: clubs CNAME to Cloudflare, certs and routing handled automatically (~$0.10/domain/month). Consider if Caddy ops burden is too high.
