---
name: context-structure-management
description: 'Knowledge base structure maintenance protocols for src/agent-system/database/roles. Use when the Knowledge Steward audits documentation consistency or manages multi-agent pattern integration.'
domain: management
last_updated: 2026-02-24
---

# ?? Context Structure Management Skill

**Summary:** Ez a skill definiálja a `src/agent-system/database/roles` mappa struktúrájának mélyreható ismeretét és karbantartási protokolljait. A Knowledge Steward elsõdleges eszköze a dokumentációs konzisztencia fenntartásához.

> **?? 2026-02-20 – Refactoring:** `docs/agent/` ? `src/agent-system/database/roles/` átnevezés + **Multi-Agent Pattern** integráció.
> Minden role-hoz érhetõ el `agents/` (specializált sub-agentek) és `instructions/` (best practice guidelines).
> Érintett role-ok: ui_ux_designer, backend_developer, frontend_developer, qa_tester, architect, knowledge_steward.

---

## ?? Mikor töltsd be?

- **Új ágens létrehozásakor**: Biztosítani kell a megfelelõ mappastruktúrát és kötelezõ fájlokat.
- **Dokumentáció validációjakor**: Ellenõrizni kell, hogy minden fájl a megfelelõ helyen van és követi a struktúrális szabályokat.
- **Refaktorálás/átszervezés során**: Biztonságosan kell tudni mozgatni vagy átnevezni fájlokat.
- **Üres mappák/félkész strukturák kezelésekor**: Azonosítani kell a hiányosságokat.
- **Inkonzisztenciák javításakor**: Egységesíteni kell az elnevezéseket és struktúrákat.

---

## ?? A `src/agent-system/database/roles` Mappa Architekturális Szabályai

### 1. Hierarchikus Felépítés

```
src/agent-system/database/roles/
+¦¦ core/                    # Globális, role-független dokumentumok
+¦¦ {role_name}/             # Role-specifikus mappák
-   +¦¦ {role}.role.md       # Szerepkör definíció (KÖTELEZÕ)
-   +¦¦ {role}.workflow.md   # Munkafolyamat (KÖTELEZÕ)
-   +¦¦ {role}.runbook.md    # Üzemeltetési kézikönyv (KÖTELEZÕ)
-   +¦¦ agents/              # ?? Specializált sub-agentek (OPCIONÁLIS)
-   -   +¦¦ README.md        # Delegation strategy + döntési táblázat
-   -   L¦¦ {sub_agent}.agent.md
-   +¦¦ instructions/        # ?? Best practice irányelvek (OPCIONÁLIS)
-   -   +¦¦ README.md        # Instructions usage guide
-   -   L¦¦ {topic}.instructions.md
-   +¦¦ skills/              # Technikai képességek (OPCIONÁLIS)
-   -   L¦¦ {skill}.skill.md
-   +¦¦ templates/           # Kimeneti sablonok (OPCIONÁLIS)
-   -   L¦¦ {template}.template.md
-   L¦¦ prompts/             # Kommunikációs sablonok (OPCIONÁLIS)
-       L¦¦ {target_role}_{action}.message.md
L¦¦ orchestrator/            # Speciális orchestrator logika
```

#### Multi-Agent Pattern – Jelenlegi állapot (2026-02-20)

| Role | agents/ | instructions/ | Sub-Agent Count | Instruction Count |
|------|:-------:|:-------------:|:---------------:|:-----------------:|
| ui_ux_designer | ? | ? | 2 | 4 |
| backend_developer | ? | ? | 3 | 7 |
| frontend_developer | ? | ? | 0 | 7 |
| qa_tester | ? | ? | 2 | 3 |
| architect | ? | ? | 2 | 2 |
| knowledge_steward | ? | ? | 2 | 4 |
| tech_lead | ? | ? | 2 | 2 |
| orchestrator | ? | ? | 2 | 2 |
| product_owner | ? | ? | 1 | 1 |
| devils_advocate | ? | ? | 1 | 2 |

### 2. Dokumentum Típusok és Frontmatter

Minden markdown fájlnak tartalmaznia kell YAML frontmatter-t:

```yaml
---
id: {type}-{name}
title: "{Human Readable Title}"
type: {role|workflow|runbook|skill|template|...}
scope: {global|project|agent}
last_updated: YYYY-MM-DD
---
```

#### Típus Katalógus

