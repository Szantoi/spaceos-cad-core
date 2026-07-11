# ADR-038 — Offcut Creation at Plan Freeze

> **Status:** Proposed — 2026-04-20
> **Architect:** Gábor (Founder) · **Drafted by:** Architect (Claude.ai session)
> **Scope:** Mini-spec · 1 fejlesztési task (CUTTING-042) · ~1-2 dev nap
> **Supersedes:** — · **Related:** CUTTING-028 (Inventory /api/inventory/offcuts endpoint DONE) · CUTTING-038 (`IInventoryReservationAdapter` DONE) · Session B+C Cutting Planning · `SpaceOS_Q3_Planning_Brief_v1.md` Téma 2
> **Implementation target:** Session C lezárása után (CUTTING-039/040/041 után), CUTTING-042 task

---

## 1. Context

A Soft Launch LIVE állapotban a Cutting → Inventory offcut loop **nem zár be**. A `SpaceOS_Q3_Planning_Brief_v1.md` Téma 2 jelzi:

> _"Amikor egy `CuttingJob` befejeződik (`MarkAsCut()`), az `ICuttingEventPublisher.PublishJobCompletedAsync()` tüzel — de az Inventory `WidthMm=0, HeightMm=0` méretű Offcut rekordot hoz létre. Az offcut tracking nem működik."_

### Mi létezik ma (2026-04-20)

| Komponens | Státusz | Megjegyzés |
|---|---|---|
| Inventory `/api/inventory/offcuts` POST endpoint | ✅ DONE (CUTTING-028, commit `2fe889e`) | WidthMm + HeightMm + MaterialCode + SourceId fogadva |
| Inventory Offcut aggregate + Reuse workflow | ✅ DONE (INVENTORY-051 Phase 1) | Full lifecycle endpoint × 6 |
| `IInventoryReservationAdapter` a Cutting-ben | ✅ DONE (CUTTING-038) | Reservation flow, rollback |
| `CuttingPlan.Freeze()` FSM transition | ✅ DONE (Session B) | `CuttingPlanStatus.Frozen` · `CuttingPlanFrozen` domain event |
| `NestingResult.PanelAssignments[].WastePieces[]` | ✅ DONE (`SpaceOS.Nesting.Algorithms` 1.0.0) | Guillotine + FFDH — waste dimensions elérhetők |
| `CuttingPlanFrozen` event handler ami Offcut-ot regisztrál | ❌ HIÁNYZIK | **ez a gap** |

### A gyökérok

A `CuttingJob.MarkAsCut()` idején a domain entitás **nem tárolja** az elhelyezett darabok és a maradékok dimenzióit — azok a `NestingResult.PanelAssignments` listájában vannak, ami a Freeze pillanatában volt deterministic. A `PublishJobCompletedAsync` emiatt `0x0` placeholdert küld.

---

## 2. Decision

| Kérdés | Döntés |
|---|---|
| **Mikor keletkezik az Offcut?** | **Plan Freeze időpontban**, atomikus batch-ben. NEM a `MarkAsCut()`-kor. |
| **Adat forrása?** | `NestingResult.PanelAssignments[].WastePieces[]` — a Freeze-kor immutable-lé váló nesting output |
| **Ki a felelős?** | Új event handler: `RegisterOffcutsOnPlanFrozenHandler` a Cutting Application layerben |
| **Hogyan hív Inventory-t?** | A **létező** `IInventoryReservationAdapter`-t bővítjük `RegisterOffcutsAsync(planId, offcuts, ct)` metódussal — HTTP POST `/api/inventory/offcuts/batch` (új Inventory endpoint) |
| **Failure semantics v1?** | **Best-effort + idempotency key** (`planId`) + 3x exponential retry. Ha végleg fail → audit event + operátor alert. Plan marad Frozen (nem rollback-elhető). |
| **Offcut küszöb?** | Tenant config: `cutting.offcut.min_width_mm` + `min_height_mm` (default 400mm, Growth Strategy Q6). A küszöb alatti waste → `WasteRegistered` esemény, de **nem** kerül be Offcut-ként. |
| **MarkAsCut szerepe?** | **Változatlan** — execution tracking marad. A tervezett vs. tényleges waste discrepancy-t egy későbbi sprint kezeli (ld. §7). |

### Rejected alternatives

