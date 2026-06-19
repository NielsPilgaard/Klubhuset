# Task 24: Paste-import of students and parents

## Background

Schools typically have student and parent data in KMD Elev, Excel, or Google Sheets. Instead of building IST/KMD file format integration (no demand yet), we build a paste-based import flow where admin copies data from a spreadsheet and pastes it directly into the UI.

## Scope

Import of:
- Classes (created automatically from student data)
- Students (name + class)
- Parents (name, email, phone, address, postal code, city) — up to 2 per student

Out of scope: staff, rooms, courses, schedule data (2D grid problem, separate task).

## CSV columns (tab- or comma-separated from Excel/Sheets)

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

Column order is not fixed — user pastes column by column into a predefined grid (see flow below).

## Import logic

### Classes
- Upsert by `(TenantId, Name)` — create if not found, otherwise use existing

### Students
- Skip if `(TenantId, Name, ClassId)` already exists
- No upsert key on students — name+class as identity

### Parents
- **Upsert by `(TenantId, Email)`** — on email collision: overwrite `Name`, `Phone`, `Address`, `PostalCode`, `City` with new values. `AddressProtected` and `ShareContactInfo` are **not** overwritten on re-import (admin-controlled flags, preserve existing value). `KeycloakSubject` is never overwritten — set only on first invite acceptance.
- Existing `ParentStudent` links for the matched parent are preserved; new links from the current import row are added if not already present.
- Rows without email and without name: skip
- Rows with email but without name: accept (name is nullable on Parent model)
- `AddressProtected` always set to `false` on **first create** — admin toggles manually afterwards; not touched on update
- `ShareContactInfo` set to `false` on **first create** (default); not touched on update

### ParentStudent links
- Create link if not already exists
- Cascade delete handled by existing FK configuration

### Invitations
- Import does **not** send invitation emails automatically during the import itself
- After import, a "Send invitations" step is shown in the same flow: admin sees list of all parents without `KeycloakSubject` (i.e., unactivated accounts). UI details:
  - Each parent row has a checkbox; a "Vælg alle" toggle selects/deselects all
  - List is filterable by name or email (client-side, no extra API call)
  - A summary line above the list shows "X forældre vil modtage en invitation" updating as selection changes
  - "Send invitationer" button triggers bulk send; disabled until at least one parent is selected
  - Confirmation dialog before send: "Er du sikker? X forældre modtager en invitation." with Cancel / Confirm
  - After send: success toast with count; list re-fetches and hides now-invited parents

### Duplicate handling on re-import
- Parents: overwrite with new data (upsert by email)
- Students: skip existing (name+class match)
- Classes: use existing

## Import flow in UI

Grid with predefined column headers is shown immediately — no textarea step, no column mapping step. Admin works column by column:

1. **Grid**: Shows all 14 columns as headers, empty rows. Admin clicks first cell in a column, pastes — fills down from that cell.
2. **Paste handling**: Custom `onPaste` handler on the grid container. Parses `clipboardData.getData('text/plain')` — Excel/Sheets always outputs tab-separated columns + newline-separated rows. Single-column paste (e.g. 100 student names) = one tab-column, fills down into selected column starting at focused row.
3. **Live validation**: Rows with missing Class or Student name highlighted in red. Duplicate parent emails highlighted in orange.
4. **Preview**: Running summary above grid — "3 classes, 47 students, 89 parents will be created / 8 parents will be updated". Updates as user edits.
5. **Confirm**: Admin clicks "Import" — data posted to API.
6. **Bulk invitation**: After import, list of parents without active account shown — admin selects and sends invitations in bulk.

### Frontend implementation notes
- No third-party grid library — plain React table with editable cells + custom paste handler (~100 lines)
- Paste target: focused cell determines start row + column. Tab-delimited data fills right; newline-delimited fills down.
- Rows added dynamically as paste overflows existing row count
- No dependency on react-data-grid or Handsontable (react-data-grid column paste is "wontfix")

## API

```
POST /api/v1/imports/students-and-parents
Content-Type: application/json
Authorization: Bearer <admin-token>
```

Request body: parsed rows from frontend (after column mapping).

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

## Future scope (not now)

- IST/KMD file format import (only if customers request it)
- Untis XML import for schedule data
- Import of staff and rooms (separate paste flow, lower priority)
