# Årsrul — Year-End Class Roll-Up

## Problem

When a school year ends, every class advances one grade. "1.a" becomes "2.a", "2.b" becomes "3.b", etc. The outgoing graduating class is archived. A new entry class is created. Without tooling this is tedious manual rename work across schemas, week plans, and rosters.

## What we're NOT building

- Automatic year inference from class name (names are free text, not structured)
- Migration of WeekPlan history to the new class entity
- Any changes to the Schema date-range system (it already handles multi-year fine)

## User flow

Admin (Hanne) opens **Indstillinger → Årsrul**. One page, one action.

1. System shows current classes in a list with a rename field pre-filled with a suggested new name (editable).
2. Hanne marks which classes to archive (graduating class gets archived, new entry class gets a blank name she fills in).
3. She clicks **Udfør årsrul**. Confirmation dialog: "Dette omdøber X klasser og arkiverer Y. Det kan ikke fortrydes."
4. Done. Classes are renamed. Archived classes are soft-deleted (hidden from schedule builder, accessible in read-only archive view).

## Data model changes

### Add `ArchivedAt` to `Class`

```csharp
public DateTimeOffset? ArchivedAt { get; set; }
```

- `null` = active
- Non-null = archived, hidden from all normal queries via global query filter extension
- EF Core global query filter: add `&& e.ArchivedAt == null` to Class filter

No new entity needed. No schema copy. Schemas keep their FK to the (now-renamed) class — historical data stays intact.

## API

### `POST /api/v1/classes/year-roll`

Request body:

```json
{
  "renames": [
    { "classId": "...", "newName": "2.a" },
    { "classId": "...", "newName": "3.b" }
  ],
  "archive": ["<classId-of-graduating-class>"],
  "create": [
    { "name": "0.a" }
  ]
}
```

Response: `204 No Content` on success, `ProblemDetails` on validation failure.

**Validation:**
- All `classId` values must belong to the tenant
- `newName` must be unique within tenant after rename (check for collisions)
- Cannot rename and archive the same class in one request
- `create` entries must have valid names (same rules as normal class creation)

**Authorization:** admin only (same guard as class create/delete endpoints).

**Transactional:** entire operation runs in one EF Core transaction. Partial success not allowed.

## Frontend

New page: **Indstillinger → Årsrul** (route: `/settings/year-roll`).

Layout:
- Table with columns: Nuværende navn | Ny navn (input) | Arkivér (checkbox)
- "+ Opret ny klasse" row at the bottom for the entry class
- "Udfør årsrul" button → confirmation dialog → POST → success toast

UX rules:
- Pre-fill new name suggestion: strip trailing digit, increment (best-effort, editable). If name doesn't end in a digit, leave blank for manual entry.
- Warn inline if two classes would get the same new name (client-side validation before submit).
- Archived classes disappear from the main nav immediately after roll.

## Archived classes view

Separate tab in Indstillinger: **Arkiverede klasser**. Read-only list. No restore (keep it simple — if needed, admin can recreate manually).

## Migrations

1. `AddArchivedAtToClass` — adds nullable `ArchivedAt` column to `Classes` table.
2. Update global query filter on `Class` to exclude archived.

## Test coverage

- API integration test: full year-roll request — renames apply, archived class hidden from list endpoint, new class appears.
- API integration test: collision detection — two classes renamed to same name returns 400.
- Playwright e2e: Hanne completes a year roll from settings page, sees renamed classes in schedule builder.

## Out of scope

- Bulk schema copy/clone to new year (schemas have date ranges — create new ones manually)
- Student roster management (not in product)
- Undo / restore archived classes
