---
title: 'Parent calendar grid parity + parent self-service stamdata'
purpose: 'Give parents the same calendar grid admin/staff see (read-only), and let parents edit their own contact info instead of relying on admin.'
description: >-
  Two fixes, both complete. (1) Extracted the read-only grid rendering out
  of CalendarPage into a shared CalendarGrid component so parents get the
  same month grid/carousel view, no edit affordances. (2) Added
  self-service profile edit via the ParentMeController PATCH /contact
  endpoint behind a new "Min profil" page. Remaining work is verification
  only.
status: 'Done'
---

# Parent calendar grid parity + parent self-service stamdata

## TL;DR

Two independent fixes, grilled, scoped, and implemented:

1. **Calendar**: shared read-only `CalendarGrid` component extracted out
   of `CalendarPage.tsx` (buildMonthGrid, month cards, DayPopover display
   mode). `ParentCalendarPage.tsx` renders it instead of the old flat
   list. `CalendarPage.tsx` now reads `isAdmin` via `useAuth()` instead of
   `keycloak.hasRealmRole('admin')` directly (fixed latent bug that broke
   superadmin "view as parent"). Parent keeps `.ics` export. Staff/teacher
   already saw the grid before this change (route not `AdminRoute`-gated)
   — out of scope.
2. **Profile**: new `foraeldrevisning/profil` page
   (`ParentProfilePage.tsx`), routed and linked from parent nav. Backend
   `PATCH /api/v1/parents/me/contact` now accepts `Name`; `GET
   /api/v1/parents/me` returns Phone/Address/PostalCode/City/
   ShareContactInfo. Admin's parallel `UpdateParentContactRequest`/
   `EditContactModal` also gained `Name` for parity/override.

## Context — findings from explore + grill

**Calendar:**
- `web/src/pages/parent/ParentCalendarPage.tsx` — flat list grouped by
  month, no grid.
- `web/src/pages/CalendarPage.tsx` — hand-rolled grid (no lib), already
  threads `isAdmin: boolean` into `DayPopover` and gates create/edit/delete
  UI + mutations behind it. `isAdmin` sourced from
  `keycloak.hasRealmRole('admin')` (line ~548), not the app's
  `useAuth()`/`AuthContext.viewAs` convention used elsewhere (breaks
  `ViewModeToolbar` superadmin view-as for this page specifically).
- Backend `CalendarController`: single `GET /api/v1/calendar` used by both
  admin and parent already, no separate parent controller. Mutating
  endpoints already `[Authorize(Roles = Roles.Admin)]`. `GET /export.ics`
  has no role restriction.
- Route `kalender` (App.tsx ~225) is NOT wrapped in `AdminRoute` → staff/
  teacher already hit the admin grid today. Confirms scope is parent-only.

**Profile:**
- `Parent` model: `Name`, `Email`, `Phone`, `Address`, `PostalCode`,
  `City`, `ShareContactInfo`, `AdresseBeskyttet`, `AvatarUrl`.
- `ParentMeController` (`api/v1/parents/me`, `[Authorize(Roles=Parent)]`):
  `GET /` returns only `Id, Name, AvatarUrl, Classes, Students` — missing
  contact fields. `PATCH /contact` already implemented, accepts
  `UpdateContactRequest(Phone, Address, PostalCode, City,
  ShareContactInfo)` — no `Name`. Avatar presign/confirm already
  self-service. **Nothing in the frontend calls PATCH /contact today.**
- No `MyProfile`/self-service edit page exists anywhere in the app for any
  role — this is the first. `StaffMeController` is avatar-only, no contact
  patch precedent to mirror.
- Admin-side reference: `ParentsPage.tsx` `EditContactModal` (~line
  157-253) edits Phone/Address/PostalCode/City via admin's own
  `PATCH /api/v1/parents/{id}/contact` (`ParentsController`,
  `UpdateParentContactRequest` — currently no `Name` either). No format
  validation beyond trim-to-null.

## Decisions from grilling

- Extract shared read-only grid component (not just `isAdmin=false` reuse)
  — cleaner boundary, admin-only mutation code never ships to parent route.
- Fix `CalendarPage.tsx` auth source to `useAuth()` while touching the file.
- Scope calendar fix to parents only; staff/teacher unaffected (already
  grid).
- Parent keeps `.ics` export button — backend already allows any
  authenticated user, zero extra work.
- Editable parent fields: **Name, Phone, Address, PostalCode, City,
  ShareContactInfo**. Not editable by parent: Email, AdresseBeskyttet.
- Name validation: both parent (`ParentMeController`) and admin
  (`ParentsController`) endpoints apply identical rules — trim surrounding
  whitespace, require non-empty after trim, reject values over 200 chars.
  No new audit/logging (no existing audit infra; disproportionate for a name
  field).
- New dedicated page `foraeldrevisning/profil` ("Min profil") in parent
  nav — not a modal off an existing page. Matches existing parent route
  pattern (`ParentRoute` wrapper, lazy-loaded).
- No new notification type when parent self-edits contact info — admin
  sees it silently next time they load Foraeldre page (matches "don't
  invent scope not asked for").
- Admin's `EditContactModal`/`UpdateParentContactRequest` also gains `Name`
  — admin retains override/moderation capability now that parents can
  self-edit it.

## Proposed scope

### Backend

- `ParentMeController.GET /` — extend `ParentMeDto` to include `Phone,
  Address, PostalCode, City, ShareContactInfo`.
- `ParentMeController` — extend `UpdateContactRequest` with `Name`
  (required, ≤200); apply same field to the entity on `PATCH /contact`.
- `ParentsController` — extend `UpdateParentContactRequest` with `Name`;
  apply on admin's `PATCH /{id}/contact`.
- No new migration — no schema change, only DTO/request shape.
- Regenerate OpenAPI spec + typed client (`/codegen`) after DTO changes.

### Frontend

- Extract shared read-only calendar grid component from `CalendarPage.tsx`
  (grid building, month card rendering, `DayPopover` in display-only mode).
  `CalendarPage.tsx` keeps admin edit wrapper (`EntryModal`, delete
  dialogs, mutations) around the shared component.
- `CalendarPage.tsx`: replace `keycloak.hasRealmRole('admin')` with
  `useAuth().isAdmin`.
- `ParentCalendarPage.tsx`: render the shared grid component instead of
  the flat list; keep `.ics` export button.
- New `ParentProfilePage.tsx` at route `foraeldrevisning/profil`:
  - Fetch current values via extended `GET /parents/me`.
  - Form: Navn, Telefon, Adresse, Postnr., By, "Del mine
    kontaktoplysninger" toggle.
  - Submit via `patchApiV1ParentsMeContact` (already generated in SDK,
    just extend the request shape via codegen).
  - Add "Min profil" link to parent nav alongside Skema/Kalender/Ugeplan.
- `ParentsPage.tsx` `EditContactModal`: add Navn field, wire to extended
  `UpdateParentContactRequest`.

## Verification

Per AGENTS.md: `/verify` (TS build, dotnet format, dotnet build, API
integration tests) then `/test` (Playwright e2e) before declaring done.
New Playwright coverage: parent views calendar grid (read-only, no
edit/delete controls rendered), parent edits and saves profile contact
info, admin sees parent's self-edited Name/Phone reflected in
`ParentsPage`.
