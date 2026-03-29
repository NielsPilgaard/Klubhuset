# Monorepo with OpenAPI-based type sharing

**Status**: Accepted

## Decision

Single Git repository with two top-level folders: `/api` (ASP.NET Core) and `/web` (React + Vite + TypeScript). The API generates an OpenAPI spec at build time. The web app uses a codegen tool (Kiota or NSwag) to generate a typed API client from that spec. Types are never duplicated by hand.

## Reason

A monorepo makes it trivial to keep API and client in sync — one PR can span both. OpenAPI codegen eliminates the class of bugs where the frontend and backend have diverged on types. A separate-repo approach would add friction to every cross-cutting change.
