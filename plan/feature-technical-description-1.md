---
goal: Műszaki leírás generálás (a Doorstar dokumentum-négyes 4. eleme)
version: 1.0
date_created: 2026-07-10
owner: Cabinet root
status: 'Done'
tags: [feature, mcphost, docs, doorstar]
---

# Bevezetés

A tankönyvi séma (faipari_muszaki_dokumentacio_rag.md §2.1) szerint a műszaki leírás:
név + befoglaló méret (korpusznál front W×H, majd mélység) + felhasznált anyagok +
szerkezeti felépítés + felületkezelés. Nálunk kiegészül a record_design_intent-tel
gyűjtött tervezői szándékokkal (REQ-008 hozadéka: jobb dokumentáció).

## 1. Requirements & Constraints
- REQ-001: `TechnicalDescriptionGenerator` — pure static; bemenet: Skeleton + intents + katalógus; kimenet: strukturált szekciók + kész magyar markdown.
- REQ-002: Befoglaló méret a korpusz-konvencióval: Szélesség × Magasság × Mélység mm (tankönyv 2.1).
- REQ-003: Anyagok kategóriánként (lapanyag/hátlap/élzáró), katalógus-névvel; segédanyag/ragasztó NEM (tankönyv).
- REQ-004: Szerkezeti felépítés a Rebuild-logika alapján (oldalak átmenők, fedél/fenék közéjük, hátlap BackOffset beütéssel), élzárás megjelölve.
- REQ-005: Felületkezelés a felület-attribútumból (laminált→nem igényel; festett→festés; fóliás→fóliázás).
- REQ-006: `skeleton_technical_description` tool — webre kész JSON (szekciók) + `markdown` mező.

## 2. Implementation Steps
- TASK-001 ✅: `Docs/TechnicalDescriptionGenerator.cs` — pure static; szekciók + kész magyar markdown; felületkezelés-szöveg a felület-attribútumból (laminált→nem igényel, festett→festés, fóliás→fóliázás).
- TASK-002 ✅: `Tools/DocsTools.cs` — `skeleton_technical_description` (strukturált JSON + `markdown` mező).
- TASK-003 ✅: 7 unit teszt (TechnicalDescriptionTests) → össz. **46 teszt PASS**; smoke 16 tool → **PASS** (méret 800×720×560 a Width-módosítás után; Korpusz/Hátlap/Élzáró szerepek; 3 gyűjtött szándék a dokumentumban).

**Eredmény:** a Doorstar dokumentum-négyes TELJES: Műszaki leírás ✅, Anyagszükséglet ✅, Szabásterv ✅, Árkalkuláció ✅ — mind a parametrikus Skeleton-modellből, webre kész JSON-ként (REQ-006/007/008 mind teljesül).
