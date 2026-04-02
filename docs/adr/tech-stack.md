# Tech stack

**Status**: Accepted

## Decision

| Layer              | Choice                                      |
| ------------------ | ------------------------------------------- |
| API                | ASP.NET Core Web API, C# 13 / .NET 10       |
| ORM                | Entity Framework Core                       |
| Database           | PostgreSQL (self-hosted in Docker Compose)  |
| Frontend           | React + Vite + TypeScript + Tailwind CSS    |
| Auth / SSO         | Keycloak (Docker Compose service)           |
| Object storage     | OVHCloud Object Storage (S3-compatible, EU) |
| Local S3 emulation | LocalStack (v3, no license required)        |
| Local orchestration| .NET Aspire (v13)                           |
| Hosting            | OVHCloud VPS + Dokploy + Docker Compose     |

## Reason

The developer has strong existing C# expertise (ASP.NET Core, EF Core). Using the primary language of expertise reduces risk and development time for a v1. React + TypeScript is widely known and pairs well with an OpenAPI-generated typed client from the ASP.NET API. Tailwind keeps styling simple and consistent without a design system overhead. Keycloak provides battle-tested multi-tenant auth with OIDC/JWT support — and opens the door for UniLogin integration later. OVHCloud is EU-based and co-locates VPS and object storage to minimize vendor count and latency. .NET Aspire provides a single `aspire run` command to orchestrate all local dev services (PostgreSQL, pgAdmin, Keycloak, LocalStack) with built-in dashboard, health checks, and OpenTelemetry — replacing a hand-maintained docker-compose.yml for local development.
