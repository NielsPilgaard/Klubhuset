# Task: Parent Module Extensions (Task 22)

## Context

Task 17 (parent module) is ~95% done. Core infrastructure exists: Parent/Student/ParentInvitation models, invite flow, ParentClassAccess policy, read-only schema/calendar/ugeplan pages, admin UI.

This task completes the parent module into a full communication platform and adds shared profile image support across all user types.

---

## What already exists (do not re-implement)

| Thing | Location |
|---|---|
| Parent model (Phone, Address, PostalCode, City, ShareContactInfo) | `api/Skoleoverblikket.Api/Models/Parent.cs` |
| ParentInvitationService (token, email, Keycloak) | `api/Skoleoverblikket.Api/Services/ParentInvitationService.cs` |
| InvitationAcceptPage (staff + parent, all states) | `web/src/pages/InvitationAcceptPage.tsx` |
| ParentClassAccess policy + handler | `api/Skoleoverblikket.Api/Auth/ParentClassAccessRequirement.cs` |
| ParentMeController (GET /api/v1/parents/me) | `api/Skoleoverblikket.Api/Controllers/ParentMeController.cs` |
| Parent read-only pages (schema, calendar, ugeplan) | `web/src/pages/parent/` |
| Auth context isParent, parent sidebar nav | `web/src/auth/AuthContext.ts`, `web/src/components/Sidebar.tsx` |
| S3-compatible storage (`IObjectStorage`, presigned upload) | `api/Skoleoverblikket.Api/Storage/IObjectStorage.cs` |
| School logo upload pattern | `api/Skoleoverblikket.Api/Models/School.cs` (`LogoUrl`) |
| Email sender | `api/Skoleoverblikket.Api/Email/IEmailSender.cs` — `Task SendAsync(EmailMessage, cancellationToken)` |
| Presign+confirm pattern | `api/Skoleoverblikket.Api/Controllers/FilesController.cs` — HMAC-signed confirm token |

### Email sender interface (for NotificationService)

```csharp
// api/Skoleoverblikket.Api/Email/IEmailSender.cs
Task SendAsync(EmailMessage message, CancellationToken cancellationToken);

record EmailMessage(string To, string Subject, string HtmlBody, string? PlainTextBody = null);
```

Inject `IEmailSender` — already registered in DI.

### Presign+confirm flow (for avatar upload — same pattern as FilesController)

Avatar upload does NOT use the full FilesController HMAC token. Simpler pattern:

1. `POST /api/v1/parents/me/avatar/presign`
   - Body: `{ contentType: "image/jpeg"|"image/png"|"image/webp", fileSizeBytes: number }` (max 5 MB)
   - Calls `storage.GeneratePresignedUploadUrlAsync(key, contentType, fileSizeBytes, expiry=15min, cancellationToken)`
   - Returns: `{ uploadUrl: string, objectKey: string }`
   - S3 key pattern: `avatars/{tenantId}/parents/{parentId}{ext}`

2. Client PUTs binary to `uploadUrl` directly (browser fetch, no auth header)

3. `POST /api/v1/parents/me/avatar/confirm`
   - Body: `{ objectKey: string }`
   - Derives public URL via `storage.GetKeyFromPublicUrl` inverse — actually just store the key and construct public URL from it (see how `SchoolFile.Url = publicUrl` is set from the presign response)
   - Sets `parent.AvatarUrl = publicUrl`, saves, returns 204
   - Validate `objectKey` starts with `avatars/{tenantId}/parents/{parentId}` — prevents a parent confirming another parent's avatar key

Same pattern for staff (`avatars/{tenantId}/staff/{staffId}{ext}`) and students (`avatars/{tenantId}/students/{studentId}{ext}`).

---

## Feature 1: Profile Images (Parent, Staff, Student)

Optional avatar for all user types.

### Backend

Add `AvatarUrl string?` field to:
- `Parent` model
- `Staff` model
- `Student` model

