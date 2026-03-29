# DECISIONS.md

Product and architecture decisions are documented in [docs/decisions/](decisions/).

## Index

| Decision | Title |
|----------|-------|
| [member-based-pricing](decisions/member-based-pricing.md) | Member-based pricing, not team-based |
| [free-tier-payment-bypass](decisions/free-tier-payment-bypass.md) | Free tier is fully bypassed from payments |
| [transaction-fees-to-members](decisions/transaction-fees-to-members.md) | Transaction fees charged to members, not clubs |
| [competitor-specific-importers](decisions/competitor-specific-importers.md) | No generic CSV import; competitor-specific importers only |
| [custom-domain-cname](decisions/custom-domain-cname.md) | Custom domain via CNAME, self-serve (Forening+) |
| [white-label-paid-only](decisions/white-label-paid-only.md) | White-label is paid-only |
| [out-of-scope-mvp](decisions/out-of-scope-mvp.md) | Out of scope for MVP |
| [dgi-dif-export-paid](decisions/dgi-dif-export-paid.md) | DGI/DIF statistics export is a paid feature |
| [mobile-first-ui](decisions/mobile-first-ui.md) | Mobile-first UI |
| [afdeling-term](decisions/afdeling-term.md) | "Afdeling" term kept; one afdeling = one sport |
| [traener-message-permission](decisions/traener-message-permission.md) | Træner message permission is admin-configurable per club |
| [team-calendar-exceptions](decisions/team-calendar-exceptions.md) | Full team calendar with exception support |
| [transactional-email-tbd](decisions/transactional-email-tbd.md) | Transactional email: Scaleway TEM via SMTP |
| [tech-stack](decisions/tech-stack.md) | Tech stack |
| [monorepo-openapi](decisions/monorepo-openapi.md) | Monorepo with OpenAPI-based type sharing |
| [path-based-tenant-routing](decisions/path-based-tenant-routing.md) | Path-based tenant routing at MVP; slug rules and immutability |
| [self-hosted-postgres-backups](decisions/self-hosted-postgres-backups.md) | Self-hosted PostgreSQL with manual backup strategy |
