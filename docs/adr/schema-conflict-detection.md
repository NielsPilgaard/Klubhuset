# Real-time schema conflict detection

**Status**: Accepted

## Decision

The schema builder validates conflicts in real time as the admin assigns lessons. Conflicts (teacher double-booking, room double-booking, aide double-booking) are shown immediately — not after saving.

## Reason

Manual conflict detection is the #1 pain point in schema planning. Detecting conflicts after the schema is "done" forces the admin to redo work. Real-time validation catches problems at the moment of assignment, when they are cheapest to fix. This is the core value proposition of the product.

## Implementation note

Because classes can have different time slot structures (not aligned), conflict detection must compare actual clock-time overlap — not just matching time slot indices. A teacher assigned to 8:10–8:55 in one class and 8:30–9:15 in another is a conflict.