| Alternatíva | Miért nem |
|---|---|
| **A) CuttingJob tárolja a PlacedDimensions-t** (brief opció A) | Denormalizáció nélkül is elérhető a NestingResult-ból. A PlacedDimensions job-on való tárolása csak a tüneti hibát oldaná — a tervezett vs. tényleges waste közötti szakadékot nem. |
| **C) Outbox pattern v1-ben** | +2 dev nap, és a KERNEL AuditChain outbox kiemelése Module szintre self-contained refactor. v1-ben best-effort elegendő; outbox-ra v2-ben felülvizsgálandó (ld. §7 kockázat). |
| **D) MarkAsCut-kor Offcut regisztráció** | Helyi tünetkezelés — de a valóság az, hogy a **tervezett** offcut a Freeze-kor véglegesül, nem a végrehajtáskor. MarkAsCut utáni regisztráció csak a **tényleges** Offcut-ot rögzíti, ami jobb adatminőség, de bonyolultabb (§7 backlog). |

---

## 3. Architecture

### 3.1 Event flow

```
CuttingPlan.Freeze()                       [Cutting Domain]
    │ status: Published → Frozen
    │ raises → CuttingPlanFrozen(planId, tenantId, frozenAt)
    ▼
FreezePlanHandler                          [Cutting Application]
    │ calls nesting provider (or cached result if Published)
    │ loads NestingResult (immutable snapshot per plan)
    │ saves result → PlanNestingSnapshot table
    ▼
PopDomainEvents + DispatchAsync            [DomainEventDispatcher middleware]
    ▼
RegisterOffcutsOnPlanFrozenHandler         [Cutting Application · NEW]
    │ extracts WastePieces from NestingResult
    │ filters by tenant offcut threshold (cutting.offcut.min_*)
    │ batches into OffcutBatchRequest
    ▼
IInventoryReservationAdapter               [Cutting Infrastructure · EXTENDED]
    │ RegisterOffcutsAsync(planId, batch, ct)
    │ HTTP POST /api/inventory/offcuts/batch
    │ idempotency-key: plan-{planId}
    │ retry: 3x exponential (100ms, 500ms, 2500ms)
    ▼
Inventory /api/inventory/offcuts/batch     [Modules.Inventory · NEW endpoint]
    │ upsert by (tenantId, sourceType=CuttingPlan, sourceId=planId)
    │ idempotent — duplicate batch returns existing IDs
```

### 3.2 Interface extension — `IInventoryReservationAdapter`

```csharp
// Modules.Cutting/Application/Adapters/IInventoryReservationAdapter.cs
public interface IInventoryReservationAdapter
{
    // Meglévő (CUTTING-038)
    Task<Result<ReservationId>> ReserveAsync(
        ReservationRequest request, CancellationToken ct);

    Task<Result> ReleaseAsync(ReservationId id, CancellationToken ct);

    // ÚJ — ADR-038
    Task<Result<IReadOnlyList<OffcutId>>> RegisterOffcutsAsync(
        OffcutBatchRequest request, CancellationToken ct);
}

// Application/DTOs/OffcutBatchRequest.cs
public sealed record OffcutBatchRequest(
    Guid PlanId,
    Guid TenantId,
    IReadOnlyList<OffcutItem> Offcuts,
    string IdempotencyKey);  // = $"plan-{PlanId}"

public sealed record OffcutItem(
    string MaterialCode,
    decimal Thickness,
    int WidthMm,
    int HeightMm,
    Guid PanelAssignmentId,
    string? Notes);
```

### 3.3 New handler — `RegisterOffcutsOnPlanFrozenHandler`

