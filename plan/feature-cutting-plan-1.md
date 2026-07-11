---
goal: Szabásterv (szabásjegyzék + tábla-becslés) + VPS lapszabász-payload
version: 1.0
date_created: 2026-07-10
owner: Cabinet root
status: 'Done'
tags: [feature, mcphost, cutting, doorstar, vps-integration]
---

# Bevezetés

A Doorstar 'Asztalos - Szabászati Tételek' dokumentum megfelelője: a Skeleton BOM-jából
szabásjegyzék (cut pieces ráhagyással + rostirány), anyagonkénti tábla-becslés, és a VPS
lapszabász-modul (EPIC-CUTTING-Q3) bemenő payloadja. A tényleges nesting a VPS dolga —
mi a szabásjegyzéket állítjuk elő és submitálásra készítjük.

## 1. Requirements & Constraints
- REQ-001: `CuttingPlanner` — BOM-sorokból CutPiece-ek: kész méret + vágási méret (ráhagyással), rostirány, él.
- REQ-002: Ráhagyás (sizing allowance) konfigurálható, per-él mm; alapértelmezett a woodworking-standard szerint.
- REQ-003: Anyagonkénti tábla-becslés standard táblaméretből (becsült db, kihasználtsági faktorral). NESTING = VPS.
- REQ-004: `skeleton_cutting_plan` tool — szabásjegyzék + tábla-becslés (webre kész JSON).
- REQ-005: `skeleton_cutting_sheet` tool — VPS lapszabász draft-séma payload (name, length_mm, width_mm, thickness_mm, materialId, edgingId, quantity + metadata{source, sha256, generatedAt}). SubmitCuttingSheetAsync-ready.
- CON-001: NINCS éles VPS-hívás (a BOM-submit API még nem él, Week 4-5) — csak a payloadot építjük.
- CON-002: Rostirány/tábla-becslés informatív; a valódi nesting+optimalizálás a VPS EPIC-CUTTING-Q3.

## 2. Implementation Steps
- TASK-001 ✅: `Cutting/StandardBoards.cs` — kategóriánkénti tábla (bútorlap/front 2800x2070 grain=hossz; hátlap grain=nincs), UsableFactor 0.8.
- TASK-002 ✅: `Cutting/CuttingPlanner.cs` — CutPiece + CuttingMaterialSummary + CuttingPlan + Plan(lines, catalog, allowanceMm). Pure static.
- TASK-003 ✅: `skeleton_cutting_plan` (szabásjegyzék + tábla-becslés) + `skeleton_cutting_sheet` (VPS-payload: length_mm/width_mm=vágási méret, metadata{source, sha256, allowanceMm, generatedAt}, submitted=false).
- TASK-004 ✅: 4 unit teszt (CuttingPlannerTests) → össz. **30 teszt PASS**; smoke bővítve 13 tool → **PASS**.

**Eredmény:** build 0 error, 30 teszt PASS, smoke PASS. Ráhagyás 10mm → cut 740 vs finished 720, grain hossz, tábla-becslés, VPS-payload sha256-tal. A nesting a VPS EPIC-CUTTING-Q3.

## 3. Files
- CabinetBilder.McpHost/Cutting/StandardBoards.cs (új)
- CabinetBilder.McpHost/Cutting/CuttingPlanner.cs (új)
- CabinetBilder.McpHost/Tools/CuttingTools.cs (új)
- CabinetBilder.McpHost.Tests/* (új tesztek)

## 4. Risks & Assumptions
- ASSUMPTION-001: alap ráhagyás = 0 mm/él (laminált végméretre vág); a shop felülírhatja (allowanceMm).
- ASSUMPTION-002: standard tábla 2800x2070 mm; kihasználtsági faktor 0.8; tábla-becslés = ceil(összterület / (tábla * faktor)).
- ASSUMPTION-003: rostirány kategóriából/felületből (bútorlap/front = hossz; hdf = nincs).
