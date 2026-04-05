# Task: Calendar Sync and Repeating Events

Two new calendar features: external calendar sync and repeating events.

---

## 1. Repeating events

Allow `CalendarEntry` records to recur on a schedule (e.g. every week, every two weeks, monthly).

- Add recurrence fields to `CalendarEntry` (or a separate `CalendarEntryRecurrence` table): `RecurrenceRule` (e.g. RRULE string or enum-based), `RecurrenceEnd` (date or count).
- When rendering the calendar, expand recurring entries into individual occurrences within the visible date range — do not store each occurrence as a separate row.
- Editing a recurring event must offer: "edit this occurrence only" vs. "edit all future occurrences".
- Deleting: same split — single or all future.
- Applies to **skemaer og kalender** (schema entries and calendar events), **not** ugeplan (weekly plan).
- Do not edit existing EF Core migration files. Generate a new migration via `scripts/add-migration.ps1 <Name>`.

---

## 2. Kalender eksport (Google, Outlook, etc.)

Allow users to download their school calendar and/or schedule as an `.ics` file for one-time import into an external calendar app.

### Scope

- Targets: **skemaer** (class schedules) and **kalender** (calendar events). Excludes ugeplan.
- Any iCalendar-compatible app (Google Calendar, Outlook, Apple Calendar).

### Approach: one-time `.ics` download

Generate and download an iCalendar (`.ics`) file the user can import manually. No tokens, no subscriptions, no sync.

- `GET /api/v1/calendar/export.ics` — requires a valid JWT bearer token, returns `text/calendar`.
- Response contains calendar entries + schema entries for the authenticated user's scope, as `VEVENT` records.
- No token management, no database changes needed beyond what already exists.

### UI

- "Eksportér til kalender" button in the calendar view that triggers the download.
- Brief instructions: "Åbn filen i Google Calendar, Outlook eller Kalender (iPhone/Mac) for at importere begivenhederne."

### Constraints

- Endpoint requires a valid JWT bearer token — standard auth, no special handling.
- All database queries must apply tenant scoping via the global query filter.
- Use `ProblemDetails` for API errors.

---

## Constraints (both features)

- All `CalendarEntry` and schema queries must include `TenantId` scoping — never bypass the global query filter.
- Do not edit existing EF Core migration files.
- Use `ProblemDetails` for all API errors.
