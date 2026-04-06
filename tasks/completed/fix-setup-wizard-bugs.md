# Task: Fix Setup Wizard Bugs

Fix two bugs in the onboarding setup wizard and add standard course import.

---

## 1. Break duration input: number field prefilled with 0 causes "015" problem

**Symptom**: The break duration input fields in the setup wizard are prefilled with `0`. When the user types `15`, the field shows `015` instead of `15`.

**Fix**: On focus, clear the field if its current value is `0` (or use `select()` to select all). This is a standard UX fix for numeric inputs with default 0.

Likely fix in the React component — find the `<input type="number"` for break duration. Add:

```tsx
onFocus={(e) => { if (e.target.value === '0') e.target.value = '' }}
```

Or use a controlled pattern that treats `0` as empty on focus. Do not apply this to other number inputs that already work correctly — the TODO says only break duration inputs are affected.

---

## 2. Submit then return looks like nothing was saved

**Symptom**: After clicking submit on any setup step and returning to it, the form fields appear empty/default, even though the data was saved. This erodes trust.

**Fix**: When the user navigates back to a completed setup step, the form must be pre-populated with the saved values. 

Find the setup step components. On mount, load existing data from the API (or from React Query cache) and set the form state with those values. Show a subtle visual indicator that the step is already complete (e.g., a checkmark in the step indicator, or a "Gemt" label near the submit button).

---

## 3. Import standard courses in wizard

Add an optional step (or a button within the courses step) in the setup wizard that imports a standard set of Danish primary school subjects (fag). 

Standard fag to seed (use Danish names):
- Dansk
- Matematik
- Engelsk
- Naturfag / Natur og teknologi
- Historie
- Samfundsfag
- Kristendom / Religion
- Idræt
- Musik
- Billedkunst
- Håndværk og design
- Madkundskab
- Geografi
- Biologi
- Fysik/kemi
- Tysk / Fransk (second foreign language)

Implementation:
- Add a button "Importér standardfag" in the fag/courses setup step.
- On click, call a new API endpoint `POST /api/v1/courses/import-standard` that creates any of the above courses that don't already exist for the tenant.
- Return the list of created courses.
- The endpoint must be tenant-scoped — `TenantId` from `ITenantContext`.
- If a course with the same name already exists for the tenant, skip it (no duplicates, no error).

---

## Constraints

- `TenantId` always from `ITenantContext`, never from request body.
- All API errors use `ProblemDetails`.
- Do not edit existing migration files.
