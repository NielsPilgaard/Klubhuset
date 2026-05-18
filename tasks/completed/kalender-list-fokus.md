# Task: Kalender — klik på liste-begivenhed fokuserer i kalender

## Problem
When a user clicks an event in a list (sidebar or entry list), the calendar grid does not react — it stays wherever it is scrolled and nothing is highlighted.

## Current state
`CalendarPage` has a full school-year grid of monthly mini-calendars. There is no separate list/sidebar component visible in the current code. Clarify before implementing:
- Is there a planned list view (e.g. a right-side panel showing all entries)?
- Or does the existing entry list (inside `DayPopover`) need cross-referencing to the grid?

## Desired behavior (once clarified)
1. User clicks an event in the list
2. Calendar scrolls to the correct month's mini-calendar
3. The day cell for that event gets a highlight ring (e.g. `ring-2 ring-brand-500`)
4. Optionally: `DayPopover` opens automatically for that date

## Implementation notes (tentative)
- Add `highlightedDate: string | null` state to `CalendarPage`
- Scroll target: use `useRef` on each month section + `scrollIntoView({ behavior: 'smooth' })`
- Highlight: pass `highlightedDate` down to the day cell render; add conditional ring class
- Auto-open popover: set `openPopover` state to the highlighted date after scroll

## Files affected
- `web/src/pages/CalendarPage.tsx`
- Possibly: new list/sidebar component (depends on clarification)

## Before starting
Confirm with developer: is there a list view planned or already in a branch? What triggers the "click on list event"?
