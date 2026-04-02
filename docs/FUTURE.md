# Future Expansion Ideas

These features are **out of scope for MVP** but worth exploring post-launch. Nothing here is committed — it's a parking lot for ideas.

---

## Teacher Module (extends existing)

- **SFO-plan**: weekly SFO schedule, optionally changes week-to-week. Option to email all parents of SFO-enrolled children when the plan changes.
- **Email to parents**: teachers can email all parents of a single class; admins can email the whole school. BCC support. Must be GDPR-compliant (consent, unsubscribe, data handling).

## Parent Module

- **Kontaktbog**: log of events/notes per child, visible to parents and teachers.
- **Multi-child support**: parents with multiple children see all in one view.
- **Fraværsregistrering**: register absence, with integration to [Netprotokollen / Karakternet](https://netprotokollen.karakternet.dk/parents).
- **Kontakter**: directory of other parents (address, phone, etc.) — privacy/GDPR considerations apply.
- **Adressebeskyttelse**: parents with navne- og adressebeskyttelse (CPR-lovens §28) must have their address, phone, and other contact info hidden from other parents, the parent directory, and any exports. Must be a flag on the parent account that suppresses contact info everywhere except for school admin. This is a legal requirement — not optional.
- **SFO-skema**: parents can view the SFO schedule for their child.

## Student Module

Students can log in and view the platform — their own schedule, assignments, etc. Separate from the Parent Module (parents see their children's data; students see their own).

- **Student logins**: students get their own accounts with a limited, read-only view.
- **Student schedule view**: individual schedule derived from class assignment (useful when a student attends cross-class electives).
- **Age-appropriate UI**: simpler interface than the teacher/admin views.

## Board Module (Bestyrelsesmodul)

Isolated from teacher data at the same school by default. Access can be explicitly granted in either direction (board → teacher data, teacher → board data).

- **File storage**: shared document storage for the board.
- **"Stå mål med"-detection**: AI-assisted check of whether the school's curriculum meets the "stå mål med" requirements from the Danish Ministry of Education. (Speculative — needs research.)

## Other Ideas

These were explicitly excluded from v1 — revisit based on school feedback.

- **Student grading**: karaktergivning / elevvurdering.
- **SMS notifications**: schedule changes, absence alerts, etc.
- **Native mobile app**: dedicated iOS/Android app (current approach is responsive web).
- **Accounting integration**: e.g. e-conomic, Dinero, or similar Danish accounting tools.
- **Parent/student logins**: flagged as a v2 candidate in the ADR. Prerequisite for the Parent Module above.
