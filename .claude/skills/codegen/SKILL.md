---
name: codegen
description: Regenerate OpenAPI spec then frontend API client for Skoleoverblikket. USE when user says 'codegen', 'swagger gen', 'regenerate api', 'run codegen', 'generate api client', 'regenerate spec', 'regenerate openapi', or after controller/model/endpoint changes.
---

Run in order — openapi-ts depends on the spec being current:

1. `dotnet build api/Skoleoverblikket.Api/Skoleoverblikket.Api.csproj` — regenerates `openapi/Skoleoverblikket.Api.json` via `OpenApiGenerateDocumentsOnBuild`
2. `cd web && npm run api:generate` — generates typed client from the spec

Report success or paste errors verbatim.
