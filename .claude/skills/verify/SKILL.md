---
name: verify
description: "Run all local validation checks for Skoleoverblikket before declaring work done. USE THIS SKILL when the user says 'verify', 'check everything', 'validate', 'is this ready', 'does it compile', 'are there errors', or similar. Runs: ESLint, TypeScript build, dotnet format, dotnet build, API integration tests, and Playwright e2e."
---

# Verify Skill

Runs all checks matching CI. Use this after every code change to confirm nothing is broken before committing or declaring done.

## Steps — run in order

### 1. ESLint

```bash
cd web && npm run lint
```

Catches unused vars, import errors, and lint violations. CI runs this. If this fails, fix before proceeding.

### 2. TypeScript build

```bash
cd web && npm run build
```

Catches type errors and API shape mismatches. If this fails, fix before proceeding — do not move on.

### 3. dotnet format check

```bash
dotnet format api/Skoleoverblikket.Api/Skoleoverblikket.Api.csproj --verify-no-changes
```

CI enforces this. If it reports violations, run without `--verify-no-changes` to auto-fix, then re-check.

### 4. dotnet build

```bash
dotnet build api/Skoleoverblikket.Api/Skoleoverblikket.Api.csproj --configuration Release -p:CI=true
```

Catches C# compile errors, missing references, OpenAPI spec drift.

### 5. API integration tests

```bash
dotnet test --configuration Release
```

Runs all tUnit integration tests. Uses Testcontainers — needs Docker running. If Docker is not running, say so explicitly rather than claiming failure is a code bug.

### 6. Playwright e2e

```bash
cd web && SKIP_ASPIRE=1 npx playwright test --reporter=line
```

`SKIP_ASPIRE=1` assumes Aspire stack is already running. Omit it if it's not running.

## What to report

After all steps complete, report:

- Pass/fail per step
- For failures: exact error message and file:line
- Whether failure is code, environment (Docker not running, Node not installed), or config

## Rules

- Never skip a step because a previous step passed
- If step 1 or 2 fails, fix it before running step 3 — compiler errors make dotnet format output noisy
- Do not report work as done until all six steps pass