```csharp
// Modules.Cutting/Application/EventHandlers/RegisterOffcutsOnPlanFrozenHandler.cs
public sealed class RegisterOffcutsOnPlanFrozenHandler(
    IPlanNestingSnapshotRepository snapshotRepo,
    IInventoryReservationAdapter inventoryAdapter,
    ITenantConfigProvider configProvider,
    ILogger<RegisterOffcutsOnPlanFrozenHandler> logger)
    : INotificationHandler<CuttingPlanFrozen>
{
    public async Task Handle(CuttingPlanFrozen evt, CancellationToken ct)
    {
        var snapshot = await snapshotRepo
            .GetByPlanIdAsync(evt.PlanId, ct).ConfigureAwait(false);
        if (snapshot is null)
        {
            logger.LogWarning("No nesting snapshot for plan {PlanId}", evt.PlanId);
            return;  // best-effort
        }

        var minW = await configProvider.GetIntAsync(
            evt.TenantId, "cutting.offcut.min_width_mm", 400, ct).ConfigureAwait(false);
        var minH = await configProvider.GetIntAsync(
            evt.TenantId, "cutting.offcut.min_height_mm", 400, ct).ConfigureAwait(false);

        var offcuts = snapshot.PanelAssignments
            .SelectMany(pa => pa.WastePieces
                .Where(w => w.WidthMm >= minW && w.HeightMm >= minH)
                .Select(w => new OffcutItem(
                    pa.MaterialCode, pa.Thickness,
                    w.WidthMm, w.HeightMm,
                    pa.Id, Notes: null)))
            .ToList();

        if (offcuts.Count == 0) return;  // no qualifying offcuts

        var request = new OffcutBatchRequest(
            evt.PlanId, evt.TenantId, offcuts,
            IdempotencyKey: $"plan-{evt.PlanId}");

        var result = await inventoryAdapter
            .RegisterOffcutsAsync(request, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            // best-effort: log + audit, NO rollback of Freeze
            logger.LogError(
                "Offcut registration failed for plan {PlanId}: {Errors}",
                evt.PlanId, string.Join(", ", result.Errors));
            // TODO: emit CriticalAuditEvent for operator alert (out of scope for ADR-038)
        }
    }
}
```

### 3.4 Modules.Inventory endpoint (új) — `POST /api/inventory/offcuts/batch`

```csharp
// Modules.Inventory/Api/Endpoints/OffcutEndpoints.cs — NEW route
app.MapPost("/api/inventory/offcuts/batch",
    async (OffcutBatchRequest req, IMediator med, CancellationToken ct) =>
    {
        var cmd = new RegisterOffcutBatchCommand(req);
        var result = await med.Send(cmd, ct);
        return result.ToHttp();
    })
    .RequireAuthorization("InventoryWrite");
```

**Idempotency:** Inventory oldal `(TenantId, SourceType=CuttingPlan, SourceId=PlanId)` unique constraint + upsert. Duplikált batch ugyanazon Offcut ID-kat adja vissza.

---

## 4. Migration & Backwards Compatibility

| Kérdés | Döntés |
|---|---|
| **Adatmigráció a LIVE Doorstar adatokra** | **No-op.** Doorstar 2026-04-20-án ment LIVE — nincs még historikus `CuttingPlanFrozen` event, aminek retroaktívan Offcut-ot kellene kapnia. |
| **Régi 0x0 Offcut rekordok** (ha vannak Inventory-ban) | Egyszeri cleanup script: `DELETE FROM spaceos_inventory.offcuts WHERE width_mm=0 OR height_mm=0` — külön INFRA task, nem az ADR része. |
| **`PublishJobCompletedAsync` 0x0 hívás** | **Deprecálva**: a `MarkAsCut()` handler továbbra is tüzeli a `CuttingJobCompleted` event-et, de az **nem hív** Inventory Offcut endpointot. Execution tracking megmarad, Offcut registry kizárólag Freeze-kor. |
| **Schema migration (Inventory)** | Új UNIQUE constraint `(TenantId, SourceType, SourceId)` a `spaceos_inventory.offcuts` táblán. Új migration `20260422_0001_AddOffcutIdempotencyConstraint`. |

---

## 5. Definition of Done — CUTTING-042 task

### Interface gates
- [ ] `IInventoryReservationAdapter.RegisterOffcutsAsync` metódus hozzáadva + XML doc
- [ ] `OffcutBatchRequest` + `OffcutItem` DTO-k Contracts 1.4.0-ba kerülnek (NuGet bump)
- [ ] `CuttingPlanFrozen` domain event létezik és hordozza `PlanId` + `TenantId` + `FrozenAt`

### Implementation gates
- [ ] `RegisterOffcutsOnPlanFrozenHandler` létezik, `INotificationHandler<CuttingPlanFrozen>`
- [ ] `CuttingInventoryAdapter` (HTTP adapter) implementálja `RegisterOffcutsAsync`-et polly retry-val
- [ ] Tenant config olvasás `cutting.offcut.min_width_mm` + `min_height_mm` (default 400mm)
- [ ] Küszöb alatti waste **nem** kerül Offcut-ba (filter in handler)
- [ ] Modules.Inventory `/api/inventory/offcuts/batch` endpoint létezik, idempotent
- [ ] Migration: `AddOffcutIdempotencyConstraint`

