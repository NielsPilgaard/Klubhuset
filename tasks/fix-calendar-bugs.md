# Task: Fix Calendar Bugs

Fix several bugs and missing features in the school calendar feature.

---

## 1. Missing standard holidays

File: wherever standard holidays are seeded/generated (search for `Vinterferie`, `Juleferie`, or the seed logic).

- **Vinterferie (week 42)** is missing from the standard holiday list. Add it.
- **Juleferie (Christmas)** is missing. Add it.

Typical Danish school year coverage for standard holidays:
- Efterårsferie: week 42 (Vinterferie/Efterårsferie)
- Juleferie: ~23 Dec – ~2 Jan
- Vinterferie: week 7 or 8 (varies by region)
- Påskeferie: computed from Easter (already present per the TODO)
- Sommerferie: late June to mid-August

---

## 2. School year date range

The calendar must match the Danish school year: **starts in August, ends in June (sometimes July)**. Verify that:
- The calendar view defaults to showing August–June.
- The calendar does not show months outside the school year as primary navigation.
- Any "create school year" or "initialize calendar" logic uses August start / June end.

---

## 3. Saturdays/Sundays included in standard vacation ranges

When inserting standard vacation `CalendarEntry` records, dates must be constrained to **Monday–Friday only** (school days). Saturdays and Sundays must not be included.

Find the code that generates/inserts standard vacation date ranges. When computing `StartDate` and `EndDate` for a vacation period, either:
- Store the full calendar range but skip weekends when rendering, **or** (preferred)
- Clip the stored `StartDate`/`EndDate` to Monday–Friday bounds.

Reproduce: "2 January 2027 is a Saturday" — it currently appears as a vacation day. After the fix it must not.

---

## 4. Clicking a date shows what's on that date

When a user clicks a date cell in the calendar view, show a tooltip or popover that displays:
- Events/entries on that date (title, type badge)
- An "Tilføj begivenhed" action (see bug 5 below)

The existing list view below the calendar is acceptable to keep, but the click-to-show interaction is what makes the calendar usable for Hanne.

---

## 5. "Tilføj begivenhed" accessible from date click, not standalone button

Remove (or demote) the standalone "Add event" button. Instead, clicking a date opens the event creation form/modal with the date pre-filled. This is the standard calendar UX pattern and matches user expectations.

---

## Constraints

- All `CalendarEntry` queries must include `TenantId` scoping via the global query filter — never bypass it.
- Do not edit existing EF Core migration files. If a schema change is needed, generate a new migration with `scripts/add-migration.ps1 <Name>`.
- Use `ProblemDetails` for all API errors.
