---
title: 'Decrease module pricing to 300 kr/mo flat, add 9000 kr/yr full-package bundle'
purpose: 'Cut customer-facing prices across all three subscription items (Basis, Board, Parent) to a flat 300 kr/mo each, and introduce a 9000 kr/yr bundle price for schools buying all three yearly. No change to our own running costs — this is a price decrease, not a COGS reduction.'
description: >-
  Basis, Board, and Parent modules currently price at 499 / 299 / 499 kr/mo
  respectively (docs/PRICING.md is stale and predates modules entirely — real
  prices live only in Stripe). New pricing: 300 kr/mo flat per module
  (symmetric, easy to explain), and a combined 9000 kr/yr price for schools
  buying all three modules yearly (implemented as three per-module yearly
  Stripe prices that sum to 9000, not a single bundle SKU — preserves the
  existing per-module Stripe subscription item architecture). Existing
  subscribers are migrated to the new prices, not grandfathered.
status: 'Ready'
---

# Decrease module pricing to 300 kr/mo flat, add 9000 kr/yr bundle

## TL;DR

Cut prices: Basis 499→300, Board 299→300, Parent 499→300 (all kr/mo). Add a
yearly option per module priced so that Basis+Board+Parent together cost
exactly 9000 kr/yr if a school buys all three yearly — implemented as three
separate yearly Stripe Prices (not one bundle SKU), so `AddModuleAsync` /
`RemoveModuleAsync` / `SwitchIntervalAsync` (`SubscriptionService.cs`) need
no code changes, only new Price IDs in config. Existing subscriptions are
migrated to the new prices (no grandfathering). `docs/PRICING.md` is
rewritten — it currently doesn't mention modules at all.

Sanity-checked against competitors: SkoleIntra ~19,000 kr/yr for a ~300-student
school (full platform, 4 modules), Docendo 499 kr/mo flat / 12,000 kr/yr floor
(schema-only, no modules), SkolePlan (closest match — friskole admin +
pedagogic platform) doesn't publish pricing. 9000/yr is ~53% below SkoleIntra
and ~25% below Docendo's floor while covering more ground than either.

## Context

Pricing today (confirmed with the developer, not in any doc). All prices are
kr/mo per school (`school-based-pricing.md` — flat fee per tenant, not
per-student):

| Item                          | Monthly (kr/school) |
|-------------------------------|---------------------:|
| Base plan (`BasePriceId*`)    | 499                  |
| `BoardModule`                 | 299                  |
| `ParentModule`                | 499                  |

`docs/PRICING.md` describes a single flat 499 kr/mo "Basis" tier with no
module concept — written before `SubscriptionModule` (`ParentModule`,
`BoardModule`, `Models/SubscriptionModule.cs`) and the base+add-on billing
model shipped (task 34) and is stale. It is not the source of truth.

Each module is billed as its own Stripe `SubscriptionItem` with its own
Price (`StripeOptions.ModulePriceIds[module][interval]`,
`SubscriptionService.cs`). The base plan has its own monthly/yearly Price
pair (`BasePriceIdMonthly` / `BasePriceIdYearly`). Interval switching
(`SwitchIntervalAsync`) already updates every item on a subscription — base
plus all active modules — in one Stripe call. This architecture is kept
as-is; only the Price amounts change.

## Decision (confirmed with developer)

- **New monthly prices**: base plan, `BoardModule`, and `ParentModule` all
  300 kr/mo per school — flat and symmetric across all three, so pricing
  communication stays simple ("300 per module," no exceptions). `BoardModule`
  moves from 299→300 (effectively flat, intentional for symmetry) while the
  base plan and `ParentModule` both drop ~40%.
- **New yearly prices**: one yearly Stripe Price per item (base plan,
  `BoardModule`, `ParentModule`), each discounted off the 300 kr/mo rate such
  that a school buying all three yearly pays exactly 9000 kr/yr total per
  school. A school buying only one or two items yearly pays that item's
  yearly price alone (proportionate discount), not a fixed fraction of
  9000 — there is no separate "partial bundle" price tier.
- **No single bundle SKU.** The 9000/yr figure is a sum of three per-module
  yearly prices, not one product in Stripe. This keeps `AddModuleAsync`,
  `RemoveModuleAsync`, and `SwitchIntervalAsync` working unchanged — they
  already operate per-module-item.
- **Existing subscribers are migrated**, not grandfathered — since this is a
  price decrease, no customer is worse off. Existing Stripe subscription
  items get their Price swapped to the new Price IDs.

## Scope

1. **Compute yearly-per-module prices** that sum to 9000 kr/yr when all
   three are active. Simplest split: equal thirds (3000 kr/yr each, i.e.
   250 kr/mo-equivalent, ~17% off the 300/mo rate) unless the developer
   wants an uneven split (e.g. Basis priced higher since it's
   mandatory-in-practice). Confirm split before creating Stripe Prices.
2. **Create new Stripe Prices** (test mode first, then live) — manual
   Stripe Dashboard work, not code:
   - Basis monthly (300), Basis yearly
   - Board monthly (300), Board yearly
   - Parent monthly (300), Parent yearly
3. **Update config** with new Price IDs: `StripeOptions.BasePriceIdMonthly`
   / `BasePriceIdYearly`, `ModulePriceIds["BoardModule"]["Monthly"/"Yearly"]`,
   `ModulePriceIds["ParentModule"]["Monthly"/"Yearly"]` in
   `appsettings.Development.json` and production secrets/environment.
4. **Migrate existing subscriptions**: for every active Stripe subscription,
   update each item's Price to the corresponding new Price ID
   (`ProrationBehavior = "none"`, matching the pattern already used in
   `SwitchIntervalAsync`). Write a one-off script or admin endpoint —
   whichever fits the existing backoffice tooling
   (`SuperAdminTenantsController`) better; do not hand-edit in Stripe
   Dashboard for more than a handful of schools.
5. **Rewrite `docs/PRICING.md`**: replace the single-tier "Basis 499"
   description with the modular model — Basis/Board/Parent at 300 kr/mo
   each, yearly bundle pricing summing to 9000 kr/yr for all three, and
   note that `docs/adr/school-based-pricing.md` (flat-fee-per-school,
   not per-student) still holds — modular add-on pricing doesn't
   contradict it.
6. **Update UI pricing displays**: `web/src/pages/BillingPage.tsx` and the
   marketing/landing page pricing section — replace hard-coded or
   assumed old numbers with the new prices. Confirm against
   `tasks/todo.md` items 6–8 (landing page pricing, sub page pricing,
   Stripe yearly toggle) — this task's output is exactly what those
   checklist items should now verify.
7. **Regenerate OpenAPI/client** if any price-related response shape
   changes (unlikely — prices aren't returned by the API today, Stripe
   Checkout handles display) — confirm, don't assume.

## Out of scope

- Changing our own infra/hosting/Stripe running costs — this task is a
  customer price decrease only, developer confirmed COGS is unaffected.
- Adding new subscription modules beyond the existing Basis/Board/Parent.
- Changing the flat-fee-per-school philosophy (`school-based-pricing.md`
  ADR stays valid — modules are still priced by cost-to-serve/value, not
  per-student).
- A single "bundle" Stripe SKU — explicitly rejected in favor of three
  yearly Prices summing to 9000, to avoid new logic in `SubscriptionService`.
