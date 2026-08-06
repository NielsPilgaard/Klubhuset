import { test, expect } from '@playwright/test'

// Simulates pasting tab-separated rows (as copied from Excel/Google Sheets)
// into the grid, starting at a given cell.
async function pasteIntoGrid(
  page: import('@playwright/test').Page,
  testId: string,
  text: string
) {
  await page.evaluate(
    ({ testId, text }) => {
      const el = document.querySelector<HTMLInputElement>(`[data-testid="${testId}"]`)
      if (!el) throw new Error(`cell ${testId} not found`)
      el.focus()
      const dataTransfer = new DataTransfer()
      dataTransfer.setData('text/plain', text)
      const event = new ClipboardEvent('paste', {
        bubbles: true,
        cancelable: true,
        clipboardData: dataTransfer,
      })
      el.dispatchEvent(event)
    },
    { testId, text }
  )
}

test.describe('ImportPage — spreadsheet paste import', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/import')
    await expect(page.getByRole('heading', { name: 'Importer data' })).toBeVisible({
      timeout: 15_000,
    })
  })

  test('classes tab: pasting multi-row spreadsheet data creates classes', async ({ page }) => {
    await page.getByRole('button', { name: 'Klasser (valgfri)' }).click()

    const stamp = Date.now()
    const className1 = `E2E-${stamp}A`
    const className2 = `E2E-${stamp}B`

    // Two tab-separated rows pasted starting at the first "name" cell —
    // mirrors copying a two-column block out of Excel.
    await pasteIntoGrid(
      page,
      'paste-grid-0-name',
      `${className1}\t3\nElevrig klasse\n${className2}\t4\tAnden klasse`
    )

    await expect(page.getByTestId('paste-grid-0-name')).toHaveValue(className1)
    await expect(page.getByTestId('paste-grid-0-gradeLevel')).toHaveValue('3')
    await expect(page.getByTestId('paste-grid-1-name')).toHaveValue('Elevrig klasse')
    await expect(page.getByTestId('paste-grid-2-name')).toHaveValue(className2)

    await expect(page.getByText(/klar til import/)).toBeVisible()

    await page.getByRole('button', { name: 'Importér' }).click()

    await expect(page.getByText('Import fuldført')).toBeVisible({ timeout: 10_000 })
    await expect(page.getByText(/klasse\(r\) oprettet/)).toBeVisible()
  })

  test('classes tab: required column missing shows inline error state', async ({ page }) => {
    await page.getByRole('button', { name: 'Klasser (valgfri)' }).click()

    await page.getByTestId('paste-grid-0-description').fill('Kun beskrivelse, intet navn')

    const nameCell = page.getByTestId('paste-grid-0-name')
    await expect(nameCell).toHaveAttribute('aria-invalid', 'true')

    // Import button stays disabled — no valid rows (name is required).
    await expect(page.getByRole('button', { name: 'Importér' })).toBeDisabled()
  })

  test('students tab: manual row entry enables import and shows summary', async ({ page }) => {
    const stamp = Date.now()
    const studentName = `E2E Elev ${stamp}`

    await page.getByTestId('paste-grid-0-className').fill(`E2E-${stamp}`)
    await page.getByTestId('paste-grid-0-studentName').fill(studentName)

    await expect(page.getByText(/elev\(er\) klar til import/)).toBeVisible()
    await page.getByRole('button', { name: 'Importér' }).click()

    await expect(page.getByText('Import fuldført')).toBeVisible({ timeout: 10_000 })
    await expect(page.getByText(/elev\(er\) oprettet/)).toBeVisible()
  })
})
