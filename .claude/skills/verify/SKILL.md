---
name: verify
description: "Run all local validation checks for Skoleoverblikket before declaring work done. USE THIS SKILL when the user says 'verify', 'check everything', 'validate', 'is this ready', 'does it compile', 'are there errors', or similar. Runs: TypeScript build, dotnet format, dotnet build, API integration tests. Does NOT run Playwright e2e — use /test for that."
---

# Verify Skill

Runs all fast, non-Playwright checks. Use this after every code change to confirm nothing is broken before committing or declaring done.

## Steps — run in order

### 1. TypeScript build

```bash
cd web && npm run build
```

Catches type errors, import errors, and API shape mismatches. This is what CI runs. If this fails, fix before proceeding — do not move on.

### 2. dotnet format check

```bash
dotnet format api/Skoleoverblikket.Api/Skoleoverblikket.Api.csproj --verify-no-changes
```

CI enforces this. If it reports violations, run without `--verify-no-changes` to auto-fix, then re-check.

### 3. dotnet build

```bash
dotnet build api/Skoleoverblikket.Api/Skoleoverblikket.Api.csproj --configuration Release -p:CI=true
```

Catches C# compile errors, missing references, OpenAPI spec drift.

### 4. API integration tests

```bash
dotnet test --configuration Release
```

Runs all tUnit integration tests. Uses Testcontainers — needs Docker running. If Docker is not running, say so explicitly rather than claiming failure is a code bug.

## What to report

After all steps complete, report:

- Pass/fail per step
- For failures: exact error message and file:line
- Whether failure is code, environment (Docker not running, Node not installed), or config

## Rules

- Never skip a step because a previous step passed
- If step 1 or 2 fails, fix it before running step 3 — compiler errors make dotnet format output noisy
- Do not report work as done until all four steps pass
- For Playwright e2e, use the `/test` skill instead
