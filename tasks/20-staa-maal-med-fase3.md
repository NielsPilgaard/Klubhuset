---
title: 'Stå mål med — Fase 3 (Årsplan / Undervisningsplan)'
purpose: 'Scope the long-term, separate-module feature for storing and publishing undervisningsplaner and læringsmål per Friskoleloven §1a.'
description: >-
  Proposed Fase 3 of the "stå mål med" compliance feature: structured storage
  and public/PDF publishing of per-class-per-course teaching plans and
  learning goals, plus registration of the school's chosen §1a compliance
  path (A–E). Significantly larger scope than Fase 1–2 and not a natural
  extension of the schema planner — effectively a separate module. No code
  exists yet; this is a scoping doc, not an implementation plan.
status: 'Proposed'
---

# Stå mål med — Fase 3 (Årsplan / Undervisningsplan)

## TL;DR

Fase 1+2 (structural subject-coverage view + UVM timetal comparison) are
**built and live** — `StaaMaalMedController` (`GET /api/v1/staa-maal-med/coverage`)
computes green/yellow/red/missing coverage per class from live `SchemaSlot`
data against UVM's vejledende timetal, plus `UnexpectedGradeCategories`
flagging courses scheduled at grades UVM doesn't define them for. Fase 3 is
a different, much larger feature: storing and publishing full
undervisningsplaner/læringsmål per §1a, with new `TeachingPlan`,
`TeachingGoal`, `CompliancePath` entities. No auto-certification, no AI
quality assessment, no STUK/UVM integration — see task 19 §4 for why.

## Context

Fase 1+2 are done — see `StaaMaalMedController.cs`
(`api/Skoleoverblikket.Api/Controllers/StaaMaalMedController.cs`), which
serves structural coverage data derived entirely from existing `SchemaSlot`
records. That data source is why Fase 1–2 fit naturally inside the schema
planner: no new entities, just a read-model over data already there.

Fase 3 is categorically different. §1a compliance also requires publishing
actual teaching plans and goals per course/class/year, plus documenting
which of five recognized compliance paths (A–E) the school follows — none of
which exists in current schema data. See task 19 §6 ("Fase 3") for the
original framing and §4 ("Regelbaseret vs. AI-assisteret check") for why
this stays structural/documentary rather than an automated quality
judgment.

## Proposed scope

### 1. Årsplaner pr. klasse og fag

Each course-per-class needs an associated teaching plan describing:

- Læringsmål for skoleåret
- Valgte metoder og materialer
- Evalueringsform

Stored structurally in the database, publishable as PDF export or a public URL.

### 2. §1a compliance-stier

Friskolen must document that undervisningen "står mål med" folkeskolens, via
one of five recognized paths:

- **Sti A**: Folkeskolens Fælles Mål anvendes direkte
- **Sti B**: Skolens egne mål, der svarer til Fælles Mål
- **Sti C**: Mål der i omfang og niveau svarer til folkeskolens
- **Sti D**: Internationale programmer (f.eks. IB)
- **Sti E**: Anden dokumenteret tilgang

System must record which path the school uses and store supporting documentation.

### 3. Tilsynsstøtte

Tilsynsrapporter and selvevalueringer must be exportable for use at the
external tilsynsførende's visit.

### 4. Data model (new entities)

- `TeachingPlan` — undervisningsplan tied to `Class` + `Course` + skoleår
- `TeachingGoal` — concrete læringsmål under a plan
- `CompliancePath` — school's chosen §1a-sti per skoleår

Requires new EF Core migrations and new API endpoints.

## Hvad der IKKE skal implementeres

- Automatisk "stå mål med"-certificering
- AI-vurdering af undervisningskvalitet
- Integration med STUK eller UVM's systemer
- Juridisk rådgivning eller certifikater

## Open questions

- Publishing mechanism: PDF export, public URL, or both?
- Is this a module gated behind `SubscriptionModulesController` like the
  parent module, or bundled?
- Does `CompliancePath` need to support switching sti mid-skoleår, or is it
  fixed once set?

## Referencer

Se [task 19](completed/19-staa-maal-med.md), afsnit 6 "Fase 3" og afsnit 4
"Regelbaseret vs. AI-assisteret check" for baggrund og afgrænsning.
