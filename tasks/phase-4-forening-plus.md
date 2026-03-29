# Phase 4 — Forening+ features

## Goal
Deliver the premium tier capabilities: custom domains, subdomain routing, and custom member fields.

---

## Tasks

### Custom domain

- [ ] Admin enters custom domain in dashboard (e.g. `members.minforening.dk`)
- [ ] System shows single DNS instruction: add CNAME pointing to `clubs.klubhuset.dk`
- [ ] Platform provisions SSL automatically (Caddy on-demand TLS or Cloudflare for SaaS — decide at Phase 4 start)
- [ ] Custom domain routes to correct tenant
- [ ] Domain verification check with helpful error messages if DNS not yet propagated

### Subdomain routing (upgrade from path-based)

- [ ] Wildcard DNS `*.klubhuset.dk` configured
- [ ] Wildcard SSL cert provisioned
- [ ] Tenant resolution middleware updated to resolve from subdomain as well as path
- [ ] Existing path-based URLs continue to work (redirect or alias)

### Custom member fields

- [ ] Admin can define extra fields for their member registration form (text, checkbox, dropdown)
- [ ] Custom fields appear on self-registration form and member profile
- [ ] Custom field data included in CSV exports

### Priority support

- [ ] In-app support contact / ticket submission for Forening+ clubs
- [ ] Flag Forening+ tickets in any support tooling used
