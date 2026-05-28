# Student Module Plan

## Context

Schools need a way for students (primarily young children) to log in and see their schedule, weekly plan, and eventually submit homework. The student entity and admin CRUD already exist. This module adds student authentication (picture password), a student portal, and homework submission linked to lesson slots.

Student logins are deliberately low-security — picture passwords are designed for young children who can't type complex passwords. Admins and teachers can reset passwords trivially.

---

## Scope

### Phase 1 — Student Auth + Portal (no homework yet)
1. Student login: pick class → type username OR scan QR code → enter picture password
2. Student portal: schedule view + ugeplan view (read-only)
3. Admin: set picture password sequence for student, generate QR code link

### Phase 2 — Homework Submission
4. Student submits homework linked to a `WeekPlanSlot`
5. Teacher sees submissions in the ugeplan UI

---

## Architecture Decisions

### Picture Password
- Fixed set of ~25 illustrated icons (SVG, bundled in frontend). Examples: horse, ball, sun, cat, tree, star, heart, fish, house, car, moon, flower, cloud, apple, dog, bird, boat, key, bell, crown, diamond, hat, rocket, leaf, butterfly.
- Password = ordered sequence of 4 icons from the set (chosen by admin or student on first login).
- Stored as `string` on `Student` — e.g. `"horse,ball,sun,sun"` — hashed with BCrypt (same as Keycloak password pattern but self-managed, since Keycloak is overkill for picture passwords).
- **No Keycloak account for students.** Students authenticate via a custom `/api/v1/student-auth/login` endpoint that issues a short-lived JWT directly. This avoids Keycloak complexity for a low-security flow.
- JWT claims: `sub` = student ID, `tenant_id`, role = `"student"`.

### Student Identity Step (before picture password)
Two supported flows:
1. **Username flow**: Student types a short username (set by admin, e.g. "lars7"). School displays all usernames on whiteboard or prints a list.
2. **QR code flow**: Admin generates a per-student QR code link (`/elev/{token}`) — scanning takes the student straight to the picture-password screen, pre-filled with their identity.

Both flows converge on the same picture-password screen.

### Student JWT Auth
- New `StudentAuthController` issues JWTs signed with the same key as the API uses.
- Existing `[Authorize(Roles = "student")]` guards student endpoints.
- `AuthConstants.Roles` gets a new `"student"` constant.
- `KeycloakRolesClaimsTransformer` already handles role extraction from JWT — but student JWTs are issued by our own API, not Keycloak. Need a fallback: if JWT issuer is `"skoleoverblikket"`, extract role from `role` claim directly instead.

### Homework Submission
- New entity: `HomeworkSubmission` — links `StudentId` + `WeekPlanSlotId`, has `TextContent` (nullable, 4000 chars), `FileUrl` (nullable), `SubmittedAt`.
- Existing `WeekPlanSlot.Lektier` field = teacher-written homework *description* (what to do). Student submissions are separate records.
- Teacher sees submissions count on `WeekPlanSlot` in ugeplan UI; clicks to view.

---

## Data Model Changes

### `Student` entity additions
```csharp
public string? Username { get; set; }          // short, unique per tenant. e.g. "lars7"
public string? PicturePasswordHash { get; set; } // BCrypt hash of "horse,ball,sun,sun"
public string? LoginToken { get; set; }         // opaque token for QR code URL, unique per tenant
public DateTimeOffset? LoginTokenCreatedAt { get; set; }
```

Migration: `AddStudentAuth`

### New entity: `HomeworkSubmission`
```csharp
public sealed class HomeworkSubmission : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public Guid WeekPlanSlotId { get; set; }
    public WeekPlanSlot WeekPlanSlot { get; set; } = null!;
    public string? TextContent { get; set; }    // max 4000 chars
    public string? FileUrl { get; set; }        // presign/confirm pattern
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}
```

Indexes: `(TenantId, WeekPlanSlotId)`, `(TenantId, StudentId)`.
Unique constraint: `(TenantId, StudentId, WeekPlanSlotId)` — one submission per student per slot.

