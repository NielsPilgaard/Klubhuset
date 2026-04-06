# Task: Fix Schema Builder UX

Fix several UX issues in the class schema builder.

---

## 1. Enter key submits "add time slot" form

**Location**: The "add time slot" modal or inline form in the schema builder.

When the user has filled out a new time slot and presses `Enter`, the form should submit. Currently it does not.

**Fix**: Add `onKeyDown` handler (or use `type="submit"` on the button and wrap in a `<form>` with `onSubmit`). Ensure Enter triggers the same action as clicking the save button.

---

## 2. Slot fill-out: dropdowns are tedious

**Location**: The modal/form for adding a course, teacher, room, aide to a time slot.

Plain `<select>` dropdowns are slow for Hanne who needs to fill out many slots. Replace with a **searchable combobox** (type-to-filter). 

Implementation options (pick the simplest that works):
- Use a `<datalist>` with `<input list="...">` — zero dependencies, browser-native, filters as you type.
- Or a simple controlled `<input>` with a filtered dropdown list below it (no external library needed — keep it simple).

The combobox must:
- Show all options by default when focused.
- Filter options as the user types (case-insensitive match on name).
- Select an option on click or Enter.
- Show the selected item's name in the input after selection.

Apply to: fag (course), lærer (teacher), lokale (room), pædagog (aide) fields.

---

## 3. Teacher initials on schema modules

**Location**: Each filled slot ("modul") in the schema builder grid.

Show the teacher's initials on the module chip/card so younger students can recognize who's teaching. Initials should be short (2–3 characters), displayed below or next to the course name.

- If `Teacher.Initials` exists as a field, use it.
- If not, derive from the first letters of the teacher's name.
- If no teacher is assigned, show nothing.

Keep the module compact — initials in `text-xs text-gray-500` or similar.

---

## 4. Change timetable layout per class / schema

**Location**: Schema settings / schema detail view.

Currently the time table layout (time slots, break structure) is shared across all schemas or cannot be changed per class. Allow each schema to have its own layout.

This likely means: the `Schema` entity should have its own set of `TimeSlot` records, not inherit from a global set. 

**Investigation required**: Check how `TimeSlot` is currently associated (global table or per-schema FK). If it's global, add a `SchemaId` FK to `TimeSlot` so each schema has its own time slots. Generate a new migration.

After the model change:
- The "lektioner" (time slots) management must be scoped to the current schema, not global.
- When creating a new schema, offer to copy the time slot layout from another schema or start fresh.

---

## 5. Conflict management: show exact conflicts

**Location**: Conflict indicator in the schema builder.

Currently conflicts are detected but the user is only told "there is a conflict" without specifics. Show exactly which conflict(s) exist so Hanne can fix them.

For each conflict, display:
- Who/what is double-booked: teacher name, room name, or class name
- Which time slot (day + time)
- The two clashing entries

Display in a panel, tooltip, or modal. Format for readability — not raw data.

Example: _"Lærer Morten Nielsen er booket to gange: Tirsdag 10:00–11:00 (3.A Dansk og 4.B Matematik)"_

---

## Constraints

- All dropdowns/comboboxes: Tailwind only, no external component libraries.
- All schema/slot queries: tenant-scoped.
- Do not edit existing EF Core migration files — generate new ones for schema changes.
- `data-testid` on all new interactive elements.
