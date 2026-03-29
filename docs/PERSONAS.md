# PERSONAS.md — User Personas

These personas represent the real people who use Klubhuset. Every feature decision, every screen, every label must pass the question: **can this person use this without help?**

---

## Kirsten, 67 — Sekretær, badminton og gymnastik

Kirsten has been secretary of the club for 11 years. She keeps the member register running, chases unpaid kontingent, and makes sure nothing falls through the cracks — all as a volunteer, on top of everyday life. She currently manages everything in an Excel spreadsheet and collects payments by bank transfer. She has a smartphone and uses it daily for calls, SMS, and Facebook.

**Device**: Samsung Galaxy A-series. Occasionally uses a laptop for email.

**Comfort level**: Facebook yes. Email yes. Excel reluctantly. A new login or unfamiliar interface takes a moment — she prefers to get it right the first time rather than experiment.

**Goals**:
- Know who has paid and who hasn't, without cross-referencing a spreadsheet
- Send a message to all badminton parents when training is cancelled
- Add a new member confidently, on her own

**Fears**:
- "Deleting something by accident"
- Forms with many fields
- Anything that says "error" without explaining what to do

**Design implications**:
- Every action must have one obvious button, not a menu
- Destructive actions (delete, remove) must require confirmation with plain-language explanation
- Success states must be explicit: "Gemt!" not just a silent page reload
- Never use technical terms (slug, tenant, API, sync) in member-facing or admin UI

---

## Mads, 41 — Fodboldforælder

Mads has two kids in the club — one in U10 fodbold, one just starting gymnastics. He works full-time and handles most school and club admin on his phone while commuting or waiting at the side of the pitch. He is comfortable with technology and moves fast.

**Device**: iPhone. Uses apps constantly.

**Comfort level**: High. Uses MobilePay, MitID, Rejseplanen, and calendar apps daily.

**Goals**:
- Register both kids quickly and pay the kontingent in one go
- See when and where training is — immediately, without digging
- Know if training is cancelled before anyone has left the house

**Fears**:
- Having to re-enter the same information twice (once per child)
- Missing a cancellation notification
- Paying the wrong amount or to the wrong place

**Design implications**:
- Registration flow must handle multiple children cleanly
- Training schedule must be the first thing visible after login — not buried in a menu
- Cancellation notifications must be sent immediately on change
- MobilePay must be the obvious payment method, not an afterthought

---

## Sofie, 14 — Gymnast

Sofie has been in gymnastics since she was 5. She has her own phone and handles more club-related things herself now — checking training times, letting her træner know when she'll be absent. She knows exactly what a well-designed app looks like and has no patience for one that isn't.

**Device**: iPhone. Never uses a laptop for anything club-related.

**Comfort level**: Native smartphone user. High standards for apps — if something takes too long or feels clunky, she'll find another way.

**Goals**:
- Check when and where training is this week
- See who else is on her team
- Message the team without having to call anyone

**Fears**:
- UI that looks like it was built for a desktop
- Being redirected to a non-mobile experience
- Having to ask someone else to do something she should be able to do herself

**Design implications**:
- Member-facing UI must look clean and modern — not like a government form
- Everything must work on a small screen with one thumb
- Team roster and schedule are the two most-accessed screens — they must load fast and require no navigation depth

---

## Erik, 55 — Formand (chairman)

Erik took over as formand three years ago. He runs a small business and is used to taking responsibility — for the club, for his team, for the finances. He is not a software person, but he is capable and methodical. He uses email and online banking without trouble. He wants to understand what he's doing before he does it.

**Device**: iPad at home. Laptop at work. Android phone.

**Comfort level**: Moderate. Comfortable with email, banking, and MobilePay. Prefers clear labels and one step at a time over clever interfaces that assume prior knowledge.

**Goals**:
- See the state of the club at a glance: how many members, who hasn't paid
- Handle new member applications without reading a manual
- Export the DGI statistics report once a year, independently

**Fears**:
- Making a change that affects something he didn't intend to touch
- Not understanding what a button does before pressing it
- Needing to ask for help with a routine task

**Design implications**:
- Admin dashboard must lead with a simple summary view — numbers, not tables
- Every admin action must be reversible or at minimum explained before it executes
- The DGI export must be one button with a clear label — not hidden in settings
- Tooltips and inline help text must be in plain Danish, written for a capable adult encountering something for the first time

---

## Using these personas

When designing a screen or feature, ask:

1. **Can Kirsten complete this on her own, confidently?**
2. **Can Mads do this in 60 seconds on a phone while standing at the football pitch?**
3. **Would Sofie consider this app well-made?**
4. **Does Erik know what will happen before he clicks?**

If the answer to any of these is no, simplify. Remove a step. Rewrite the label. Make the primary action more obvious.

These are not edge cases — they are the core users. Design for them first.
