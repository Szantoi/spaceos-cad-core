---
goal: Katalógus-integráció + valós anyag-hozzárendelés a BOM-ba (MCPHost)
version: 1.0
date_created: 2026-07-10
owner: Cabinet root
status: 'Done'
tags: [feature, mcphost, catalog, bom]
---

# Bevezetés

A MCPHost BOM-ja jelenleg `materialId=""`-t ad. Ez a feature valós anyag-hozzárendelést hoz a
parametrikus modellbe (REQ-006), katalógus-toolokat ad a MCPHost-hoz, és a BOM-ot a katalógusból
dúsítja. A VPS katalógus-API még nem él (Week 5-6), ezért **interim statikus katalógust** seedelünk
a lokál SQLite cache-be; a `PullMaterialsAsync` bekötése a VPS-API élesedésekor egy sor.

## 1. Requirements & Constraints

- **REQ-001**: A Skeleton kap `CarcassMaterialId` és `BackMaterialId` String paramétert; a Rebuild
  a komponensekhez rendeli (oldalak/fedél/fenék = carcass, hátlap = back). Parametrikus elv (REQ-006).
- **REQ-002**: MCPHost `list_materials` tool — a lokál cache-ből; ha üres, interim seed.
- **REQ-003**: MCPHost `skeleton_set_material` tool — validál a katalógus ellen, majd ApplyParameter + Rebuild.
- **REQ-004**: `skeleton_compute_bom` a BOM-sorokat a katalógusból dúsítja (displayName, category, thickness, price).
- **CON-001**: A VPS-t NEM hívjuk (RISK-002 vonal) — csak lokál cache + interim seed.
- **CON-002**: Az interim katalógus Doorstar-realisztikus (bútorlap 18mm, HDF 3mm, festett/fóliás később).

## 2. Implementation Steps

- TASK-001 ✅: Core — Skeleton `CarcassMaterialId`/`BackMaterialId` String paraméter + Rebuild MaterialId-hozzárendelés (oldalak/fedél/fenék=carcass, hátlap=back). Core build 0 error.
- TASK-002 ✅: MCPHost `Catalog/CatalogSeeder.cs` — interim katalógus C#-ban (5 anyag, Doorstar-realisztikus), `EnsureSeededAsync` (üres cache → UpsertMaterialCacheAsync). JSON-fájl helyett hardcode (robusztusabb).
- TASK-003 ✅: MCPHost `Tools/CatalogTools.cs` — `list_materials` (+ seed), `list_templates`.
- TASK-004 ✅: MCPHost `skeleton_set_material` tool (target carcass/back, validál a katalógus ellen → ApplyParameter → Rebuild).
- TASK-005 ✅: `skeleton_compute_bom` katalógus-join (materialName/category/unitPrice a materialId alapján).
- TASK-006 ✅: `smoke-test.py` bővítve — 10 tool, BOM valós materialId+név, set_material carcass→SONOMA tükröződik. **PASS.**

**Eredmény:** build 0 error, smoke PASS. A BOM most valós anyagot hordoz (LAM18_W1000/HDF3_WHITE + név), a parametrikus elv megtartva (az anyag Skeleton-paraméter → set_material → Rebuild → BOM).

**Nyitott (TASK-008 akadály):** a `CabinetBilder.Tests` projekt az `Adapter.AutoCAD`-ra hivatkozik, ami AutoCAD 2027 nélkül nem fordul → a unit tesztek e gépen nem futtathatók. Megoldás-javaslat: külön AutoCAD-mentes teszt-projekt (McpHost + Skeleton domain) az Adapter-ref nélkül.

## 3. Files
- Core/Skeleton/Skeleton.cs (material paraméterek + Rebuild)
- CabinetBilder.McpHost/Catalog/interim-materials.json (új)
- CabinetBilder.McpHost/Catalog/CatalogSeeder.cs (új)
- CabinetBilder.McpHost/Tools/CatalogTools.cs (új)
- CabinetBilder.McpHost/Tools/SkeletonTools.cs (set_material + compute_bom dúsítás)

## 4. Risks & Assumptions
- RISK-001: a String-paraméter olvasása Rebuild-ben (GetParameterValue<string>) — Convert.ChangeType stringre OK.
- ASSUMPTION-001: az interim katalógus elég a PoC-hez; a VPS-API élesedésekor PullMaterials váltja ki.
