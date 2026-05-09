# Task 17 — Parent Module (Forældremodul)

## Goal

Parents get read-only access to their child's class: schema, calendar, week plan, SFO-skema. Admin registers students and invites parents by email. Same invitation flow as staff. Multiple parents per child supported (mother, father, guardians).

---

## Data model

### `Student` (new entity, tenant-scoped)

```csharp
public sealed class Student : ITenantScoped, IEntityTypeConfiguration<Student>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Name { get; set; }
    public Guid ClassId { get; set; }
    public Class Class { get; set; } = null!;
    public ICollection<Parent> Parents { get; set; } = [];
    public DateTimeOffset CreatedAt { get; init; }
}
```

No `KeycloakSubject` yet — added in a future Student Module when students get logins. The entity is designed to extend without structural changes.

### `Parent` (new entity, tenant-scoped)

```csharp
public sealed class Parent : ITenantScoped, IEntityTypeConfiguration<Parent>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public bool ShareContactInfo { get; set; }  // consent for future parent directory (Kontakt)
    public string? KeycloakSubject { get; set; }
    public ICollection<Student> Students { get; set; } = [];
    public DateTimeOffset CreatedAt { get; init; }
}
```

`ShareContactInfo` defaults to `false`. Parent sets it during onboarding. Controls visibility in the future Kontakt (parent directory) feature. When `false`, address/phone/city are hidden from other parents; visible to admin only.

### `ParentStudent` (join table, EF many-to-many)

Unique constraint on `(TenantId, ParentId, StudentId)`. One parent can have children in multiple classes; one child can have multiple parents.

### `ParentInvitation` (new entity, mirrors `StaffInvitation`)

```csharp
public sealed class ParentInvitation : ITenantScoped, IEntityTypeConfiguration<ParentInvitation>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ParentId { get; set; }
    public required string Email { get; set; }
    public required string Token { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public InvitationStatus Status { get; set; }  // reuse existing enum
    public Parent Parent { get; set; } = null!;
}
```

**Migration:** `AddParentModule` (covers all four above).

---

## Keycloak

- Add realm role `parent` to `infrastructure/keycloak/realm-export.json`
- Parent's JWT must carry `tenant_id` claim (same as admin). Use `CreateUserAsync` general method (see task note below).
- Production: add `parent` role manually via Keycloak Admin UI → Realm roles → Create role.

---

## Backend

### `KeycloakAdminService` — generalize user creation

Replace `CreateStaffUserAsync` + `CreateAdminUserAsync` with one general method:

```csharp
public async Task<string> CreateUserAsync(
    string email,
    string firstName,
    string lastName,
    string password,
    Guid? tenantId,          // null = no tenant_id attribute; parents need it set
    string? realmRole,       // null = no role; "admin", "parent", "board" etc.
    bool forcePasswordReset, // true = UPDATE_PASSWORD required action
    CancellationToken ct)
```

`CreateAdminUserAsync` and `CreateStaffUserAsync` become thin wrappers with no change to call sites.

### New: `StudentsController` (`api/v1/students`)

- `GET /api/v1/students` — list all, optional `?classId=` filter (admin only)
- `POST /api/v1/students` — register student: `{ name, classId }` (admin only)
- `PUT /api/v1/students/{id}` — update name/class (admin only)
- `DELETE /api/v1/students/{id}` — remove (admin only)

### New: `ParentsController` (`api/v1/parents`)

- `GET /api/v1/parents` — list all parents (admin only)
- `POST /api/v1/parents/invite` — invite parent, link to student(s), send email (admin only)
- `GET /api/v1/parents/{id}` — get parent detail (admin only)
- `DELETE /api/v1/parents/{id}` — remove parent + revoke Keycloak account (admin only)
- `POST /api/v1/parents/{id}/students/{studentId}` — link to additional student (admin only)
- `DELETE /api/v1/parents/{id}/students/{studentId}` — unlink from student (admin only)

### New: `ParentInvitationsController` (`api/v1/parent-invitations`)

- `POST /api/v1/parent-invitations/{token}/accept` — anonymous; accepts invite, creates Keycloak account with `parent` role + `tenant_id` attribute, marks invitation accepted

### Extend existing read endpoints

Allow `parent` role on:
- `GET /api/v1/classes/{classId}/schemas` — add `[Authorize(Roles = "admin,parent")]`
- `GET /api/v1/calendar` — add `parent` to allowed roles
- `GET /api/v1/classes/{classId}/ugeplan` — add `parent` to allowed roles
- Any future SFO endpoints — same pattern

### Authorization policy: `ParentClassAccess`

Resource-based policy. Handler resolves parent by JWT `sub` → student list → class IDs. Returns 403 if requested `classId` not in set. Admin bypasses via `[Authorize(Roles = "admin")]`.

```csharp
await _authorizationService.AuthorizeAsync(User, classId, "ParentClassAccess");
```

Register `ParentClassAccessHandler` in `Program.cs`.

---

## Frontend

### Admin routes (integrate with task 15 view modes)

- `/elever` — student list, filterable by class; same pattern as `/medarbejdere`
- `/elever/ny` — register student (name + class dropdown)
- `/elever/:studentId` — student detail: name, class, linked parents, "Inviter forælder" button

### Parent routes (simplified sidebar for `parent` role)

- `/forældre/skema` — read-only class schema (no drag-drop, no slot editing)
- `/forældre/kalender` — calendar view
- `/forældre/ugeplan` — week plan view
- `/forældre/sfo` — SFO schedule (stub if SFO not built yet)

### Sidebar

Detect `parent` role from JWT claims. Show only parent-relevant nav items. Reuse task 15 role detection if available; otherwise add `useRoles()` hook.

### Read-only schema view

Reuse existing print/view schema components. Strip all edit affordances.

### Invitation accept page

Extend `InvitationAcceptPage.tsx`. Add contact-info consent step: address, phone, `ShareContactInfo` toggle (opt-in, default off). Must be clearly labelled as optional.

---

## Admin flow

1. Admin registers student (name + class) via `/elever/ny`
2. Admin opens student detail → clicks "Inviter forælder"
3. Admin enters parent name + email (repeat for each parent/guardian)
4. Each parent receives invite email → clicks link → sets password → sets contact consent → logs in
5. Parent lands on `/forældre/skema` showing their child's class

---

## Tests

- API: invite parent → accept → GET `/api/v1/classes/{classId}/schemas` with parent JWT → 200
- API: parent JWT for wrong class → 403
- API: parent with two children in different classes → 200 for both class IDs
- Playwright: admin registers student → invites parent → parent logs in → sees schema

---

## Out of scope

- Student logins (future Student Module)
- Parent directory / Kontakt feature (future — data model is ready)
- Adressebeskyttelse (§28 CPR-loven) — future; `ShareContactInfo=false` default provides basic protection now
- Fraværsregistrering
- Multi-child switcher UI (parent with 2+ students sees all classes; no switcher needed for read-only views)
