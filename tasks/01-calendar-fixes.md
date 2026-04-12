# Task: Calendar Fixes

Four improvements to the calendar page. All closely related — same files.

---

## 1. Mark Saturday/Sunday clearly

File: `web/src/pages/CalendarPage.tsx`

Current: weekend cells use `text-gray-300 cursor-default`. They're hard to distinguish from empty padding cells.

Fix: give weekend day cells a visibly different background (e.g., `bg-gray-100`) and keep the muted text. The weekday headers (Lø, Sø) should also be visually distinct — e.g., `text-gray-400` vs `text-gray-600` for weekdays.

Look for the `di >= 5` check (around line 431) that marks weekends and apply stronger visual styling.

---

## 2. Show ISO week numbers

File: `web/src/pages/CalendarPage.tsx`

In the month grid, each week row should show its ISO week number in a leftmost column.

Implementation:
- The grid is currently `grid-cols-7`. Change to `grid-cols-[1.5rem_repeat(7,1fr)]` (or similar) to add a narrow left column.
- The weekday header row needs an empty first cell.
- For each week row (each `week` in `weeks`), compute the ISO week number from the first non-null day in that row. Use the existing `getISOWeek` helper if available, or use `date-fns` which is likely already installed.
- Display the week number in small muted text: `text-xs text-gray-400`.

---

## 3. Fix "Tilføj standardferier" for subsequent school years

Files: `web/src/pages/CalendarPage.tsx`, `api/Skoleplanen.Api/Controllers/CalendarController.cs`

### The bug

Summer vacation from school year 2025-2026 runs Jun 26 – Aug 7 of 2026. When the user switches to school year 2026-2027 in the calendar, `entriesStartYear` fetches `/calendar?year=2026` — which returns the summer vacation entry (its `EndDate.Year == 2026`). This makes `hasEntries = true`, hiding the "Tilføj standardferier" button even though no entries exist for the 2026-2027 school year.

### Fix

Change the `hasEntries` logic to check for entries that are **meaningfully within** the target school year (Aug schoolStartYear – Jul schoolEndYear), not just any entry whose year overlaps.

Option A (recommended — frontend only):
Define a helper `isEntryInSchoolYear(entry, schoolStartYear)`:
```ts
function isEntryInSchoolYear(entry: CalendarEntryDto, startYear: number): boolean {
  const schoolStart = new Date(startYear, 7, 1)   // Aug 1
  const schoolEnd = new Date(startYear + 1, 6, 31) // Jul 31 next year
  const entryStart = parseDate(entry.startDate)
  const entryEnd = parseDate(entry.endDate)
  return entryStart < schoolEnd && entryEnd > schoolStart
}
```
Then: `const hasEntries = allEntries.some(e => isEntryInSchoolYear(e, schoolStartYear))`

The "Tilføj standardferier" button is shown when `!hasEntries` — so it will appear for the new school year even if the previous summer vacation's entry overlaps into August.

### Also fix: standard vacation year parameter

When the seed button is clicked, it calls `seedMutation.mutate(defaults)` where `defaults` comes from `/calendar/defaults?year=${schoolStartYear}`. Verify that `schoolStartYear` is the year of the dropdown selection (not the current year). It should be `schoolStartYear` from state — confirm this is updated when the user changes the year dropdown.

---

## 4. Standard vacation dates — wrong year offset

File: `api/Skoleplanen.Api/Controllers/CalendarController.cs` (`ComputeDefaultHolidays`, ~line 104–154)

This was flagged as a bug: "adding standard vacations for 2026-2027 takes 1 year instead."

Check: if the user selects school year 2026-2027 in the UI dropdown and clicks "Tilføj standardferier", does the frontend pass `year=2026` to `/calendar/defaults?year=2026`?

In `ComputeDefaultHolidays(int year)`:
- `schoolStartYear = year` (2026)
- `schoolEndYear = schoolStartYear + 1` (2027)
- Sommerferie: Jun 26 – Aug 7 of **2027** ✓
- Juleferie: Dec 22 **2026** – Jan 2 **2027** ✓

This looks correct. The bug may actually be the `hasEntries` issue from fix #3, causing the wrong year's defaults to be seeded (i.e., the user is unknowingly seeding the current year, not the selected year).

Verify by logging/checking what `schoolStartYear` is passed to the seed mutation vs what the dropdown shows. If the dropdown year and the seeded year always match, the bug is only fix #3.

---

## Verification

- In the month grid, weekends (Lø/Sø) have a distinct background color.
- Each week row shows its ISO week number on the left (e.g., "42").
- Switch to school year 2026-2027 after seeding 2025-2026 → "Tilføj standardferier" button appears.
- Seed 2026-2027 → correct vacation dates (Juleferie Dec 2026, Sommerferie Jun–Aug 2027).
