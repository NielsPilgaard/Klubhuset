# Task 23: User data import from STIL

Import (and optional two-way sync) of students, staff, classes, and parents from STIL's SkoleGrunddata via UNI-Login.

## Background

STIL's **SkoleGrunddata** is the canonical user database for Danish schools: students, staff, contacts (parents), and class memberships. UNI-Login is the SSO layer on top. Instead of manual creation, we can pull data from here.

## Relevant APIs

| WS | Name | Description |
|---|---|---|
| **WS17** (wsiEKSPORT) | Export service | Batch export of entire institution's data as XML. **This is the relevant WS.** |
| **WS71** (wsiBRUGER) | User lookup | Per-user lookup during active UNI-Login session — not suited for bulk import |
| **WS10** | Import service | Schools push data *into* STIL — not relevant for us |

WS17 returns full institution export: students with UNI-Login IDs, staff, contacts, and group memberships.

## Data model (WS17 XML)

```
Person: id, name, birthdate, role (student / teacher / aide / contact)
Group:  institutionNumber, groupType, mainGroup
Membership: person → group
ContactRelation: parent → student + custody
```

## Authentication

- Requires **client certificate** (X.509): SITHS, EFOS, or Expisoft
- Requires signed **data agreement** per school — no global agreement possible
- School must approve Skoleoverblikket as data broker in their SkoleGrunddata admin
- STIL is data controller; we are data processor — full GDPR obligation

## Integration architecture (.NET)

- SOAP/WSDL service — generate proxy with `dotnet-svcutil`
- `HttpClient` configured with client certificate
- XML response mapped to our `Staff`, `Student`, `Parent` entities
- Import flow as one-time action from admin UI

## Two-way sync (public schools / folkeskoler)

For large public schools, continuous two-way sync makes sense:
- Schools already maintain SkoleGrunddata continuously (legally required for folkeskoler)
- Sync directions:
  - **STIL → Skoleoverblikket**: new students/staff/classes propagate automatically
  - **Skoleoverblikket → STIL**: via WS10 (import WS) — only relevant if we are the authoritative source for some data
- Requires webhook/polling architecture and conflict resolution

## Considerations

1. **Certificate procurement** — schools must have EFOS/Expisoft cert. Bureaucratic friction, especially for independent schools (friskoler).
2. **Data agreement per school** — cannot be done as one global agreement; each tenant approves separately.
3. **Friskole coverage** — SkoleGrunddata is *legally required* for folkeskoler, but friskoler participate voluntarily and often have incomplete/outdated data. Our primary market (friskoler) may get low value.
4. **Alternative for friskoler** — CSV import (task 24) is more pragmatic for friskoler with poor STIL data.

## Recommendation

- **Folkeskoler (secondary market)**: WS17 one-time import + optional two-way sync gives full value — data is maintained and legally required.
- **Friskoler (primary market)**: Validate whether the school actively maintains SkoleGrunddata before integrating. If not, use CSV import instead.
- Implement WS17 integration as opt-in feature behind feature flag, with fallback to manual creation.

## Documentation

- [BPI webservices overview](https://viden.stil.dk/display/INFRA2/Unilogin+SkoleGrunddata+BPI-webservices)
- [WS17 / wsiEKSPORT](https://viden.stil.dk/pages/viewpage.action?pageId=2360669)
- [WS71 / wsiBRUGER](https://viden.stil.dk/pages/viewpage.action?pageId=2360665)
- [Data access agreements](https://viden.stil.dk/display/INFRA2/Aftaler+om+dataadgang)
