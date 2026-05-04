import { defineConfig, devices } from '@playwright/test'

const WEB_URL = process.env.PLAYWRIGHT_BASE_URL ?? 'http://localhost:5173'

// Dev default matches the Aspire AppHost user-secrets value for keycloak-admin-password.
// Override with KEYCLOAK_ADMIN_PASSWORD env var in CI.
process.env.KEYCLOAK_ADMIN_PASSWORD ??= 'your-dev-password'
const ASPIRE_HOST_DIR = '../infrastructure/aspire/Skoleoverblikket.AppHost'

export default defineConfig({
  testDir: './tests/e2e',
  fullyParallel: false,
  retries: process.env.CI ? 1 : 0,
  timeout: 60_000,
  use: {
    baseURL: WEB_URL,
    trace: 'on-first-retry',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  // Start the full Aspire stack before tests if not already running.
  // Set SKIP_ASPIRE=1 to skip (e.g. when the stack is already up).
  webServer: process.env.SKIP_ASPIRE
    ? undefined
    : {
        command: `dotnet run --project ${ASPIRE_HOST_DIR}`,
        url: WEB_URL,
        timeout: 180_000,
        reuseExistingServer: true,
        stdout: 'pipe',
        stderr: 'pipe',
      },
})
