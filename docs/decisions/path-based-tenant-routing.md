# Path-based tenant routing at MVP; slug rules and immutability

**Status**: Accepted

## Decision

At MVP, clubs are served at `klubhuset.dk/{slug}` (path-based routing). Subdomain routing is deferred to Forening+ (custom domain feature).

### Slug rules

- Chosen by the club admin at signup
- Format: lowercase letters (`a-z`), digits (`0-9`), hyphens (`-`) only
- Length: 3–40 characters
- Must be globally unique across all tenants
- Reserved words are blocked: `api`, `admin`, `www`, `static`, `health`, `app`, `dashboard`, `login`, `logout`, `signup`, and others as needed
- **Immutable immediately after creation** — no self-serve rename. Slug corrections require contacting Klubhuset support.

### Security

- Slugs are resolved to a TenantId by the tenant resolution middleware. All downstream code works with TenantId only — never trusts the slug string as an authorization signal.
- Unknown slugs return HTTP 404.
- Slug → TenantId lookup is cached to avoid a DB round-trip on every request.

## Reason

Path routing requires no wildcard DNS or wildcard SSL cert, making it significantly simpler to deploy on a VPS with Dokploy. Immutable slugs eliminate a class of broken-link bugs and reduce support complexity. Resolving to a TenantId at the middleware boundary is the standard multi-tenant security pattern.