| Type | Leírás | Példa |
|------|--------|-------|
| `role` | Ágens személyiség és felelõsségek | `architect.role.md` |
| `workflow` | Munkafolyamat-lépések | `backend_developer.workflow.md` |
| `runbook` | Mûködési kézikönyv | `qa_tester.runbook.md` |
| `skill` | Technikai tudás | `backend_dotnet.knowledge.md` |
| `template` | Kimeneti sablon | `qa_signoff.template.md` |
| `core_skill` | Globális képesség | `prompt_engineering.knowledge.md` |
| `constraints` | Korlátozások | `core/constraints.md` |
| `standard` | Szabványok | `definition_of_done_standard.md` |
| `policy` | Irányelvek | `knowledge_structure.policy.md` |
| `message` | Ágensek közötti kommunikációs sablonok | `knowledge_steward_structure_audit.message.md` (CSAK `messages/` mappában!) |
| `registry` | Tudás index | `knowledge_map.md` |
| `sub_agent` | Specializált GitHub Copilot agent mód | `api-architect.agent.md` (CSAK `agents/` mappában!) |
| `instruction` | Best practice irányelvek adott technológiához | `csharp.instructions.md` (CSAK `instructions/` mappában!) |

---

## ??? Karbantartási Minták (N-shot Patterns)

### Minta 1: Új Ágens Mappa Létrehozása

**Szituáció:** Új "Security Engineer" ágens létrehozása.

**Lépések:**

```markdown
1. Hozd létre a mappa struktúrát:
   src/agent-system/database/roles/security_engineer/
   +¦¦ security_engineer.role.md
   +¦¦ security_engineer.workflow.md
   +¦¦ security_engineer.runbook.md
   +¦¦ agents/                          # Ha van specializált sub-agent
   -   L¦¦ README.md
   +¦¦ instructions/                    # Ha van releváns best practice
   -   L¦¦ README.md
   +¦¦ skills/
   L¦¦ templates/

2. Használd a Knowledge Steward sablonokat:
   - src/agent-system/database/roles/management/knowledge_steward/templates/role_structure.template.md
   - src/agent-system/database/roles/management/knowledge_steward/templates/workflow_structure.template.md
   - src/agent-system/database/roles/management/knowledge_steward/templates/runbook_structure.template.md

3. Frissítsd a knowledge_map.md fájlt:
   - Add hozzá az új ágenshez tartozó fájlokat a registry-hez.

4. Ellenõrizd a frontmatter konzisztenciát.
```

### Minta 2: Skill Hozzáadása Meglévõ Ágenshez

**Szituáció:** Új "API Documentation Skill" létrehozása a Backend Developer számára.

```markdown
1. Hozd létre a skill fájlt:
   src/agent-system/database/roles/engineering/backend_developer/skills/api_documentation.knowledge.md

2. Használd a skill_structure.template.md sablont:
   - Töltsd ki a frontmatter-t
   - Add meg a "Mikor töltsd be?" szekciót
   - Írj N-shot példákat
   - Adj hozzá gyakori hibákat

3. Kapcsold össze a backend_developer.runbook.md fájlban:
   - Add hozzá a skill referenciát a megfelelõ szekcióban

4. Frissítsd a knowledge_map.md fájlt.
```

### Minta 3: Inkonzisztencia Javítása

**Szituáció:** A "tamplate" mappákat át kell nevezni "templates"-re.

```powershell
# PowerShell példa (csak illusztráció, valós esetben file operátorokkal):
Rename-Item -Path "src/agent-system/database/roles/discovery/architect/tamplate" -NewName "templates"
Rename-Item -Path "src/agent-system/database/roles/engineering/backend_developer/tamplate" -NewName "templates"
Rename-Item -Path "src/agent-system/database/roles/engineering/frontend_developer/tamplate" -NewName "templates"
```

**Utána:**

- Ellenõrizd, hogy minden belsõ hivatkozás frissült-e.
- Futtass validációt (lásd Validation Pattern).

### Minta 4: Üres Mappa Kezelése

**Szituáció:** Az `architect/skills/` mappa üres.

**Döntési fa:**

```markdown
1. Van szükség Architect-specifikus Skillre?
   - IGEN: Hozz létre skill fájlokat (pl. architecture_patterns.knowledge.md)
   - NEM: Hagyd üresen (de dokumentáld a `knowledge_map.md`-ben, hogy szándékosan üres)

2. Ha 6+ hónapja üres és nincs terv a kitöltésére:
   - Töröld a mappát.
   - Jegyezd fel a `knowledge_map.md`-ben.
```

### Minta 5: Új Prompt Létrehozása ??

**Szituáció:** Tech Lead szeretne egy standard promptot Backend Developer-nek implementation report bekéréséhez.