New endpoints:
- `POST /api/v1/parents/me/avatar/presign` + `POST /api/v1/parents/me/avatar/confirm` — parent uploads own avatar
- `POST /api/v1/staff/me/avatar/presign` + `POST /api/v1/staff/me/avatar/confirm` — staff uploads own avatar
- `POST /api/v1/students/{id}/avatar/presign` + `POST /api/v1/students/{id}/avatar/confirm` — admin sets student avatar (`[Authorize(Roles = Roles.Admin)]`)

Validation: max 5 MB, contentType must be `image/jpeg`, `image/png`, or `image/webp` only.

### Frontend

- Avatar `<img>` or initials fallback everywhere a person is shown (parent list, staff list, contact book, messages)
- Upload via click on avatar — opens file picker, presigns, PUTs binary, confirms

### Migration: `AddAvatarUrls`
- Add `avatar_url` to `parents`, `staff`, `students` tables

---

## Feature 2: Parent Onboarding — Contact Info Step

`InvitationAcceptPage` currently jumps to redirect after accept. Parent never gets to provide phone, address, or consent.

### Backend

Add `PATCH /api/v1/parents/me/contact` to `ParentMeController`:
- Requires `parent` role JWT
- Body: `{ phone?, address?, postalCode?, city?, shareContactInfo: bool }`
- Identifies parent via `db.Parents.FirstAsync(p => p.KeycloakSubject == User.Sub())`
- Returns 204

No new migration needed — all fields already exist on `Parent`.

### Frontend

Add `'contact-info'` state to `InvitationAcceptPage.tsx` (shown after `'success'`, before redirect):
- Fields: Telefon (optional), Adresse (optional), Postnummer (optional), By (optional), Avatar upload (optional)
- `ShareContactInfo` toggle — opt-in, default off, label: *"Tillad andre forældre at se mine kontaktoplysninger"*
- All fields optional. "Spring over" link skips entire step.
- On submit: PATCH `/api/v1/parents/me/contact` → redirect to `/foraeldrevisning/skema`

**Files:**
- `web/src/pages/InvitationAcceptPage.tsx`
- `api/Skoleoverblikket.Api/Controllers/ParentMeController.cs`

---

## Feature 3: Kontakt (Parent Directory)

Read-only list of parents, visible to all authenticated users. Respects `ShareContactInfo` consent.

### Scoping rules

| Caller role | Sees |
|---|---|
| `parent` | Only parents who share a class with caller's child(ren) AND have `ShareContactInfo=true` |
| `staff` | All parents in tenant with `ShareContactInfo=true` |
| `admin` | All parents, all fields, no consent filter |

Query to find co-class parents (parent role):
```csharp
// Find studentIds of caller's children
var myStudentIds = db.Parents
    .Where(p => p.KeycloakSubject == callerId)
    .SelectMany(p => p.Students.Select(s => s.Id));

// Find classIds those students belong to
var myClassIds = db.Students
    .Where(s => myStudentIds.Contains(s.Id))
    .SelectMany(s => s.Classes.Select(c => c.Id));

// Find all students in those classes
var coClassStudentIds = db.Students
    .Where(s => s.Classes.Any(c => myClassIds.Contains(c.Id)))
    .Select(s => s.Id);

// Find parents of those students with consent
db.Parents
    .Where(p => p.ShareContactInfo && p.Students.Any(s => coClassStudentIds.Contains(s.Id)))
```

### Backend

New file: `api/Skoleoverblikket.Api/Controllers/KontaktController.cs`

`GET /api/v1/kontakt`:
- Returns `{ id, name, phone?, address?, postalCode?, city?, avatarUrl?, studentNames[] }`
- Role-based filtering as above
- Tenant-scoped via global query filter

### Frontend

New page `/foraeldrevisning/kontakt` (parent view) and `/kontakt` (staff/admin view) — **same component** `KontaktPage.tsx`, role-conditional rendering. Component fetches `/api/v1/kontakt`; backend returns already-filtered data so no role logic needed in frontend.

- List with avatar, name, phone, address, city, child name(s)
- Search by name (client-side filter)
- No edit from this view

