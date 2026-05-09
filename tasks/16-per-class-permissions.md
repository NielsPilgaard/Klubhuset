# Per-Class Permissions Task

Large schools likely want per-teacher permissions to add week plans to certain classes only.

---

## Problem

Currently any admin can edit any class's schema. For larger schools (20+ teachers), a teacher who is also an admin should only be able to modify week plans for their own classes — not accidentally edit another class's schema.

## Design

A `ClassPermission` join table grants specific staff members write access to specific classes. Admins without a class assignment retain full access to all classes (superadmins). This is additive — assigning permissions to some staff does not restrict other admins.

A dedicated "class admin" role distinct from the global `IsAdmin` flag. A teacher can be a class admin for 4A without being a school-wide admin.

## Implementation Plan

### Step 1 — Backend: `ClassPermission` entity

New entity:

```csharp
public sealed class ClassPermission : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ClassId { get; set; }
    public Class Class { get; set; } = null!;
    public Guid StaffId { get; set; }
    public Staff Staff { get; set; } = null!;
    public DateTimeOffset GrantedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

Entity config (on the class, following project convention — `IEntityTypeConfiguration<ClassPermission>` implemented on the entity):
- Unique index on `(TenantId, ClassId, StaffId)`
- Global query filter via `ITenantScoped`

New EF Core migration: `AddClassPermissions`.

### Step 2 — Backend: resource-based authorization policy

Use ASP.NET Core's `IAuthorizationService` (resource-based authorization) — the check is per-resource (which class), not just per-role, so this is the right fit per framework conventions.

```csharp
// Requirement
public class EditClassRequirement : IAuthorizationRequirement { }

// Handler — resource is the classId (Guid)
public class EditClassAuthorizationHandler
    : AuthorizationHandler<EditClassRequirement, Guid>
{
    // Inject AppDbContext + ITenantContext
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        EditClassRequirement requirement,
        Guid classId)
    {
        // 1. Must be admin
        if (!context.User.IsInRole("admin")) return;

        // 2. If no ClassPermission rows exist for this tenant → superadmin, full access
        // 3. If rows exist but none for this staff+class → deny
        // 4. If row exists for this staff+class → succeed
    }
}

// Registration in Program.cs / AuthExtensions.cs
builder.Services.AddAuthorization(opt =>
    opt.AddPolicy("EditClass", p => p.Requirements.Add(new EditClassRequirement())));
builder.Services.AddScoped<IAuthorizationHandler, EditClassAuthorizationHandler>();
```

Logic inside handler:
1. `!IsInRole("admin")` → return (not succeeded = denied)
2. No `ClassPermission` rows for tenant → `context.Succeed(requirement)` (superadmin)
3. `ClassPermission` row exists for `(staffId, classId)` → `context.Succeed(requirement)`
4. Otherwise → return (denied)

Usage in controllers:
```csharp
var result = await _authz.AuthorizeAsync(User, classId, "EditClass");
if (!result.Succeeded) return Forbid();
```

Single handler = single place to update logic. Consistent with ASP.NET Core resource-based auth docs.

### Step 3 — Backend: class permissions API

```
GET    /api/v1/classes/{classId}/permissions        → list granted staff
POST   /api/v1/classes/{classId}/permissions        → grant staff member
DELETE /api/v1/classes/{classId}/permissions/{staffId} → revoke
```

All endpoints: `[Authorize(Roles = "admin")]` — only school-wide admins manage permissions.

Response DTO:
```csharp
record ClassPermissionDto(Guid StaffId, string StaffName, DateTimeOffset GrantedAt);
```

### Step 4 — Frontend: class permissions UI

In the class detail/edit page (under `/klasser/{id}`), add a "Adgang" tab visible only to admins.

Shows a list of staff with access + a combobox to add more. Remove button per row.

Only shown when the school has at least one non-superadmin (i.e., when granular permissions make sense). If all admins are superadmins, show an info callout: "Alle administratorer har adgang til denne klasse."

### Step 5 — Enforce on schema endpoints

On `POST/PUT/DELETE` for SchemaSlots and WeekPlan entries, call `IAuthorizationService.AuthorizeAsync(User, classId, "EditClass")`. Return `403 Forbidden` with `ProblemDetails` if denied.

Schema for retrieving class context: resolve `classId` from the slot/weekplan being edited.

### Step 6 — Tests

- API integration test: superadmin can edit any class schema
- API integration test: admin with class permission can edit assigned class, gets 403 on others
- API integration test: granting/revoking permissions reflects immediately on subsequent requests
- Playwright e2e: admin assigns class permission → restricted admin can edit only that class

### Dependency

Task 15 (view modes) should be done first — it establishes the `CurrentStaff` context and admin route guards that this feature builds on.
