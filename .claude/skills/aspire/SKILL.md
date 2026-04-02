---
name: aspire
description: Reference guide for .NET Aspire orchestration in this project. Use this skill whenever the user mentions Aspire, the local dev stack, adding/modifying services in the orchestrator, running the local environment, or asks about docker containers for local development. Also trigger when the user wants to add a new infrastructure service (database, cache, message broker, etc.) to the local dev environment, even if they don't mention Aspire by name.
---

# .NET Aspire — Local Orchestration

Aspire v13 orchestrates the entire local dev stack. One command — `aspire run` — starts PostgreSQL, pgAdmin, Keycloak, and LocalStack with a built-in dashboard for logs, traces, and health.

## Quick reference

| Item | Value |
|------|-------|
| Aspire version | **13.2.1** |
| AppHost | `infrastructure/aspire/Skoleplanen.AppHost/` |
| ServiceDefaults | `infrastructure/aspire/Skoleplanen.ServiceDefaults/` |
| Config | `aspire.config.json` (repo root, points to AppHost) |
| Solution format | `.slnx` (not `.sln`) |
| Run command | `aspire run` from repo root |

## Installation

```powershell
irm https://aspire.dev/install.ps1 | iex
```

This installs the `aspire` CLI globally. The setup script (`scripts/setup.ps1`) handles this automatically.

## Running the stack

```bash
aspire run
```

This reads `aspire.config.json` in the repo root, which points to the AppHost project. The Aspire dashboard opens in the browser automatically showing all resources, logs, and traces.

## Container labels

Every container resource in the AppHost must carry the project label so all containers are grouped and identifiable. A `const string label = "skoleplanen"` is defined at the top of `Program.cs`.

Apply it with:
```csharp
.WithContainerRuntimeArgs("--label", $"com.docker.compose.project={label}")
```

For child containers created via callbacks (like pgAdmin), apply inside the callback:
```csharp
.WithPgAdmin(pgAdmin => pgAdmin.WithContainerRuntimeArgs("--label", $"com.docker.compose.project={label}"))
```

## Adding a new service

When adding any new container or resource to the AppHost, always include:

1. **Persistent lifetime** — `.WithLifetime(ContainerLifetime.Persistent)` so the container survives AppHost restarts during development
2. **Project label** — `.WithContainerRuntimeArgs("--label", $"com.docker.compose.project={label}")` for grouping in Docker Desktop
3. **Named endpoint** — `.WithHttpEndpoint(port: ..., targetPort: ..., name: "...")` so other resources can reference it
4. **WaitFor dependencies** — `.WaitFor(dependency)` if the service depends on another being ready first

Example pattern:
```csharp
var myService = builder.AddContainer("myservice", "image/name", "tag")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHttpEndpoint(port: 1234, targetPort: 1234, name: "http")
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={label}")
    .WaitFor(postgres);
```

## Current services

### PostgreSQL
- Shared server with two databases: `skoleplanen` (API) and `keycloak` (Keycloak's own schema)
- pgAdmin included via `.WithPgAdmin()`
- Both postgres and pgAdmin containers carry the project label

### Keycloak
- Runs as a raw container (`quay.io/keycloak/keycloak:26.2`) — there is no first-party Aspire hosting package for Keycloak
- Has its own `keycloak` database on the shared postgres server (Keycloak manages its own schema/migrations internally — never share a database between Keycloak and the API)
- Connected to postgres via `KC_DB_URL` (JDBC format), `KC_DB_USERNAME`, and `KC_DB_PASSWORD` environment variables
- Uses `ReferenceExpression.Create()` to build the JDBC URL from Aspire endpoint references at runtime

### LocalStack
- S3-compatible local emulation for OVHCloud Object Storage
- **Must use v3** (not v4+) — v4 requires a license and account
- Gateway endpoint on port 4566

## NuGet packages

All Aspire hosting packages should use version **13.2.1**:
- `Aspire.AppHost.Sdk` (SDK in csproj)
- `Aspire.Hosting.AppHost`
- `Aspire.Hosting.PostgreSQL`
- `Aspire.Hosting.JavaScript` (for React+Vite frontend, replaces the old `Aspire.Hosting.NodeJs`)

ServiceDefaults uses:
- `Microsoft.Extensions.ServiceDiscovery` (latest stable, currently 10.4.0)
- `OpenTelemetry.*` packages for tracing, metrics, and OTLP export
- `Microsoft.Extensions.Http.Resilience` for HTTP client resilience

## Wiring up new projects

When the API or web projects are created, uncomment and adjust the placeholder blocks in `Program.cs`:

```csharp
// API project
var api = builder.AddProject<Projects.Skoleplanen_Api>("api")
    .WithReference(db)
    .WaitFor(db)
    .WaitFor(keycloak);

// React + Vite frontend
var web = builder.AddNpmApp("web", workingDirectory: "../../../web", scriptName: "dev")
    .WithHttpEndpoint(port: 5173, env: "PORT")
    .WithExternalHttpEndpoints()
    .WithReference(api);
```

The API project should reference `Skoleplanen.ServiceDefaults` and call `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()` to get OpenTelemetry, health checks, and service discovery wired up automatically.
