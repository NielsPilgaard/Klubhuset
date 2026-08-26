---
title: 'ADR: Monorepo with OpenAPI-based type sharing'
status: 'Accepted'
date: '2025-01-01'
authors: 'Niels Pilgaard Grøndahl'
tags: ['architecture', 'api', 'codegen']
supersedes: ''
superseded_by: ''
description: >-
  Single repo with /api and /web; the API generates an OpenAPI spec at build
  time and the frontend generates a typed client from it via hey-api/openapi-ts,
  eliminating hand-written fetch calls and type duplication.
---

# ADR: Monorepo with OpenAPI-based type sharing

## TL;DR

One Git repo, two top-level folders: `/api` (ASP.NET Core) and `/web` (React). API generates an OpenAPI spec (Swashbuckle) at build time. Frontend runs `hey-api/openapi-ts` codegen against that spec to produce a typed client — see the `/codegen` skill. Types are never hand-duplicated; fetch calls to spec'd endpoints are never hand-written.

## Status

**Accepted**

## Context

A single developer maintaining both API and frontend needs to avoid a whole class of bugs where the two drift on request/response shapes. A separate-repo split would add coordination friction to every cross-cutting change (e.g. adding a field to an endpoint and its consumer in one PR).

## Decision

Single repository, `/api` and `/web`. The API's OpenAPI spec is the single source of truth for types. `hey-api/openapi-ts` generates the frontend's typed client from that spec (config: `web/openapi-ts.config.ts`). Regeneration is the `/codegen` skill, run after any controller/model/endpoint change.

## Consequences

### Positive

- **POS-001**: One PR can span both API and frontend changes, keeping them reviewable together.
- **POS-002**: Codegen eliminates the class of bugs where frontend and backend silently diverge on types.
- **POS-003**: No hand-written fetch calls to spec'd endpoints — the generated client is the only path, enforced by [AGENTS.md](../../AGENTS.md) coding conventions.

### Negative

- **NEG-001**: Frontend types are only as fresh as the last codegen run — a forgotten regeneration after a backend change produces stale (but type-checked, so not silently broken) client code.

## Alternatives Considered

### Kiota / NSwag

- **ALT-001**: **Description**: Microsoft-maintained OpenAPI client generators, evaluated early alongside hey-api/openapi-ts.
- **ALT-002**: **Rejection Reason**: `hey-api/openapi-ts` was adopted instead for its TypeScript-first output and lighter config surface. This ADR previously named Kiota/NSwag as the tool choice; that was stale relative to the actual codegen setup and has been corrected here.

### Separate repositories

- **ALT-003**: **Description**: Independent repos for API and frontend, coordinated via published OpenAPI spec artifacts.
- **ALT-004**: **Rejection Reason**: Adds release coordination overhead for a single-developer team where most features touch both layers in the same change.

## Related Decisions

- [tech-stack](tech-stack.md) — the ASP.NET Core + React choice this decision builds on
