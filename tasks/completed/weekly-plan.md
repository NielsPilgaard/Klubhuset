# Task: Weekly Plan (Ugeplan)

Allow teachers, admins, and aides to plan course content week by week, on top of the existing Schema/SchemaSlot grid. Integrates with the school calendar (see `tasks/calendar.md`) — holiday weeks are flagged automatically.

Route: `/klasser/:classId/ugeplan` (accessible from the class list).

---

## 1. New Entities

### `WeekPlan`

File: `api/Skoleoverblikket.Api/Models/WeekPlan.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skoleoverblikket.Api.Data;

namespace Skoleoverblikket.Api.Models;

public sealed class WeekPlan : ITenantScoped, IEntityTypeConfiguration<WeekPlan>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ClassId { get; set; }
    public Class Class { get; set; } = null!;
    public int IsoYear { get; set; }   // ISO year (distinct from calendar year for weeks 52/53)
    public int IsoWeek { get; set; }   // 1–53
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public ICollection<WeekPlanSlot> Slots { get; set; } = [];

    public void Configure(EntityTypeBuilder<WeekPlan> builder)
    {
        builder.Property(w => w.CreatedAt).HasDefaultValueSql("now()");
        builder.HasOne(w => w.Class).WithMany().HasForeignKey(w => w.ClassId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(w => new { w.TenantId, w.ClassId, w.IsoYear, w.IsoWeek }).IsUnique();
    }
}
```

### `WeekPlanSlot`

File: `api/Skoleoverblikket.Api/Models/WeekPlanSlot.cs`

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skoleoverblikket.Api.Data;

namespace Skoleoverblikket.Api.Models;

