# Task: Ugeplan Fixes

Three improvements to the weekly plan (Ugeplan) feature.

---

## 1. Go to week — add dropdown navigation

File: `web/src/pages/WeekPlanPage.tsx` (week navigator, ~line 376–398)

Currently only prev/next week buttons exist. Add a `<select>` dropdown showing all ISO weeks for the current school year, so the user can jump directly to any week.

Implementation:
- Compute the school year from the current `isoYear`/`isoWeek` (school year = Aug–Jun). Generate a list of `{ isoYear, isoWeek, label }` objects for all weeks from week containing Aug 1 to week containing Jun 30.
- Render a `<select>` in the week navigator. Each `<option>` shows "Uge {week}" (and year if it spans two years, e.g., "Uge 1, 2026").
- `onChange`: call `setIsoWeek` + `setIsoYear` with the selected values.
- Highlight the current week in the dropdown (it will be `selected` automatically).

Place the dropdown between the prev/next buttons, replacing or supplementing the "Uge X, Y" label.

---

## 2. Single holidays (Lukkedag) shown per day in the grid

Files: `web/src/pages/WeekPlanPage.tsx`, `api/Skoleoverblikket.Api/Controllers/WeekPlanController.cs`

### The bug

Single-day closures (e.g., Kristi Himmelfartsdag on a Thursday) are not shown in the weekly plan grid. The current logic only checks whether the **full week** is a holiday (`IsFullWeekCovered`). If only one day is a Lukkedag, the grid looks normal with no indication.

### Fix

**Backend** (`WeekPlanController.cs`, GET endpoint):

Add a `HolidayDays` field to `WeekPlanDto`:
```csharp
public record WeekPlanDto(
    // ... existing fields ...
    bool IsHolidayWeek,
    string? HolidayTitle,
    IReadOnlyList<HolidayDayDto> HolidayDays,  // NEW
    // ...
);

public record HolidayDayDto(int Weekday, string Title); // Weekday: 1=Mon, 5=Fri
```

After fetching `holidays` (the CalendarEntries overlapping the week), compute which specific weekdays are covered:
```csharp
var holidayDays = new List<HolidayDayDto>();
for (int d = 0; d < 5; d++)
{
    var date = weekStart.AddDays(d);
    var covering = holidays.FirstOrDefault(h => h.StartDate <= date && h.EndDate >= date);
    if (covering is not null)
        holidayDays.Add(new HolidayDayDto(d + 1, covering.Title));
}
```

**Frontend** (`WeekPlanPage.tsx`):

In the day column headers, check if `weekPlan.holidayDays` contains an entry for that weekday. If so, show a small colored badge or label below the date (e.g., `text-xs text-amber-600 font-medium`). The cells for that column should also be visually indicated (e.g., subtle amber background or pointer-events-none).

---

## 3. Ugeplan grid — verify breaks are displayed

File: `web/src/pages/WeekPlanPage.tsx`

The user reports "doesn't reflect actual schema with breaks." Investigate:
- Check if `SchemaSlot`s for time slots marked as type "Pause" (break) are included in the GET response.
- In `WeekPlanController.cs` (line ~96–101), `db.SchemaSlots` includes ALL slots including breaks — but breaks may not have `Course` set, which could cause them to be skipped.

Fix: if break time slots exist in the schema but are not shown in the ugeplan grid:
- Filter schema slots in the GET: only include slots where `ss.Course != null` (breaks have no course assignment).
- Add a visual "Pause" row in the grid for time slots that are breaks (no course). Render them as a grey separator row with the time label only — not editable.

This may require reading the `TimeSlot` model to see if there's an `IsPause` or type field.

---

## Verification

- In Ugeplan, the week navigator shows a dropdown with all weeks of the school year. Selecting a week navigates to it.
- On a week containing Kristi Himmelfartsdag (Thursday): the Thursday column header shows "Kristi Himmelfartsdag" label; Thursday cells are visually marked as closed.
- If the active schema has break time slots, they appear as grey separator rows in the grid.
