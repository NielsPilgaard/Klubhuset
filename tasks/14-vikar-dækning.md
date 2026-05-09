# Vikar-dækning — Substitute Coverage

## Problem

When a teacher or aide is absent, someone needs to cover their lessons. Today the school handles this outside the system (phone calls, paper lists). We can surface who is free during the affected time slot and make it easy to assign a substitute — so Hanne doesn't have to keep a mental map of everyone's schedule.

## What we're NOT building

- Absence/sickness registration system (HR scope creep)
- Automated SMS/push notifications to substitutes
- Substitute-pool management (dedicated vikar ansatte is already a Staff role)

## Core concept

"Who is free on Tuesday at 10:00 in week 14?" — system answers this by looking at all SchemaSlots for that week and returning staff not assigned to any conflicting slot.

A **coverage assignment** is a temporary reassignment of a SchemaSlot for a specific ISO week: replace the normal teacher/aide with a substitute (or any available staff member) for that week only. This maps cleanly onto the existing `WeekPlanSlot` — it just needs a `SubstituteTeacherId` and `SubstituteAideId` field to record who actually covers this week.

## Data model changes

### Extend `WeekPlanSlot`

```csharp
public Guid? SubstituteTeacherId { get; set; }  // FK → Staff
public Guid? SubstituteAideId { get; set; }      // FK → Staff
```

- Non-null = that person covers this lesson this week instead of the schema-assigned teacher/aide
- UI shows the substitute name (with a visual indicator) in the week view for that slot
- Substitute name shown to staff viewing their own schedule

EF config: same `DeleteBehavior.Restrict` as TeacherId/AideId on SchemaSlot.

Migration: `AddSubstituteStaffToWeekPlanSlot`

## API

### `GET /api/v1/staff/available`

Query params: `isoYear`, `isoWeek`, `weekday` (1–5), `timeSlotId`

Returns list of Staff (id, name, role) not assigned to any SchemaSlot on that weekday+timeSlot in that week. Includes both schema-assigned staff and any existing substitute assignments for that week.

### `PUT /api/v1/week-plans/{weekPlanId}/slots/{slotId}/substitute`

Request body:

```json
{
  "substituteTeacherId": "<guid or null>",
  "substituteAideId": "<guid or null>"
}
```

- Clears substitute when both fields are null (= normal coverage restored)
- Creates `WeekPlanSlot` record if it doesn't exist yet (same lazy-creation logic as existing slot endpoints)
- Returns `200 OK` with updated slot

**Validation:**
- Staff IDs must belong to tenant
- Cannot assign same person as both teacher and aide in same slot

## Frontend

### Week view changes

In the schema week view (the per-class weekly calendar):

- Slots where a substitute is assigned show the substitute's name with a small "V" badge (vikar indicator) instead of the normal teacher name
- Tapping/clicking a slot opens a slot detail panel (already exists or needs building) with a **"Tildel vikar"** button

### "Tildel vikar" flow

1. Hanne clicks **Tildel vikar** on a slot
2. Slide-over panel opens: "Hvem dækker [Matematik, tirsdag 10:00]?"
3. System shows two sections:
   - **Ledige nu** — staff free at this exact time (from `/api/v1/staff/available`)
   - **Alle medarbejdere** — full list, with conflict indicator if they're busy
4. Hanne taps a name → PUT substitute → panel closes → slot updates in-place with "V" badge

UX rules:
- Available staff shown first, sorted by name
- Busy staff shown grayed out with their conflict ("Optaget: 2.a - Dansk")
- Clearing a substitute: same panel, shows current substitute with a "Fjern vikar" button

### Staff self-view

Staff member viewing their own schedule sees substitute assignments in their schedule: if they are the assigned substitute for a slot this week, it appears in their view with a "(Vikar)" label.

## Conflict detection

Extend existing conflict detection logic to include substitute assignments:
- If staff X is assigned as substitute in slot A, they count as "busy" for that slot when checking availability for slot B at the same time.

## Test coverage

- API integration test: `/staff/available` returns only free staff for a given slot
- API integration test: assigning a substitute, then checking availability shows them as busy
- API integration test: substitute on WeekPlanSlot visible in staff schedule endpoint
- Playwright e2e: Hanne assigns a vikar to a slot, sees "V" badge, clears it again

## Out of scope

- Absence tracking / sick-day registration
- Recurring substitute assignments (cross-week)
- Substitute pay/hours tracking
- Notification to substitute (email/SMS)
