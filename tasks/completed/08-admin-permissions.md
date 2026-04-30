# Admin permissions for staff

## Goal

Allow a school admin to grant or revoke administrative permissions for any staff member — both when onboarding a new person and for existing staff. Admin is a permission a staff member has, not a fixed role type.

---

## Background

`StaffRole` (`Teacher`, `Aide`, `Substitute`) describes what a person does at the school. It is separate from whether they have platform admin access. A school leader may be a teacher who also needs admin access; a secretary is not a teacher at all but needs full admin access.

Admin access is currently enforced via the Keycloak realm role `admin` on `[Authorize(Roles = "admin")]` endpoints. The invitation flow creates a staff record and sends a registration link, but never assigns any Keycloak realm role — so accepted invitations always land with zero platform permissions.

---

## Data model

- Add `bool IsAdmin` to `Staff`
- Generate a new EF Core migration
- `IsAdmin = true` means this staff member holds the Keycloak `admin` realm role and can access admin-only endpoints

## Keycloak role sync

Keycloak is the authority for runtime auth checks, so `IsAdmin` must be kept in sync with the Keycloak `admin` realm role.

- When `IsAdmin` is set to `true`: call Keycloak Admin REST API to assign the `admin` realm role to the user (`POST /admin/realms/Skoleplanen/users/{keycloakUserId}/role-mappings/realm`)
- When `IsAdmin` is set to `false`: call Keycloak Admin REST API to remove the `admin` realm role (`DELETE /admin/realms/Skoleplanen/users/{keycloakUserId}/role-mappings/realm`)
- Only possible when `staff.KeycloakSubject` is set (i.e. the staff member has accepted their invite and has a Keycloak account)
- Use a dedicated Keycloak service account (client credentials grant) for the Admin API calls — never the logged-in user's token
- Configure the service account client ID and secret via `appsettings.json` under `Keycloak:AdminClientId` / `Keycloak:AdminClientSecret`

## Invitation flow changes

- `POST /api/v1/staff` request body gains an optional `IsAdmin` field (defaults to `false`)
- `POST /api/v1/staff-invitations/invite/{staffId}` — no change to the invite itself
- `POST /api/v1/staff-invitations/accept` — after writing `KeycloakSubject`, check `staff.IsAdmin` and immediately assign the Keycloak role if true

## Elevating existing staff

- `PATCH /api/v1/staff/{id}/admin-permission` — new endpoint, `admin` role required
  - Body: `{ "isAdmin": true|false }`
  - Returns `409` with a problem detail if `staff.KeycloakSubject` is null (staff hasn't accepted invite yet — can't sync to Keycloak)
  - On success: updates `IsAdmin` in DB and syncs role in Keycloak in the same request
- `PUT /api/v1/staff/{id}` (existing update) — allow `IsAdmin` to be updated; apply the same Keycloak sync

## Frontend

- Staff list row shows a lock/shield icon when `isAdmin = true`
- Edit staff drawer/modal gains a toggle "Administratoradgang" — visible and editable only to admins
- If the staff member hasn't accepted their invite yet (`keycloakSubject` is null), the toggle is disabled with tooltip: "Medarbejderen skal acceptere invitationen først"
- New staff creation form gains the same "Administratoradgang" toggle

## Constraints

- A staff member cannot revoke their own admin permission (prevent accidental lockout)
- At least one admin must exist per tenant at all times — enforce in the PATCH endpoint
- Keycloak sync failure must not silently leave DB and Keycloak out of sync: if the Keycloak API call fails, roll back the DB change and return a `502` problem detail
