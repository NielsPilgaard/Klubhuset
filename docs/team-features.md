# Team features

This document describes the team (hold) feature in detail. See [PRD.md](PRD.md) for context.

---

## Structure

- A **team** (hold) belongs to exactly one **afdeling** (sport).
- Each team has one **primary træner** and zero or more **extra trænere**.
- Members can self-sign up for teams (if the team is open for registration).

---

## Trænere

### Roles on a team

| Role | Description |
|---|---|
| Primary træner | Main responsible person. Appears first in team listings. |
| Extra træner | Additional helpers/assistants. Multiple allowed. |

Trænere are existing members of the club assigned to a team by an admin.

### Permissions

| Action | Admin | Primary træner | Extra træner | Member |
|---|---|---|---|---|
| View team roster | ✅ | ✅ | ✅ | Own memberships only |
| Send team message | ✅ | ✅ if club setting on | ✅ if club setting on | ❌ |
| Cancel/reschedule session | ✅ | ✅ | ❌ | ❌ |
| Edit team details | ✅ | ❌ | ❌ | ❌ |

**Club setting**: whether trænere can send messages to their own teams is a per-club toggle controlled by the admin. Off by default. See [traener-message-permission](decisions/traener-message-permission.md).

---

## Training schedule

### Weekly recurrence

Admin sets a baseline weekly pattern for the team:

- Day(s) of the week (e.g. Tuesday + Thursday)
- Time (e.g. 18:00)
- Location (e.g. "Idrætshallen, bane 2")

This pattern generates recurring sessions automatically.

### Exceptions

An admin or primary træner can create an exception to the recurring pattern:

| Exception type | Description |
|---|---|
| Cancellation | A specific session is cancelled. Members are notified by email. |
| Reschedule | A specific session is moved to a different date/time/location. Members are notified by email. |

Exception notifications are sent immediately when the exception is saved. The notification email includes the original session details and the new details (or cancellation notice).

---

## Team messages

Admins and (optionally) trænere can send a text message to all current members of a team.

- **Delivery**: transactional email to each team member's registered email address
- **Sender display name**: the club name (white-label) or Klubhuset (free tier)
- **Reply-to**: the sender's email address (so members can reply directly)
- **History**: sent messages are logged in the admin dashboard

### Who can send

- **Admin**: always, to any team
- **Primary træner**: if the club has enabled the træner messaging setting
- **Extra træner**: same condition as primary træner

---

## Season management (paid tiers only)

- Teams can be associated with a season (e.g. "Efterår 2025", "Forår 2026")
- Season has a start date and end date
- Kontingent is tied to a season for paid tier clubs
- At season rollover: admin starts a new season, teams are carried over, members re-confirm enrollment (or are auto-enrolled, configurable)
