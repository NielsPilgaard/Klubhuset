# UNI•Login SSO Integration (Extra Module)

UNI•Login is Denmark's national SSO for educational institutions, operated by STIL. It is the de facto standard for folkeskoler — staff and teachers already have UNI•Login accounts provisioned by their municipality.

This is an **optional paid add-on module**, not part of the base plan. It removes the signup barrier for folkeskoler who want staff to log in with existing UNI•Login credentials rather than creating a new Keycloak account. Pricing TBD.

## How it works

UNI•Login acts as an external identity provider. Keycloak stays as the internal auth layer — UNI•Login is added as a federated IdP (OIDC or SAML 2.0) in Keycloak. On first login, the UNI•Login subject is mapped to the matching staff record in the tenant.

Vendor registration is required: register as a service provider with STIL and sign a data processor agreement. No public SDK — standard OIDC/SAML against STIL's metadata endpoints.

## Tasks

- [ ] Research STIL SP registration process and data processor agreement requirements
- [ ] Add UNI•Login as a federated external IdP in Keycloak (OIDC)
- [ ] Map UNI•Login `sub` claim to existing tenant staff records on first login
- [ ] Handle the case where no staff record matches (invite flow or error state)
- [ ] Test with a folkeskole pilot school
