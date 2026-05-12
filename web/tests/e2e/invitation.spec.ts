import { test, expect, type Page } from '@playwright/test'

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
  await page.waitForURL((url) => url.port !== '8080', { timeout: 30_000 })
  await expect(page).toHaveURL(/\/dashboard/, { timeout: 20_000 })
}

test.describe('Staff invitation flow', () => {
  test('invited staff member can log in and lands on their own schedule, not dashboard', async ({ page, request }) => {
    const staffEmail = uniqueStaffEmail()
    const staffName = 'Test Medarbejder'

    // --- Step 1: admin logs in ---
    await loginAsAdmin(page)

    // --- Step 2: admin creates staff member via API ---
    // Use the generated client indirectly: call the API with the bearer token from localStorage/cookie.
    // Easiest: navigate to staff page and create via UI, or call the API directly using request fixture
    // with the session cookie that Keycloak set after login.
    const createStaffRes = await page.request.post('/api/v1/staff', {
      data: { name: staffName, email: staffEmail, role: 'Teacher' },
    })
    expect(createStaffRes.ok(), `Create staff failed: ${createStaffRes.status()}`).toBeTruthy()
    const staff = await createStaffRes.json() as { id: string }

    // --- Step 3: admin sends invitation ---
    await request.delete(`${MAILPIT_API}/messages`)
    const inviteRes = await page.request.post(`/api/v1/staff-invitations/invite/${staff.id}`, {
      data: {},
    })
    expect(inviteRes.ok(), `Send invitation failed: ${inviteRes.status()}`).toBeTruthy()

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

    // --- Step 5: open invitation page in a fresh context (not logged in) ---
    await page.context().clearCookies()
    await page.goto(invitationUrl!)

    // Invitation page should load with school name visible
    await expect(page.getByText(/inviteret/i)).toBeVisible({ timeout: 10_000 })

    // --- Step 6: click login button, get redirected to Keycloak ---
    await page.getByRole('button', { name: /opret konto|acceptér/i }).click()
    await page.waitForURL(/localhost:8080/, { timeout: 15_000 })

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

    await page.locator('#username').fill(staffEmail)
    await page.locator('#password').fill(tempPassword)
    await page.getByRole('button', { name: /log ind|sign in/i }).click()

    // Keycloak forces UPDATE_PASSWORD — fill new password
    await page.waitForURL(/localhost:8080.*password|update-password/i, { timeout: 10_000 }).catch(() => {})
    if (page.url().includes('localhost:8080')) {
      const newPassword = 'NewPass456!'
      // Keycloak update-password form has password-new and password-confirm fields
      const newPassField = page.locator('#password-new, input[name="password-new"]')
      const confirmField = page.locator('#password-confirm, input[name="password-confirm"]')
      if (await newPassField.isVisible({ timeout: 5_000 }).catch(() => false)) {
        await newPassField.fill(newPassword)
        await confirmField.fill(newPassword)
        await page.getByRole('button', { name: /gem|submit|opdater|update/i }).click()
      }
    }

    // --- Step 8: back on invitation page with ?accept=1 — invitation auto-accepted ---
    await page.waitForURL(/\/invitation\//, { timeout: 20_000 })
    await expect(page.getByText(/invitation accepteret/i)).toBeVisible({ timeout: 10_000 })

    // --- Step 9: click "Gå til mit skema" — must land on /mig/skema, NOT /dashboard ---
    const apiErrors: { url: string; status: number }[] = []
    page.on('response', (res) => {
      if (res.url().includes('/api/v1/') && res.status() === 403) {
        apiErrors.push({ url: res.url(), status: res.status() })
      }
    })

    await page.getByRole('link', { name: /gå til mit skema/i }).click()
    await expect(page).toHaveURL(/\/mig\/skema/, { timeout: 15_000 })

    // Wait for all deferred queries to settle
    await page.waitForTimeout(2_000)
    expect(
      apiErrors,
      `Got 403 errors on staff schedule page:\n${apiErrors.map((e) => e.url).join('\n')}`
    ).toHaveLength(0)
  })
})