Migration: `AddHomeworkSubmissions`

---

## Backend — New Endpoints

### `StudentAuthController` — `/api/v1/student-auth`
| Endpoint | Auth | Purpose |
|----------|------|---------|
| `POST /login` | anonymous | Username + picture password → JWT |
| `GET /qr/{token}` | anonymous | QR token lookup → student info (name, class) for pre-fill |
| `POST /login-by-token` | anonymous | QR token + picture password → JWT |

### `StudentsController` additions
| Endpoint | Auth | Purpose |
|----------|------|---------|
| `PUT /api/v1/students/{id}/username` | admin | Set username |
| `PUT /api/v1/students/{id}/picture-password` | admin | Set/reset picture password |
| `POST /api/v1/students/{id}/login-token` | admin | Generate/regenerate QR token |
| `GET /api/v1/students/{id}/login-token` | admin | Get QR token (for rendering QR code) |

### `HomeworkController` — `/api/v1/homework`
| Endpoint | Auth | Purpose |
|----------|------|---------|
| `GET /api/v1/homework?weekPlanSlotId={id}` | student, admin, staff | List submissions for a slot |
| `POST /api/v1/homework` | student | Submit homework |
| `PUT /api/v1/homework/{id}` | student (own only) | Update submission |
| `DELETE /api/v1/homework/{id}` | student (own only) | Retract submission |

Student can only see/modify their own submission. Admin/staff see all submissions for a slot.

