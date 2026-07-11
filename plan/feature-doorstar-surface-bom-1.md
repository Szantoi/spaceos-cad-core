---
goal: Doorstar felület-attribútum (festett/fóliás) + anyagszükséglet-összesítő a BOM-ban
version: 1.0
date_created: 2026-07-10
owner: Cabinet root
status: 'Done'
tags: [feature, mcphost, bom, doorstar]
---

# Bevezetés

A valós Doorstar gyártás-dokumentáció (`Műhely - Menyíségek.pdf`) az anyagszükségletet
**felület-variánsonként** (festett/fóliás) bontja. A BOM-nak hordoznia kell a felület-attribútumot,
és kell egy anyagszükséglet-összesítő (anyagonként/felületenként: db, terület m², becsült költség).
A felület az anyag tulajdonsága (MaterialDto.BodyJson `finish`), ezért a MCPHost-oldali katalógus-join-ban
származtatjuk — a parametrikus domain (Skeleton) tiszta marad.

## 1. Requirements & Constraints
- REQ-001: `MaterialFinish` helper — a BodyJson `finish` → normalizált magyar felület-címke (festett/fóliás/laminált/hdf).
- REQ-002: `skeleton_compute_bom` minden sora kap `surface` mezőt.
- REQ-003: `BomAggregator` — a BOM-sorokat anyagonként összesíti (surface, db, terület m², egységár, becsült költség).
- REQ-004: `skeleton_material_summary` tool — az összesítést adja (Anyagszükséglet-kimenet), webre kész JSON.
- CON-001: NINCS VPS-hívás; lokál cache/interim katalógus.
- CON-002: A felület származtatott (anyag-tulajdonság), nem a Skeleton-domain része.

## 2. Implementation Steps
- TASK-001 ✅: `Catalog/MaterialFinish.cs` (BodyJson finish → magyar címke: festett/fóliás/laminált/hdf hátlap/ismeretlen).
- TASK-002 ✅: `Bom/BomAggregator.cs` (`MaterialSummaryLine` + `Summarize` anyagonként: surface, db, terület m², egységár, becsült költség).
- TASK-003 ✅: `skeleton_compute_bom` minden sora `surface` mezőt kap; új `skeleton_material_summary` tool (lines + totalAreaM2 + totalEstimatedCost).
- TASK-004 ✅: 10 új unit teszt (MaterialFinishTests 7, BomAggregatorTests 3) → össz. **26 teszt PASS**; smoke bővítve 11 tool + material_summary → **PASS**.

**Eredmény:** build 0 error, 26 unit teszt PASS, stdio smoke PASS. A BOM felület-attribútumot hordoz, az összesítő a Doorstar 'Menyíségek' kimenet felé visz (anyag+felület+terület+becsült költség).

## 3. Files
- CabinetBilder.McpHost/Catalog/MaterialFinish.cs (új)
- CabinetBilder.McpHost/Bom/BomAggregator.cs (új)
- CabinetBilder.McpHost/Tools/SkeletonTools.cs (surface + material_summary)
- CabinetBilder.McpHost.Tests/* (új tesztek)

## 4. Risks & Assumptions
- ASSUMPTION-001: terület = length*width/1e6 * quantity (él/hulladék nélkül, PoC).
- ASSUMPTION-002: egységár m²-alapú (MaterialDto.Price), becsült költség = terület * egységár.
