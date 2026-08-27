---
description: Fix batch — Kontakter fold-out, ParentsPage address/phone edit, Kontaktbog notify-picker, notification bell desktop, notification click-to-navigate
---

# TL;DR

Five fixes agreed after grilling:
1. Kontakter (`ParentDirectoryPage.tsx`) — collapse contact info behind click, add email.
2. `ParentsPage.tsx` — show + admin-edit phone/address via new PATCH endpoint + modal.
3. Kontaktbog — parent picks which staff to notify per-message (multiselect+search, grouped: class-relevant staff first, rest below, alphabetical within group). Opt-in, none pre-checked. All staff can still read thread regardless.
4. Notification bell — add to desktop `Sidebar.tsx` brand row (top-right), needs `variant="dark"` prop since sidebar bg is dark.
5. Notification click — navigate to relevant route per `NotificationType` + `ReferenceId`, in addition to marking read.

## 1. Kontakter fold-out

- File: `web/src/pages/ParentDirectoryPage.tsx`
- Currently: card list, phone/address/city always visible inline if present (no email field in DTO at all).
- Change: collapse contact block (phone/address/city/email) behind click-to-expand per card, collapsed by default. Click name/card header to toggle.
- Backend: add `Email` to `KontaktParentDto` (`KontaktController.cs:14-22`) and populate it — respect same `AdresseBeskyttet` redaction as other fields (or clarify if email should always show; default to same redaction rule as phone/address since it's PII).
- Update `KontaktControllerKontaktParentDto` in generated types after codegen.

## 2. ParentsPage phone/address

- Backend: `Parent` entity already has `Phone/Address/PostalCode/City` (`Models/Parent.cs:8-44`) but `ParentDto` only exposes `Phone` (`ParentsController.cs:23`, `ToDto` at `:191-199`).
  - Extend `ParentDto` to include `Address, PostalCode, City`.
  - Add new endpoint `PATCH /api/v1/parents/{id}/contact`, admin-only (existing `[Authorize(Roles = Roles.Admin)]` at controller level), body `UpdateParentContactRequest(string? Phone, string? Address, string? PostalCode, string? City)`. Separate from existing `PATCH /parents/{id}/adresse-beskyttelse`.
- Frontend: `ParentsPage.tsx`
  - Add compact "Telefon" column to table (address stays modal-only, don't crowd table).
  - Add edit (pencil) icon in Handlinger column opening an edit modal (same pattern as existing `InviteModal`) with Phone/Address/PostalCode/City fields, calls new PATCH endpoint, invalidates `getApiV1ParentsQueryKey()`.

## 3. Kontaktbog notify-picker

- Scope: parent-side compose only. Staff-side reply keeps current behavior (notify all parents of student, unchanged).
- Backend:
  - `ContactThreadsController.AddMessage` (`:251-333`) needs to accept an optional `NotifyStaffIds: List<Guid>?` in the request body (parent-sent messages only).
  - `SendNotificationsAsync` (`:377-418`): when sender is parent, instead of notifying `db.Staff.AsNoTracking().ToListAsync()` (all staff), notify only staff whose Id is in `NotifyStaffIds`. Empty/null list → notify nobody (message still posts, still readable by all staff — no read-access change, this only affects notification fan-out).
  - Add endpoint (or extend existing thread-fetch) to return the staff picker list for a given student: union of (a) staff with a `SchemaSlot` (TeacherId or AideId) linked via Course→Class to the student's class, (b) staff with `ClassPermission` on that class. Include a `isRelevant: bool` flag per staff so frontend can group. Return all tenant staff (not just relevant ones) so "rest" group is populated.
- Frontend: `ParentKontaktbogPage.tsx`
  - New multiselect+search component for the compose box: search input filters by name, list grouped under two headers ("Klassens personale" / relevant, and "Øvrige" / rest), alphabetical within each group, checkboxes, mobile-friendly (full-width tap targets, no hover-only affordances).
  - None pre-checked by default each time compose box opens/resets.
  - Send button remains enabled with zero staff selected (message still posts).

## 4. Notification bell on desktop

- `NotificationBell.tsx`: add `variant: 'light' | 'dark'` prop (default `'light'`).
  - `dark`: icon `text-brand-100 hover:text-white hover:bg-brand-800` (adjust to match sidebar palette), badge unchanged (red badge works on both).
  - Dropdown panel itself stays white/light in both variants.
- `Sidebar.tsx`: import `NotificationBell`, render with `variant="dark"` in the brand row (`:867-890`) in the space next to the mobile-only close button — make bell visible `lg:flex` (or just default-visible, non-mobile-hidden) since mobile already has its own bell in `Layout.tsx`'s mobile header. Ensure it doesn't double-render on mobile (wrap in `hidden lg:block` inside Sidebar, since Sidebar itself is present in DOM on mobile too via slide-out).
- `Layout.tsx` mobile header bell (`:65`) stays as-is, `variant="light"` (default).

## 5. Notification click navigates

- `NotificationsController.cs` / `NotificationDto`: no backend change needed — `Type` + `ReferenceId` already sufficient.
- `NotificationBell.tsx`:
  - Add `useNavigate` (react-router) and role check (`useAuth` — `isAdmin`/`isParent`/staff) to pick correct base path per audience.
  - On notification click: call `markRead(n.id)` (existing), close dropdown, then navigate based on `switch (n.type)`:
    - `NewContactMessage` → kontaktbog page with thread selected. Check `ParentKontaktbogPage.tsx`/`KontaktbogPage.tsx` for the actual query-param/selection mechanism (`useSearchParams`/`threadId` already present in both files per exploration) and match it — e.g. `/kontaktbog?threadId={referenceId}` (staff) or `/foraeldrevisning/kontaktbog?threadId={referenceId}` (parent).
    - `NewMessage` / `GroupMessage` → `/beskeder` (role-appropriate; single route serves both per `App.tsx:321`).
    - `WeekPlanChanged` → ugeplan route; needs `ReferenceId` to resolve a `classId` if going to `/klasser/{classId}/ugeplan`, else fall back to parent's `/foraeldrevisning/ugeplan`.
    - `AbsenceConfirmed` / `AbsenceDismissed` → `/fravaer` (staff) or `/foraeldrevisning/fravaer` (parent).
    - `VacationRegistrationOpened` → `/foraeldrevisning/ferieindmelding` (parent-only notification type).
  - Verify each `ReferenceId` semantic against what the backend actually sets when creating each notification type (grep `notificationService.CreateAsync` call sites for the `referenceId` argument passed per type) before wiring — don't guess.

## Post-implementation

Run `/codegen` after backend DTO/endpoint changes (item 2, 3) before touching frontend. Run `/verify` and relevant `/test` flows before declaring done, per AGENTS.md.
