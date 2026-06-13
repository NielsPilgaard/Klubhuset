# Test Coverage Tracking

## Status legend
- ✅ done
- 🔄 in progress
- ⬜ pending

---

## Tier 1 — Security / auth enforcement

| Area | File | Status | Notes |
|------|------|--------|-------|
| AbsenceController — confirm/dismiss role enforcement | `AbsenceTests.cs` | ✅ done | Uses EditClassRequirement — 12 tests |
| CoursesController — full CRUD + tenant isolation | `CoursesCrudTests.cs` | ✅ done | 14 tests |
| ContactThreadsController — parent can't create thread for other parent's student | `ContactThreadsTests.cs` | ✅ done | 6 tests |
| MessagesController — consent-based recipient filtering | `MessagesTests.cs` | ✅ done | 6 tests |
| KontaktController — ShareContactInfo consent filtering | `KontaktTests.cs` | ✅ done | 4 tests |
| BoardMembersController — teacher-data-access flag enforcement | `BoardMemberTests.cs` | ✅ done | 8 tests |

## Tier 2 — Core CRUD with no coverage

| Area | File | Status | Notes |
|------|------|--------|-------|
| VacationRegistrationController — window CRUD, parent submit, CSV export | `VacationRegistrationTests.cs` | ✅ done | 10 tests |
| StaffInvitationsController — token flow, accept state | `StaffInvitationTests.cs` | ✅ done | 7 tests |
| ParentInvitationsController — token flow, accept state | `ParentInvitationTests.cs` | ✅ done | 5 tests |
| SchedulesController — date filtering, cross-tenant perms | `SchedulesTests.cs` | ✅ done | 5 tests |
| NotificationsController — preference upsert, read-all idempotence | `NotificationsTests.cs` | ✅ done | 7 tests |

## Tier 3 — Feature correctness

| Area | File | Status | Notes |
|------|------|--------|-------|
| StaaMaalMedController — coverage calc accuracy | `StaaMaalMedTests.cs` | ⬜ pending | |
| ReportsController — Excel export data | `ReportsTests.cs` | ⬜ pending | |
| BillingController — trial days, subscription state | `BillingTests.cs` | ⬜ pending | |

## Already covered (baseline)

| Area | File |
|------|------|
| CalendarController — CRUD, recurrence, defaults + date assertions | `CalendarCrudTests.cs` |
| TimeSlotTemplate — break boundary sliding validation | `TimeSlotTemplateTests.cs` |
| ClassesController — sorting, year-roll | `ClassesSortingTests.cs`, `YearRollTests.cs` |
| ClassPermissionsController — grant/revoke | `ClassPermissionsTests.cs` |
| FilesController — tenant isolation | `FilesTenantIsolationTests.cs` |
| RoomsController — CRUD, tenant isolation | `RoomsCrudTests.cs`, `TenantIsolationTests.cs` |
| SchemasController — conflict detection, non-admin perms | `ConflictDetectionTests.cs`, `NonAdminSchemaEditTests.cs` |
| SfoController — shifts, staff assignment | `SfoTests.cs` |
| StaffController — admin perms, view mode | `StaffAdminPermissionTests.cs`, `ViewModeTests.cs` |
| WeekPlanController — CRUD, perms | `WeekPlanTests.cs`, `WeekPlanPermissionsTests.cs` |
| VikarController — substitute assignment | `VikarTests.cs` |
