# AGENTS.md

This file defines how AI agents should work in this codebase.

## Project context

Klubhuset is a multi-tenant, white-label SaaS for Danish sports societies (foreninger). Each tenant has their own branded space. End users never see the Klubhuset brand — they see their club's logo and name. The platform is Danish-language only, targeting the Danish market.

Reference club profile: ~1100 members, 8 sports, mid-sized Danish town — use this as the mental model for a typical Forening tier customer.

**Simplicity is a core product value.** The users are a 67-year-old sekretær, a busy parent on a phone at the football pitch, a 14-year-old gymnast, and a 55-year-old volunteer formand. See [docs/PERSONAS.md](../docs/PERSONAS.md). Every UI decision must pass: can Kirsten (the sekretær) complete this without calling her grandchild?

## Architecture principles

**Multi-tenancy**: every database query must be scoped to a tenant. Never leak data across tenants. Tenant ID must be present on every query that touches member, team, afdeling, or payment data. Enforced via EF Core global query filter:
```csharp
HasQueryFilter(e => e.TenantId == _tenantContext.TenantId)
```
Never bypass this filter. Never trust a slug string as an authorization signal — always resolve to a TenantId at the middleware boundary.

**White-label**: branding (logo, club name, colors) is tenant-specific and must be applied at the UI layer. No branding values may be hard-coded anywhere.

**Simplicity first**: the primary user is a 60-year-old volunteer chairman (formand) with low technical sophistication. Every feature must be operable without technical knowledge. Prefer fewer options over more. Prefer obvious over clever.

**Mobile-first**: all member-facing UI must work fully on a phone. Admin UI must work on tablet and desktop.

**Payment tier separation**:

- Free tier: clubs use their own MobilePay Business account. The platform is bypassed entirely — Klubhuset never touches their money and has no role in the payment flow.
- Paid tiers (Forening, Forening+): payments go through platform infrastructure (Stripe Connect + MobilePay Subscriptions). Transaction fees are passed to members, not clubs.

## Coding conventions

### API (ASP.NET Core / C#)

- **API style**: RESTful, versioned at `/api/v1/`. OpenAPI spec is generated from code (Swashbuckle). Do not hand-write the spec.
- **Error responses**: all API errors use `ProblemDetails` format (RFC 7807). Never return plain strings or custom error shapes.
- **Auth**: all endpoints require a valid Keycloak-issued JWT bearer token unless explicitly decorated to allow anonymous. Never skip auth on endpoints that touch tenant data.
- **EF Core migrations**: never modify an existing migration file. Always generate a new migration for schema changes.
- **Tenant scoping**: the `ITenantContext` service is injected and used in the `DbContext` global query filter. Never pass TenantId as a method parameter through business logic — it must come from the context.

### Frontend (React / TypeScript)

- **API client**: always use the generated typed client (Kiota or NSwag output). Never hand-write fetch calls to API endpoints that are in the spec.
- **Styling**: Tailwind utility classes only. No CSS-in-JS, no inline `style` props, no separate `.css` files for component styles.
- **Components**: functional components with hooks only. No class components.

### General

- Style and naming conventions (indentation, casing, imports) are enforced by `.editorconfig` and the project linting setup. Do not duplicate those rules here.
- Afdeling = one sport. One club has many afdelinger. Each afdeling has many teams (hold).

## Testing

See [docs/TESTING.md](../docs/TESTING.md) for the full strategy. Summary of rules agents must follow:

- **Two layers only**: API integration tests (tUnit + WebApplicationFactory + Testcontainers) and Playwright e2e for critical flows. Nothing else.
- **Never mock `DbContext` or `ITenantContext`** — use a real PostgreSQL test database via Testcontainers. Mocks bypass the global query filter.
- **Never test private or internal methods** — only via public HTTP endpoints or rendered UI.
- **Playwright selectors use `data-testid` only** — never CSS classes or DOM structure.
- **One test file per feature flow**, not per class.
- When in doubt whether something needs a test: does a silent break block Kirsten? If yes, test it.

## What agents must never do

- Write database queries without tenant scoping
- Hard-code branding, colors, club names, or any tenant-specific values
- Add features not described in [PRD.md](../docs/PRD.md) without explicit instruction from the developer
- Break the payment tier separation (free = direct MobilePay, paid = platform-mediated via Stripe)
- Introduce complexity that a non-technical volunteer would not be able to operate
- Implement out-of-scope features (booking, webshop, SMS, access control, accounting integration, native mobile app)
- Modify existing EF Core migration files
- Trust a URL slug as an authorization token — always resolve to TenantId first
