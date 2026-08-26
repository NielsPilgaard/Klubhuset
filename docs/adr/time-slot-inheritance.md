---
title: 'ADR: Time slot inheritance with per-class overrides'
status: 'Accepted'
date: '2025-01-01'
authors: 'Niels Pilgaard Grøndahl'
tags: ['product', 'schema-planner']
supersedes: ''
superseded_by: ''
description: >-
  Schools define a default time slot template at onboarding; classes inherit
  it but are not forced to align — friskoler have varied lesson/break
  structures that a single rigid grid would not fit.
---

# ADR: Time slot inheritance with per-class overrides

## TL;DR

A school sets a default time slot template (lesson durations, breaks) via the onboarding wizard. Each class inherits it automatically but can override individual slots — classes are not forced to align with the school default or each other. One class can run 8:10–8:45 while another runs 8:15–8:50.

## Status

**Accepted**

## Context

Friskoler have varied structures: some have strict fixed breaks, others let teachers manage breaks as needed, and some classes need longer lessons for certain subjects (see [docs/PERSONAS.md](../PERSONAS.md) and [docs/PRD.md](../PRD.md) target segments). A rigid shared grid would not match how these schools actually operate.

## Decision

A school defines a default time slot template during onboarding via the time slot wizard. Each class inherits this template automatically. Classes can override individual time slots at any time after initial setup.

## Consequences

### Positive

- **POS-001**: The wizard gives every school a sensible default, reducing setup time for the common case.
- **POS-002**: Per-class overrides accommodate real friskole variation without forcing a workaround.

### Negative

- **NEG-001**: Because classes are not forced onto a shared grid, downstream conflict detection must compare actual clock-time overlap rather than slot index — see [schema-conflict-detection](schema-conflict-detection.md).

## Alternatives Considered

### Single school-wide rigid grid

- **ALT-001**: **Description**: All classes share one fixed time slot structure with no per-class override.
- **ALT-002**: **Rejection Reason**: does not match how friskoler with varied lesson/break structures actually run their school day.

## Related Decisions

- [schema-conflict-detection](schema-conflict-detection.md) — the detection mechanism this decision's unaligned grids require
