---
title: 'ADR: Real-time schema conflict detection'
status: 'Accepted'
date: '2025-01-01'
authors: 'Niels Pilgaard Grøndahl'
tags: ['product', 'schema-planner']
supersedes: ''
superseded_by: ''
description: >-
  Teacher/room/aide double-booking is detected in real time as the admin
  assigns lessons, comparing actual clock-time overlap rather than time slot
  index — this is the core value proposition of the product.
---

# ADR: Real-time schema conflict detection

## TL;DR

The schema builder validates teacher, room, and aide double-booking in real time as lessons are assigned — not after saving. Because classes can have different (unaligned) time slot structures, conflicts are detected by comparing actual clock-time overlap, not matching slot indices. This is the core value proposition of the product.

## Status

**Accepted**

## Context

Manual conflict detection is the #1 pain point schools face when building schedules in spreadsheets — see [docs/PRD.md](../PRD.md) "Problem being solved." Detecting conflicts only after a schema is "done" forces the admin to redo work. See [time-slot-inheritance](time-slot-inheritance.md) for why classes are not forced onto a shared time grid, which is what makes clock-time comparison (rather than slot-index comparison) necessary here.

## Decision

Conflicts (teacher double-booking, room double-booking, aide double-booking) are shown immediately as the admin assigns a lesson — not after saving.

## Consequences

### Positive

- **POS-001**: Problems are caught at the moment of assignment, when they are cheapest to fix, instead of after a full schema is built.
- **POS-002**: This is the core value proposition of the product relative to spreadsheet-based scheduling.

### Negative

- **NEG-001**: Because classes can have different time slot structures, conflict detection must compare actual clock-time overlap — not just matching time slot indices. A teacher assigned to 8:10–8:55 in one class and 8:30–9:15 in another is a conflict. This is more complex to implement and test than index-based comparison.

## Alternatives Considered

### Post-save (batch) conflict detection

- **ALT-001**: **Description**: Validate the full schema for conflicts only when the admin explicitly saves or publishes it.
- **ALT-002**: **Rejection Reason**: forces rework after the fact — the admin builds most of the schema, then discovers conflicts and has to unwind assignments. Defeats the product's core value proposition.

## Related Decisions

- [time-slot-inheritance](time-slot-inheritance.md) — why classes have independent time grids, which is why clock-time (not slot-index) comparison is required
