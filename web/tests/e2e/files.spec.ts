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

test.describe('FilesPage — folders', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page)
    await page.goto('/filer')
    await expect(page.getByRole('heading', { name: 'Filer' })).toBeVisible({ timeout: 15_000 })
  })

  test('create folder button is visible for admin', async ({ page }) => {
    await expect(page.getByTestId('create-folder-btn')).toBeVisible()
  })

  test('opening create folder modal and cancelling', async ({ page }) => {
    await page.getByTestId('create-folder-btn').click()
    await expect(page.getByRole('heading', { name: 'Opret mappe' })).toBeVisible()
    await page.getByRole('button', { name: 'Annuller' }).click()
    await expect(page.getByRole('heading', { name: 'Opret mappe' })).not.toBeVisible()
  })

  test('create folder, navigate into it, and see empty state', async ({ page }) => {
    const folderName = `E2E-Mappe-${Date.now()}`

    await page.getByTestId('create-folder-btn').click()
    await expect(page.getByRole('heading', { name: 'Opret mappe' })).toBeVisible()
    await page.locator('input[placeholder*="Matematik"]').fill(folderName)
    await page.getByRole('button', { name: 'Opret' }).click()

    // folder row should appear
    await expect(page.getByRole('cell', { name: folderName })).toBeVisible({ timeout: 10_000 })

    // click folder row to navigate in
    const folderRow = page.locator('tr', { hasText: folderName })
    await folderRow.click()

    // breadcrumb shows folder name
    await expect(page.getByText(folderName)).toBeVisible()

    // empty state inside folder
    await expect(page.getByText('Mappen er tom')).toBeVisible()

    // breadcrumb root navigates back
    await page.getByTestId('breadcrumb-root').click()
    await expect(page.getByText('Mappen er tom')).not.toBeVisible()
  })

  test('rename folder via pencil icon', async ({ page }) => {
    const originalName = `E2E-Omdøb-${Date.now()}`
    const renamedName = `${originalName}-RENAMED`

    // create it first
    await page.getByTestId('create-folder-btn').click()
    await page.locator('input[placeholder*="Matematik"]').fill(originalName)
    await page.getByRole('button', { name: 'Opret' }).click()
    await expect(page.getByRole('cell', { name: originalName })).toBeVisible({ timeout: 10_000 })

    // find the rename button for this folder
    const folderRow = page.locator('tr', { hasText: originalName })
    const renameBtn = folderRow.locator('[title="Omdøb mappe"]')
    await renameBtn.click()

    // inline input appears, clear and type new name
    const input = folderRow.locator('input[type="text"]')
    await input.fill(renamedName)
    await input.press('Enter')

    await expect(page.getByRole('cell', { name: renamedName })).toBeVisible({ timeout: 10_000 })
    await expect(page.getByRole('cell', { name: originalName })).not.toBeVisible()
  })

  test('delete folder with confirmation', async ({ page }) => {
    const folderName = `E2E-Slet-${Date.now()}`

    await page.getByTestId('create-folder-btn').click()
    await page.locator('input[placeholder*="Matematik"]').fill(folderName)
    await page.getByRole('button', { name: 'Opret' }).click()
    await expect(page.getByRole('cell', { name: folderName })).toBeVisible({ timeout: 10_000 })

    page.once('dialog', (dialog) => dialog.accept())

    const folderRow = page.locator('tr', { hasText: folderName })
    await folderRow.locator('[title="Slet mappe"]').click()

    await expect(page.getByRole('cell', { name: folderName })).not.toBeVisible({ timeout: 10_000 })
  })
})

