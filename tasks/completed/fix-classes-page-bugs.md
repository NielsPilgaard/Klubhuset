# Task: Fix Classes Page Bugs

Fix UX bugs on the `/klasser` page and the Ugeplan view.

---

## 1. Clicking a class row should expand/collapse it

Currently the class row must be clicked somewhere specific (or via a button). The entire class row should be clickable to toggle expand/collapse.

Find the class row component in `ClassesPage.tsx` (or similar). Make the row `cursor-pointer` and attach `onClick` to the row container to toggle the expanded state.

---

## 2. Ugeplan shows GUID at the top

**Symptom**: The Ugeplan page (`/klasser/:classId/ugeplan`) shows the class `id` (a raw GUID) in the heading instead of the class name.

**Fix**: Load the class name and display it in the heading. The class data may already be in the React Query cache from the classes list. Use `useQuery(['classes'])` to find the class by `classId` param, or add a dedicated `GET /api/v1/classes/:id` endpoint if one doesn't exist. Show the class name (e.g., `3.A`) in the page title.

Check `WeekPlanPage.tsx` — this may already be partially implemented. Verify the heading reads `{className} · Ugeplan`, not the raw ID.

---

## 3. Ugeplan: one holiday day blocks entire week

**Symptom**: When a single day in the week is marked as a holiday (e.g., a `Lukkedag`), the entire week grid is greyed out and `isHolidayWeek` is `true`.

**Root cause**: The `WeekPlanController` likely uses a loose overlap check that returns `isHolidayWeek = true` for any overlap, even a single day. The intent is: mark `isHolidayWeek = true` only when the **entire Mon–Fri range** is covered by Ferie/Lukkedag entries.

**Fix in `WeekPlanController.cs`**: Change the holiday check so that `isHolidayWeek` is `true` only when the full week (Monday through Friday) is covered by holiday entries. A single-day `Lukkedag` should NOT block the whole week — it should only visually mark that one day column in the grid (future enhancement), but must not block the week.

For now: set `isHolidayWeek = false` unless the holiday covers the full Mon–Fri span. The individual day marking can be a follow-up.

---

## 4. Back navigation: "lektioner" goes all the way back to /klasser

**Symptom**: When inside `/klasser/:classId/lektioner` (time slots for a class) and clicking back, the user is taken to `/klasser` (class list) instead of back to the class detail view.

## **Fix**: Change the back button in the lektioner view to navigate to `/klasser/:classId` (or whatever the class detail route is), not to `/klasser`. Use `navigate(-1)` if the router history is reliable, or hard-code the parent route.

---

---

## Constraints

- Never show raw GUIDs to end users anywhere. GUIDs are internal identifiers only.
- `data-testid` attributes must be used for any new interactive elements that would be tested.
- Do not add CSS classes or inline styles — use Tailwind utility classes only.
