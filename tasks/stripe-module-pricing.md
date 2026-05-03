# Task: Stripe module-based pricing architecture

## Context

Skoleplanen will offer add-on modules (Forældremodul, Bestyrelsesmodul) on top of the Basis plan.
Architecture decision: one Stripe subscription per school with multiple line items — base plan + per-module flat add-ons.
This gives schools a single monthly invoice and lets us add/remove modules mid-cycle via Stripe's `SubscriptionItemService`.

Billing is currently disabled (`SELF_SERVE_ENABLED = false` in `BillingPage.tsx`), so all API changes ship dark until modules are ready to sell.

---

## Step 1 — `StripeOptions.cs`

**File:** `api/Skoleplanen.Api/StripeOptions.cs`

- Rename `PriceId` → `BasePriceId` (keep `[Required]`)
- Add `public Dictionary<string, string> ModulePriceIds { get; init; } = new();`

**File:** `api/Skoleplanen.Api/Services/SubscriptionService.cs` line 76

- `stripeOptions.Value.PriceId` → `stripeOptions.Value.BasePriceId`

**Files:** `api/Skoleplanen.Api/appsettings.json` + `appsettings.Development.json`

- Rename key `"PriceId"` → `"BasePriceId"` in the `"Stripe"` section

---

## Step 2 — `SubscriptionModule` enum

**New file:** `api/Skoleplanen.Api/Models/SubscriptionModule.cs`

```csharp
namespace Skoleplanen.Api.Models;

public enum SubscriptionModule
{
    ParentModule,
    BoardModule,
}
```

Keys must match the dictionary keys in `StripeOptions.ModulePriceIds`.

---

## Step 3 — `SubscriptionModuleItem` entity

**New file:** `api/Skoleplanen.Api/Models/SubscriptionModuleItem.cs`

```csharp
namespace Skoleplanen.Api.Models;

public sealed class SubscriptionModuleItem
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public Subscription Subscription { get; set; } = null!;
    public SubscriptionModule Module { get; set; }
    public string StripeSubscriptionItemId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}
```

Use `IEntityTypeConfiguration<SubscriptionModuleItem>` (project convention — wire up via `ApplyConfigurationsFromAssembly`):
- Unique index on `(SubscriptionId, Module)`
- Cascade delete on FK

**`api/Skoleplanen.Api/Models/Subscription.cs`** — add nav property:

```csharp
public ICollection<SubscriptionModuleItem> ActiveModules { get; set; } = [];
```

**`AppDbContext`** — add:

```csharp
public DbSet<SubscriptionModuleItem> SubscriptionModuleItems => Set<SubscriptionModuleItem>();
```

**Run migration:**

```
dotnet ef migrations add Add_SubscriptionModuleItems --project api/Skoleplanen.Api
```

---

## Step 4 — Register `SubscriptionItemService` in DI

In whichever file registers the other Stripe services (e.g. `StripeExtensions.cs` or `Program.cs`), add:

```csharp
services.AddSingleton<SubscriptionItemService>();
```

---

## Step 5 — `SubscriptionService` additions

**File:** `api/Skoleplanen.Api/Services/SubscriptionService.cs`

Inject `SubscriptionItemService` via constructor.

**Add `AddModuleAsync`:**

```csharp
public async Task AddModuleAsync(Guid schoolId, SubscriptionModule module, CancellationToken ct = default)
{
    var sub = await GetOrCreateAsync(schoolId, ct);

    if (sub.StripeSubscriptionId is null)
        throw new InvalidOperationException("School does not have an active Stripe subscription.");

    if (!stripeOptions.Value.ModulePriceIds.TryGetValue(module.ToString(), out var priceId))
        throw new InvalidOperationException($"No Stripe price configured for module {module}.");

    var alreadyActive = await db.SubscriptionModuleItems
        .AnyAsync(m => m.SubscriptionId == sub.Id && m.Module == module, ct);
    if (alreadyActive) return;

    var item = await subscriptionItemService.CreateAsync(new SubscriptionItemCreateOptions
    {
        Subscription = sub.StripeSubscriptionId,
        Price = priceId,
        Quantity = 1,
    }, cancellationToken: ct);

    db.SubscriptionModuleItems.Add(new SubscriptionModuleItem
    {
        Id = Guid.NewGuid(),
        SubscriptionId = sub.Id,
        Module = module,
        StripeSubscriptionItemId = item.Id,
        CreatedAt = DateTimeOffset.UtcNow,
    });
    await db.SaveChangesAsync(ct);
}
```

**Add `RemoveModuleAsync`:**

```csharp
public async Task RemoveModuleAsync(Guid schoolId, SubscriptionModule module, CancellationToken ct = default)
{
    var sub = await GetOrCreateAsync(schoolId, ct);

    var moduleItem = await db.SubscriptionModuleItems
        .FirstOrDefaultAsync(m => m.SubscriptionId == sub.Id && m.Module == module, ct);
    if (moduleItem is null) return;

    await subscriptionItemService.DeleteAsync(moduleItem.StripeSubscriptionItemId,
        new SubscriptionItemDeleteOptions(), cancellationToken: ct);

    db.SubscriptionModuleItems.Remove(moduleItem);
    await db.SaveChangesAsync(ct);
}
```

---

## Step 6 — `BillingController` endpoints

**File:** `api/Skoleplanen.Api/Controllers/BillingController.cs`

Add three endpoints, all behind `[Authorize(Roles = "admin")]`:

```csharp
[HttpGet("modules")]
public async Task<ActionResult<IEnumerable<SubscriptionModule>>> GetActiveModules(CancellationToken ct)

[HttpPost("modules")]
public async Task<IActionResult> AddModule([FromBody] ModuleRequest request, CancellationToken ct)

[HttpDelete("modules/{module}")]
public async Task<IActionResult> RemoveModule(SubscriptionModule module, CancellationToken ct)
```

- `InvalidOperationException` → 400 ProblemDetails
- `StripeException` → 502 ProblemDetails

---

## Step 7 — Stripe dashboard setup (manual, before enabling billing)

1. Create a Stripe Product for each module (ParentModule, BoardModule)
2. Create a monthly recurring Price on each product
3. Add the price IDs to `appsettings` under `Stripe:ModulePriceIds`:
   ```json
   "ModulePriceIds": {
     "ParentModule": "price_xxx",
     "BoardModule":  "price_yyy"
   }
   ```
4. Note: `SubscriptionItemService.CreateAsync` is the correct approach for adding modules to an existing subscription mid-cycle — Stripe handles proration automatically. No new Checkout session needed.

---

## Verification

1. `dotnet build` — no compile errors after rename
2. Migration SQL looks correct: new table, FK, unique index on `(SubscriptionId, Module)`
3. `dotnet test` — API integration tests pass
4. Manual: add a module via `POST /api/v1/billing/modules` against a test school with an active Stripe subscription; verify line item appears in Stripe dashboard and row appears in DB
5. Manual: remove the module; verify line item removed from Stripe and row deleted
