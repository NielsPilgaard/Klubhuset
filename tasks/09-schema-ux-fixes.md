# Task: Schema UX Fixes

Small UX fixes to the schema builder and class list. All changes are frontend-only.

---

## 1. Module form — submit on Enter

File: `web/src/pages/SchemaBuilderPage.tsx` (AssignmentPanel, ~line 169–324)

The form wrapping the assignment panel should submit when the user presses Enter **and** no dropdown is currently open. The SearchableSelect component already handles Enter to select an option when the dropdown is open. The fix is: when the dropdown is closed and the user presses Enter on a focused field, call `handleSubmit()`.

Check how the form's `onSubmit` is wired. If it's a `<form onSubmit={...}>`, confirm that all `<input>` elements inside are type="text" (not type="submit"), which would already trigger `onSubmit` on Enter in most browsers. If the form already works this way, add a hidden `<button type="submit" />` inside the form as a fallback.

---

## 2. Drag-and-drop — first drop appears duplicated

File: `web/src/pages/SchemaBuilderPage.tsx` (handleDragEnd, ~line 580–638)

Reproduce: drag a module to an empty cell for the first time. The module appears in both the original and the destination cell. Moving it again removes the original.

The bug is likely caused by optimistic local state update + the DragOverlay rendering the card while the mutation is in flight, making it look like two copies exist. Or: the `activeDragId` is not cleared immediately after drop.

Fix: ensure `setActiveDragId(null)` is called at the **top** of `handleDragEnd` before any async logic. Also check that the local slot map update (`setSlots` or equivalent) correctly removes the source entry — do not wait for the mutation to succeed before removing the ghost.

---

## 3. Dropdowns — always show all options

File: `web/src/pages/SchemaBuilderPage.tsx` (SearchableSelect component, ~line 85–164)

Current behavior: the selected value is stored as the input text, so if "Biologi" is in the field, typing must clear it to see other options.

Fix:
- Separate the display state from the query state. When a value is selected, show it as a **chip/tag** (small pill with × to clear), and clear the input so the full list is shown again.
- When the input is focused (even with a chip selected), show the full dropdown.
- On chip ×: clear selection and show full list.

This is the highest-priority UX fix — it makes adding modules much smoother for Hanne.

---

## 4. Click schema card to navigate — remove "Rediger" button

File: `web/src/pages/ClassesPage.tsx` (~line 308–324)

Currently the schema list shows a schema name (clickable) + a separate "Rediger" button (also navigates to the same route). Remove the "Rediger" button. Make the entire schema card row clickable (cursor-pointer, hover highlight). The schema name alone navigating is sufficient.

Keep the copy icon button and "Aktivér" button — only remove the redundant "Rediger" text button.

---

## Verification

- Open schema builder, add a module: press Tab to move through fields, then Enter on the last field → form should submit.
- Drag a module to an empty cell on first use → no duplication visible.
- Click a course dropdown with "Biologi" selected → full list visible immediately without clearing.
- In ClassesPage, schema rows have no "Rediger" button; clicking the row navigates to the schema builder.
