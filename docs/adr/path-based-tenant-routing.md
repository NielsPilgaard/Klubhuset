---
title: 'ADR: Path-based tenant routing; slug rules and immutability'
status: 'Accepted'
date: '2025-01-01'
authors: 'Niels Pilgaard Grøndahl'
tags: ['architecture', 'multi-tenancy', 'security']
supersedes: ''
superseded_by: ''
description: >-
  Schools are served at Skoleoverblikket.dk/{slug} rather than subdomains, with
  immutable slugs resolved to a TenantId at the middleware boundary — never
  trusted directly as an authorization signal.
---

# ADR: Path-based tenant routing; slug rules and immutability

## TL;DR

Tenants are routed by URL path (`/{slug}`), not subdomain — no wildcard DNS/SSL needed on a VPS deployment. Slugs are chosen once at signup, immutable after creation, and always resolved to a `TenantId` at the middleware boundary. Code never trusts the slug string itself as an authorization signal.

## Status

**Accepted**

## Context

Multi-tenant routing needed a scheme that (a) deploys simply on a single OVHCloud VPS via Dokploy, and (b) cannot be exploited as a spoofable authorization signal.

## Decision

Schools are served at `Skoleoverblikket.dk/{slug}` (path-based routing). Subdomain routing is deferred to a possible future premium tier.

### Slug rules

- Chosen by the school admin at signup.
- Format: lowercase letters (`a-z`), digits (`0-9`), hyphens (`-`) only.
- Length: 3–40 characters.
- Must be globally unique across all tenants.
- Reserved words are blocked: `api`, `admin`, `www`, `static`, `health`, `app`, `dashboard`, `login`, `logout`, `signup`, and others as needed.
- **Immutable immediately after creation** — no self-serve rename. Slug corrections require contacting Skoleoverblikket support.

### Security

- Slugs are resolved to a `TenantId` by the tenant resolution middleware. All downstream code works with `TenantId` only — never trusts the slug string as an authorization signal.
- Unknown slugs return HTTP 404.
- Slug → `TenantId` lookup is cached to avoid a DB round-trip on every request.

## Consequences

### Positive

- **POS-001**: No wildcard DNS or wildcard SSL cert required — simpler to deploy on a VPS with Dokploy than subdomain routing would be.
- **POS-002**: Immutable slugs eliminate a class of broken-link bugs (bookmarks, printed URLs) and reduce support complexity.
- **POS-003**: Resolving to `TenantId` at the middleware boundary is the standard multi-tenant security pattern — no controller or service code can accidentally trust a slug.

### Negative

- **NEG-001**: Slug typos or unwanted choices at signup require a manual support request to fix, since renames aren't self-serve.

## Alternatives Considered

### Subdomain routing

- **ALT-001**: **Description**: `{slug}.skoleoverblikket.dk` per tenant.
- **ALT-002**: **Rejection Reason**: requires wildcard DNS and a wildcard SSL certificate, adding deployment complexity on a single VPS. Deferred to a possible future premium tier rather than ruled out permanently.

## Related Decisions

- [tech-stack](tech-stack.md) — OVHCloud VPS + Dokploy hosting this decision optimizes for
