---
title: 'ADR: Responsive UI — laptop-first admin, phone-friendly views'
status: 'Accepted'
date: '2025-01-01'
authors: 'Niels Pilgaard Grøndahl'
tags: ['ui', 'design']
supersedes: ''
superseded_by: ''
description: >-
  The schema builder is optimised for laptop screens where the admin actually
  works; teacher/aide schedule views must work fully on a phone. No feature may
  require a specific screen size.
---

# ADR: Responsive UI — laptop-first admin, phone-friendly views

## TL;DR

Admin interface (schema builder, staff management, dashboard) is laptop-first — it needs screen space and that's where Hanne works. Teacher/aide schedule views must work fully on a phone — that's where Thomas and Mikkel check theirs. No feature may be unusable at any screen size.

## Status

**Accepted**

## Context

The schema builder is a grid-based tool that benefits from screen real estate; forcing it mobile-first would compromise its core usability. Teachers and aides check schedules on phones between classes — see [docs/PERSONAS.md](../PERSONAS.md). A single design approach optimized for one device would fail the other user group.

## Decision

The admin interface is designed laptop-first. Teacher and aide schedule views must work fully on a phone. No feature may require a specific screen size to operate, but the schema builder is optimised for laptop use over mobile.

## Consequences

### Positive

- **POS-001**: The schema builder gets the screen real estate a grid tool needs, matching how the primary admin user (school secretary) actually works.
- **POS-002**: Teachers and aides get a schedule view that works well on the device they actually use — no "desktop site squeezed onto a phone" experience.

### Negative

- **NEG-001**: Two distinct responsive design targets (laptop-first admin, phone-first staff views) means UI work can't use one universal breakpoint strategy across the whole app.

## Alternatives Considered

### Mobile-first admin UI

- **ALT-001**: **Description**: Design the schema builder mobile-first, scale up to laptop.
- **ALT-002**: **Rejection Reason**: would compromise the grid-based schema builder's usability on the device the admin actually uses it on.

## Related Decisions

- [tech-stack](tech-stack.md) — Tailwind CSS is the styling mechanism this policy is implemented with
