# Task 20 — Course Selection Sidebar

## Context
When building a class schema (ugeskema) in the schema editor, teachers/admins currently must remember course IDs or navigate away to find courses. Docendo solves this with a persistent sidebar listing all tenant courses. We want the same UX in Skoleoverblikket's schema editor.

The sidebar appears in the schema slot editor (the drag-and-drop/click grid where slots are assigned to courses for a given class). It is a secondary panel, visible alongside the schedule grid on desktop.

## Specific Requirements
- Show **all courses belonging to the current tenant** (no user-specific filtering)
- Display per course: name, color swatch, category (SubjectCategory label if set)
- Courses sorted alphabetically by name
- Sidebar is **read-only** — it does not support editing courses inline

## Functionality
- **Select**: clicking a course in the sidebar pre-fills it as the selected course when creating/editing a slot
- **Search**: a text input filters the list by course name (client-side, instant)
- **Scroll**: list scrolls independently; no pagination needed (typical school has <60 courses)
- **Highlight**: currently selected course is visually highlighted in the sidebar

## Acceptance Criteria
- [ ] Sidebar renders on schema editor page (desktop ≥1024px; hidden/collapsed on mobile)
- [ ] Lists all tenant courses fetched from `GET /api/v1/courses`
- [ ] Each row shows color swatch + course name + optional category badge
- [ ] Search input filters list in real time
- [ ] Clicking a course sets it as the active course for the next slot action
- [ ] Selected course is highlighted (border or background)
- [ ] Sidebar is hidden on mobile (screen width <1024px); accessible via a toggle button

## Technical Notes
- API: `GET /api/v1/courses` → `CourseDto[]` (fields: `id`, `name`, `color`, `category`)
- Example response:
  ```json
  [
    { "id": "uuid", "name": "Dansk", "color": "#3b82f6", "category": "Humaniorafag" },
    { "id": "uuid", "name": "Matematik", "color": "#10b981", "category": null }
  ]
  ```
- Use existing generated query hook: `getApiV1CoursesOptions()` from `@tanstack/react-query.gen`
- Add sidebar as a new component `CoursesSidebar` in `web/src/components/`
- Integrate into the schema slot editor page (find the schema editor page/component)
- No new API endpoints needed — courses endpoint already exists
- Permissions: only authenticated users reach the schema editor; no extra auth needed

## Done Criteria (QA)
- Hanne (school secretary) can open schema editor and see all school courses without navigating away
- Searching "mat" shows only courses containing "mat" (case-insensitive)
- Selecting "Dansk" from sidebar and clicking a slot pre-selects Dansk in the slot form
