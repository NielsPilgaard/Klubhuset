# PERSONAS.md — User Personas

These personas represent the real people who use {{PRODUCT_NAME}}. Every feature decision, every screen, every label must pass the question: **can this person use this without help?**

---

## Hanne, 58 — Skolesekretær

Hanne has been the school secretary for 12 years. She runs the daily administration: answers the phone, handles enrollment, coordinates with teachers, and builds the weekly schema every term. She currently does this in an Excel spreadsheet — moving cells around, checking for conflicts by eye, printing the result and posting it in the teachers' lounge. She is not a technical person, but she is thorough and careful.

**Device**: Laptop at work (Windows). Samsung phone for quick checks at home.

**Comfort level**: Email and Excel yes. Online banking yes. New software takes her a moment — she prefers to understand what a button does before pressing it. She does not experiment; she follows a path that works.

**Goals**:
- Build next term's schema without double-booking teachers or rooms
- See at a glance if a teacher has too many or too few hours
- Print a clean weekly schema for each class and for the teachers' lounge

**Fears**:
- "Deleting something by accident"
- A change that silently affects another class's schema
- Forms with too many fields
- Anything that says "error" without explaining what to do

**Design implications**:
- Every action must have one obvious button, not a menu
- Conflicts must be shown immediately and in plain Danish — not after saving
- Destructive actions (delete, remove) must require confirmation with plain-language explanation
- Success states must be explicit: "Gemt!" not just a silent page reload
- The schema builder must work well on a laptop screen — this is where Hanne works
- Never use technical terms (slug, tenant, API, sync) in any user-facing UI

---

## Thomas, 44 — Lærer

Thomas teaches dansk and historie across four classes — from 3. klasse to 7. klasse. He has been at the school for 8 years. He is comfortable with technology and uses his phone constantly. He checks his schedule between classes, during breaks, and sometimes from home in the evening to plan the next day.

**Device**: iPhone. Laptop at school for preparing lessons.

**Comfort level**: High. Uses MitID, e-Boks, MobilePay, and calendar apps daily.

**Goals**:
- See his own weekly schedule at a glance — which classes, which rooms, what time
- Know immediately if a room or time has changed
- Not have to ask Hanne for information he should be able to find himself

**Fears**:
- Missing a schedule change and showing up to the wrong room
- Having to dig through menus to find basic information
- Being asked to do admin tasks that aren't his job

**Design implications**:
- Personal schedule view must be the first thing visible after login — not buried in a menu
- Schedule changes must be clearly marked (visual indicator, notification)
- Teacher view must be clean and read-only — no admin controls visible
- Must work perfectly on a phone screen

---

## Birgitte, 51 — Skoleleder

Birgitte is the school principal. She oversees everything — staffing, budgets, parent communication, and long-term planning. She is the one who approves the schema and makes sure UVM requirements are met. She is capable with technology but has no patience for tools that waste her time. She needs overview, not detail.

**Device**: Laptop at school. iPad at home. iPhone.

**Comfort level**: Moderate to high. Comfortable with email, online banking, and school administration tools. Prefers dashboards and summaries over raw data.

**Goals**:
- See the state of the schema at a glance: is it complete? Are all classes covered?
- Check total course hours against UVM minimumstimetal
- Identify staffing gaps: which courses need a teacher assigned?
- Export a summary for the school board or UVM reporting

**Fears**:
- Approving a schema that doesn't meet UVM requirements
- Not noticing a staffing gap until it's too late
- Needing to learn a complex new tool when the old one "works fine"

**Design implications**:
- Dashboard must lead with summary numbers — classes with complete schemas, unassigned slots, total hours
- Stats and exports must be accessible in one or two clicks — not hidden in settings
- UVM hour tracking must be clear: are we above or below the requirement per course?
- Inline help text must be in plain Danish, written for a capable adult encountering something for the first time

---

## Mikkel, 29 — Vikar / pædagog

Mikkel is a part-time substitute teacher and pedagogue. He works across multiple classes throughout the week, filling in wherever needed. His schedule changes more often than anyone else's. He checks his phone in the morning to see where he needs to be. He does not care about the admin side of the tool — he just needs his schedule.

**Device**: iPhone. Never uses a laptop for anything school-related.

**Comfort level**: Native smartphone user. High standards for apps — if something takes too long or feels clunky, he'll just text Hanne instead.

**Goals**:
- Check where he needs to be today and this week
- See if anything changed since yesterday
- Not have to log in through a complicated process

**Fears**:
- Showing up to the wrong room because the schedule changed and he wasn't told
- UI that looks like it was built for desktop and squeezed onto a phone
- Having admin features cluttering his view

**Design implications**:
- Schedule view must be the default screen — no navigation required
- Must work on a phone with one thumb
- Changes since last visit should be visually highlighted
- No admin controls, no editing — pure read-only for this role
- Login must be fast (saved session, biometric if possible via Keycloak)

---

## Using these personas

When designing a screen or feature, ask:

1. **Can Hanne build the schema on her own, confidently, on her laptop?**
2. **Can Thomas find his schedule in 10 seconds on his phone?**
3. **Does Birgitte get the overview she needs without digging?**
4. **Does Mikkel see only what he needs — nothing more?**

If the answer to any of these is no, simplify. Remove a step. Rewrite the label. Make the primary action more obvious.

These are not edge cases — they are the core users. Design for them first.
