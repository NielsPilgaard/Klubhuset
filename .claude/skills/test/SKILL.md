---
name: test
description: "Run the Playwright E2E test suite for Skoleoverblikket. USE THIS SKILL when the user explicitly asks to run Playwright/E2E tests, or after UI changes where browser-level verification is needed. For compile checks, dotnet format, and API integration tests without Playwright, use /verify instead. The Aspire stack is assumed to already be running."
---

# Test Skill

Runs the full Playwright E2E suite against the already-running Aspire dev stack.

> For TypeScript build, dotnet format, dotnet build, and API integration tests, use `/verify` instead.

## Command

Always run from the repo root:

```bash
cd web && SKIP_ASPIRE=1 npx playwright test --reporter=line
```

`SKIP_ASPIRE=1` tells Playwright not to start Aspire — assume it's already up. Omit it if the Aspire stack is not running.

## What to report

After the run, report clearly:

- **Pass / fail count** — e.g. "17 passed" or "14 passed, 3 failed"
- **Which tests failed** — name and file, verbatim from the output
- **Failure details** — the assertion error and the line it failed on
- **Whether this is a code bug or a test environment issue** — if a test fails because Aspire isn't running or a port is unreachable, say so explicitly rather than diving into code fixes

## Fixing failures

If tests fail:

1. Read the failure output carefully before touching any code
2. Check whether the failure is environmental (stack not running, wrong port, leftover state from a previous run) vs a real regression
3. If it's a real regression, investigate the root cause in the relevant spec file and the frontend/API code it exercises
4. Fix the underlying issue, then re-run to confirm green

Do not comment out failing tests or lower assertion thresholds to make tests pass.

## Test locations

- Spec files: `web/tests/e2e/`
- Playwright config: `web/playwright.config.ts`
- npm script shorthand: `npm run test:e2e:skip-aspire` (from `web/`)
