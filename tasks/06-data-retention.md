# Todo

## Automated data deletion after subscription cancellation

### Context

The privacy policy (Privatlivspolitik) states that school data is retained for 90 days after subscription cancellation, then permanently deleted. This is currently not implemented — deletion is manual.

This task implements the automated cleanup so the stated policy is actually enforced.

Let's change this to 180 days, if there's no compliance constraint there

### What to build

#### 1. Track cancellation date

When a Stripe subscription is cancelled (webhook event: `customer.subscription.deleted`), record the cancellation timestamp on the tenant record.

Add a nullable column to the `Tenants` table:

```
SubscriptionCancelledAt DateTime? (UTC)
```

Generate a new EF Core migration for this column. Do not modify existing migrations.

The existing Stripe webhook handler should set this field when the subscription moves to a cancelled state.

#### 2. Background job: delete expired tenants

Add a background service (`IHostedService` or Hangfire recurring job — match whatever background job pattern is already in use in the API) that:

- Runs once daily (e.g. 02:00 UTC)
- Queries for tenants where `SubscriptionCancelledAt` is not null and `SubscriptionCancelledAt <= now - 90 days`
- For each matching tenant, performs a hard delete in this order:
  1. Delete all uploaded files from OVHcloud storage for that tenant
  2. Delete all tenant data from the database (cascades via EF Core, or explicit ordered deletes — whichever is already the pattern)
  3. Delete the tenant row itself
- Logs each deletion (tenant ID, deletion timestamp) at `Information` level
- If file storage deletion fails, log the error and skip database deletion for that tenant (do not leave orphaned DB rows with missing files — fail safe)

#### 3. Self-serve data export before deletion

Before a tenant's data is deleted, they should be able to export it. Check whether an export feature already exists (`/eksporter` route exists in the frontend). If a full export (all schedules, staff, classes as a ZIP or PDF bundle) is not yet available, add a task note — do not implement it in this task.

#### 4. Notify before deletion

Send a warning email to the school's admin account 7 days before the 90-day window expires (i.e. at day 83 post-cancellation). Email should:

- Be in Danish
- State clearly that data will be deleted in 7 days
- Include a link to log in and export data
- Include kontakt@skoleoverblikket.dk for questions

Use whatever transactional email provider is already in use. If none exists, add a task note — do not introduce a new provider without explicit instruction.

### Constraints

- New EF Core migration required — do not modify existing ones
- Tenant scoping must be maintained throughout — never delete across tenant boundaries in a single query
- All deletion must be permanent (hard delete) — no soft-delete tombstones
- The 90-day period is measured from `SubscriptionCancelledAt`, not from the last login or any other signal
- Write an integration test that verifies: a tenant with `SubscriptionCancelledAt = 91 days ago` is deleted, and a tenant with `SubscriptionCancelledAt = 89 days ago` is not

### Out of scope

- Allowing schools to manually trigger early deletion (a future self-serve feature)
- Anonymisation instead of deletion
- Any changes to the billing flow itself
