# Task: Fix Print / Udskriv Klasse

Fix the class schedule print view ("Udskriv klasse") which currently shows only a few time slots with no course or teacher data.

---

## Problem

The print view for a class schedule (`udskriv klasse`) is broken:
- Only a few time slots are shown (truncated/incomplete).
- No courses (fag) are displayed in the slots.
- No teachers (lærere) are shown.

The print output should look like the schema builder view but formatted for print: full week grid, all time slots, courses and teachers in each slot.

---

## Investigation

Find the print view component/page (search for `udskriv`, `print`, or similar in `web/src/`). Identify:
1. What API endpoint it calls to fetch schema data.
2. Whether the API response includes course and teacher data in the slots (check `SchemaSlot` includes).
3. Whether the frontend is rendering the course/teacher fields at all.

---

## Fix

### API side

Ensure the schema/slot endpoint used by print includes:
- `CourseId` + `CourseName` (or the full `Course` object)
- `TeacherId` + `TeacherName` (or `Teacher.Initials` + `Teacher.FullName`)
- `RoomId` + `RoomName`
- All time slots for the schema — verify the query is not accidentally paginated or limited.

If the query uses `.Select()`, make sure it projects course and teacher navigation properties. If it uses `.Include()`, verify `Include(s => s.Course)` and `Include(s => s.Teacher)` are present.

### Frontend side

In the print component, render each slot cell with:
- Course name (fag)
- Teacher initials or name
- Room name (lokale), if space allows

Use a clean print-friendly layout: white background, black text, grid with borders. `@media print` or a dedicated print stylesheet via Tailwind's `print:` variants.

Ensure all time slots from the schema are rendered — check whether the component iterates over all slots or uses a subset.

---

## Constraints

- The print view must work for all screen sizes when opened in a browser tab, and be properly formatted when `Ctrl+P` / browser print is triggered.
- All schema queries must be tenant-scoped.
- Do not add mock data — always fetch from the real API.