**Files:**
- `api/Skoleoverblikket.Api/Controllers/KontaktController.cs` (new)
- `web/src/pages/KontaktPage.tsx` (new)
- `web/src/components/Sidebar.tsx` — add "Kontakt" nav item for all roles
- `web/src/App.tsx` — add routes

No new migration needed.

---

## Feature 4: Fraværsregistrering (Absence Registration)

Parent reports child absent. Teacher confirms or dismisses. Admin sees overview.

### Data model

```csharp
// api/Skoleoverblikket.Api/Models/AbsenceReport.cs
AbsenceReport {
    Guid Id,
    Guid TenantId,
    Guid StudentId,
    Guid ReportedByParentId,
    DateOnly Date,           // single day or range start
    DateOnly? EndDate,       // null = single day
    string? Reason,          // max 500 chars
    AbsenceStatus Status,    // enum: Reported | Confirmed | Dismissed
    Guid? ConfirmedByStaffId,
    DateTimeOffset? ConfirmedAt,
    DateTimeOffset CreatedAt
}
```

### Authorization pattern for parent-to-student access

Same pattern as `ParentClassAccess`. Before reporting absence:
```csharp
var parentOwnsStudent = await db.Parents
    .AnyAsync(p => p.KeycloakSubject == User.Sub() && p.Students.Any(s => s.Id == req.StudentId), cancellationToken);
if (!parentOwnsStudent) return Forbid();
```

### Backend

New file: `api/Skoleoverblikket.Api/Controllers/AbsenceController.cs`

- `POST /api/v1/absence` — parent: `{ studentId, date, endDate?, reason? }` — validates parent owns student
- `GET /api/v1/absence?classId=&from=&to=` — admin/staff: absence list for class/date range
- `GET /api/v1/absence/mine` — parent: own reports
- `POST /api/v1/absence/{id}/confirm` — staff/admin: `Status=Confirmed`, set `ConfirmedByStaffId`, `ConfirmedAt`
- `POST /api/v1/absence/{id}/dismiss` — staff/admin: `Status=Dismissed`
- `DELETE /api/v1/absence/{id}` — parent: cancel own report (only if `Status=Reported`)

**Notification stub**: `confirm` and `dismiss` endpoints should call `notificationService.CreateAsync(...)` — but stub it with a no-op interface at first (Feature 5 implements the real service). Define `INotificationService` interface in Feature 4 so the controller can compile independently:

```csharp
// api/Skoleoverblikket.Api/Services/INotificationService.cs
Task CreateAsync(Guid recipientId, RecipientType recipientType, NotificationType type,
    Guid? referenceId, string body, CancellationToken cancellationToken);
```

Register a `NullNotificationService : INotificationService` stub in DI. Feature 5 replaces it.

### Frontend

- `/foraeldrevisning/fravaer` — parent view: list of own reports + "Indmeld fravær" button
  - Form: child picker (if multiple children), date range, optional reason
- `/fravaer` — staff/admin view:
  - Table per class with unconfirmed badge, confirm/dismiss actions
  - Basic analytics: total absences this month per class, top 5 most absent students (by count), trend sparkline (last 8 weeks)
- Sidebar: add "Fravær" for parents and staff

**Files:**
- `api/Skoleoverblikket.Api/Models/AbsenceReport.cs` (new)
- `api/Skoleoverblikket.Api/Services/INotificationService.cs` (new — interface + null stub)
- `api/Skoleoverblikket.Api/Controllers/AbsenceController.cs` (new)
- `web/src/pages/parent/ParentFravaerPage.tsx` (new)
- `web/src/pages/FravaerPage.tsx` (new)
- `web/src/components/Sidebar.tsx`
- `web/src/App.tsx`
- Migration: `AddAbsenceReport`

---

## Feature 5: Notifications

In-app bell + email for all roles. Parent-controlled opt-out per notification type.

### Data model

