# Task 18 — Board Module (Bestyrelsesmodul)

## Goal

Board members (bestyrelse) get a dedicated login with a separate isolated view. They see aggregated stats and shared board files. Access to teacher data (schemas, staff, classes) is configurable per board member. Board file storage is isolated from school files.

---

## Data model

### `BoardMember` (new entity, tenant-scoped)

```csharp
public sealed class BoardMember : ITenantScoped, IEntityTypeConfiguration<BoardMember>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public string? KeycloakSubject { get; set; }
    public bool CanAccessTeacherData { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}
```

`CanAccessTeacherData` defaults to `false`. Admin can toggle per member.

### `BoardMemberInvitation` (new entity, mirrors `StaffInvitation`)

```csharp
public sealed class BoardMemberInvitation : ITenantScoped, IEntityTypeConfiguration<BoardMemberInvitation>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid BoardMemberId { get; set; }
    public required string Email { get; set; }
    public required string Token { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public InvitationStatus Status { get; set; }
    public BoardMember BoardMember { get; set; } = null!;
}
```

### `BoardFile` (new entity, tenant-scoped — separate from `SchoolFile`)

Board files have no `CourseId` relationship and may evolve different folder semantics. A discriminator on `SchoolFile` would couple unrelated ownership concerns; new entities keep them independent.

```csharp
public sealed class BoardFile : ITenantScoped, IEntityTypeConfiguration<BoardFile>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public required string StorageKey { get; set; }
    public required string Url { get; set; }
    public Guid? FolderId { get; set; }
    public Guid UploadedBy { get; set; }  // BoardMember.Id
    public DateTimeOffset UploadedAt { get; init; }
}
```

### `BoardFileFolder` (new entity, mirrors `SchoolFileFolder`)

Hierarchical folder structure for board documents. Same parent/children pattern as `SchoolFileFolder`.

**Migration:** `AddBoardModule` (covers all four above).

---

## Keycloak

- Add realm role `board` to `infrastructure/keycloak/realm-export.json`
- Board member's JWT must carry `tenant_id` claim. Use `CreateUserAsync` general method (see task 17 — same generalization).
- **Production:** add `board` role manually via Keycloak Admin UI → Realm roles → Create role (description: "Bestyrelsesmedlem — adgang til bestyrelsesmodul"). No `[Authorize(Roles = "board")]` guard works until this exists in the target realm.

---

## Backend

### New: `BoardMembersController` (`api/v1/board-members`)

- `GET /api/v1/board-members` — list (admin only)
- `POST /api/v1/board-members/invite` — invite board member, send email (admin only)
- `GET /api/v1/board-members/{id}` — get detail (admin only)
- `DELETE /api/v1/board-members/{id}` — remove + revoke Keycloak account (admin only)
- `PATCH /api/v1/board-members/{id}/teacher-data-access` — toggle `CanAccessTeacherData` (admin only)

### New: `BoardFilesController` (`api/v1/board-files`)

Same shape as `FilesController` but scoped to `BoardFile`/`BoardFileFolder`. Allows `board` role to upload, download, and manage folders. Admin also has full access.

- `GET /api/v1/board-files` — list files
- `POST /api/v1/board-files` — upload (presigned S3 URL flow)
- `DELETE /api/v1/board-files/{id}` — delete
- `POST /api/v1/board-files/{id}/move` — move to folder
- `GET /api/v1/board-files/folders` — list folders
- `POST /api/v1/board-files/folders` — create folder
- `DELETE /api/v1/board-files/folders/{id}` — delete folder

### New: `BoardInvitationsController` (`api/v1/board-invitations`)

- `POST /api/v1/board-invitations/{token}/accept` — anonymous; accepts invite, creates Keycloak account with `board` role + `tenant_id` attribute

### Extend existing read endpoints

- Stats endpoints (`/api/v1/stats`) — allow `board` role always (aggregated data, no PII)
- Schema/staff/class read endpoints — allow `board` role only if `CanAccessTeacherData == true` (see policy below)

### Authorization policy: `CanAccessTeacherData`

Custom ASP.NET Core authorization policy. Handler resolves `BoardMember` by JWT `sub`, checks `CanAccessTeacherData` flag. Non-board roles (admin, staff) are not subject to this policy.

```csharp
// Register in Program.cs:
services.AddAuthorization(options =>
{
    options.AddPolicy("CanAccessTeacherData", policy =>
        policy.Requirements.Add(new TeacherDataAccessRequirement()));
});
services.AddScoped<IAuthorizationHandler, TeacherDataAccessHandler>();

// Usage in controllers — endpoints that board members can access conditionally:
[Authorize(Roles = "admin")]          // admins always pass
// + in action body:
if (User.IsInRole("board"))
    await _authorizationService.AuthorizeAsync(User, "CanAccessTeacherData");
// or use [Authorize(Policy = "CanAccessTeacherData")] on endpoints that are board-only
```

---

## Frontend

### Board routes (board-specific sidebar for `board` role)

- `/bestyrelse/oversigt` — stats dashboard (class count, course hours, staff overview); reuses existing stats data, read-only presentation
- `/bestyrelse/filer` — board file explorer (separate from `/filer`)
- `/bestyrelse/skemaer` — class schemas read-only (only shown if `CanAccessTeacherData`)
- `/bestyrelse/medarbejdere` — staff list read-only (only shown if `CanAccessTeacherData`)

### Admin settings (`/indstillinger`)

Add section "Bestyrelsesmedlemmer":

- List of board members with name, email, `CanAccessTeacherData` toggle
- "Inviter bestyrelsesmedlem" button

### Sidebar

Detect `board` role from JWT. Show board-relevant nav items. Hide `CanAccessTeacherData`-gated items if flag is false (frontend hides; backend still enforces).

---

## Tests

- API: invite board member → accept → GET `/api/v1/stats` with board JWT → 200
- API: board member with `CanAccessTeacherData=false` → GET `/api/v1/staff` → 403
- API: board member with `CanAccessTeacherData=true` → GET `/api/v1/staff` → 200
- API: board member → GET/POST `/api/v1/board-files` → 200
- API: board member → GET `/api/v1/files` (school files) → 403 (school files are not board files)
- Playwright: admin invites board member → board member logs in → sees stats + board files
- Playwright: admin toggles `CanAccessTeacherData` on → board member sees schema nav item

---

## Out of scope

- Board member grading or curriculum editing
- Direct messaging between board and teachers
- "Stå mål med" implementation (research only this task)
- Audit log of board member access (future)
