---
name: orchestrator-calibration
description: 'Knowledge Steward calibration and knowledge update protocols for global and project-level knowledge bases. Use when updating knowledge after Architect approval or calibrating agent behaviors.'
domain: management
last_updated: 2026-02-24
---

?? Kalibrációs Kategóriák

1. Tech Stack Frissítés (Global Skills):
   * Ha egy új .NET könyvtár vagy EF Core megoldás stabilnak bizonyult az Epic során.
   * Mûvelet: src/agent-system/database/roles/global/skills/{tech}\.skill.md frissítése az új mintákkal.
2. Pattern & Standard Finomítás:
   * Ha a Clean Architecture implementációja során hatékonyabb mappastruktúra vagy naming convention alakult ki.
   * Mûvelet: src/agent-system/database/roles/global/core/constraints.md vagy standards/ frissítése.
3. Sablon (Template) Optimalizálás:
   * Ha a task\.template.md vagy epic\_plan\.template.md bizonyos szekciói feleslegesek voltak, vagy hiányzott belõlük valami (pl. Security check).
   * Mûvelet: src/agent-system/database/roles/global/templates/\*.md módosítása.

?? Orchestrator Protokoll

* Step 1: Architect Sign-off beolvasása (4. szekció: Kalibrációs jóváhagyás).
* Step 2: Konfliktus ellenõrzés (A módosítás nem sért-e más globális szabályt?).
* Step 3: Fájlok frissítése az "Incremental Change" elv mentén (csak a szükséges sorok módosítása).
* Step 4: Verifikáció: A frissített Skill/Template kompatibilis-e a Loader Policy-val.