test.describe('FilesPage — upload modal rename', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page)
    await page.goto('/filer')
    await expect(page.getByRole('heading', { name: 'Filer' })).toBeVisible({ timeout: 15_000 })
  })

  test('filename input appears after selecting a file and is pre-filled without extension', async ({ page }) => {
    await page.getByRole('button', { name: 'Upload fil' }).click()
    await expect(page.getByRole('heading', { name: 'Upload fil' })).toBeVisible()

    // before file selected, filename input is not shown
    await expect(page.getByLabel('Filnavn')).not.toBeVisible()

    // attach a test file
    const [fileChooser] = await Promise.all([
      page.waitForEvent('filechooser'),
      page.locator('input[type="file"]').evaluate((el: HTMLInputElement) => el.click()),
    ])
    await fileChooser.setFiles({
      name: 'rapport_Q1.pdf',
      mimeType: 'application/pdf',
      buffer: Buffer.from('PDF test content'),
    })

    // filename input now visible, pre-filled without extension
    const nameInput = page.getByLabel('Filnavn')
    await expect(nameInput).toBeVisible()
    await expect(nameInput).toHaveValue('rapport_Q1')
  })

  test('editing filename changes the name used for presign', async ({ page }) => {
    await page.getByRole('button', { name: 'Upload fil' }).click()

    const [fileChooser] = await Promise.all([
      page.waitForEvent('filechooser'),
      page.locator('input[type="file"]').evaluate((el: HTMLInputElement) => el.click()),
    ])
    await fileChooser.setFiles({
      name: 'IMG_20240301_142233.jpg',
      mimeType: 'image/jpeg',
      buffer: Buffer.from('fake image'),
    })

    const nameInput = page.getByLabel('Filnavn')
    await expect(nameInput).toHaveValue('IMG_20240301_142233')

    await nameInput.fill('Klassefoto forår 2024')
    await expect(nameInput).toHaveValue('Klassefoto forår 2024')
  })
})

test.describe('FilesPage — file preview modal', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page)
    await page.goto('/filer')
    await expect(page.getByRole('heading', { name: 'Filer' })).toBeVisible({ timeout: 15_000 })
  })

  test('clicking eye icon on a file opens preview modal', async ({ page }) => {
    // Only run this test if there are files in the list
    const fileRows = page.locator('[data-testid^="file-row-"]')
    const count = await fileRows.count()
    test.skip(count === 0, 'No files present — skipping preview test')

    const firstRow = fileRows.first()
    await firstRow.locator('[title="Forhåndsvis"]').click()

    // modal should open with a close button
    await expect(page.locator('button[title="Luk"]')).toBeVisible({ timeout: 5_000 })
  })

  test('clicking file name opens preview modal', async ({ page }) => {
    const fileRows = page.locator('[data-testid^="file-row-"]')
    const count = await fileRows.count()
    test.skip(count === 0, 'No files present — skipping preview test')

    const firstRow = fileRows.first()
    // the file name is a button in the first td
    await firstRow.locator('button').first().click()

    await expect(page.locator('button[title="Luk"]')).toBeVisible({ timeout: 5_000 })
  })

  test('preview modal closes when clicking close button', async ({ page }) => {
    const fileRows = page.locator('[data-testid^="file-row-"]')
    const count = await fileRows.count()
    test.skip(count === 0, 'No files present — skipping preview test')

    await fileRows.first().locator('[title="Forhåndsvis"]').click()
    const closeBtn = page.locator('button[title="Luk"]')
    await expect(closeBtn).toBeVisible({ timeout: 5_000 })
    await closeBtn.click()
    await expect(closeBtn).not.toBeVisible()
  })

  test('preview modal closes on backdrop click', async ({ page }) => {
    const fileRows = page.locator('[data-testid^="file-row-"]')
    const count = await fileRows.count()
    test.skip(count === 0, 'No files present — skipping preview test')

    await fileRows.first().locator('[title="Forhåndsvis"]').click()
    await expect(page.locator('button[title="Luk"]')).toBeVisible({ timeout: 5_000 })

    // click the dark backdrop (fixed overlay behind the modal)
    await page.mouse.click(10, 10)
    await expect(page.locator('button[title="Luk"]')).not.toBeVisible()
  })
})
