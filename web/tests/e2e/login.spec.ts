import { test, expect } from '@playwright/test'

const TEST_EMAIL = 'admin@debugskolen.dk'
const TEST_PASSWORD = 'test1234'

test('login redirects through Keycloak and lands on dashboard', async ({ page }) => {
  await page.goto('/login')

  // /login triggers keycloak.login() which redirects to Keycloak UI
  await page.waitForURL(/localhost:8080/, { timeout: 15_000 })

  await page.locator('#username').fill(TEST_EMAIL)
  await page.locator('#password').fill(TEST_PASSWORD)
  await page.getByRole('button', { name: /log ind|sign in/i }).click()

  // Wait for Keycloak to process login and redirect back to the app
  await page.waitForURL((url) => url.port !== '8080', { timeout: 30_000 })

  // Keycloak redirects back to /dashboard
  await expect(page).toHaveURL(/\/dashboard/, { timeout: 20_000 })
})

test('login with wrong password stays on Keycloak with error', async ({ page }) => {
  await page.goto('/login')

  await page.waitForURL(/localhost:8080/, { timeout: 15_000 })

  // Use a non-existent account so Keycloak brute-force protection doesn't lock the shared admin account
  await page.locator('#username').fill('ikkeeksisterende@testskole.dk')
  await page.locator('#password').fill('forkertadgangskode')
  await page.getByRole('button', { name: /log ind|sign in/i }).click()

  await expect(page.locator('.alert-error, #input-error, [class*="error"]')).toBeVisible({ timeout: 10_000 })
  expect(page.url()).toContain('localhost:8080')
})
