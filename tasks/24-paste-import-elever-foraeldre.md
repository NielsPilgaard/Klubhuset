# Task 24: Paste-import of students, parents, staff, rooms, and board members

## Background

Schools typically have student/parent/staff data in KMD Elev, Excel, or Google Sheets. Instead of building IST/KMD file format integration (no demand yet), we build a paste-based import flow where admin copies data from a spreadsheet and pastes it directly into the UI.

## Scope

Import of:
- Classes (created automatically from student data)
- Students (name + class)
- Parents (name, email, phone, address, postal code, city) — up to 2 per student
- Staff (name, email, phone, role)
- Rooms (name, description, capacity)
- Board members (name, email, CanAccessTeacherData)

Out of scope: courses, schedule data (2D grid problem, separate task).

## UI structure

Each entity type gets its own tab in the import flow. Tabs: **Elever & forældre** | **Personale** | **Lokaler** | **Bestyrelsesmedlemmer**. Admin works on one tab at a time; Import button is per-tab. All tabs share the same paste-grid UX.

---

## Tab 1: Elever & forældre

### CSV columns

| Column | Model field | Required |
|---|---|---|
| Class | `Class.Name` | Yes |
| Student name | `Student.Name` | Yes |
| Parent 1 name | `Parent.Name` | No |
| Parent 1 email | `Parent.Email` | No |
| Parent 1 phone | `Parent.Phone` | No |
| Parent 1 address | `Parent.Address` | No |
| Parent 1 postal code | `Parent.PostalCode` | No |
| Parent 1 city | `Parent.City` | No |
| Parent 2 name | `Parent.Name` | No |
| Parent 2 email | `Parent.Email` | No |
| Parent 2 phone | `Parent.Phone` | No |
| Parent 2 address | `Parent.Address` | No |
| Parent 2 postal code | `Parent.PostalCode` | No |
| Parent 2 city | `Parent.City` | No |

### Import logic

#### Classes
- Upsert by `(TenantId, Name)` — create if not found, otherwise use existing

#### Students
- Skip if `(TenantId, Name, ClassId)` already exists
- No upsert key on students — name+class as identity

#### Parents
- **Upsert by `(TenantId, Email)`** — on email collision: overwrite `Name`, `Phone`, `Address`, `PostalCode`, `City` with new values. `AddressProtected` and `ShareContactInfo` are **not** overwritten on re-import (admin-controlled flags, preserve existing value). `KeycloakSubject` is never overwritten — set only on first invite acceptance.
- Existing `ParentStudent` links for the matched parent are preserved; new links from the current import row are added if not already present.
- Rows without email and without name: skip
- Rows with email but without name: accept (name is nullable on Parent model)
- `AddressProtected` always set to `false` on **first create** — admin toggles manually afterwards; not touched on update
- `ShareContactInfo` set to `false` on **first create** (default); not touched on update

#### ParentStudent links
- Create link if not already exists
- Cascade delete handled by existing FK configuration

#### Duplicate handling on re-import
- Parents: overwrite with new data (upsert by email)
- Students: skip existing (name+class match)
- Classes: use existing

### Invitations (parents)
- Import does **not** send invitation emails automatically during the import itself
- After import, a "Send invitations" step is shown in the same flow: admin sees list of all parents without `KeycloakSubject` (i.e., unactivated accounts). UI details:
  - Each parent row has a checkbox; a "Vælg alle" toggle selects/deselects all
  - List is filterable by name or email (client-side, no extra API call)
  - A summary line above the list shows "X forældre vil modtage en invitation" updating as selection changes
  - "Send invitationer" button triggers bulk send; disabled until at least one parent is selected
  - Confirmation dialog before send: "Er du sikker? X forældre modtager en invitation." with Cancel / Confirm
  - After send: success toast with count; list re-fetches and hides now-invited parents

### API

```
POST /api/v1/imports/students-and-parents
Content-Type: application/json
Authorization: Bearer <admin-token>
```

Response:
```json
{
  "classesCreated": 3,
  "studentsCreated": 47,
  "studentsSkipped": 2,
  "parentsCreated": 61,
  "parentsUpdated": 8,
  "parentStudentLinksCreated": 89,
  "warnings": [
    { "row": 12, "message": "Parent 1 email missing — parent created without login capability" }
  ]
}
```

---

## Tab 2: Personale (Staff)

### CSV columns

| Column | Model field | Required |
|---|---|---|
| Name | `Staff.Name` | Yes |
| Email | `Staff.Email` | No |
| Phone | `Staff.Phone` | No |
| Role | `Staff.Role` | No |
| Administrator | `Staff.IsAdmin` | No |

`Role` values (Danish labels in UI, enum stored in DB): `Lærer` (`Teacher`), `Pædagog` (`Aide`), `Vikar` (`Substitute`). Default to `Lærer` if blank. Case-insensitive match on import.

`Administrator` accepted values: `ja` / `nej` / `true` / `false` / `1` / `0` / blank (default `false`). Case-insensitive.