public sealed class WeekPlanSlot : ITenantScoped, IEntityTypeConfiguration<WeekPlanSlot>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid WeekPlanId { get; set; }
    public WeekPlan WeekPlan { get; set; } = null!;
    public Guid SchemaSlotId { get; set; }
    public SchemaSlot SchemaSlot { get; set; } = null!;

    [StringLength(8000)]
    public string? Beskrivelse { get; set; }

    [StringLength(8000)]
    public string? Lektier { get; set; }

    /// <summary>Course override for this week. Null = use SchemaSlot.Course.</summary>
    public Guid? FagSwapCourseId { get; set; }
    public Course? FagSwapCourse { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<WeekPlanSlotFile> Files { get; set; } = [];

    public void Configure(EntityTypeBuilder<WeekPlanSlot> builder)
    {
        builder.Property(s => s.UpdatedAt).HasDefaultValueSql("now()");
        builder.HasOne(s => s.WeekPlan).WithMany(w => w.Slots).HasForeignKey(s => s.WeekPlanId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(s => s.SchemaSlot).WithMany().HasForeignKey(s => s.SchemaSlotId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(s => s.FagSwapCourse).WithMany().HasForeignKey(s => s.FagSwapCourseId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(s => new { s.WeekPlanId, s.SchemaSlotId }).IsUnique();
    }
}
```

### `WeekPlanSlotFile`

File: `api/Skoleoverblikket.Api/Models/WeekPlanSlotFile.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skoleoverblikket.Api.Data;

namespace Skoleoverblikket.Api.Models;

/// <summary>Join table: attaches an existing SchoolFile to a WeekPlanSlot.</summary>
public sealed class WeekPlanSlotFile : ITenantScoped, IEntityTypeConfiguration<WeekPlanSlotFile>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid WeekPlanSlotId { get; set; }
    public WeekPlanSlot WeekPlanSlot { get; set; } = null!;
    public Guid SchoolFileId { get; set; }
    public SchoolFile SchoolFile { get; set; } = null!;

    public void Configure(EntityTypeBuilder<WeekPlanSlotFile> builder)
    {
        builder.HasOne(f => f.WeekPlanSlot).WithMany(s => s.Files).HasForeignKey(f => f.WeekPlanSlotId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(f => f.SchoolFile).WithMany().HasForeignKey(f => f.SchoolFileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(f => new { f.WeekPlanSlotId, f.SchoolFileId }).IsUnique();
    }
}
```

---

## 2. AppDbContext

File: `api/Skoleoverblikket.Api/Data/AppDbContext.cs`

Add three DbSets after `SchoolFiles`:

```csharp
public DbSet<WeekPlan> WeekPlans => Set<WeekPlan>();
public DbSet<WeekPlanSlot> WeekPlanSlots => Set<WeekPlanSlot>();
public DbSet<WeekPlanSlotFile> WeekPlanSlotFiles => Set<WeekPlanSlotFile>();
```

---

## 3. EF Core Migration

Use `scripts/add-migration.ps1 Add_WeekPlan` to generate the migration.

Never edit existing migration files.

---

## 4. API Controller

File: `api/Skoleoverblikket.Api/Controllers/WeekPlanController.cs`

Route prefix: `api/v1/classes/{classId:guid}/ugeplan`

```csharp
[ApiController]
[Route("api/v1/classes/{classId:guid}/ugeplan")]
[Authorize]
public sealed class WeekPlanController(AppDbContext db, ITenantContext tenant) : ControllerBase
```

### DTOs (nested records in the controller):

```csharp
record WeekPlanSlotFileDto(Guid Id, Guid SchoolFileId, string FileName, string Url);

record WeekPlanSlotDto(
    Guid Id,                    // WeekPlanSlot.Id (Guid.Empty if no WeekPlanSlot row yet)
    Guid SchemaSlotId,
    DayOfWeek Weekday,
    Guid TimeSlotId,
    string TimeSlotLabel,       // TimeSlot.Label ?? SortOrder.ToString()
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid CourseId,              // effective: FagSwapCourseId ?? SchemaSlot.CourseId
    string CourseName,
    Guid? OriginalCourseId,     // null when no swap
    string? OriginalCourseName,
    string? Beskrivelse,
    string? Lektier,
    IReadOnlyList<WeekPlanSlotFileDto> Files);

record WeekPlanDto(
    Guid Id,                    // Guid.Empty if no WeekPlan row exists yet
    Guid ClassId,
    int IsoYear,
    int IsoWeek,
    DateOnly WeekStartDate,     // Monday of that ISO week
    DateOnly WeekEndDate,       // Friday of that ISO week
    bool IsHolidayWeek,
    string? HolidayTitle,       // first overlapping Ferie/Lukkedag entry title, else null
    IReadOnlyList<WeekPlanSlotDto> Slots);

record UpsertWeekPlanSlotRequest(
    Guid SchemaSlotId,
    string? Beskrivelse,
    string? Lektier,
    Guid? FagSwapCourseId);

record AddFileToSlotRequest(Guid SchoolFileId);
```

### Endpoints:

**GET `/api/v1/classes/{classId}/ugeplan?isoYear=&isoWeek=`**
- Auth: `[Authorize]`
- Both params required (400 if missing; valid range: year 2020–2100, week 1–53).
- Logic:
  1. Validate `ClassId` exists under tenant (404 if not).
  2. Load active `Schema` for the class (`IsActive == true`). If none: return `WeekPlanDto` with empty `Slots`, `IsHolidayWeek = false`.
  3. Load all `SchemaSlots` for that schema including `TimeSlot` and `Course`.
  4. Compute `WeekStartDate = DateOnly.FromDateTime(ISOWeek.ToDateTime(isoYear, isoWeek, DayOfWeek.Monday))`, `WeekEndDate = WeekStartDate.AddDays(4)`.
  5. Query `CalendarEntries` for overlapping Ferie/Lukkedag:
     ```csharp
     var holidays = await db.CalendarEntries
         .AsNoTracking()
         .Where(e =>
             (e.Type == CalendarEntryType.Ferie || e.Type == CalendarEntryType.Lukkedag) &&
             e.StartDate <= weekEnd && e.EndDate >= weekStart)
         .OrderBy(e => e.StartDate)
         .ToListAsync(cancellationToken);
     ```
  6. Load `WeekPlan` for `(classId, isoYear, isoWeek)` with `WeekPlanSlots` + `Files` + `FagSwapCourse`. If none exists, continue with null (do NOT create on GET).
  7. Build one `WeekPlanSlotDto` per `SchemaSlot`, merging with `WeekPlanSlot` data when available.
- Returns `200 WeekPlanDto`.

**PUT `/api/v1/classes/{classId}/ugeplan/slots?isoYear=&isoWeek=`**
- Auth: `[Authorize]` (all authenticated — teachers, aides, admins can write)
- Body: `UpsertWeekPlanSlotRequest`
- Logic:
  1. Validate `ClassId` belongs to tenant (404 if not).
  2. Validate `SchemaSlotId` belongs to the active schema of this class (400 if not).
  3. If `FagSwapCourseId` provided, validate it belongs to tenant (400 if not).
  4. Find or create `WeekPlan` for `(classId, isoYear, isoWeek)` — **create here if it doesn't exist** (`TenantId = tenant.TenantId`).
  5. Find or create `WeekPlanSlot` for `(weekPlanId, schemaSlotId)` (`TenantId = tenant.TenantId`).
  6. Update `Beskrivelse`, `Lektier`, `FagSwapCourseId`. Set `UpdatedAt = DateTimeOffset.UtcNow`.
  7. `SaveChangesAsync`. Return `200 WeekPlanSlotDto`.

**POST `/api/v1/classes/{classId}/ugeplan/slots/{slotId:guid}/files`**
- Auth: `[Authorize]`
- Body: `AddFileToSlotRequest`
- Validate `WeekPlanSlotId` belongs to a WeekPlan under this class+tenant (404 if not).
- Validate `SchoolFileId` exists under tenant (400 if not).
- Create `WeekPlanSlotFile`. Set `TenantId = tenant.TenantId`.
- On unique index violation (already linked): return `409 Problem("Filen er allerede tilknyttet denne lektion")`.
- Returns `201 WeekPlanSlotFileDto`.

**DELETE `/api/v1/classes/{classId}/ugeplan/slots/{slotId:guid}/files/{fileId:guid}`**
- Auth: `[Authorize]`
- Removes the `WeekPlanSlotFile` link only (not the `SchoolFile` itself). Returns `204`. `404` if not found.

---

## 5. Frontend

### Route

File: `web/src/App.tsx`

Add inside the authenticated `<Route path="/" element={<Layout />}>` block:

```tsx
<Route path="klasser/:classId/ugeplan" element={<WeekPlanPage />} />
```

Add import: `import WeekPlanPage from './pages/WeekPlanPage'`

### ClassesPage — "Ugeplan" button

File: `web/src/pages/ClassesPage.tsx`

Add a "Ugeplan" button per class row (at the class level, not per schema — Ugeplan always reads the active schema). Place it in the class row action area alongside the existing controls:

```tsx
<button
  onClick={() => navigate(`/klasser/${cls.id}/ugeplan`)}
  className="px-3 py-1.5 text-xs font-medium text-brand-600 bg-brand-50 hover:bg-brand-100 rounded-md transition-colors"
>
  Ugeplan
</button>
```

### Page: `WeekPlanPage.tsx`

File: `web/src/pages/WeekPlanPage.tsx`

`usePageTitle('Ugeplan')`. Full-height flex column: `flex flex-col h-full`.

#### Top bar (`shrink-0 bg-white border-b border-gray-200 px-4 lg:px-6 py-3 flex items-center justify-between`):

- Left: `Link to="/klasser"` back arrow (chevron-left SVG) + class name + "· Ugeplan" heading
- Center: week navigator
  - "← Forrige uge" button (`text-sm text-gray-600 hover:text-gray-900 px-2 py-1 rounded hover:bg-gray-100`)
  - "Uge {isoWeek}, {isoYear}" bold label (`text-sm font-semibold text-gray-900 mx-3`)
  - "Næste uge →" button (same style)
  - "Denne uge" small link button (`text-xs text-brand-600 hover:underline ml-2`)
- Right: empty for now

#### ISO week helpers (inside the file, no date library needed):

```typescript
function getISOWeek(date: Date): number {
  const d = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()))
  const dayNum = d.getUTCDay() || 7
  d.setUTCDate(d.getUTCDate() + 4 - dayNum)
  const yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1))
  return Math.ceil((((d.getTime() - yearStart.getTime()) / 86400000) + 1) / 7)
}

