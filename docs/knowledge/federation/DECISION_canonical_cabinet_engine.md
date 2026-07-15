# DÖNTÉS-JAVASLAT: A kanonikus Cabinet-motor — `CabinetBilder.Core` vs. platform `Modules.Cabinet`

> Státusz: **JAVASLAT — VPS-válaszra vár** (CP-CAD-3 sarokkő, EPIC-CAD-ENGINE).
> Készítette: Cabinet root, 2026-07-14. Címzett: VPS architect/root.
> Ez a sarokkő „döntés dokumentálva" fele; a „VPS-válasz" fele az MCP-n kiment.

## Kontextus — hol a duplikáció-kockázat

A VPS `ECOSYSTEM_MODULE_ARCHITECTURE.md` (ADR-019) a `Modules.Cabinet`-et így definiálja:

> **Modules.Cabinet** *(ÚJ)* — „CabinetOrder aggregate, **korpusz-specifikus domain
> (fiókok, polcok, frontok, vasalatok)**" — státusz: *Tervezendő*.

Ugyanezt a korpusz-domaint a lokál Cabinet-oldal **már implementálja**, éles, tesztelt kóddal:

| Réteg | Hol | Mit birtokol |
|---|---|---|
| Parametrikus motor | `CabinetBilder.Core` (`Skeleton`, `SkeletonComponent`, `SkeletonParameter`) | Geometria (`Skeleton.Rebuild()`), alkatrész-dekompozíció, paraméter-kényszerek, validáció |
| BOM + árkalkuláció | `CabinetBilder.McpHost` (`BomAggregator`, `CostingTools`, `CuttingPlanner`, `LaborEstimator`) | Anyagszükséglet, 11-lépéses árkalkuláció, szabásterv, munkaidő |
| Platform-adapter | `CabinetBilder.SpaceOsModule` (`CabinetSkeletonProduct : IParametricProduct`) | A Skeleton kiajánlása a SpaceOS Kernel `IParametricProduct` kontraktusán |
| MCP-felület | `CabinetBilder.McpHost` (7 tool, smoke PASS — CP-CAD-1) | `skeleton_create → compute_bom → cost_calculation` lánc |

A `CabinetSkeletonProduct` már ma is kimondja az álláspontot (kód-idézet):

```csharp
// The Skeleton computes its own geometry (see Skeleton.Rebuild()), so the
// Kernel-supplied engine isn't needed here — it's accepted only to satisfy
// the interface signature.
public Task<GeometryResult> GenerateGeometry(IGeometryEngine engine) {
    _skeleton.Rebuild();               // a domain-logika a Core-ban van
    ...
}
```

Ha a `Modules.Cabinet` (platform, TS) **újraimplementálja** a fiók/polc/front/vasalat-geometriát és a BOM-ot, akkor két, egymással sodródó igazságunk lesz ugyanarra a korpusz-domainre — pontosan az a duplikáció, amit az ADR-019 elve (közös vs. trade-specifikus) el akar kerülni. Itt a duplikáció nem „közös vs. trade-specifikus", hanem **„platform-újraimplementálás vs. meglévő kanonikus motor"**.

## Javasolt döntés (Cabinet-oldal)

**A `CabinetBilder.Core` a kanonikus korpusz-geometria/BOM/parametrikus motor. A platform `Modules.Cabinet` NEM implementálja újra a domaint — vékony driver, ami delegál.**

Konkrét felelősség-vágás:

- **`Modules.Cabinet` (platform, TS) birtokolja:** `CabinetOrder` aggregate, tenant/B2B handshake, rendelés-állapot, modul-engedélyezés — vagyis a *platform/orchestration* aggnát. Geometriát/BOM-ot NEM számol.
- **`CabinetBilder.Core` + McpHost (C#) birtokolja:** minden korpusz-számítás (geometria, alkatrészek, BOM, szabásterv, árkalkuláció). A platform ezt az `IParametricProduct` adapteren / McpHost tool-láncon **hívja**, nem másolja.
- **Integrációs felület:** a már meglévő `CabinetSkeletonProduct : IParametricProduct` és a McpHost 7-tool szerződés. Egy határ, egy igazság.

Ez az ADR-019 saját elvének kiterjesztése: a trade-specifikus *számítás* itt már létezik egy kanonikus motorban — ne derítsük le újra TS-ben.

## A VPS-nek feltett eldöntendő kérdés

1. Elfogadható-e, hogy a `Modules.Cabinet` **driver-modul**, ami a korpusz-geometriát/BOM-ot a `CabinetBilder.Core`/McpHost-ra delegálja (nem reimplementál)?
2. Ha igen: a delegálás felülete a meglévő `IParametricProduct` adapter + McpHost tool-lánc legyen, vagy a VPS más integrációs határt szeretne (pl. REST-kontraktus a McpHost előtt)?
3. Ha nem (a platform saját TS-domaint akar a korpuszra): mi a `CabinetBilder.Core` jövőbeli szerepe — csak AutoCAD-oldali CAD-motor marad, és a gyártás-előkészítő domain átköltözik a platformra? (Ez nagy irányváltás, külön ADR kell hozzá.)

## Következmény, ha elfogadják

- CP-CAD-3 sarokkő teljesül (VPS-válasz + dokumentált döntés).
- A `Modules.Cabinet` terve driver-modullá szűkül; a `CabinetBilder.*` marad a korpusz-igazság.
- A duplikáció strukturálisan kizárva, nem review-val karbantartva.
