# Task 25: Schema Import via AI

## Context

Schools switching to Skoleoverblikket have existing schedules in other tools — exported as Excel, CSV, or PDF. Manually re-entering every slot is a barrier to adoption. This feature lets admins upload their current schedule file and get slot suggestions back — same ghost-slot UX as task 21 (accept individually or all at once).

**No FastAPI sidecar.** Pure .NET: extract text from file, send to Alexandra AI LLM, parse structured response. Same AI setup as task 21.

---

## Supported input formats

| Format | Extraction library | Notes |
|--------|--------------------|-------|
| CSV | Built-in (`StreamReader`) | Raw text → LLM |
| Excel (.xlsx / .xls) | **ClosedXML** (already in csproj) | Read cells as text table → LLM |
| PDF | **PdfPig** (add to csproj, Apache 2.0) | Extract text per page → LLM |

Images NOT supported — too expensive (would require vision LLM).

---

## .NET Changes

### New NuGet

```xml
<PackageReference Include="UglyToad.PdfPig" Version="..." />
```

Only addition needed. ClosedXML already present.

### New files

- `Services/SchemaImportService.cs` — text extraction per format + Alexandra AI call + fuzzy-match
- `Services/SchemaImportTextExtractor.cs` — per-format extraction logic (CSV, Excel, PDF)

### Modified files

- `Controllers/SchemasController.cs` — add `POST .../import` endpoint
- `Services/ServicesExtensions.cs` — register `SchemaImportService`
- `appsettings.json` — no new config needed (reuses `AlexandraAi` section from task 21)
- `Skoleoverblikket.Api.csproj` — add PdfPig

### New endpoint

```
POST /api/v1/classes/{classId}/schemas/{schemaId}/import
Content-Type: multipart/form-data
Body: file (csv/xlsx/xls/pdf, max 10 MB)
```

Response:
```csharp
record SchemaImportSuggestionResponse(
    IReadOnlyList<ImportedSlotSuggestion> Slots,
    IReadOnlyList<string> UnresolvedCourseNames,
    IReadOnlyList<string> UnresolvedTeacherNames,
    IReadOnlyList<string> UnresolvedRoomNames
);

record ImportedSlotSuggestion(
    Guid? TimeSlotId,       // null = no matching timeslot found
    DayOfWeek Weekday,
    Guid? CourseId,
    string CourseName,      // raw name from import (for unresolved display)
    Guid? TeacherId,
    string TeacherName,
    Guid? RoomId,
    string? RoomName
);
```

---

## Service flow (`SchemaImportService`)

1. Extract text from uploaded file (`SchemaImportTextExtractor`)
2. Load tenant context: courses, staff, rooms, timeslots for this schema
3. Build LLM prompt:
   - Extracted text
   - Known entity names as hints
   - Instruction to output structured JSON (`ImportedSlotSuggestion[]` shape)
4. Call Alexandra AI via `IChatClient` (same setup as task 21, `GetResponseAsync<T>`)
5. Fuzzy-match LLM-returned names to entity IDs
6. Return `SchemaImportSuggestionResponse`

### Text extraction per format

```csharp
// CSV
using var reader = new StreamReader(stream);
return await reader.ReadToEndAsync();

// Excel (ClosedXML — already referenced)
using var workbook = new XLWorkbook(stream);
// iterate sheets → rows → cells → build text table

// PDF (PdfPig)
using var doc = PdfDocument.Open(stream);
// concat text from all pages
```

### Fuzzy matching

Normalize: lowercase, trim, strip punctuation. Then:
1. Exact match
2. Contains match  
3. Levenshtein ≤ 2 (implement inline, ~15 lines, no new NuGet)

---

## LLM prompt strategy

Send extracted text + context:

```
Du er en assistent der udtrækker skemainformation fra tekst.

Eksisterende fag: [liste]
Eksisterende lærere: [liste]  
Eksisterende lokaler: [liste]
Eksisterende tidspunkter: [liste med labels]

Udtræk alle skemaposter fra denne tekst og returner JSON:
[{ weekday: 0-6, timeslot_label: "...", course_name: "...", teacher_name: "...", room_name: "..." }]

Brug navne fra de eksisterende lister hvis muligt. Tekst:
[extracted text]
```

Use `Microsoft.Extensions.AI` `GetResponseAsync<ImportedSlotDto[]>` for typed output (same as task 21 pattern).

---

## Frontend

**New**: `SchemaImportModal.tsx`  
**Modified**: `SchemaBuilderPage.tsx` — add "Importer skema" button (admin only)

Flow:
1. "Importer skema" button opens modal
2. File picker — accepts `.csv,.xlsx,.xls,.pdf`
3. Upload via `POST /import` (multipart, no presign needed — file goes directly to API, not S3)
4. Loading state ~5–15 s: spinner + "Læser skema..."
5. Ghost slots rendered same as task 21 suggestions
6. Amber warning banner listing unresolved names
7. Accept individually or "Anvend alle" → calls existing `PUT .../slots`

**No S3 presign needed** — file goes directly to the API endpoint, processed in memory, discarded. No persistence required.

Use generated typed API client after spec updates.

---

## Task 21 dependency

Task 21 and task 25 share:
- Same Alexandra AI `IChatClient` config
- Same ghost-slot frontend component (task 21 defines it, task 25 reuses)

If task 21 not yet implemented when this is built, we need to add the AI client setup as part of this task.

---

## Aspire / infrastructure

No changes — no new services, no new containers.

---

## Verification

1. Upload a simple CSV with schedule data → endpoint returns correct slots
2. Upload an Excel file → same
3. Upload a PDF with text → same
4. Unresolved names appear in response + displayed in frontend banner
5. Accept all → slots appear in schema grid
6. `/verify` passes
7. `/test` Playwright: upload → review ghost slots → accept all