### Import logic

- **Upsert by `(TenantId, Email)`** — on email collision: overwrite `Name`, `Phone`, `Role` with new values
- Rows without email: **upsert by `(TenantId, Name)`** — on name collision: overwrite `Phone`, `Role`
- Rows without email and without name: skip
- `KeycloakSubject` never overwritten on re-import
- `IsAdmin` **is** overwritten on re-import — allows bulk-granting or revoking admin via spreadsheet
- **Exception**: if the imported row matches the currently authenticated user's `KeycloakSubject`, `IsAdmin` is not changed — prevents self-demotion
- `AvatarUrl` not imported

### Invitations (staff)
- Same post-import invitation step as parents — admin sees list of staff without `KeycloakSubject`, selects, bulk-sends invites
- Summary line: "X medarbejdere vil modtage en invitation"
- Confirmation: "Er du sikker? X medarbejdere modtager en invitation."

### API

```
POST /api/v1/imports/staff
Content-Type: application/json
Authorization: Bearer <admin-token>
```

Response:
```json
{
  "staffCreated": 18,
  "staffUpdated": 3,
  "staffSkipped": 0,
  "warnings": [
    { "row": 5, "message": "Ukendt rolle 'Rengøring' — sat til 'Andet'" }
  ]
}
```

---

## Tab 3: Lokaler (Rooms)

### CSV columns

| Column | Model field | Required |
|---|---|---|
| Name | `Room.Name` | Yes |
| Description | `Room.Description` | No |
| Capacity | `Room.Capacity` | No |

`Capacity` must be a positive integer if provided; non-numeric values produce a row warning and the field is left null.

### Import logic

- **Upsert by `(TenantId, Name)`** — on name collision: overwrite `Description`, `Capacity`
- Rows without name: skip

### API

```
POST /api/v1/imports/rooms
Content-Type: application/json
Authorization: Bearer <admin-token>
```

Response:
```json
{
  "roomsCreated": 12,
  "roomsUpdated": 2,
  "roomsSkipped": 0,
  "warnings": [
    { "row": 3, "message": "Kapacitet 'mange' er ikke et tal — felt ignoreret" }
  ]
}
```

---

## Tab 4: Bestyrelsesmedlemmer (Board members)

### CSV columns

| Column | Model field | Required |
|---|---|---|
| Name | `BoardMember.Name` | Yes |
| Email | `BoardMember.Email` | Yes |
| Adgang til lærerdata | `BoardMember.CanAccessTeacherData` | No |

`CanAccessTeacherData` accepted values: `ja` / `nej` / `true` / `false` / `1` / `0` / blank (default `false`). Case-insensitive.

### Import logic

- **Upsert by `(TenantId, Email)`** — on email collision: overwrite `Name`, `CanAccessTeacherData`
- Rows without email: skip (email is required on `BoardMember`)
- Rows without name: skip
- `KeycloakSubject` never overwritten on re-import

### Invitations (board members)
- Same post-import invitation step — admin sees board members without `KeycloakSubject`
- Summary line: "X bestyrelsesmedlemmer vil modtage en invitation"
- Confirmation: "Er du sikker? X bestyrelsesmedlemmer modtager en invitation."

### API

```
POST /api/v1/imports/board-members
Content-Type: application/json
Authorization: Bearer <admin-token>
```

Response:
```json
{
  "boardMembersCreated": 7,
  "boardMembersUpdated": 1,
  "boardMembersSkipped": 0,
  "warnings": []
}
```

---

## Shared paste-grid UX (all tabs)

1. **Grid**: Shows predefined column headers, empty rows. Admin clicks first cell in a column, pastes — fills down from that cell.
2. **Paste handling**: Custom `onPaste` handler on the grid container. Parses `clipboardData.getData('text/plain')` — Excel/Sheets always outputs tab-separated columns + newline-separated rows. Single-column paste = one tab-column, fills down into selected column starting at focused row.
3. **Live validation**: Rows with missing required fields highlighted in red. Duplicate upsert-key values (email or name) highlighted in orange with a "vil opdatere eksisterende" tooltip.
4. **Preview**: Running summary above grid — e.g. "18 medarbejdere oprettes / 3 opdateres". Updates as user edits.
5. **Confirm**: Admin clicks "Importér" — data posted to API.
6. **Post-import invitation step** (Staff, Parents, Board members tabs only): list of newly imported records without active account shown — admin selects and bulk-sends invitations.

### Frontend implementation notes
- No third-party grid library — plain React table with editable cells + custom paste handler
- Paste target: focused cell determines start row + column. Tab-delimited fills right; newline-delimited fills down.
- Rows added dynamically as paste overflows existing row count
- No dependency on react-data-grid or Handsontable
- Shared `<PasteGrid>` component parameterised by column definitions; each tab passes its own column config

## Future scope (not now)

- IST/KMD file format import (only if customers request it)
- Untis XML import for schedule data
- Import validation report downloadable as CSV
