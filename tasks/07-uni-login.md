# UNI•Login SSO Integration (Extra Module)

UNI•Login is Denmark's national SSO for educational institutions, operated by STIL. It is the de facto standard for folkeskoler — staff and teachers already have UNI•Login accounts provisioned by their municipality.

This is an **optional paid add-on module**, not part of the base plan. It removes the signup barrier for folkeskoler who want staff to log in with existing UNI•Login credentials rather than creating a new Keycloak account. Pricing TBD.

## How it works

UNI•Login acts as an external identity provider. Keycloak stays as the internal auth layer — UNI•Login is added as a federated IdP (OIDC or SAML 2.0) in Keycloak. On first login, the UNI•Login subject is mapped to the matching staff record in the tenant.

Vendor registration is required: register as a service provider with STIL and sign a data processor agreement. No public SDK — standard OIDC/SAML against STIL's metadata endpoints.

## STIL Registration Process

### Prerequisites

- MitID Erhverv with permission: *"Tilslutning: Ret til at administrere aftaler på tilslutning.stil.dk"*
- Also need: *"Provider Portal: Right to manage services"* for udbyderportal.stil.dk
- Company must be registered in CVR (already done)

### Steps

1. **Register as tjenesteudbyder** on [tilslutning.stil.dk](https://tilslutning.stil.dk) — create a tjeneste and request connection to "Unilogin Broker OIDC"
2. **Build + test** via [udbyderportal.stil.dk](https://udbyderportal.stil.dk) — self-service portal for creating/managing the OIDC service
3. **Production approval** — Provider Portal cannot self-approve production; must file a support case at [stil.dk/support](https://www.stil.dk/support) with:
   - OIDC metadata JSON named `{supportcasenr}_prod_oidc_metadata.json`
   - Declaration that UNI•Login serves a relevant educational purpose in the service
4. **Each pilot school** approves the dataaftale in their own UNI•Login admin tool

**STIL support hours: Mon–Fri 08:00–14:00** — contact via [stil.dk/support](https://www.stil.dk/support) (case-based, no public email)

### Technical requirements

- OIDC, follows **OIO OIDC 0.9** profile
- **PKCE required**, confidential client
- Standard OIDC endpoints — no proprietary SDK

### Data agreements (three required)

| Agreement | Between |
|-----------|---------|
| Tilslutningsaftale | Skoleoverblikket ↔ STIL (via tilslutning.stil.dk) |
| Dataaftale | Skoleoverblikket ↔ each school (school approves in their UNI•Login admin) |
| Databehandleraftale | Skoleoverblikket ↔ each school (standard Datatilsynet template) |

**Note for friskoler:** Schools do NOT need a separate databehandleraftale with STIL itself — only with Skoleoverblikket as vendor. ([source](https://www.friskolerne.dk/nyheder/artikel/ingen-databehandleraftale-ved-brug-af-unilogin))

### Key links

- OIDC docs: [viden.stil.dk/display/OFFSKOLELOGIN/OIDC](https://viden.stil.dk/display/OFFSKOLELOGIN/OIDC)
- OIDC FAQ: [FAQ: Tilslutning af OIDC tjeneste](https://viden.stil.dk/display/OFFSKOLELOGIN/FAQ:+Tilslutning+af+OIDC+tjeneste+i+Unilogin)
- Technical requirements: [viden.stil.dk Tekniske krav](https://viden.stil.dk/display/OFFSKOLELOGIN/Tekniske+krav)
- Connect service: [viden.stil.dk Tilslut tjeneste](https://viden.stil.dk/display/OFFSKOLELOGIN/Tilslut+tjeneste)

## Tasks

- [ ] Get MitID Erhverv permissions for tilslutning.stil.dk and udbyderportal.stil.dk
- [ ] Register on tilslutning.stil.dk — request "Unilogin Broker OIDC"
- [ ] Add UNI•Login as a federated external IdP in Keycloak (OIDC)
- [ ] Map UNI•Login `sub` claim to existing tenant staff records on first login
- [ ] Handle the case where no staff record matches (invite flow or error state)
- [ ] Test with a folkeskole pilot school
