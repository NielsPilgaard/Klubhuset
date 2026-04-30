/**
 * Security assertions for the Skoleplanen Keycloak realm.
 *
 * These tests hit the Keycloak admin REST API to verify critical security
 * settings and run behavioural checks that would catch regressions if the
 * realm is reconfigured carelessly.
 */
import { test, expect, type APIRequestContext } from '@playwright/test'

// Use 127.0.0.1 so Node's DNS resolution doesn't pick IPv6 loopback
const KEYCLOAK_BASE = 'http://127.0.0.1:8080'
const REALM = 'Skoleplanen'
const ADMIN_REALM = 'master'

async function getAdminToken(request: APIRequestContext): Promise<string> {
  // Use the bootstrap admin credentials configured in the Aspire AppHost
  const res = await request.post(
    `${KEYCLOAK_BASE}/realms/${ADMIN_REALM}/protocol/openid-connect/token`,
    {
      form: {
        grant_type: 'password',
        client_id: 'admin-cli',
        username: 'admin',
        password: process.env.KEYCLOAK_ADMIN_PASSWORD ?? 'ci-test-password',
      },
    },
  )
  expect(res.ok(), `Failed to obtain admin token: ${res.status()}`).toBeTruthy()
  const body = await res.json()
  return body.access_token as string
}