```csharp
// api/Skoleoverblikket.Api/Models/Notification.cs
Notification {
    Guid Id, Guid TenantId,
    Guid RecipientId, RecipientType RecipientType,  // enum: Parent | Staff | Board
    NotificationType Type,  // enum: NewMessage | NewContactMessage | WeekPlanChanged | AbsenceConfirmed | AbsenceDismissed
    Guid? ReferenceId,
    string Body,            // max 300, pre-rendered Danish text
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt
}

// api/Skoleoverblikket.Api/Models/NotificationPreference.cs
NotificationPreference {
    Guid Id, Guid TenantId,
    Guid UserId, UserType UserType,
    NotificationType Type,
    bool InApp,   // default true
    bool Email    // default true
}
```

### NotificationService (replaces null stub from Feature 4)

```csharp
// api/Skoleoverblikket.Api/Services/NotificationService.cs
// Implements INotificationService
// Constructor: AppDbContext db, ITenantContext tenant, IEmailSender email, IConfiguration config

public async Task CreateAsync(Guid recipientId, RecipientType recipientType, NotificationType type,
    Guid? referenceId, string body, CancellationToken cancellationToken)
{
    // 1. Check NotificationPreference — if none exists, default InApp=true, Email=true
    // 2. If InApp: insert Notification row
    // 3. If Email: get recipient email from db (Parent.Email or Staff.Email), send via IEmailSender
    //    Email footer: "Du modtager denne e-mail, fordi du har tilmeldt dig notifikationer.
    //                   Log ind og gå til Indstillinger → Notifikationer for at ændre dine præferencer."
}
```

Replace `NullNotificationService` registration in DI with `NotificationService`. No controller changes needed — they already inject `INotificationService`.

### Backend

New file: `api/Skoleoverblikket.Api/Controllers/NotificationsController.cs`

- `GET /api/v1/notifications` — last 50, sorted newest first
- `POST /api/v1/notifications/{id}/read`
- `POST /api/v1/notifications/read-all`
- `GET /api/v1/notification-preferences`
- `PUT /api/v1/notification-preferences` — upsert all preferences for caller

