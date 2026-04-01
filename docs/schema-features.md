# Schema features

This document describes the schema planner (skemaplanlægger) feature in detail. See [PRD.md](PRD.md) for context.

---

## Structure

### School-level defaults

A school defines a **time slot template** (lektionsstruktur) that serves as the default for all classes:

- Default lesson duration (e.g. 45 minutes)
- Break schedule (optional — some schools have fixed breaks between lessons, others let teachers manage breaks as needed)
- School day start time (e.g. 8:00)
- Days of the week included in the schema (typically Monday–Friday)

This template is set up during onboarding via a guided wizard.

### Per-class schemas

Each **class** (klasse, e.g. 2.b, 9.a) has its own weekly schema (skema). The schema is a grid of time slots × weekdays, where each cell can be assigned a course, a teacher, and a room.

- Time slots are **inherited from the school's default template** but can be overridden per class.
- Classes are NOT forced to align. One class can have a session at 8:10–8:55 while another has 8:15–9:00. There is no global constraint that all classes share the same time grid.
- Each school typically has one active schema set per term (semester), but can create a new one at any time.

---

## Time slot wizard (onboarding)

When a school is first set up, the admin is guided through a wizard to define the default time slot structure:

1. **Lesson duration**: "Hvor lange er jeres lektioner?" (e.g. 45 min, 50 min, 60 min)
2. **First break**: "Hvor lang er første pause?" (e.g. 10 min, 15 min, or "Vi har ingen faste pauser")
3. **Subsequent breaks**: "Er der flere pauser? Hvornår og hvor lange?"
4. **School day bounds**: "Hvornår starter og slutter skoledagen?"

The wizard generates a default weekly time slot grid. This grid can be edited later. Per-class overrides are supported at any time.

Schools that don't use fixed breaks can skip the break questions entirely — their time slots will be contiguous lessons with no break slots.

---

## Entities

| Entity | Danish term | Description |
|---|---|---|
| Class | Klasse | A group of students (e.g. 2.b, 5.a, 9.a). Each class has its own schema. |
| Course | Fag | A subject (e.g. dansk, matematik, idræt). Courses are taught by teachers. |
| Teacher | Lærer | A staff member who teaches one or more courses. |
| Aide | Pædagog / vikar | A support staff member who assists in lessons. |
| Room | Lokale | A physical location (e.g. "Lokale 12", "Gymnastiksalen", "Musiklokalet"). |
| Time slot | Lektion | A time period in the weekly grid (e.g. Monday 8:00–8:45). |

---

## Schema builder

The schema builder is the primary admin interface. It displays a weekly grid for a selected class.

### Grid layout

- **Columns**: weekdays (Monday–Friday, configurable)
- **Rows**: time slots (inherited from school default or overridden per class)
- **Cells**: each cell represents one lesson. A cell is assigned a **course**, a **teacher**, and optionally a **room** and/or an **aide**.

### Assigning lessons

Admin fills cells by selecting:
1. Course (required)
2. Teacher (required)
3. Room (optional but recommended)
4. Aide (optional)

### Conflict detection

Conflicts are detected **in real time** as the admin builds the schema. A conflict is shown immediately — not after saving.

| Conflict type | Description |
|---|---|
| Teacher double-booking | A teacher is assigned to two different classes at the same time. |
| Room double-booking | A room is assigned to two different classes at the same time. |
| Aide double-booking | An aide is assigned to two different classes at the same time. |

Conflicts are shown visually on the grid (highlight the conflicting cell) and in a summary panel. The admin must resolve conflicts before the schema can be marked as complete.

**Note on time slot alignment**: because classes can have different time slot structures, a "same time" conflict means any overlap in actual clock time — not just matching slot indices.

---

## Printable views

The platform generates print-friendly schema views:

| View | Description |
|---|---|
| Per-class schema | Weekly timetable for one class. Intended for posting in classrooms. |
| Per-teacher schema | Weekly timetable for one teacher across all their classes. |
| Per-room schema | Weekly timetable for one room showing all classes using it. |

These views must:
- Be clean enough to print on A4 paper
- Work as a standalone document (no app needed to read them)
- Include the school name, term, and generation date

---

## File explorer

Files (PDFs, documents, images) can be uploaded and linked to courses.

- **Upload**: admin or teacher uploads a file
- **Link to course**: each file is associated with one or more courses
- **Browse**: files can be browsed by course
- **Storage**: OVHCloud Object Storage (S3-compatible, EU region)
- **Limits**: storage quota per school tier (100 GB on Basis, 1000 GB on Skole+)

---

## Stats

| Stat | Description |
|---|---|
| Hours per course per class | How many hours of dansk does 5.a have this week? Towards minimumstimetal. |
| Hours per teacher | How many teaching hours does Thomas have this week? |
| Hours per aide | How many hours is Mikkel scheduled for? |
| Unassigned slots | Which class has time slots without a course/teacher assigned? |

Stats are shown on the admin dashboard and are exportable.

---

## Permissions

| Action | Admin | Teacher | Aide |
|---|---|---|---|
| View all schemas | ✅ | ❌ | ❌ |
| View own schedule | ✅ | ✅ | ✅ |
| View room schedules | ✅ | ✅ | ❌ |
| Edit schema (assign lessons) | ✅ | ❌ | ❌ |
| Upload files | ✅ | ✅ | ❌ |
| View files | ✅ | ✅ | ✅ |
| View stats | ✅ | Own hours only | Own hours only |
| Print schemas | ✅ | Own schedule | Own schedule |
| Manage staff | ✅ | ❌ | ❌ |
| Manage classes/courses | ✅ | ❌ | ❌ |
