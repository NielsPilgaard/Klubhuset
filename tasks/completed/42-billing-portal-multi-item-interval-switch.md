---
title: 'Verify and fix Stripe Portal interval switching for multi-item subscriptions'
purpose: 'Determine whether Stripe Customer Portal safely handles monthly<->yearly switching for subscriptions with a base price plus separate module add-on items, and fix or restrict it if not.'
description: >-
  Subscriptions carry one base-plan Stripe subscription item plus a separate
  item per active module (task 34, base+add-on model). Stripe's built-in
  Customer Portal price-switch UI is documented to target a single
  subscription item; behavior with multiple items per subscription is
  unverified. Investigate actual Portal behavior in Stripe test mode and
  either confirm it is safe or implement a fix.
status: 'Completed'
---

# Verify and fix Stripe Portal interval switching for multi-item subscriptions

## TL;DR

Task 34 (yearly billing) gave every school one base-plan Stripe subscription
item plus one item per active module (`ModulePriceIds[module][interval]`).
Stripe's Customer Portal "switch plan" feature is designed around a single
subscription item; with our multi-item model, a customer clicking
monthly→yearly in Portal may only swap the base item and silently leave
module items on the old interval — or fail outright. This has not been
verified against real Stripe test-mode behavior. Investigate first, then
either confirm safety or implement a fix (custom API-driven switch endpoint,
or restrict Portal's configured switchable prices to base-only and handle
module re-pricing via webhook).

Also covers the "Out of scope" proration claim from task 34: verify
`proration_behavior`, billing-cycle-anchor handling, and invoice timing for
a monthly→yearly switch in test mode before continuing to exclude
proration UI/messaging.

## Context

`SubscriptionService.CreateBillingPortalSessionAsync`
(`api/Skoleoverblikket.Api/Services/SubscriptionService.cs:142`) creates a
plain Portal session with no `Flow` or price-switch restriction — it relies
entirely on the Portal configuration set in the Stripe Dashboard (task 34's
manual setup step). `AddModuleAsync` adds each module as its own
`SubscriptionItem` with its own Price (base+add-on, not a bundled Price).

Stripe's documented Portal "update subscription" flow lets a customer switch
the Price of *a* subscription item, but the dashboard configuration and API
behavior for subscriptions with several items (one base, N modules) is not
confirmed. If a customer switches only the base item to yearly via Portal,
`HandleSubscriptionChangedAsync` → `ApplyIntervalFromBasePlan`
(`SubscriptionService.cs:422`) would correctly detect the new base price and
set `sub.Interval = Yearly`, but the module items would remain on their
original monthly Prices — a billing/interval mismatch that `AddModuleAsync`
would not catch since it only checks price config, not existing item state.

## Scope

1. In Stripe test mode, create a subscription with a base item + at least
   one module item, then drive the Portal's monthly↔yearly switch as a real
   customer would. Record what actually happens to each item.
2. Based on findings, choose one:
   - **Portal is safe as configured** (e.g. Stripe already restricts the
     switch to the base item only and modules are unaffected) — document
     the verified behavior and close this task.
   - **Portal is unsafe** — implement an API-driven switch: a new endpoint
     that updates all of a subscription's items to the target interval's
     Prices in one Stripe API call sequence, replacing reliance on Portal's
     native switch for this case. Update the Portal Dashboard config to
     stop offering the native switch if it's replaced.
3. Record proration/billing-cycle-anchor/invoice-timing findings from step 1
   and update task 34's "Out of scope" section (or supersede it here) with
   verified behavior instead of the assumed "Portal handles it
   automatically."
4. Add Stripe test-mode coverage (existing stripe-mock/test harness) for
   whichever path is chosen.

## Out of scope

- Multi-year or other billing intervals — monthly/yearly only, per task 34.