function getISOWeekYear(date: Date): number {
  const d = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()))
  d.setUTCDate(d.getUTCDate() + 4 - (d.getUTCDay() || 7))
  return d.getUTCFullYear()
}
```

Week navigation state:
```typescript
const [isoYear, setIsoYear] = useState(() => getISOWeekYear(new Date()))
const [isoWeek, setIsoWeek] = useState(() => getISOWeek(new Date()))
```

For `prevWeek`/`nextWeek`: decrement/increment `isoWeek`, roll over year using `ISOWeek.getWeeksInYear`-equivalent logic (week 53 exists only in some years — if incrementing week 52 and next year's Jan 1 is in week 1, go to week 1 of next year).

#### Holiday banner:

Shown below top bar when `weekPlanData?.isHolidayWeek`:

```tsx
<div className="shrink-0 bg-blue-50 border-b border-blue-200 px-4 lg:px-6 py-2">
  <span className="text-blue-700 text-sm font-medium">Feriuge — {weekPlanData.holidayTitle}</span>
</div>
```

#### Grid area (`flex-1 overflow-y-auto`):

CSS grid: `grid grid-cols-[auto_1fr_1fr_1fr_1fr_1fr]` (time label column + Mon–Fri).

**Header row**: empty time cell, then 5 day columns showing weekday name + actual date (`WeekStartDate + offset`, formatted as `{dd}. {MMM}` in da-DK locale).

```typescript
const WEEKDAYS = ['Mandag', 'Tirsdag', 'Onsdag', 'Torsdag', 'Fredag']
// Weekday DayOfWeek values: Monday=1 ... Friday=5
```

**Rows**: one per unique `TimeSlotId` in `weekPlanData.slots`, ordered by `StartTime`. Show the time label in the first column (`text-xs text-gray-500 p-2 font-mono`).

**Slot cell** (`bg-white border border-gray-100 p-2 min-h-[80px] relative group`):

When no schema slot exists for that weekday+timeSlot: render `bg-gray-50` empty cell.

When a slot exists:
- Course badge: small pill using same `COURSE_COLORS` cycle from `SchemaBuilderPage` (keyed by `courseId`)
- If `FagSwap`: strikethrough original course name in `text-xs text-gray-400 line-through`, then effective course in `text-xs font-semibold text-brand-700`
- `Beskrivelse` in `text-xs text-gray-700 line-clamp-2 mt-1`
- If `Lektier`: pencil icon + `text-xs text-amber-700`
- If files: paperclip icon + count badge (`text-xs text-gray-500`)
- Bottom-right: edit button — `opacity-0 group-hover:opacity-100 transition-opacity absolute bottom-1 right-1 p-1 rounded text-gray-400 hover:text-gray-700` with pencil SVG. Clicking opens edit modal.

**Holiday week**: when `isHolidayWeek`, add `pointer-events-none opacity-60` to the grid area.

#### Slot edit modal:

`fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40` → `bg-white rounded-2xl shadow-xl w-full max-w-lg`

Title: `"Rediger lektion — {courseName}, {weekdayLabel}"`

Fields:

1. **Beskrivelse** — `<textarea rows={4}` — label "Beskrivelse", placeholder "Hvad skal der ske i denne lektion?"
2. **Lektier** — `<textarea rows={3}` — label "Lektier", placeholder "Opgaver til næste gang..."
3. **Fagbytte** — `<select>` loaded from `useQuery(['courses'])`. First option: `"Intet fagbytte (brug skemaets fag)"` (value = `""`). Then all tenant courses. Pre-select active swap if any.
4. **Filer** — multi-select checklist of all tenant `SchoolFile`s (from `useQuery(['files'])`). Each row: `<label>` with checkbox, file name, size in `text-xs text-gray-500`. Pre-checked = files currently in `slot.files`. Checkbox change fires the add/remove mutation immediately (fire-and-forget, not bundled into main save).

Footer: "Gem" button (`bg-brand-600 text-white rounded-lg px-4 py-2 text-sm font-medium`) + "Annuller" button.

On "Gem": call PUT upsert mutation. On success: invalidate `['weekplan', classId, isoYear, isoWeek]` and close modal.

#### React Query hooks:

```typescript
const { data: weekPlanData, isLoading } = useQuery({
  queryKey: ['weekplan', classId, isoYear, isoWeek],
  queryFn: () => api.get<WeekPlanDto>(`/classes/${classId}/ugeplan?isoYear=${isoYear}&isoWeek=${isoWeek}`),
  enabled: !!classId,
})