**Lépések:**

```markdown
1. Használd a prompt template-et:
   src/agent-system/database/roles/management/knowledge_steward/templates/prompt_structure.template.md

2. Töltsd ki a frontmatter-t:
   ---
   id: message-tech_lead_backend_developer_implementation_report
   title: "Tech Lead ? Backend Developer: Implementation Report Request"
   type: message
   scope: global
   category: development
   created: 2026-02-15
   last_updated: 2026-02-15
   usage: "Tech Lead által használt üzenet Backend Developer-tõl implementation report bekéréséhez"
   initiator: "tech_lead"
   target: "backend_developer"
   ---

3. Mentsd el a megfelelõ helyre:
   src/agent-system/database/roles/discovery/tech_lead/messages/backend_developer_implementation_report.message.md

4. Frissítsd a knowledge_map.md fájlt:
   Add hozzá a Prompt Sablonok táblázathoz.

5. (Opcionális) Ha a prompts/ mappa üres volt, dokumentáld az elsõ használatot.
```

**Figyelem:** Prompt fájlok CSAK a `messages/` mappában tárolhatók, és tartalmazniuk kell az `initiator` és `target` mezõket a frontmatter-ben.

### Minta 6: Jegyzetek Fájlok Tisztítása

**Szituáció:** `jegyzet.txt`, `jegyzet.text` fájlok találhatók az ágens mappákban.

**Protokoll:**

```markdown
1. Olvasd el a jegyzet tartalmát.
2. Van benne értékes információ?
   - IGEN: Intergáld a megfelelõ dokumentumba (skill, runbook, workflow)
   - NEM: Töröld
3. Soha ne hagyd meg a jegyzetek fájlokat production állapotban.
```

---

## ?? Gyakori Hibák és Megoldások

| Hiba | Ok | Megoldás |
|------|----|---------:|
| Elírás: "tamplate" | Régóta bent van a struktúrában | Globális átnevezés "templates"-re, hivatkozások frissítése |
| Eltérõ mappa elnevezések ("skill" vs "skills") | Nincs egyértelmû szabály | **Szabály**: Használd a **"skills"** többes számot (kivéve már meglévõ orchestrator és knowledge_steward) |
| Hiányzó frontmatter | Manuálisan létrehozott fájl | Adj hozzá frontmatter-t a típusnak megfelelõen |
| Üres skill mappák | Még nincs technikai tartalom | Töltsd ki vagy töröld, dokumentáld a knowledge_map.md-ben |
| Jegyzetek/jegyzet fájlok | Munka közben maradt | Intergáld vagy töröld, ne hagyd ott |
| Duplikált skill-ek | Nem egyértelmû tulajdonjog | Mozgasd a közös skill-t a `core/` mappába |
| Rossz scope frontmatter-ben | Nem gondolták át | Ágens-specifikus = "agent", Globális = "global", Projekt-specifikus = "project" |

---

## ? Validációs Checklist

A Knowledge Steward ezen checklist alapján ellenõrzi a strukturális integritást:

### Ágens Szintû Validáció

- [ ] **Kötelezõ fájlok léteznek:**
  - [ ] `{role}.role.md`
  - [ ] `{role}.workflow.md`
  - [ ] `{role}.runbook.md`

- [ ] **Frontmatter konzisztencia:**
  - [ ] Minden fájl tartalmaz frontmatter-t
  - [ ] `id` mezõ formátuma: `{type}-{name}`
  - [ ] `type` mezõ érvényes (lásd Típus Katalógus)
  - [ ] `last_updated` dátum formátum: `YYYY-MM-DD`

- [ ] **Mappa elnevezések:**
  - [ ] "templates" (többesszám, nem "tamplate")
  - [ ] "skills" (többesszám, új ágenseknél)

- [ ] **Jegyzetek tisztasága:**
  - [ ] Nincs `jegyzet.txt` vagy hasonló
  - [ ] Nincs debug log fájl (kivéve `core/logs/`)

### Globális Validáció

- [ ] **knowledge_map.md naprakész:**
  - [ ] Minden új fájl szerepel benne
  - [ ] Nincs törött hivatkozás

- [ ] **Konzisztens több ágens között:**
  - [ ] Template-k követik a meta-template struktúrát
  - [ ] Skill-ek követik a skill_structure.template.md-t
  - [ ] Nincsenek duplikált vagy ellentmondó szabályok

---

## ?? Context Optimization Szabályok

### Token Hatékonyság