### Test gates
- [ ] Unit: handler happy path — 3 waste piece, mind kvalifikált → 3 Offcut
- [ ] Unit: handler küszöb-filter — 2 small + 1 nagy → 1 Offcut
- [ ] Unit: handler snapshot hiányzik → warning log, no throw
- [ ] Integration: Inventory adapter retry 3x network fail után → success
- [ ] Integration: idempotency — kétszer tüzelt `CuttingPlanFrozen` ugyanazon `planId` → 0 duplikált Offcut (ugyanaz az ID visszaadva)
- [ ] E2E: full Doorstar flow — DoorOrder → CuttingSheet → Plan Freeze → Offcut látható `/api/inventory/offcuts` listában
- [ ] ≥ 8 új teszt összesen

### Security gates
- [ ] `TenantId` a `CuttingPlanFrozen` event-ből (nem request DTO-ból) — SEC-01 konform
- [ ] Inventory endpoint RLS FORCE a `spaceos_inventory.offcuts`-en — már megvan (CUTTING-028)
- [ ] Cross-tenant teszt: Tenant A Frozen event nem hoz létre Tenant B Offcut-ot

### Documentation
- [ ] `docs/knowledge/context/cutting.md` frissítve
- [ ] `SpaceOS_Modules_Cutting_Core_Architecture_v4.md` §3 "NEM scope" sor eltávolítva (Offcut loop már core)
- [ ] `Codebase_Status_YYYYMMDD` → ADR-038 felvéve

---

## 6. Effort & Sequencing

| Sub-task | Becsült idő | Függés |
|---|---|---|
| Inventory `/api/inventory/offcuts/batch` endpoint + idempotency migration | 0.5 nap | — |
| Contracts 1.4.0 bump (OffcutBatchRequest DTO) | 0.25 nap | — |
| `IInventoryReservationAdapter.RegisterOffcutsAsync` + HTTP impl | 0.25 nap | Contracts 1.4.0 |
| `RegisterOffcutsOnPlanFrozenHandler` + tenant config olvasás | 0.25 nap | Adapter kész |
| Tesztek (8+ új) | 0.5 nap | Handler kész |
| Integration + E2E validáció Doorstar stagingen | 0.25 nap | Mind kész |
| **Összesen** | **~2 dev nap** | — |

**Kiadás:** Session C (CUTTING-041) lezárása után. Nem blokkolja Session C-t, de nem is fut párhuzamosan vele (ugyanaz az adapter kód-útvonal).

---

## 7. Risks & Open Questions

| Kockázat | Súlyosság | Mitigáció |
|---|---|---|
| **Best-effort adapter hívás sikertelen** → Plan Frozen, de Offcut nincs Inventory-ban | Közepes | v1: audit event + operátor alert. v2: outbox pattern refactor (külön sprint, ha valós probléma jelentkezik) |
| **Tervezett vs. tényleges waste discrepancy** — MarkAsCut-kor kiderül hogy több/kevesebb a waste | Alacsony v1-ben | v1-ben nem tracked. v2-ben `OffcutActualized` event + discrepancy report. Ehhez kell egy follow-up ADR (jelölt: ADR-039). |
| **Plan Freeze visszafordítása (Unfreeze) Offcut-tal** | Alacsony | Session B FSM: Frozen → Closed egyirányú. Ha a jövőben Unfreeze jön, az ADR-t revíziózni kell (Offcut release). |
| **Inventory endpoint lassú batch-re** (500+ offcut / plan) | Alacsony | Inventory bulk insert EF Core extension-nel — benchmark Doorstar reális mérettel (jellemzően 10-50 offcut / plan) |
| **Idempotency key ütközés** — ugyanaz a `planId` két különböző freeze-e után | Nincs | Session B FSM: Frozen egyszer fordul elő per plan. Double-freeze domain-level block. |

### Open questions (nem blokkoló)

1. **OpenTimestamps integráció** (projekt memory-ból): Az Offcut registration is SHA-256 audit chain része legyen? — jelöltként hagyva, Audit Chain extension ADR külön.
2. **FreeTier Offcut scope** (Growth Strategy Session D): FreeTier anonymous flow is Offcut-ot regisztráljon-e, vagy csak paid tenant? — `ReservationSource` enum már létezik (CUTTING-038 extension), Session D döntés.

---

## 8. Approval

Jóváhagyásra: **Gábor (Founder)** · Implementációs csomag: **Claude Code** (CUTTING-042 taskon keresztül, Session C lezárása után).

**Sign-off sor:**

```
[ ] Gábor — ADR-038 elfogadva, CUTTING-042 task kiadható
[ ] Architect — Mini-spec szakmailag zárva
```