### Student-facing read endpoints (existing, need student role added)
- `GET /api/v1/classes/{classId}/schemas` — already has `ParentClassAccessHandler`; extend to also allow student role (student's `ClassId` must match).
- `GET /api/v1/classes/{classId}/ugeplan` — same extension.

---

## Frontend — New Pages

### Login flow (unauthenticated, no layout chrome)
- `/elev/login` — Step 1: type username OR "scan QR code" button (just a link to show QR instructions)
- `/elev/{token}` — QR landing: shows student name/avatar, goes straight to picture-password screen
- `/elev/password` — Picture-password entry: 4×4 grid of icons, student taps 4 in sequence, submit

Picture-password UI: show the full icon set in a grid. As student taps icons, show selected sequence at the top (masked after a moment like a PIN). Submit when 4 selected.

### Student portal (authenticated, student role)
Route guard: `StudentRoute` component (parallel to `ParentRoute`).

| Route | Page | Notes |
|-------|------|-------|
| `/min/skema` | `StudentSchemaPage` | Read-only schedule, reuse parent schema component |
| `/min/ugeplan` | `StudentUgeplanPage` | Read-only ugeplan + homework submission per slot |
| `/min` | redirect to `/min/skema` | |

### Admin additions (in `StudentsPage.tsx`)
- Per-student expandable row or modal tab: "Login-indstillinger"
  - Set username field
  - Picture-password picker (show icon grid, admin taps 4)
  - "Generer QR-kode" button → shows QR code image (use a QR library like `qrcode.react`)
  - "Nulstil adgangskode" button

### QR Code Printing
Two print flows:

**Individual**: In the student's login-settings panel, a "Print QR-kode" button opens a print-optimized page (`/print/elev/{studentId}/qr`) showing:
- Student name + avatar
- Class name
- QR code (large, ~6×6 cm)
- School name
Styled with `@media print` CSS — no chrome, pure card layout. Teacher can cut out and laminate.

**Bulk (class)**: In `StudentsPage`, a "Print QR-koder for klasse" button (per class filter) opens `/print/klasse/{classId}/qr`:
- Grid of student cards, one per student, all on one or more A4 pages
- Each card: name, avatar, QR code
- `@media print` auto-formats for A4, 4–6 cards per page
- Standard browser print dialog handles paper/scale

Both print pages are public-ish routes (no login required if you have the URL) OR require admin auth — **require admin auth** since QR tokens grant login access.

New files:
- `web/src/pages/print/StudentQrPrintPage.tsx` — single student
- `web/src/pages/print/ClassQrPrintPage.tsx` — full class

### Ugeplan — homework submission (student view)
In `StudentUgeplanPage`, each `WeekPlanSlot` shows:
- Teacher's `Lektier` text (read-only)
- "Min aflevering" section: text area + file upload + submit button
- If already submitted: shows their submission with edit/delete options

---

## Auth Integration Detail

Student JWT structure (issued by `StudentAuthController`):
```json
{
  "sub": "<student-guid>",
  "tenant_id": "<tenant-guid>",
  "class_id": "<class-guid>",
  "role": "student",
  "iss": "skoleoverblikket",
  "exp": "<now + 8h>"
}
```

`ParentClassAccessHandler` → extend or create `StudentClassAccessHandler` that reads `class_id` from JWT claims. Since it's in the token, no DB lookup needed for class authorization.

JWT validation: add a second `TokenValidationParameters` for the self-issued student JWTs (same signing key, different issuer string `"skoleoverblikket"`). Or simpler: use the same issuer as Keycloak for now and just add `"student"` to `AuthConstants.Roles`. Decide during implementation — leaning toward separate issuer for clarity.

---

## Files to Create / Modify

### New files
- `api/.../Models/HomeworkSubmission.cs`
- `api/.../Controllers/StudentAuthController.cs`
- `api/.../Controllers/HomeworkController.cs`
- `api/.../Services/StudentAuthService.cs` — JWT issuance, BCrypt verify
- `api/.../Migrations/AddStudentAuth.cs` (via `/add-migration`)
- `api/.../Migrations/AddHomeworkSubmissions.cs` (via `/add-migration`)
- `web/src/pages/StudentLoginPage.tsx`
- `web/src/pages/StudentPortalSchemaPage.tsx`
- `web/src/pages/StudentPortalUgeplanPage.tsx`
- `web/src/components/PicturePasswordPicker.tsx`
- `web/src/pages/print/StudentQrPrintPage.tsx`
- `web/src/pages/print/ClassQrPrintPage.tsx`

### Modified files
- `api/.../Models/Student.cs` — add `Username`, `PicturePasswordHash`, `LoginToken`, `LoginTokenCreatedAt`
- `api/.../Auth/AuthConstants.cs` — add `Roles.Student = "student"`
- `api/.../Controllers/StudentsController.cs` — add username/password/token endpoints
- `api/.../Program.cs` — register `StudentAuthService`, JWT validation config
- `web/src/App.tsx` — add student routes, `StudentRoute` guard
- `web/src/pages/StudentsPage.tsx` — add login-settings UI per student

---

## Icon Set (built-in, Phase 1)

25 SVG icons bundled in `web/src/assets/picture-icons/`: horse, ball, sun, cat, tree, star, heart, fish, house, car, moon, flower, cloud, apple, dog, bird, boat, key, bell, crown, diamond, hat, rocket, leaf, butterfly.

Each icon has an ID string used in the password hash input. Display in a 5×5 grid on the password screen.

---

## Verification

1. `/verify` — TypeScript build + dotnet format + dotnet build + API integration tests
2. Manual: create student in admin, set username + picture password, log in via `/elev/login`, verify JWT, view schedule
3. Manual: generate QR code, navigate to `/elev/{token}`, complete picture password, verify redirect to portal
4. Manual: submit homework on a slot, view as admin in ugeplan
5. `/test` — Playwright e2e for the critical login flow

---

## Open Questions / Risks

- **BCrypt in API**: verify `BCrypt.Net-Next` is already a dependency; if not, add it. Alternative: use PBKDF2 via `Rfc2898DeriveBytes` (no new package).
- **JWT self-issuance**: confirm the signing key approach — likely use `IConfiguration["Jwt:Secret"]` already used for Keycloak token validation, or add a separate `Jwt:StudentSecret`.
- **QR code frontend library**: `qrcode.react` (MIT, well-maintained) or `react-qr-code` — either works. Both support rendering to SVG/canvas for print.
- **Phase 2 homework**: file upload for student submissions reuses the presign/confirm S3 pattern from avatar uploads. Implement in Phase 2.
