# Task: Schema Start/End Dates (Replace Active/Draft)

Replace the current `SchemaStatus` enum + `IsActive` boolean with a `StartDate`/`EndDate` date range. A schema is considered "active" when today's date falls within its range. No manual activation needed.

---

## Why

The current active/draft model is confusing — users don't see a clear benefit. Date ranges are self-explanatory: a schema for "August 2025 – June 2026" is obviously the current one.

---

## Backend Changes

### 1. Update `Schema.cs`

File: `api/Skoleoverblikket.Api/Models/Schema.cs`

- Remove: `SchemaStatus Status` and `bool IsActive`
- Add: `DateOnly? StartDate` and `DateOnly? EndDate` (nullable for backwards compat with existing schemas)
- Remove: `SchemaStatus` enum entirely

### 2. Generate migration

Run: `scripts/add-migration.ps1 AddSchemaDateRange`

The migration should:
- Add `StartDate` (date, nullable) and `EndDate` (date, nullable) columns to `Schemas`
- Drop the `Status` column
- Drop the `IsActive` column

### 3. Update `SchemasController.cs`

File: `api/Skoleoverblikket.Api/Controllers/SchemasController.cs`

- Remove the `Activate()` endpoint (or repurpose as `SetDateRange`)
- Remove the `Complete()` endpoint (mark as done / change status)
- Add: `PUT /classes/{classId}/schemas/{schemaId}/daterange` — accepts `{ startDate, endDate }`, validates start ≤ end, saves to DB
- Update schema DTOs: replace `Status`/`IsActive` fields with `StartDate`/`EndDate`
- Anywhere that queries `s.IsActive == true` → replace with `s.StartDate <= today && s.EndDate >= today` (or `StartDate == null` means not yet scheduled)

### 4. Update `StatsController.cs`

File: `api/Skoleoverblikket.Api/Controllers/StatsController.cs`

- Replace `s.IsActive` filter with date-range logic: `s.StartDate <= today && (s.EndDate == null || s.EndDate >= today)`

### 5. Update `WeekPlanController.cs`

File: `api/Skoleoverblikket.Api/Controllers/WeekPlanController.cs`

- Replace `s.IsActive` filter with the same date-range logic as above

### 6. Update `PrintController.cs` (if it exists)

Search for any other `.IsActive` or `SchemaStatus` references — fix them all.

---

## Frontend Changes

### 7. Update `ClassesPage.tsx`

File: `web/src/pages/ClassesPage.tsx`

- Remove "Færdig"/"Kladde" status badges
- Remove "Aktiv" badge
- Remove "Aktivér" button per schema
- Add: show date range if set (e.g., "aug 2025 – jun 2026") in muted text next to schema name
- Add: a date range picker or two date inputs on the schema row (or in a modal) that calls the new `PUT daterange` endpoint
- "Current" indicator: if today is within the date range, show a subtle "Aktiv nu" badge (green dot or text)

### 8. Update `SchemaBuilderPage.tsx`

File: `web/src/pages/SchemaBuilderPage.tsx`

- Remove "Markér som færdig" button
- Remove "Aktivér" button
- Remove status/active badges from the header
- Add: date range display in the header (editable inline or via a modal)

---

## Constraints

- Do not modify existing migration files — only add new ones
- Nullable StartDate/EndDate means "no date set yet" — treat as inactive
- No schema is active if today is outside its range (strict boundary check)

---

## Verification

- Create a schema, set a date range covering today → it appears as "Aktiv nu"
- Set a date range in the past → not active
- WeekPlan still loads for the currently-active (date-range-active) schema
- Dashboard shows correct classes with missing schemas