Hook into (these already exist after Feature 4's stub is replaced):
- `AbsenceController.Confirm` → notify parent: *"[StaffName] har bekræftet [StudentName]s fravær [date]"*
- `AbsenceController.Dismiss` → notify parent: *"[StaffName] har afvist [StudentName]s fravær [date]"*
- `ContactThreadsController.SendMessage` (Feature 6) → notify other party
- `MessagesController.Send` (Feature 7) → notify recipient

### Frontend

- `NotificationBell` component in app header — unread count badge, dropdown with last 10, "Marker alle som læst"
- `/indstillinger/notifikationer` — toggle grid: rows = notification types, columns = In-app / E-mail

**Files:**
- `api/Skoleoverblikket.Api/Models/Notification.cs` (new)
- `api/Skoleoverblikket.Api/Models/NotificationPreference.cs` (new)
- `api/Skoleoverblikket.Api/Services/NotificationService.cs` (new — replaces null stub)
- `api/Skoleoverblikket.Api/Controllers/NotificationsController.cs` (new)
- `web/src/components/NotificationBell.tsx` (new)
- `web/src/pages/NotificationPreferencesPage.tsx` (new)
- `web/src/App.tsx`
- Migration: `AddNotifications`

---

## Feature 6: Kontaktbog (Contact Book)

Per-child thread between the child's parents and their class teacher. Either side can initiate and reply.

### Data model

```csharp
// api/Skoleoverblikket.Api/Models/ContactThread.cs
ContactThread {
    Guid Id, Guid TenantId,
    Guid StudentId,
    DateTimeOffset CreatedAt
}
// Unique index: (TenantId, StudentId) — one thread per child

// api/Skoleoverblikket.Api/Models/ContactMessage.cs
ContactMessage {
    Guid Id, Guid TenantId,
    Guid ThreadId,
    SenderType SenderType,   // enum: Parent | Staff
    Guid SenderId,           // ParentId or StaffId
    string Body,             // max 4000
    DateTimeOffset SentAt,
    DateTimeOffset? ReadAt   // null = unread by the other party
}
```

**Thread uniqueness**: one thread per `(TenantId, StudentId)`. "Find or create" uses `FirstOrDefaultAsync` on `(TenantId, StudentId)` — add unique DB index in migration.

### Backend

New file: `api/Skoleoverblikket.Api/Controllers/ContactThreadsController.cs`

- `GET /api/v1/contact-threads` — parent: threads for own children; staff: threads for students in their classes; admin: all
- `GET /api/v1/contact-threads/{threadId}/messages` — paginated (page/pageSize), oldest first
- `POST /api/v1/contact-threads` — `{ studentId, body }` — idempotent find-or-create thread, then add first message
- `POST /api/v1/contact-threads/{threadId}/messages` — `{ body }` — add message, call `notificationService.CreateAsync` for other party
- `POST /api/v1/contact-threads/{threadId}/read` — set `ReadAt` on all unread messages in thread where `SenderId != callerId`

### Frontend

- `/foraeldrevisning/kontaktbog` — parent view: thread list (one per child with unread badge), click to open, message input at bottom
- `/kontaktbog` — staff/admin view: same layout, filtered to own classes for teachers
- Sidebar: "Kontaktbog" for all roles

**Files:**
- `api/Skoleoverblikket.Api/Models/ContactThread.cs` (new)
- `api/Skoleoverblikket.Api/Models/ContactMessage.cs` (new)
- `api/Skoleoverblikket.Api/Controllers/ContactThreadsController.cs` (new)
- `web/src/pages/parent/ParentKontaktbogPage.tsx` (new)
- `web/src/pages/KontaktbogPage.tsx` (new)
- `web/src/components/Sidebar.tsx`
- `web/src/App.tsx`
- Migration: `AddContactBook`

---

## Feature 7: Beskeder (Messages)

Flat inbox — send to any tenant user. No real-time. Separate from Kontaktbog (Kontaktbog = child-focused teacher↔parent thread; Beskeder = general user-to-user messaging).

### Data model

```csharp
// api/Skoleoverblikket.Api/Models/Message.cs
Message {
    Guid Id, Guid TenantId,
    Guid SenderId, SenderType SenderType,        // enum: Parent | Staff | Board
    Guid RecipientId, RecipientType RecipientType,
    string Subject,   // max 200
    string Body,      // max 10000
    DateTimeOffset SentAt,
    DateTimeOffset? ReadAt
}
```

### Allowed sender→recipient combinations

| Sender | Can message |
|---|---|
| Parent | Staff (always); other Parents only if target has `ShareContactInfo=true` |
| Staff | Anyone |
| Admin | Anyone, no consent filter |

### Backend

New file: `api/Skoleoverblikket.Api/Controllers/MessagesController.cs`

- `GET /api/v1/messages/inbox` — where `RecipientId == callerId`
- `GET /api/v1/messages/sent` — where `SenderId == callerId`
- `POST /api/v1/messages` — `{ recipientId, recipientType, subject, body }` — validate allowed combo, then call `notificationService.CreateAsync`
- `POST /api/v1/messages/{id}/read`
- `GET /api/v1/messages/recipients?q=` — searchable user list; parents only see others with `ShareContactInfo=true` (except themselves and admin)

### Frontend

- `/beskeder` — unified inbox (all roles): folder tabs (Indbakke / Sendt), message detail panel, compose button
- Compose modal: recipient type-ahead search (`GET /api/v1/messages/recipients?q=`), subject, body
- Sidebar: "Beskeder" for all roles with unread badge

**Files:**
- `api/Skoleoverblikket.Api/Models/Message.cs` (new)
- `api/Skoleoverblikket.Api/Controllers/MessagesController.cs` (new)
- `web/src/pages/BeskederPage.tsx` (new)
- `web/src/components/Sidebar.tsx`
- `web/src/App.tsx`
- Migration: `AddMessages`

---

## Migrations summary

| Migration | Covers |
|---|---|
| `AddAvatarUrls` | `avatar_url` on `parents`, `staff`, `students` |
| `AddAbsenceReport` | `AbsenceReport` table |
| `AddContactBook` | `ContactThread` (unique index on `TenantId+StudentId`), `ContactMessage` |
| `AddNotifications` | `Notification`, `NotificationPreference` |
| `AddMessages` | `Message` table |

Prerequisite: `AddParentModule` migration from task 17 must exist first.

---

## Implementation order

1. **Profile images** — small, self-contained, improves all later features
2. **Contact info onboarding step** — unblocks parent data completeness
3. **Kontakt directory** — no new models, data already there
4. **Fraværsregistrering** — standalone, high parent value; define `INotificationService` stub here
5. **Notifications infrastructure** — replaces stub; needed before messaging features
6. **Kontaktbog** — depends on notifications for unread alerts
7. **Beskeder** — depends on notifications; most complex UI

---

## Verification

After each feature: `/verify` then `/test`.

Key E2E paths:
- Admin invites parent → parent accepts → contact info + avatar step → lands on skema
- Parent taps "Indmeld fravær" → teacher sees unread badge → confirms → parent gets in-app + email notification
- Parent opens kontaktbog → sends message → teacher sees unread → replies → parent notified
- Parent sends besked to teacher → teacher inbox shows it → marks read
- Parent disables email notifications → sends message → no email delivered
- Kontakt page: parent with `ShareContactInfo=false` not visible to other parents; visible to admin

---

## Implementation progress

### ✅ Feature 1: Profile Images — DONE
- Added `AvatarUrl string?` to Parent, Staff, Student models
- Added presign+confirm endpoints to ParentMeController, StaffMeController (new), StudentsController
- Migration `AddAvatarUrls` applied

### ✅ Feature 2: Contact Info Onboarding — DONE
- Added `PATCH /api/v1/parents/me/contact` to ParentMeController
- Extended InvitationAcceptPage with `'contact-info'` state (phone, address, postalCode, city, shareContactInfo toggle, "Spring over" skip)
- Parent invites go to contact-info step; staff go directly to success

### ✅ Feature 3: Kontakt Directory — DONE
- Created KontaktController.cs (`GET /api/v1/kontakt`) with role-based filtering
- Created ParentDirectoryPage.tsx (search, avatar/initials, bearer fetch)
- Routes: `/foraeldrevisning/kontakt` (ParentRoute), `/foraeldre/kontakt` (auth)
- Sidebar: "Kontakt" for parents and staff (moduleGated)
- Fixed: task spec used `s.Classes.Any(...)` but Student has single `ClassId` scalar — corrected query

### ✅ Feature 4: Fraværsregistrering — DONE
- Created AbsenceReport model + migration `AddAbsenceReport`
- Created AbsenceController (POST report, GET mine, GET list, POST confirm, POST dismiss, DELETE cancel)
- Created INotificationService + NullNotificationService (stub, registered in DI)
- Extended ParentMeDto with `IReadOnlyList<ParentStudentDto> Students` (needed by absence form)
- Created ParentFravaerPage.tsx (child picker, date range, reason, status badges, cancel)
- Created FravaerPage.tsx (staff view: confirm/dismiss, date filter, pending badge)
- Routes + sidebar added

### ✅ Feature 5: Notifications — DONE
- Created `Notification` + `NotificationPreference` models
- Created `NotificationService` (replaces `NullNotificationService` in DI)
- Created `NotificationsController` (GET last 50, mark read, mark all read, preferences GET/PUT)
- Migration `AddNotifications` + `AddIndexesForAbsenceAndNotifications` applied
- `NotificationBell` component in app header with unread badge + dropdown
- `NotificationPreferencesPage` at `/indstillinger/notifikationer` (all roles)

### ✅ Feature 6: Kontaktbog — DONE
- Created `ContactThread` + `ContactMessage` models (unique index on TenantId+StudentId)
- Created `ContactThreadsController` (GET threads, GET messages, POST create, POST reply, POST read)
- Migration `AddContactBook` applied
- `KontaktbogPage.tsx` (staff/admin view) + `ParentKontaktbogPage.tsx`
- Staff directory tab added to parent contact book view
- Routes + sidebar added

### ✅ Feature 7: Beskeder — DONE
- Created `Message` model (SenderId/SenderType, RecipientId/RecipientType, Subject max 200, Body max 10000)
- Created `MessagesController` (inbox, sent, send with consent check, read, recipient search)
- Migration `AddMessages` applied
- `BeskederPage.tsx` with 3-panel layout (folder list / message list / detail), compose with recipient typeahead
- Broadcast messaging and parent directory integrated
- Routes + sidebar added

### ✅ All features complete — /verify PASSED (ESLint, TypeScript, dotnet format, dotnet build, integration tests)

---

## Feature 8: Email to Parents (Teacher/Admin Broadcast)

Teachers can email all parents of a single class. Admins can email the whole school. BCC support. Must be GDPR-compliant (consent, unsubscribe, data handling).

### Scope rules

| Sender | Recipients |
|---|---|
| `staff` | All parents of students in a class the staff member teaches |
| `admin` | All parents in tenant |

Only parents with verified email addresses receive. Respect `ShareContactInfo` — but broadcast email is always permitted (it's school-initiated, not peer-to-peer).

### Backend

New endpoint in `MessagesController` or a new `BroadcastController`:

- `POST /api/v1/broadcast-email` — `{ classId?, subject, body }` — `classId` null = whole school (admin only)
  - Resolve recipient emails from `db.Parents` filtered by class membership
  - Send via `IEmailSender` with BCC (one call per recipient, or batch with BCC list)
  - Requires `admin` or `staff` role; staff must verify they teach the target class
  - Store a record of the broadcast (audit log): sender, recipient count, subject, timestamp

### Frontend

- In `/fravaer` or a new `/udsend-email` page: compose form with class picker (staff) or all-school toggle (admin), subject, body
- Confirm step showing recipient count before send

### GDPR notes

- Unsubscribe footer required: *"Du modtager denne e-mail fra [school name]. Log ind og gå til Indstillinger for at ændre dine e-mailpræferencer."*
- No marketing or third-party use of email addresses
- Broadcast records retained for compliance audit

---

## Feature 9: Multi-Child Support

Parent with children in multiple classes gets a child switcher dropdown in the parent portal.

### What changes

- `ParentMeDto` already returns `IReadOnlyList<ParentStudentDto> Students` — data is there
- Add child picker UI in the parent sidebar/header: dropdown showing child names + class, defaults to first child
- All parent portal pages (schema, ugeplan, kontaktbog, fravær, ferieindmelding) must respect the selected child context
- Store selected child in React state (not persisted — resets on reload is fine)

### Files

- `web/src/pages/parent/` — all parent pages need to read selected student from context
- New `web/src/contexts/SelectedStudentContext.tsx` — provides `selectedStudentId`, setter, and list of children
- `web/src/components/Sidebar.tsx` — child picker in parent sidebar

No backend changes needed.

---

## Feature 10: Adressebeskyttelse (Address Protection)

**Legal requirement** — not optional. Parents with navne- og adressebeskyttelse (CPR-lovens §28) must have their address, phone, and contact info hidden from other parents, the parent directory, and any exports. Only school admin can see the full record.

### Data model

Add `bool AdresseBeskyttet` flag to `Parent` model. Default `false`.

Migration: `AddAdresseBeskyttelse`

### Enforcement rules

| Viewer | `AdresseBeskyttet = true` parent shows as |
|---|---|
| Other parents via `GET /api/v1/kontakt` | Name only (no phone, address, city) |
| Staff via `GET /api/v1/kontakt` | Name only |
| Admin | Full record (all fields visible) |
| CSV exports (ferieindmelding, etc.) | Name only — never export address/phone |
| Beskeder recipient search | Hidden from non-admin if `AdresseBeskyttet = true` |

### Backend

- `KontaktController` — filter out contact fields when `AdresseBeskyttet = true` and caller is not admin
- Any CSV export endpoints — strip address/phone for protected parents
- `ParentsController` — admin can set/unset `AdresseBeskyttet` flag via `PATCH /api/v1/parents/{id}/adresse-beskyttelse`

### Frontend

- Admin parent list: show a shield badge on protected parents
- Admin edit parent form: toggle for adressebeskyttelse with warning label
