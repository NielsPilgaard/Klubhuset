import { test, expect, type Page } from '@playwright/test'

test.use({ storageState: { cookies: [], origins: [] } })

const MAILPIT_API = 'http://localhost:8025/api/v1'
const ADMIN_EMAIL = 'admin@debugskolen.dk'
const ADMIN_PASSWORD = 'test1234'

function uniqueStaffEmail() {
  return `staff-${Date.now()}@testskole.dk`
}

async function loginAsAdmin(page: Page) {
  await page.goto('/login')
  await page.waitForURL(/localhost:8080/, { timeout: 15_000 })
  await page.locator('#username').fill(ADMIN_EMAIL)
  await page.locator('#password').fill(ADMIN_PASSWORD)
  await page.getByRole('button', { name: /log ind|sign in/i }).click()
  await page.waitForURL((url) => url.port !== '8080', { timeout: 60_000 })
  await expect(page).toHaveURL(/\/(dashboard|backoffice)/, { timeout: 20_000 })
}

test.describe.serial('Staff invitation flow', () => {
  test('invited staff member can log in and lands on their own schedule, not dashboard', async ({ page, request }) => {
    test.setTimeout(120_000)
    const staffEmail = uniqueStaffEmail()
    const staffName = 'Test Medarbejder'

    // --- Step 1: admin logs in ---
    await loginAsAdmin(page)

    // --- Step 2: admin creates staff member via API ---
    // page.request does not carry the Keycloak bearer token. Use page.evaluate so the
    // fetch runs inside the browser where window.__keycloak (exposed in dev mode) holds the token.
    const createResult = await page.evaluate(async ({ staffName, staffEmail }: { staffName: string; staffEmail: string }) => {
      const kc = (window as unknown as { __keycloak: { token: string; updateToken: (n: number) => Promise<boolean> } }).__keycloak
      await kc.updateToken(30)
      const res = await fetch('/api/v1/staff', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${kc.token}` },
        body: JSON.stringify({ name: staffName, email: staffEmail, role: 'Teacher' }),
      })
      return { ok: res.ok, status: res.status, body: await res.json() }
    }, { staffName, staffEmail })
    expect(createResult.ok, `Create staff failed: ${createResult.status}`).toBeTruthy()
    const staff = createResult.body as { id: string }

    // --- Step 3: admin sends invitation ---
    const inviteResult = await page.evaluate(async (staffId: string) => {
      const kc = (window as unknown as { __keycloak: { token: string; updateToken: (n: number) => Promise<boolean> } }).__keycloak
      await kc.updateToken(30)
      const res = await fetch(`/api/v1/staff-invitations/invite/${staffId}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${kc.token}` },
        body: '{}',
      })
      return { ok: res.ok, status: res.status }
    }, staff.id)
    expect(inviteResult.ok, `Send invitation failed: ${inviteResult.status}`).toBeTruthy()

    // --- Step 4: extract invitation link from Mailpit ---
    // Retry a few times in case email delivery is slightly delayed
    let invitationUrl: string | undefined
    for (let i = 0; i < 10; i++) {
      const mailRes = await request.get(`${MAILPIT_API}/messages`)
      const mailBody = await mailRes.json() as { messages?: { ID: string; To: { Address: string }[]; Subject: string }[] }
      const msg = (mailBody.messages ?? []).find((m) =>
        m.To.some((to) => to.Address === staffEmail)
      )
      if (msg) {
        const detail = await request.get(`${MAILPIT_API}/message/${msg.ID}`)
        const detailBody = await detail.json() as { HTML?: string; Text?: string }
        const content = detailBody.HTML ?? detailBody.Text ?? ''
        const match = content.match(/http[^"'\s<>]+\/invitation\/[^"'\s<>]+/)
        if (match) { invitationUrl = match[0].replace(/&amp;/g, '&'); break }
      }
      await page.waitForTimeout(500)
    }
    expect(invitationUrl, 'No invitation link found in email').toBeDefined()

    // --- Step 5: open invitation page in a fresh browser context (not logged in) ---
    // A new context has no cookies or localStorage, so Keycloak starts completely fresh.
    const browser = page.context().browser()!
    const freshContext = await browser.newContext()
    const freshPage = await freshContext.newPage()

    await freshPage.goto(invitationUrl!)

    // Keycloak check-sso may do a redirect cycle (prompt=none → back to invitation URL).
    // Wait until the page is on localhost:5173 (not Keycloak) before checking the heading.
    await freshPage.waitForURL((url) => url.hostname === 'localhost' && url.port === '5173', { timeout: 30_000 })

    // Invitation page should load with heading visible
    await expect(freshPage.getByRole('heading', { name: /inviteret/i })).toBeVisible({ timeout: 15_000 })

    // --- Step 6: click login button, get redirected to Keycloak ---
    await freshPage.getByRole('button', { name: /opret konto|acceptér/i }).click()
    await freshPage.waitForURL(/localhost:8080/, { timeout: 15_000 })

    // --- Step 7: log in with temporary password from email ---
    // The invitation email contains a temporary password; extract it from Mailpit
    const mailRes2 = await request.get(`${MAILPIT_API}/messages`)
    const mailBody2 = await mailRes2.json() as { messages?: { ID: string; To: { Address: string }[] }[] }
    const msg2 = (mailBody2.messages ?? []).find((m) =>
      m.To.some((to) => to.Address === staffEmail)
    )
    expect(msg2, 'Could not re-fetch invitation email').toBeDefined()
    const detail2 = await request.get(`${MAILPIT_API}/message/${msg2!.ID}`)
    const detailBody2 = await detail2.json() as { HTML?: string; Text?: string }
    const emailText = detailBody2.Text ?? detailBody2.HTML ?? ''

    // Temporary password appears after "midlertidige adgangskode" in the plain-text email
    const passMatch = emailText.match(/midlertidige adgangskode[^:]*:\s*([^\s\n<]+)/i)
    expect(passMatch, 'No temporary password found in invitation email').toBeDefined()
    const tempPassword = passMatch![1].replace(/<[^>]+>/g, '').trim()

    await freshPage.locator('#username').fill(staffEmail)
    await freshPage.locator('#password').fill(tempPassword)
    await freshPage.getByRole('button', { name: /log ind|sign in/i }).click()

    // Keycloak forces UPDATE_PASSWORD — fill new password
    await freshPage.waitForURL(/localhost:8080.*password|update-password/i, { timeout: 10_000 }).catch(() => {})
    if (freshPage.url().includes('localhost:8080')) {
      const newPassword = 'NewPass456!'
      const newPassField = freshPage.locator('#password-new, input[name="password-new"]')
      const confirmField = freshPage.locator('#password-confirm, input[name="password-confirm"]')
      if (await newPassField.isVisible({ timeout: 5_000 }).catch(() => false)) {
        await newPassField.fill(newPassword)
        await confirmField.fill(newPassword)
        await freshPage.getByRole('button', { name: /gem|submit|opdater|update/i }).click()
      }
    }

    // --- Step 8: back on invitation page with ?accept=1 — invitation auto-accepted ---
    await freshPage.waitForURL(/\/invitation\//, { timeout: 20_000 })
    await expect(freshPage.getByText(/invitation accepteret/i)).toBeVisible({ timeout: 10_000 })

    // --- Step 9: click "Gå til mit skema" — must land on /mig/skema, NOT /dashboard ---
    const apiErrors: { url: string; status: number }[] = []
    freshPage.on('response', (res) => {
      if (res.url().includes('/api/v1/') && res.status() === 403) {
        apiErrors.push({ url: res.url(), status: res.status() })
      }
    })

    await freshPage.getByRole('link', { name: /gå til mit skema/i }).click()
    await expect(freshPage).toHaveURL(/\/mig\/skema/, { timeout: 15_000 })

    // Wait for all deferred queries to settle
    await freshPage.waitForTimeout(2_000)
    await freshContext.close()
    expect(
      apiErrors,
      `Got 403 errors on staff schedule page:\n${apiErrors.map((e) => e.url).join('\n')}`
    ).toHaveLength(0)
  })
})