test.describe('Keycloak realm security', () => {
  test('realm has bruteForceProtected enabled with tight policy', async ({ request }) => {
    const token = await getAdminToken(request)

    const res = await request.get(`${KEYCLOAK_BASE}/admin/realms/${REALM}`, {
      headers: { Authorization: `Bearer ${token}` },
    })
    expect(res.ok(), `Admin API returned ${res.status()}`).toBeTruthy()

    const realm = await res.json()

    expect(realm.bruteForceProtected, 'bruteForceProtected must be enabled').toBe(true)
    expect(realm.failureFactor, 'Max login failures should be ≤ 30').toBeLessThanOrEqual(30)
    expect(realm.maxFailureWaitSeconds, 'Max lockout should be ≤ 900s (15 min)').toBeLessThanOrEqual(900)
  })

  test('realm does not allow public user registration', async ({ request }) => {
    const token = await getAdminToken(request)

    const res = await request.get(`${KEYCLOAK_BASE}/admin/realms/${REALM}`, {
      headers: { Authorization: `Bearer ${token}` },
    })
    const realm = await res.json()

    expect(realm.registrationAllowed, 'Public self-registration must be disabled').toBe(false)
    expect(realm.duplicateEmailsAllowed, 'Duplicate emails must be disallowed').toBe(false)
  })

  test('realm does not allow editing usernames', async ({ request }) => {
    const token = await getAdminToken(request)

    const res = await request.get(`${KEYCLOAK_BASE}/admin/realms/${REALM}`, {
      headers: { Authorization: `Bearer ${token}` },
    })
    const realm = await res.json()

    expect(realm.editUsernameAllowed, 'Username (email) editing must be disabled').toBe(false)
  })

  test('realm has short access token lifetime', async ({ request }) => {
    const token = await getAdminToken(request)

    const res = await request.get(`${KEYCLOAK_BASE}/admin/realms/${REALM}`, {
      headers: { Authorization: `Bearer ${token}` },
    })
    const realm = await res.json()

    // Access tokens should not live longer than 10 minutes
    expect(realm.accessTokenLifespan, 'Access token lifetime must be ≤ 600s').toBeLessThanOrEqual(600)
  })

  test('realm SSO session idle timeout is reasonable', async ({ request }) => {
    const token = await getAdminToken(request)

    const res = await request.get(`${KEYCLOAK_BASE}/admin/realms/${REALM}`, {
      headers: { Authorization: `Bearer ${token}` },
    })
    const realm = await res.json()

    // Idle session should expire within 1 hour
    expect(realm.ssoSessionIdleTimeout, 'SSO idle timeout must be ≤ 3600s').toBeLessThanOrEqual(3600)
  })

  test('skoleplanen-web client has implicit flow disabled', async ({ request }) => {
    const token = await getAdminToken(request)

    const res = await request.get(`${KEYCLOAK_BASE}/admin/realms/${REALM}/clients`, {
      headers: { Authorization: `Bearer ${token}` },
      params: { clientId: 'skoleplanen-web' },
    })
    expect(res.ok()).toBeTruthy()

    const clients: { clientId: string; implicitFlowEnabled: boolean }[] = await res.json()
    const webClient = clients.find((c) => c.clientId === 'skoleplanen-web')

    expect(webClient, 'skoleplanen-web client must exist').toBeDefined()
    expect(webClient!.implicitFlowEnabled, 'Implicit flow must be disabled').toBe(false)
  })

  test('skoleplanen-web redirect URIs contain no wildcards beyond localhost and production domain', async ({ request }) => {
    const token = await getAdminToken(request)

    const res = await request.get(`${KEYCLOAK_BASE}/admin/realms/${REALM}/clients`, {
      headers: { Authorization: `Bearer ${token}` },
      params: { clientId: 'skoleplanen-web' },
    })
    const clients: { clientId: string; redirectUris: string[] }[] = await res.json()
    const webClient = clients.find((c) => c.clientId === 'skoleplanen-web')!

    const allowedPatterns = [
      /^http:\/\/localhost:\d+\//,
      /^https:\/\/skoleplanen\.dk\//,
    ]

    for (const uri of webClient.redirectUris) {
      const allowed = allowedPatterns.some((p) => p.test(uri))
      expect(allowed, `Unexpected redirect URI: ${uri}`).toBe(true)
    }

    // Must not have a bare wildcard '*'
    expect(webClient.redirectUris).not.toContain('*')
  })

  test('brute-force protection locks account after repeated failures', async ({ page, request }) => {
    // Purge any existing lockout state by using a fresh email
    // (we deliberately do NOT use the seeded admin account to avoid breaking other tests)
    const testEmail = `brute-force-test@debugskolen.dk`

    // First create the user via the admin API
    const token = await getAdminToken(request)
    const createRes = await request.post(`${KEYCLOAK_BASE}/admin/realms/${REALM}/users`, {
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      data: {
        username: testEmail,
        email: testEmail,
        enabled: true,
        emailVerified: true,
        credentials: [{ type: 'password', value: 'CorrectPass123!', temporary: false }],
      },
    })
    // 409 means the user already exists from a previous run — that's fine
    expect([201, 409]).toContain(createRes.status())

    // Attempt login with wrong password 6 times
    for (let i = 0; i < 6; i++) {
      await page.goto(
        `${KEYCLOAK_BASE}/realms/${REALM}/protocol/openid-connect/auth` +
          `?client_id=skoleplanen-web&response_type=code&redirect_uri=http://localhost:5173/dashboard`,
      )
      await page.locator('#username').fill(testEmail)
      await page.locator('#password').fill('WrongPassword!')
      await page.getByRole('button', { name: /log ind|sign in/i }).click()
      await page.waitForTimeout(200)
    }

    // Now try with the correct password — should be locked out
    await page.goto(
      `${KEYCLOAK_BASE}/realms/${REALM}/protocol/openid-connect/auth` +
        `?client_id=skoleplanen-web&response_type=code&redirect_uri=http://localhost:5173/dashboard`,
    )
    await page.locator('#username').fill(testEmail)
    await page.locator('#password').fill('CorrectPass123!')
    await page.getByRole('button', { name: /log ind|sign in/i }).click()

    // Should still be on Keycloak with an error (account temporarily locked)
    expect(page.url()).toMatch(/127\.0\.0\.1:8080|localhost:8080/)
    await expect(page.locator('.alert-error, #input-error, [class*="error"]')).toBeVisible({
      timeout: 10_000,
    })

    // Cleanup — delete the test user
    const usersRes = await request.get(`${KEYCLOAK_BASE}/admin/realms/${REALM}/users`, {
      headers: { Authorization: `Bearer ${token}` },
      params: { email: testEmail },
    })
    const users: { id: string }[] = await usersRes.json()
    if (users.length > 0) {
      await request.delete(`${KEYCLOAK_BASE}/admin/realms/${REALM}/users/${users[0].id}`, {
        headers: { Authorization: `Bearer ${token}` },
      })
    }
  })
})