1. **Csak a releváns fájlokat töltsd be:**
   - Ha Backend Developer taskon dolgozol, NE töltsd be a Frontend skill-eket.

2. **Lazy Loading:**
   - Elõször csak a role és workflow fájlt töltsd be.
   - Skill-eket csak akkor, ha specifikus technikai kérdés merül fel.

3. **Context Slicing:**
   - Closure után az ágens dokumentumokat archívumban tárold, ne a munka memóriában.

### Struktúra Frissítési Protokoll

1. **Incremental Change:**
   - Soha ne írj felül teljes mappa struktúrát.
   - Célzott módosítások (egy fájl, egy szekció).

2. **Conflict Detection:**
   - Ellenõrizd, hogy a változtatás nem sérti-e más ágens dokumentumát.

3. **Rollback Safety:**
   - Minden strukturális változtatásról készíts changelog bejegyzést.

---

## ?? Kapcsolódó Skillek és Dokumentumok

- **[Prompt Engineering Skill](../core/prompt_engineering.knowledge.md)** - Kommunikációs minták
- **[Knowledge Structure Policy](../core/knowledge_structure.policy.md)** - Fájl hierarchia, elnevezési konvenciók, betöltési logika
- **[Knowledge Map](../core/knowledge_map.md)** - Teljes fájl registry
- **[Orchestrator Calibration Skill](./orchestrator_calibration.knowledge.md)** - Globális frissítési protokoll
- **[Skill Structure Template](../templates/skill_structure.template.md)** - Meta-template skillekhez

---

## ?? Strukturális Statisztikák (Referencia)

**Jelenlegi állapot (2026-02-18):**

| Ágens | Role | Workflow | Runbook | Skills | Templates | Prompts |
|-------|:----:|:--------:|:-------:|:------:|:---------:|:-------:|
| architect | ? | ? (2) | ? | ? (1) | ? (2) | ? (1) |
| backend_developer | ? | ? | ? | ? (4) | ? (2) | ? (2) |
| devils_advocate | ? | ? | ? | ? (0) | ? (1) | ? (1) |
| frontend_developer | ? | ? | ? | ? (3) | ? (1) | ? (2) |
| knowledge_steward | ? | ? (3) | ? | ? (2) | ? (5+) | ? (1) |
| orchestrator | ? | ? (2) | ? | ? (3) | ? (3) | ? (7+) |
| product_owner | ? | ? (2) | ? | ? (1) | ? (4) | ? (2) |
| qa_tester | ? | ? | ? | ? (1) | ? (2) | ? (3) |
| tech_lead | ? | ? (3) | ? | ? (1) | ? (3) | ? (3) |

**Összesen:** 9 ágens, ~80+ dokumentum

### Product Owner Teljes Struktúra (2026-02-18 – Új szerepkör)

```
src/agent-system/database/roles/discovery/product_owner/
+¦¦ product_owner.role.md
+¦¦ product_owner.runbook.md
+¦¦ skills/
-   L¦¦ domain_quality_mapping.knowledge.md      # DQM Canvas, ISO 25010, Fitness Functions
+¦¦ templates/
-   +¦¦ dqm_canvas.template.md               # Minden Epic elõtt kötelezõ
-   +¦¦ epic_proposal.template.md            # PO ? Architect
-   +¦¦ strategic_directive.template.md      # PO ? Orchestrator
-   L¦¦ health_report.template.md            # Milestone Health Report
+¦¦ prompts/
-   +¦¦ orchestrator_strategic_directive.message.md
-   L¦¦ architect_epic_proposal.message.md
L¦¦ workflows/
    +¦¦ product_owner.workflow.md            # Section A (Closure), B (On-demand), C (Health)
    L¦¦ product_owner_multi_workspace.workflow.md
```

### Projekt-szintû PO Deliverable-ök (docs/{project}/)

| Fájl | Létrehozza | Mikor |
|:-----|:-----------|:------|
| `product_backlog.md` | PO | Epic Closure Review után (Section A4) |
| `po_strategic_directive.md` | PO | Epic Closure után és On-demand (Section A5, B3) |
| `po_health_report_{YYYY-MM}.md` | PO | Milestone-onként (Section C) |
| `epics/{EPIC}/dqm_canvas.md` | PO | Minden új Epic indítása elõtt |
| `epics/{EPIC}/epic_proposal.md` | PO | Új Epic javaslat Architectnek |

---

*Ez a skill a Knowledge Steward "emlékezete" a dokumentációs struktúráról. Használd validációhoz, refaktoráláshoz és új tartalom létrehozásához.*
