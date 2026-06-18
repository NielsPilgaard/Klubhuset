# Task 22 — Aula Widget (Info Screen / Noticeboard)

## Goal

Build an Aula-compatible iframe widget that displays the school's daily/weekly schema on Aula dashboards and notice boards. Schools install the widget in Aula; it fetches schedule data from Skoleoverblikket using a JWT issued by Aula.

---

## Background

Aula is Denmark's national school-home communication platform. Third-party suppliers can embed widgets on Aula dashboards. Widget types:

| Type | Auth | Use case |
|---|---|---|
| Normal | None | Generic content, no user identity |
| Secure | UNI-Login JWT | Personalized per user |
| Noticeboard | Institution JWT | Info screen in school hallway |
| **Iframe** | Either, via postMessage | Framework-agnostic — **our pick** |

An **Iframe Noticeboard widget** is the right fit: our stack is React/ASP.NET (not Vue.js), and the primary use case is a schema displayed on a hallway screen with no personal user context.

Docs: [T0150 Widget Guide](https://aulainfo.dk/media/cabl3kdt/t0150-widget-guide.pdf) · [T0150 Widget Local Development](https://backend.aulainfo.dk/media/i4hf1nxm/t0150-widget-local-development.pdf)

---

## Data model

### `TenantSettings` extension (or separate field on `Tenant`)

Add `AulaInstitutionCode` (`string?`) — admin enters this in `/indstillinger`. Used to map an inbound Aula JWT (`sub` claim = institution code) to a `TenantId`.

```csharp
public string? AulaInstitutionCode { get; set; }
```

**Migration:** `AddAulaInstitutionCode`

---

## Auth flow

1. Aula loads our iframe widget URL (e.g. `https://widget.skoleoverblikket.dk/aula`)
2. Widget calls `window.parent.postMessage({ request: 'getAulaToken' }, '*')`
3. Aula responds with JWT via `message` event
4. JWT claims (noticeboard token):
   - `sub` — institution code
   - `noticeboardid` — notice board ID
   - `session_uuid`
   - `exp`, `iss: "Aula"`, `aud: <our system ID from KOMBIT>`
5. Widget sends JWT as `Authorization: Bearer <token>` to `/api/v1/widget/schedule`
6. API validates JWT signature against **Aula's production certificate** (obtained from KOMBIT toolkit portal after approval), resolves `sub` → `TenantId`, returns schedule JSON

---

## Backend

### New: `WidgetController` (`/api/v1/widget`)

- `GET /api/v1/widget/schedule?week=2026-W25` — **anonymous endpoint**, auth via Aula JWT in `Authorization` header (not Keycloak)
  - Validate JWT: signature (Aula cert), `iss == "Aula"`, `aud == <our system ID>`, not expired
  - Extract `sub` (institution code) → look up `TenantId` via `AulaInstitutionCode`
  - Return schedule for requested week (or current week if omitted), same shape as existing schema endpoints but read-only and stripped of PII

JWT validation uses a dedicated validator — **not** the Keycloak auth middleware. Register as a named auth scheme or validate manually in the controller action.

Store Aula's public certificate as a config value (`Aula:PublicCertificate`), obtained after KOMBIT approval.

---

## Frontend (widget)

Separate minimal React app hosted at `widget.skoleoverblikket.dk` (or a route on the main app). No auth UI, no sidebar.

```typescript
// On mount
window.parent.postMessage({ request: 'setIframeHeight', metadata: { height: 800 } }, '*');
window.parent.postMessage({ request: 'getAulaToken' }, '*');

window.addEventListener('message', (event) => {
  const allowed = ['https://aula.dk', 'http://localhost:5173', 'https://localhost:8080'];
  if (!allowed.includes(event.origin)) return;
  if (event.data.type === 'token') {
    setAulaToken(event.data.data.token);
  }
});
```

Props available from Aula (via postMessage `getProps`):
- `currentWeekNumber` — format `"2026-W25"` — use to page the schema
- `placement` — `full | medium | narrow`
- `isMobileApp`
- `institutionCode` (redundant — already in JWT)

Widget shows: today's schema per class, or full week view depending on `placement`.

---

## Admin settings (`/indstillinger`)

Add field under a new "Aula integration" section:

- **Aula institutionskode** — text input, saved to `Tenant.AulaInstitutionCode`
- Helper text: "Find institutionskoden i Aula under Institutionsindstillinger."

---

## Approval process (KOMBIT)

1. Apply to become Widget Supplier → email `aula@kombit.dk` with company info + purpose
2. KOMBIT grants access to **Aula External Test 2** environment
3. Build & test locally using Aula's dev environment (Node + Docker, clone from their Azure DevOps)
4. Submit source zip via **Toolkit** (their case portal) for code review
5. KOMBIT approves → deployed to production (~twice/month)
6. Major feature changes require re-approval; cosmetic/bugfixes do not

After approval, obtain:
- Our **system ID** (`aud` claim value)
- Aula's **production signing certificate** for JWT validation

---

## Tests

- API: valid Aula JWT with known institution code → GET `/api/v1/widget/schedule` → 200 with schedule data
- API: expired Aula JWT → 401
- API: JWT with unknown institution code → 404
- API: JWT with wrong `aud` → 401
- Playwright: widget page loads, postMessages token mock, renders schedule

---

## Out of scope

- Personalized (per-user) Secure widget — noticeboard only for now
- Push/badge notifications to Aula users
- SSO shortcuts
- Folkeskole-specific integrations
