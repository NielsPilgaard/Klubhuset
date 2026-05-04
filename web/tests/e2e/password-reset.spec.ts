import { test, expect } from '@playwright/test'

const KEYCLOAK_BASE = 'http://localhost:8080'
const MAILPIT_API = 'http://localhost:8025/api/v1'
const TEST_EMAIL = 'admin@debugskolen.dk'

test('password reset sends email via Keycloak', async ({ page, request }) => {
  // Purge Mailpit inbox so we start clean
  await request.delete(`${MAILPIT_API}/messages`)

  // Navigate to Keycloak login page for the Skoleoverblikket realm
  await page.goto(
    `${KEYCLOAK_BASE}/realms/Skoleoverblikket/protocol/openid-connect/auth` +
      `?client_id=skoleoverblikket-web&response_type=code&redirect_uri=http://localhost:5173/dashboard`,
  )

  // Click "Glemt adgangskode?"
  await page.getByRole('link', { name: /glemt adgangskode/i }).click()

  // Fill in the email and submit
  await page.locator('#username').fill(TEST_EMAIL)
  await page.getByRole('button', { name: /send nulstillingslink/i }).click()

  // Keycloak should show the Danish success message
  await expect(page.locator('.alert-success')).toBeVisible({ timeout: 10_000 })
  await expect(page.locator('.alert-success')).toContainText('e-mail')

  // Verify Mailpit received the reset email
  const response = await request.get(`${MAILPIT_API}/messages`)
  expect(response.ok()).toBeTruthy()

  const body = await response.json()
  const messages: { To: { Address: string }[]; Subject: string }[] = body.messages ?? []

  const resetEmail = messages.find(
    (m) =>
      m.To.some((to) => to.Address === TEST_EMAIL) &&
      /reset|nulstil|gendan/i.test(m.Subject),
  )

  expect(resetEmail, 'No password reset email found in Mailpit').toBeDefined()
})
