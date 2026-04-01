# Time slot inheritance with per-class overrides

**Status**: Accepted

## Decision

A school defines a default time slot template (lesson durations and breaks) during onboarding via a wizard. Each class inherits this template automatically. Classes can override individual time slots — they are NOT forced to align with the school default or with each other. One class can have 8:10–8:45, another 8:15–8:50.

## Reason

Friskoler have varied structures. Some have strict fixed breaks; others let teachers manage breaks as needed. Some classes have longer lessons for certain subjects. Forcing all classes onto a single rigid grid would not match how these schools actually work. The wizard provides a sensible default to reduce setup time, while per-class overrides accommodate the reality.
