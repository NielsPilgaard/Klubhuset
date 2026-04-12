# Task: ClassesPage UI Cleanup

Clean up the class card (the expandable row in the class list) to reduce visual clutter.

---

## Current state

File: `web/src/pages/ClassesPage.tsx` (~line 461–518)

The class card header row shows:
- Expand/collapse chevron icon
- Class name (clickable)
- Description (truncated)
- "Ugeplan" button (brand-colored, right side)
- Edit pencil icon button
- Delete trash icon button

This is too many actions in one row, especially for Hanne using it on a phone.

---

## Fix

**Goal**: make the class row clean. Primary action = expand/collapse. Secondary actions = available but not visually dominant.

Recommended approach:
1. Remove the standalone "Ugeplan" button from the class card header row. Instead, add "Ugeplan" as a link in the **expanded** schema section (next to each schema's actions, or as a header link). The ugeplan is schema-specific anyway — it makes more sense there.
2. Move the edit (pencil) and delete (trash) icons into a `...` overflow menu (a simple popover or dropdown) triggered by a single `⋯` button on the right. This collapses 2–3 buttons into 1.
3. Keep the class name as the expand/collapse trigger. Make the entire header row clickable for expand/collapse.

The result: class header = just the class name + chevron + one `⋯` menu button.

---

## Constraints

- Do not remove any functionality — edit, delete, and ugeplan must still be accessible.
- Keep `data-testid` attributes intact for Playwright tests.
- The "Ugeplan" link moved to the expanded section should navigate to `/klasser/{classId}/ugeplan`.

---

## Verification

- Class list rows are visually clean with just the name and a `...` button.
- Clicking `...` opens a small menu with Edit and Delete options.
- Expanding a class shows schemas with a Ugeplan link per class.
- All existing functionality (edit modal, delete confirmation, schema navigation) still works.
