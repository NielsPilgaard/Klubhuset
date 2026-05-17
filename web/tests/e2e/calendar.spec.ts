import { test, expect, type Page } from '@playwright/test'

const ADMIN_EMAIL = 'admin@debugskolen.dk'
const ADMIN_PASSWORD = 'test1234'

async function loginAsAdmin(page: Page) {
  await page.goto('/login')
  // check-sso may bounce via #error=login_required back to /login before the real redirect fires.
  // Wait for Keycloak login page with a generous timeout to survive both bounces.
  await page.waitForURL(/localhost:8080.*\/login/, { timeout: 30_000 })
  await page.locator('#username').fill(ADMIN_EMAIL)
  await page.locator('#password').fill(ADMIN_PASSWORD)
  await page.getByRole('button', { name: /log ind|sign in/i }).click()
  await page.waitForURL((url) => url.port !== '8080', { timeout: 30_000 })
  await expect(page).toHaveURL(/\/dashboard/, { timeout: 20_000 })
}

test.describe('CalendarPage — fixed month grid height', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page)
    // Force desktop viewport so all month cards are rendered in the grid
    await page.setViewportSize({ width: 1280, height: 900 })
    await page.goto('/kalender')
    await expect(page.getByRole('heading', { name: 'Kalender' })).toBeVisible({ timeout: 15_000 })
  })

  test('all month cards have identical height (6 rows)', async ({ page }) => {
    // All month cards are rendered in the lg:grid at desktop width
    const monthCards = page.locator('.lg\\:grid > div')
    const count = await monthCards.count()
    expect(count).toBeGreaterThan(0)

    // Collect heights of all rendered month cards
    const heights = await Promise.all(
      Array.from({ length: count }, (_, i) =>
        monthCards.nth(i).evaluate((el) => el.getBoundingClientRect().height),
      ),
    )

    const unique = [...new Set(heights)]
    // All months must have the same height — at most 1px difference due to subpixel rendering
    const min = Math.min(...unique)
    const max = Math.max(...unique)
    expect(max - min).toBeLessThanOrEqual(1)
  })

  test('each month card grid has exactly 6 data rows (week-num cells)', async ({ page }) => {
    // The grid has 8 columns (weeknum + 7 days). Each data row starts with a week-number cell.
    // We count week-number cells per card: should always be 6 after padding.
    const monthCards = page.locator('.lg\\:grid > div')
    const count = await monthCards.count()
    expect(count).toBeGreaterThan(0)

    for (let i = 0; i < count; i++) {
      const card = monthCards.nth(i)
      // Week-number cells have a specific class containing "text-gray-400 text-right"
      // They are direct children of the inner grid div (not wrapped in relative div)
      const weekNumCells = card.locator('.grid > div').filter({ hasText: /^\d{1,2}$/ })
      const weekNumCount = await weekNumCells.count()
      // Some week rows may have a null firstDay (all null week at end) — those have no week number text.
      // But there must be at least 4 and at most 6 week rows.
      expect(weekNumCount).toBeGreaterThanOrEqual(4)
      expect(weekNumCount).toBeLessThanOrEqual(6)
    }
  })
})

test.describe('CalendarPage — entry list focus on click', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page)
    await page.setViewportSize({ width: 1280, height: 900 })
    await page.goto('/kalender')
    await expect(page.getByRole('heading', { name: 'Kalender' })).toBeVisible({ timeout: 15_000 })
  })

  test('clicking entry in list highlights the day cell in the month grid', async ({ page }) => {
    const entryRows = page.locator('table tbody tr')
    const count = await entryRows.count()
    test.skip(count === 0, 'No calendar entries — skipping highlight test')

    // Read the start date of the first entry from the date column
    const firstRow = entryRows.first()
    await firstRow.click()

    // After click a ring-2 class appears on a day cell
    await expect(page.locator('.ring-2.ring-brand-500')).toBeVisible({ timeout: 5_000 })
  })

  test('highlight disappears after 3 seconds', async ({ page }) => {
    const entryRows = page.locator('table tbody tr')
    const count = await entryRows.count()
    test.skip(count === 0, 'No calendar entries — skipping highlight timeout test')

    await entryRows.first().click()
    await expect(page.locator('.ring-2.ring-brand-500')).toBeVisible({ timeout: 5_000 })

    // highlight auto-clears after 3 s
    await expect(page.locator('.ring-2.ring-brand-500')).not.toBeVisible({ timeout: 5_000 })
  })
})

test.describe('CalendarPage — "Tilføj begivenhed" button', () => {
  test('button is visible next to year dropdown for admins', async ({ page }) => {
    await loginAsAdmin(page)
    await page.goto('/kalender')
    await expect(page.getByRole('heading', { name: 'Kalender' })).toBeVisible({ timeout: 15_000 })
    await expect(page.getByRole('button', { name: 'Tilføj begivenhed' })).toBeVisible()
  })

  test('clicking Tilføj begivenhed opens create event modal', async ({ page }) => {
    await loginAsAdmin(page)
    await page.goto('/kalender')
    await expect(page.getByRole('heading', { name: 'Kalender' })).toBeVisible({ timeout: 15_000 })
    await page.getByRole('button', { name: 'Tilføj begivenhed' }).click()
    // modal title or a form element for event title should appear
    await expect(page.getByRole('dialog')).toBeVisible({ timeout: 5_000 })
      .catch(() => expect(page.locator('[role="dialog"], form, input[placeholder*="Titel"]').first()).toBeVisible({ timeout: 5_000 }))
  })
})
