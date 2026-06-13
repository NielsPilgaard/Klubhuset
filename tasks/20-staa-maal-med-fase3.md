# Task 20: Stå mål med — Fase 3 (Årsplan / Undervisningsplan)

**Status**: Future / ikke planlagt  
**Forudsætning**: Fase 1 og Fase 2 er færdige (se task 19)

---

## Beskrivelse

Fase 3 handler om opbevaring og offentliggørelse af undervisningsplaner og læringsmål i henhold til friskoleloven § 1a. Det er et markant større scope end Fase 1–2 og hænger ikke naturligt sammen med skemaplanlæggeren — det er i praksis et separat modul.

---

## Hvad Fase 3 indebærer

### Årsplaner pr. klasse og fag

Hvert fag pr. klasse skal have en tilknyttet undervisningsplan, der beskriver:

- Læringsmål for skoleåret
- Valgte metoder og materialer
- Evalueringsform

Disse skal opbevares struktureret i databasen og kunne offentliggøres (enten som PDF-eksport eller som en offentlig URL).

### § 1a compliance-stier

Friskolen skal dokumentere, at undervisningen "står mål med" folkeskolens. Der er fem anerkendte stier:

- **Sti A**: Folkeskolens Fælles Mål anvendes direkte
- **Sti B**: Skolens egne mål, der svarer til Fælles Mål
- **Sti C**: Mål der i omfang og niveau svarer til folkeskolens
- **Sti D**: Internationale programmer (f.eks. IB)
- **Sti E**: Anden dokumenteret tilgang

Systemet skal kunne registrere hvilken sti skolen anvender, og gemme den tilhørende dokumentation.

### Tilsynsstøtte

Tilsynsrapporter og selvevalueringer skal kunne genereres som eksport til brug ved den eksterne tilsynsførendes besøg.

---

## Datakrav (nye entiteter)

- `TeachingPlan` — en undervisningsplan knyttet til `Class` + `Course` + skoleår
- `TeachingGoal` — konkrete læringsmål under en plan
- `CompliancePath` — registrering af skolens valgte § 1a-sti pr. skoleår

Disse kræver EF Core-migrationer og nye API-endpoints.

---

## Hvad der IKKE skal implementeres

- Automatisk "stå mål med"-certificering
- AI-vurdering af undervisningskvalitet
- Integration med STUK eller UVM's systemer
- Juridisk rådgivning eller certifikater

---

## Referencer

Se task 19, afsnit 6 "Fase 3" og afsnit 4 "Regelbaseret vs. AI-assisteret check" for baggrund og afgrænsning.