const { data: courses } = useQuery({
  queryKey: ['courses'],
  queryFn: () => api.get<CourseDto[]>('/courses'),
})

const { data: files } = useQuery({
  queryKey: ['files'],
  queryFn: () => api.get<FileDto[]>('/files'),
})

const upsertSlotMutation = useMutation({
  mutationFn: (req: UpsertWeekPlanSlotRequest) =>
    api.put(`/classes/${classId}/ugeplan/slots?isoYear=${isoYear}&isoWeek=${isoWeek}`, req),
  onSuccess: () => qc.invalidateQueries({ queryKey: ['weekplan', classId, isoYear, isoWeek] }),
})

const addFileMutation = useMutation({
  mutationFn: ({ slotId, schoolFileId }: { slotId: string; schoolFileId: string }) =>
    api.post(`/classes/${classId}/ugeplan/slots/${slotId}/files`, { schoolFileId }),
  onSuccess: () => qc.invalidateQueries({ queryKey: ['weekplan', classId, isoYear, isoWeek] }),
})

const removeFileMutation = useMutation({
  mutationFn: ({ slotId, fileId }: { slotId: string; fileId: string }) =>
    api.delete(`/classes/${classId}/ugeplan/slots/${slotId}/files/${fileId}`),
  onSuccess: () => qc.invalidateQueries({ queryKey: ['weekplan', classId, isoYear, isoWeek] }),
})
```

**Local types** (until OpenAPI is regenerated):
```typescript
interface WeekPlanSlotFileDto { id: string; schoolFileId: string; fileName: string; url: string }
interface WeekPlanSlotDto {
  id: string; schemaSlotId: string; weekday: number; timeSlotId: string
  timeSlotLabel: string; startTime: string; endTime: string  // TimeOnly serializes as "HH:mm:ss"
  courseId: string; courseName: string
  originalCourseId: string | null; originalCourseName: string | null
  beskrivelse: string | null; lektier: string | null
  files: WeekPlanSlotFileDto[]
}
interface WeekPlanDto {
  id: string; classId: string; isoYear: number; isoWeek: number
  weekStartDate: string; weekEndDate: string
  isHolidayWeek: boolean; holidayTitle: string | null
  slots: WeekPlanSlotDto[]
}
```

After `dotnet build` regenerates `web/src/api/schema.d.ts`, update `client.ts` to re-export from `components['schemas']['...']`.

---

## 6. Integration Tests

File: `api/tests/Skoleoverblikket.Api.IntegrationTests/WeekPlanTests.cs`

Follow the pattern of existing `RoomsCrudTests.cs` (TUnit, no mocking, real PostgreSQL via Testcontainers).

Tests:
- `GetWeekPlan_NoActiveSchema_ReturnsEmptySlots`
- `GetWeekPlan_WithActiveSchema_ReturnsSlotCount`
- `GetWeekPlan_HolidayWeek_IsHolidayWeekTrue` — seed a `CalendarEntry` of type `Ferie` spanning the test week
- `UpsertSlot_CreatesBeskrivelse_AndReturnsMergedSlot`
- `UpsertSlot_WithFagSwap_ReturnsOverriddenCourseName`
- `AddFile_ThenRemoveFile_RoundTrips`
- `AddFile_Duplicate_Returns409`
- `TenantIsolation_WeekPlanNotVisibleToOtherTenant`

Add to `TestDataBuilder`:
```csharp
public static async Task<CalendarEntry> CreateCalendarEntryAsync(
    IServiceProvider services, Guid tenantId,
    CalendarEntryType type, string title, DateOnly startDate, DateOnly endDate)

