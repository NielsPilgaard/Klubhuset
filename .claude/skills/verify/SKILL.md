---
name: verify
description: "Run all local validation checks for Skoleoverblikket before declaring work done. USE THIS SKILL when the user says 'verify', 'check everything', 'validate', 'is this ready', 'does it compile', 'are there errors', or similar. Runs: ESLint, TypeScript build, dotnet format, dotnet build, API integration tests, and Playwright e2e."
---

# Verify Skill

Runs all checks matching CI via `verify.ps1` at the repo root.

## Run

```powershell
# Check everything
pwsh scripts/verify.ps1

# Auto-fix dotnet formatting, then check
pwsh scripts/verify.ps1 -Fix

# Skip integration tests (quick compile check)
pwsh scripts/verify.ps1 -SkipTests
```

The script runs all steps and **collects all errors before exiting** — you see every failure at once, not just the first one.

## Steps

1. **ESLint** — `cd web && npm run lint`
2. **TypeScript build** — `cd web && npm run build`
3. **dotnet format** — `--verify-no-changes` (or auto-fix with `-Fix`)
4. **dotnet build** — Release, `-p:CI=true`
5. **API integration tests** — tUnit + Testcontainers (skipped with `-SkipTests`; needs Docker)

## Playwright e2e

Playwright is not part of `verify.ps1` — use the `/test` skill for that.

## Rules

- Run `verify.ps1` after every code change, before committing or declaring done
- Use `-Fix` when dotnet format is the only failure — it auto-fixes and reruns clean
- If Docker is not running, integration tests fail with a container error — say so explicitly rather than treating it as a code bug
- Do not report work as done until the script exits 0
