# Task: Kalender — fast størrelse pr. måned

## Problem
Each month's mini-calendar grid in `CalendarPage` has a variable number of rows (4–6 weeks). When navigating between months or scrolling past months with different row counts, the page layout shifts — content below the calendar jumps up or down.

## Root cause
`buildMonthGrid()` returns only the weeks that exist in that month. A month starting on Saturday with 31 days spans 6 rows; February in a non-leap year can fit in 4. The grid renders with `grid-rows-auto` so height varies.

## Fix
Give each month's week grid a fixed height equivalent to 6 rows. Empty rows render as blank cells.

Approach:
- In `buildMonthGrid`, always return exactly 6 rows (pad with `null`-only rows at the end if fewer than 6)
- Each day cell has a fixed height (e.g. `h-8` or `h-9`) so the total is always `6 × cell-height`

## Files affected
- `web/src/pages/CalendarPage.tsx`
  - `buildMonthGrid()` function (~line 73): pad to always return 6 rows
  - Month grid render: ensure day cells have explicit fixed height
