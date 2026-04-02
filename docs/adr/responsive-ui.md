# Responsive UI: laptop-first admin, phone-friendly views

**Status**: Accepted

## Decision

The admin interface (schema builder, staff management, dashboard) is designed laptop-first — it must work well on a laptop-sized screen. Teacher and aide schedule views must work fully on a phone. No feature may require a specific screen size to operate, but the schema builder is optimised for laptop use.

## Reason

The schema builder is a grid-based tool that benefits from screen real estate. The primary admin user (school secretary) works on a laptop. Teachers and aides check their schedules on phones between classes. Designing the admin UI mobile-first would compromise the schema builder's usability; designing teacher views desktop-first would miss how teachers actually use the tool.