public static async Task<WeekPlan> CreateWeekPlanAsync(
    IServiceProvider services, Guid tenantId, Guid classId, int isoYear, int isoWeek)
```

---

## 7. Constraints

- `TenantId` always comes from `tenant.TenantId` — never from request body or URL.
- All three entities implement `ITenantScoped` — global query filter applies automatically.
- Never edit existing migration files.
- `ISOWeek` utilities are in `System.Globalization` — no extra NuGet packages needed.
- GET endpoints: `[Authorize]`. PUT/POST/DELETE: `[Authorize]` (all authenticated users — teachers and aides can write their own plans, not just admins).
- Do NOT create `WeekPlan` on GET — create it lazily on the first PUT.
- Integration with calendar is a pure LINQ query — no FK between `WeekPlan` and `CalendarEntry`. The `WeekPlanController` queries `CalendarEntries` directly to compute `IsHolidayWeek` and `HolidayTitle`.

---

## Integration with Calendar

`WeekPlanController.GetWeekPlan` computes the Mon–Fri `DateOnly` range for the requested ISO week, then queries `CalendarEntries` for overlapping `Ferie`/`Lukkedag` entries. The result populates `WeekPlanDto.IsHolidayWeek` and `WeekPlanDto.HolidayTitle`. The frontend reads these fields to show/hide the holiday banner and to grey out the grid. The frontend does **not** query the calendar endpoint directly.

Dependency: the Calendar feature (`tasks/calendar.md`) must be implemented first, as it creates the `CalendarEntries` table this feature reads.
