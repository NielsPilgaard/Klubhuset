---
title: 'Parent-teacher meeting sign-up (skole-hjem-samtaler)'
purpose: 'Scope sign-up flow for parent-teacher conference slots — admin/teacher define sessions with time slots, parents book per child.'
description: >-
  New feature. Admin or teacher creates a meeting session (date + time
  range + slot duration + one location), system auto-generates slots,
  parents of students in the class book one slot per child, self-service
  cancel/rebook. Two-tier model like VacationRegistrationWindow/Entry.
status: 'Proposed'
---

# Parent-teacher meeting sign-up (skole-hjem-samtaler)

## TL;DR

Admin or teacher opens a sign-up session for a class: date, start/end time,
slot duration, one location (room or link). System generates slots.
Parents linked to a student in that class book one slot per child; can
cancel and rebook themselves. New feature end to end — no existing model,
controller, or UI. Closest structural analog: `VacationRegistrationWindow`
/ `VacationRegistrationEntry`.

## Context

No existing concept of bookable time slots anywhere in the codebase.
Closest patterns:
- `VacationRegistrationWindow`/`Entry` — admin-defined window, parent
  submits per-student entry. Reuse this window/entry shape.
- `CalendarEntry` — has `RecurrenceRule`/date range, not slot-based; not a
  fit (meetings are discrete slots, not recurring calendar events).
- `ContactThreadsController`/`ContactThread` — per-student parent↔teacher
  messaging; unrelated feature, not reused here.

Danish domain term: **skole-hjem-samtale** (school-home conference).

## Decisions from grilling

- **Creator**: both admin and teacher can create sessions. Teacher creates
  for their own class(es); admin can create for any class.
- **Scope**: per class, one child per slot (not group slots, not
  whole-school/any-staff booking).
- **Booking limit**: one slot per child — a parent with two kids in the
  same class books two separate slots.
- **Cancellation**: self-service. Parent can cancel their booking, which
  reopens the slot; parent (or another parent) can rebook.
- **Slot creation**: auto-generated from date + start time + end time +
  slot duration (e.g. 15 min) — teacher/admin does not add slots one by
  one.
- **Location**: single field per session (e.g. "Lokale 4" or a video
  link), not per slot.
- **Access rule**: any parent linked to the student can book — no
  additional consent flag (unlike `ShareContactInfo`, which gates sharing
  contact info *between* parents; this is parent↔school only).

## Proposed scope

### Data model

`MeetingSession` (the window):
- `Id`, `TenantId`, `ClassId`, `Title`
- `Date` (`DateOnly`), `StartTime`/`EndTime` (`TimeOnly`), `SlotDurationMinutes`
- `Location` (string, nullable)
- `CreatedByStaffId`
- `CreatedAt`

`MeetingSlot` (generated on session create, one row per slot):
- `Id`, `TenantId`, `SessionId` (FK), `StartTime`/`EndTime` (`TimeOnly`)
- `BookedByParentId` (nullable Guid, FK to `Parent`)
- `StudentId` (nullable Guid, FK to `Student`) — set on booking
- `BookedAt` (nullable)

Slot generation happens server-side on session create: iterate
`StartTime`→`EndTime` in `SlotDurationMinutes` steps, insert one
`MeetingSlot` row per step. Booking is an update on the slot row (claim
`BookedByParentId`+`StudentId`), guarded by a unique/conditional constraint
so two parents can't win the same slot in a race — mirrors the unique
index pattern on `VacationRegistrationEntry` (`TenantId, WindowId,
StudentId`), here needs `TenantId, SlotId` uniqueness on the booked state
(e.g. partial unique index on `SlotId` where `BookedByParentId is not
null`, or optimistic concurrency token).

New EF Core migration via `/add-migration` — never edit existing
migrations.

### Backend

New `MeetingSessionsController` (`/api/v1/meeting-sessions`):
- `POST` — create session (admin or teacher of the class), generates slots.
- `GET` — list sessions (scoped: teacher sees own/class sessions, admin
  sees all, parent sees sessions for their children's classes).
- `GET /{id}/slots` — list slots with booking state.
- `DELETE /{id}` — admin/teacher cancels whole session (need to decide:
  block if slots already booked, or cascade-cancel + notify booked
  parents — pick cascade + notify, consistent with self-service
  cancellation elsewhere).
- `POST /{id}/slots/{slotId}/book` — parent books a slot for one of their
  children (validate parent-student link, validate student is in the
  session's class, validate slot not already booked).
- `POST /{id}/slots/{slotId}/cancel` — parent cancels own booking (must be
  the booking parent), reopens slot.

Authorization: teacher role scoped to own class(es) — check existing
`ClassPermissionsController` pattern for how "own class" is resolved for
teachers before reinventing it.

Notifications: reuse `INotificationService` — notify parent on booking
confirmation, notify parent on session cancellation
(`NotificationsController`/`NotificationPreference` pattern already
supports per-type opt-out).

### Frontend

- New admin/teacher page: create session (class picker, date, time range,
  slot duration, location), view slot grid with booked/open state.
- New parent page (likely under existing parent module nav, alongside
  Kontaktbog/Fravær): list open sessions for their children's class(es),
  pick a slot per child, see own booking, cancel button.
- Regenerate typed API client (`/codegen`) after controller/DTO changes.

## Open questions

- Session delete with existing bookings: cascade-cancel + notify (assumed
  above) — confirm before implementing, this is a judgment call not yet
  explicitly grilled.
- Is this gated behind the same Stripe subscription module as the rest of
  the parent module (`SubscriptionModulesController`), or included in
  base? Assume same gate as parent module unless told otherwise — it's
  parent-facing and lives in that surface.
- Reminder notification (e.g. day before the meeting)? Out of scope for
  v1 unless requested — not part of core booking flow.
