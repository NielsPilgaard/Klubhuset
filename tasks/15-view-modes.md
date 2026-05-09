# View Modes Task

In order to make the app more user friendly, we should introduce view modes to show people exactly what we expect them to want to see:

## View modes:

- Parent (future)
- Board (future)
- Administrator (Secretary, school leader, office)
- Teacher
- Aide (probably same as Teacher)

## Admin

Admins should be able to see everything, including how other view modes perceive the app, and also the dashboard
Vikar dækning, årsrul.

## Parents (future)

Want to see calenders and week plans.

## Teachers (+aides)

Want to see calenders, week plans, missing entries from week plans, their daily routine (where to be when). Also files
Same for aides maybe, but with more focus on just daily routine?
Vikar dækning?

## Board (future)

UVM requirements, stats

It should be possible to assign admins to classes, a teacher can be an admin, any role can be an admin really.

---

## Implementation Plan

### Context

Current state: all authenticated users see identical sidebar and all routes. `Staff.IsAdmin` controls Keycloak `admin` realm role (used for backend `[Authorize(Roles = "admin")]`). `Staff.Role` is `Teacher | Aide | Substitute`. No frontend role-awareness exists.

### Step 1 — Backend: `GET /api/v1/staff/me` endpoint

Add endpoint to `StaffController` returning the current user's Staff record (matched by `KeycloakSubject` claim).

```csharp
// Response DTO
record CurrentStaffDto(Guid Id, string Name, StaffRole Role, bool IsAdmin);
```

- Resolve staff by matching `HttpContext.User.FindFirst("sub")` against `Staff.KeycloakSubject`
- Return 404 if no match (uninvited user who somehow authenticated)
- No migration needed — all fields already exist

### Step 2 — Frontend: current user context

Add `useCurrentStaff` hook that fetches `/api/v1/staff/me` via the generated client. Expose via a `CurrentStaffContext` so any component can read `{ role, isAdmin }` without prop drilling.

```ts
// web/src/auth/CurrentStaffContext.tsx
interface CurrentStaff {
  id: string
  name: string
  role: 'Teacher' | 'Aide' | 'Substitute'
  isAdmin: boolean
}
```

Wrap in `Layout.tsx` (already wraps all protected routes) so context is available everywhere.

### Step 3 — Sidebar: role-filtered nav items

Convert `navItems` in [web/src/components/Sidebar.tsx](../web/src/components/Sidebar.tsx) from a static array to a function that takes `CurrentStaff` and returns filtered items.

**Admin sees**: Oversigt, Klasser, Kalender, Medarbejdere, Fag, Lokaler, Filer, Eksporter, Abonnement, Indstillinger  
**Teacher/Aide sees**: Kalender, Filer  
(Possibly Vikar dækning in future — keep `roles` field on nav items for easy extension)

Nav item shape:
```ts
interface NavItem {
  to: string
  label: string
  icon: ReactNode
  adminOnly?: boolean  // hidden for teachers/aides
}
```

### Step 4 — Route protection

In [web/src/App.tsx](../web/src/App.tsx), wrap admin-only routes (`/klasser`, `/medarbejdere`, `/fag`, `/lokaler`, `/eksporter`, `/dashboard`, `/indstillinger`, `/setup`) with a guard component that redirects non-admins to `/kalender`.

```tsx
// AdminRoute: redirects to /kalender if !currentStaff.isAdmin
```

Non-admins landing on `/` also redirect to `/kalender` instead of `/dashboard`.

### Step 5 — Admin "view as" switcher (optional, lower priority)

Admins can preview the app as a Teacher. Add a dropdown in the sidebar footer (visible to `isAdmin` only) with options: `Admin | Lærer`. When "Lærer" is selected, store in local state and apply the teacher nav filter. Does not change backend permissions — purely cosmetic. Useful for admins who are also teachers and want to see their own schedule view.

### Step 6 — Teacher's "My Schedule" page

Teachers need a `/mit-skema` page showing:
- Their SchemaSlots for the current week (query SchemaSlots where `TeacherId = currentStaff.Id`)
- Daily routine view: "Monday 8:00 — 4A Matematik — Room 12"

Backend: `GET /api/v1/staff/me/skema?week=2026-W19` — returns slots for that week.  
Frontend: simple list grouped by day. Mobile-first (phone-friendly).

### Step 7 — Tests

- API integration test: `GET /api/v1/staff/me` returns correct staff for authenticated user
- API integration test: admin-only route returns 403 for teacher JWT
- Playwright e2e: teacher login → sees Kalender not Klasser in sidebar
- Playwright e2e: admin login → sees all nav items

### What stays future

- Parent view mode
- Board view mode
- Vikar dækning access for teachers (depends on task 14)
