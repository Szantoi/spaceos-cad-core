---
goal: Élzárás + 11 lépéses árkalkuláció + VPS-beküldés (outbox) a MCPHost-ban
version: 1.0
date_created: 2026-07-10
owner: Cabinet root
status: 'Done'
tags: [feature, mcphost, edging, costing, outbox, doorstar]
---

# Bevezetés

Három összefüggő feature: (A) élzárás — a BomLine.EdgingId kitöltése + élhossz/élköltség;
(B) árkalkuláció — a tankönyvi 11 lépéses összetett séma (woodwork_domain.md §10, fig-2.21),
konfigurálható százalékokkal; (C) VPS-beküldés — a cutting-sheet payload tartós beírása a
lokál SQLite outboxba (OutboxOperation.SubmitCuttingSheet), ahonnan az OutboxWorker + éles
HTTP-kliens élesedésekor magától kimegy.

## 1. Requirements & Constraints
- REQ-A1: Skeleton `EdgingId` String paraméter (default ABS2_WHITE); ComputeBom a korpusz-sorokra teszi (hátlapra nem).
- REQ-A2: Katalógus: élzáró anyagok (ABS2_WHITE, ABS2_SONOMA; ár fm-alapú, BodyJson unit=fm).
- REQ-A3: `EdgingCalculator` — élhossz: max(L,W)*db (1 hosszú él/panel, PoC); élzárónkénti összesítés (fm + költség).
- REQ-A4: `skeleton_material_summary` kap `edging` blokkot; `skeleton_set_material` target='edging'.
- REQ-B1: `CostCalculator` — 11 lépés: anyag, bér(óra*órabér), járulék%, egyéb, közvetlen, általános%(közvetlenre), önköltség, nyereség%(önköltségre), kalkulált, nettó (1000-re LE kerekítve), bruttó (áfa%). MINDEN % konfigurálható paraméter (tankönyv: ne drótozzuk be).
- REQ-B2: `skeleton_cost_calculation` tool — anyagköltség automatikusan (lap + élzáró), a többi input paraméter; kimenet: mind a 11 lépés címkézve.
- REQ-C1: `skeleton_submit_cutting_sheet` — payload → EnqueueOutboxAsync(SubmitCuttingSheet); őszinte válasz: Pending marad, míg a worker/HTTP él.
- CON-1: CatalogSeeder v2: akkor is seedel, ha a cache-ből HIÁNYZIK valamely interim kód (a meglévő client.db-ben már bent az 5 régi anyag!).

## 2. Implementation Steps
- TASK-001 ✅: Core EdgingId param (default ABS2_WHITE) + ComputeBom edging (MaterialId==CarcassMaterialId sorokra; hátlap null).
- TASK-002 ✅: CatalogSeeder v2 (üres VAGY hiányzó interim kód → seed; etag interim-v2) + ABS2_WHITE/ABS2_SONOMA (ár fm-alapú).
- TASK-003 ✅: `Edging/EdgingCalculator.cs` (élhossz=max(L,W)*db; élzárónkénti fm+költség) + material_summary `edging` blokk (totalEstimatedCost-ba beszámít) + set_material target='edging'.
- TASK-004 ✅: `Costing/CostCalculator.cs` (11 lépés, minden % paraméter, lépésenként egész Ft, nettó 1000-re LEFELÉ) + `skeleton_cost_calculation` tool (anyagköltség automatikus: lap+élzáró).
- TASK-005 ✅: `skeleton_submit_cutting_sheet` — payload-builder kiemelve (BuildCuttingSheetPayload tuple sha256-tal), EnqueueOutboxAsync(SubmitCuttingSheet) → tartós Pending az SQLite outboxban.
- TASK-006 ✅: 9 új teszt (CostCalculatorTests 3 — a tankönyvi fig-2.21 példa SZÁMRA egyezik: 157986/189583/218020/276860; EdgingCalculatorTests 5; CatalogSeederTests v2 3-ra bővítve) → össz. **39 teszt PASS**; smoke 15 tool → **PASS** (élzáró 2.97m/652.96 Ft; kalkuláció nettó 139000/bruttó 176530; outboxPending 1).

## 3. Risks & Assumptions
- ASSUMPTION-A: élhossz = 1 hosszú él/panel (PoC; a valós élzárás-térkép későbbi finomítás).
- ASSUMPTION-B: nettó eladási ár kerekítés = 1000-re LEFELÉ (tankönyv: "akár lefelé is").
- ASSUMPTION-C: az outbox-enqueue a helyes beküldési út (durable); az OutboxWorker a hostban tiltva marad.
