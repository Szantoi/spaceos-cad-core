---
name: project-documentation-structure
description: 'Project documentation hierarchical structure management for docs/{project}/. Use when the Orchestrator creates or validates project folder integrity and mandatory files.'
domain: management
last_updated: 2026-02-24
---

# ?? Orchestrator: Projekt Dokumentációs Struktúra Menedzsment Skill

**Summary:** Ez a skill definiálja a `docs/{project}/` mappa hierarchikus struktúráját, a kötelezõ és opcionális fájlokat, azok létrehozási és karbantartási protokolljait. Az Orchestrator elsõdleges eszköze a projekt dokumentációs integritás fenntartásához.

---

## ?? Mikor töltsd be?

- **Új projekt indításakor**: Projekt dokumentációs struktúra inicializálása
- **Epic Planning elõtt**: Ellenõrizni kell, hogy a projekt dokumentáció rendben van-e
- **Task Dispatching elõtt**: Validálni kell, hogy az Epic és Task dokumentációk léteznek-e
- **Project State frissítésekor**: Konzisztencia ellenõrzés a state dokumentumokkal
- **Blocker handling során**: Dokumentációs hiányosságok azonosítása
- **Project audit alkalmával**: Teljes dokumentációs integritás ellenõrzés

---

## ??? A `docs/{project}/` Mappa Architekturális Szabályai

### 1. Hierarchikus Felépítés

```
docs/{project}/
+¦¦ goal.md                              # Project célja (KÖTELEZÕ)
+¦¦ state.md                             # Aktuális projekt állapot (KÖTELEZÕ - Orchestrator tartja karban)
+¦¦ dependency_map.md                    # Epic/Task függõségek (KÖTELEZÕ - Orchestrator tartja karban)
+¦¦ orchestrator_decision_log.md         # Orchestrator döntési napló (KÖTELEZÕ - Orchestrator tartja karban)
+¦¦ backlog.md                           # Jövõbeli Epic-ek, ötletek (OPCIONÁLIS)
+¦¦ changelog.md                         # Projekt szintû változások (OPCIONÁLIS)
+¦¦ README.md                            # Projekt áttekintés (AJÁNLOTT)
-
+¦¦ standards/                           # Projekt-specifikus szabványok (AJÁNLOTT)
-   +¦¦ architecture_standards.md       # Architektúrális szabályok
-   +¦¦ coding_standards.md             # Kódolási konvenciók
-   +¦¦ testing_standards.md            # Tesztelési követelmények
-   L¦¦ documentation_standards.md      # Dokumentációs követelmények
-
+¦¦ decisions/                           # Architecture Decision Records (AJÁNLOTT)
-   +¦¦ ADR-001-{title}.md              # ADR formátum
-   +¦¦ ADR-002-{title}.md
-   L¦¦ ...
-
+¦¦ epics/                               # Epic-ek hierarchikus mappái (KÖTELEZÕ)
-   +¦¦ EPIC-001-{title}/               # Epic mappa (formátum: EPIC-XXX-title)
-   -   +¦¦ plan.md                     # Epic Plan (KÖTELEZÕ - Architect)
-   -   +¦¦ epic_review.md              # Epic Review (KÖTELEZÕ - Tech Lead, closure-kor)
-   -   +¦¦ architect_signoff.md        # Architect Sign-off (KÖTELEZÕ - Architect, closure-kor)
-   -   +¦¦ backlog.md                  # Epic-specifikus backlog (OPCIONÁLIS)
-   -   +¦¦ state.md                    # Epic állapot (OPCIONÁLIS - ha micro-management kell)
-   -   -
-   -   L¦¦ tasks/                      # Task-ok mappája
-   -       +¦¦ TASK-001.md             # Task Plan (KÖTELEZÕ - Tech Lead)
-   -       +¦¦ TASK-002.md
-   -       +¦¦ implementation_reports/ # Implementációs riportok (KÖTELEZÕ - Developer)
-   -       -   +¦¦ TASK-001_backend_implementation_report.md
-   -       -   +¦¦ TASK-002_frontend_implementation_report.md
-   -       -   L¦¦ ...
-   -       L¦¦ qa_reports/             # QA riportok (KÖTELEZÕ - QA Tester)
-   -           +¦¦ TASK-001_qa_signoff.md
-   -           +¦¦ TASK-002_qa_signoff.md
-   -           L¦¦ ...
-   -
-   +¦¦ EPIC-002-{title}/
-   L¦¦ ...
-
L¦¦ archived/                            # Lezárt Epic-ek archívuma (OPCIONÁLIS)
    +¦¦ EPIC-001-{title}/               # Knowledge Steward archiválja ide
    L¦¦ ...
```

---

## ?? Kötelezõ Projekt-szintû Dokumentumok

### 1. `goal.md` - Projekt Célja

**Tulajdonos**: Product Owner / Stakeholder
**Létrehozás**: Projekt indításakor (legelsõ dokumentum)
**Frissítés**: Ritkán (csak stratégiai változás esetén)
**Formátum**:

```yaml
---
id: project-{project_name}-goal
title: "{Project Name} - Project Goal"
type: goal
scope: project
created: YYYY-MM-DD
last_updated: YYYY-MM-DD
---

# ?? Project Goal: {Project Name}

## 1. Üzleti Cél
{Miért csináljuk ezt a projektet? Mi az üzleti érték?}

## 2. Felhasználói Persona
{Ki használja? Milyen problémát old meg?}

## 3. Success Kritériumok
- [ ] {Mérhetõ kritérium 1}
- [ ] {Mérhetõ kritérium 2}

## 4. Scope (Határok)
**IN SCOPE**: {Mi tartozik bele}
**OUT OF SCOPE**: {Mi NEM tartozik bele}

## 5. Constraints (Korlátok)
- **Technológiai**: {Tech stack, platformok}
- **Idõbeli**: {Deadline-ok}
- **Erõforrás**: {Budget, team size}
```

**Orchestrator ellenõrzés**: Ha nincs `goal.md`, a projekt **NEM indítható**!

---

### 2. `state.md` - Projekt Állapot (Orchestrator Dokumentum)

**Tulajdonos**: Orchestrator (kizárólagosan!)
**Létrehozás**: Elsõ Epic Planning elõtt (vagy `project_state_snapshot.message.md` használatával)
**Frissítés**: **Minden Epic/Task állapotváltozásnál (KÖTELEZÕ!)**
**Formátum**: Használd a `src/agent-system/database/roles/management/orchestrator/templates/project_state.template.md` sablont

**Szekciók**:

- Project Overview (metrics)
- Epic State Map (táblázat)
- Task State Map (táblázat)
- Blocker Register
- Next Steps (Action Queue)
- Dependency Graph (Mermaid)
- Project Timeline
- Notes & Context

**Kritikus szabály**: Ha az Orchestrator NEM frissíti ezt a dokumentumot, **context loss** következik be!

---

### 3. `dependency_map.md` - Epic/Task Függõségek (Orchestrator Dokumentum)

**Tulajdonos**: Orchestrator (kizárólagosan!)
**Létrehozás**: Elsõ Epic Planning után (amikor már van min. 2 Epic)
**Frissítés**: Epic Planning, Task Breakdown, Párhuzamosítási döntések alkalmával
**Formátum**: Használd a `src/agent-system/database/roles/management/orchestrator/templates/epic_dependency_map.template.md` sablont

**Szekciók**:

- Dependency Matrix (Epic-szintû)
- Dependency Graph (Mermaid)
- Task-szintû Függõségek
- Critical Path Analysis
- Circular Dependency Check
- Dependency Change Log

**Kritikus szabály**: Minden párhuzamosítási döntés **CSAK** dependency_map.md alapján hozható!

---

### 4. `orchestrator_decision_log.md` - Döntési Napló (Orchestrator Dokumentum)

**Tulajdonos**: Orchestrator (kizárólagosan!)
**Létrehozás**: Elsõ Epic prioritizálási döntésnél
**Frissítés**: Minden jelentõs döntésnél (Epic prioritás, blocker handling, resource allocation, timeline change)
**Formátum**: Használd a `src/agent-system/database/roles/management/orchestrator/templates/decision_log.template.md` sablont

**Szekciók**:

- Decision Entry Template
- Decision History (DEC-001, DEC-002, ...)
- Decision Statistics
- Reverted Decisions

**Kritikus szabály**: Minden döntésnél dokumentáld:

- Context (miért kellett dönteni)
- Alternatives Considered (min. 2-3 opció)
- Decision & Rationale
- Impact (Epic/Task/Timeline/Resources)
- Follow-up Actions

---

## ?? Epic-szintû Dokumentumok

### Epic Mappa Elnevezési Konvenció

**Formátum**: `EPIC-XXX-{short-title}`

- `XXX`: 3 jegyû Epic sorszám (001, 002, 003, ...)
- `{short-title}`: Rövid, kisbetûs, kötõjellel elválasztott cím

**Példák**:

- ? `EPIC-001-user-authentication`
- ? `EPIC-002-product-catalog`
- ? `Epic 001 User Authentication` (space, nagybetû)
- ? `EPIC-1-auth` (nem 3 jegyû)

---

### 1. `plan.md` - Epic Plan (Architect Dokumentum)

**Tulajdonos**: Architect
**Létrehozás**: Epic Planning Phase (Architect Workflow végrehajtása)
**Frissítés**: Epic Planning során (Design döntések, ADR-ek)
**Formátum**: Használd a `src/agent-system/database/roles/discovery/architect/templates/epic_plan.template.md` sablont

**Szekciók**:

- Epic Goal & Scope
- Architektúrális döntések
- Quality Attributes (Performance, Security, Scalability)
- Epic-szintû függõségek
- ADR referenciák
- Tech Stack
- Risk Analysis
- Success Criteria

**Orchestrator ellenõrzés**: Epic Planning **NEM fejezõdhet be** `plan.md` nélkül!

---

### 2. `epic_review.md` - Epic Review (Tech Lead Dokumentum)

**Tulajdonos**: Tech Lead
**Létrehozás**: Epic Closure Phase (minden Task Done után)
**Frissítés**: Epic lezáráskor (egyszer)
**Formátum**: Használd a `src/agent-system/database/roles/discovery/tech_lead/templates/epic_review.template.md` sablont

**Szekciók**:

- Epic összefoglaló
- Task-ok státusza
- Technikai kihívások
- Lessons Learned
- Calibration Instructions (globális skill/template frissítési javaslatok)
- Metrics (idõ, scope, quality)

**Orchestrator ellenõrzés**: Epic **NEM zárható le** `epic_review.md` nélkül!

---

### 3. `architect_signoff.md` - Architect Sign-off (Architect Dokumentum)

**Tulajdonos**: Architect
**Létrehozás**: Epic Closure Phase (Tech Lead epic_review.md után)
**Frissítés**: Epic lezáráskor (egyszer)
**Formátum**: Használd a `src/agent-system/database/roles/discovery/architect/templates/architect_signoff.template.md` sablont

**Szekciók**:

- Epic Review Validation
- Architectural Compliance (Clean Architecture, DDD betartása)
- Quality Gate Pass/Fail
- Sign-off Decision (Approved/Conditional/Rejected)
- Calibration Instructions (globális skill/standards frissítési javaslatok)

**Orchestrator ellenõrzés**: Epic **NEM zárható le** `architect_signoff.md` nélkül! Ha "Rejected" › Epic visszamegy Planning-be!

---

## ?? Task-szintû Dokumentumok

### Task File Elnevezési Konvenció

**Formátum**: `TASK-XXX.md`

- `XXX`: 3 jegyû Task sorszám (001, 002, 003, ...)
- Epic-tõl független globális számozás (TASK-001, TASK-002, TASK-003, ... projekt szinten)

---

### 1. `TASK-XXX.md` - Task Plan (Tech Lead Dokumentum)

**Tulajdonos**: Tech Lead
**Létrehozás**: Task Planning Phase (Tech Lead Workflow végrehajtása)
**Frissítés**: Task Planning során
**Formátum**: Használd a `src/agent-system/database/roles/discovery/tech_lead/templates/task.template.md` sablont

**Helye**: `docs/{project}/epics/{EPIC}/tasks/TASK-XXX.md`

**Szekciók**:

- Task Goal
- Implementation Details
- Acceptance Criteria (DoD)
- Dependencies (Task-Task függõség)
- Assigned Role (Backend Dev/Frontend Dev/QA)
- Estimated Effort
- Technical Notes

**Orchestrator ellenõrzés**: Task **NEM indítható** `TASK-XXX.md` nélkül!

---

### 2. Implementation Reports (Developer Dokumentumok)

**Tulajdonos**: Backend Developer / Frontend Developer
**Létrehozás**: Task Implementation befejezése után
**Frissítés**: Implementation során (egyszer)
**Formátumok**:

- Backend: `src/agent-system/database/roles/engineering/backend_developer/templates/implementation_report.template.md`
- Frontend: `src/agent-system/database/roles/engineering/frontend_developer/templates/implementation_report.template.md`

**Helye**: `docs/{project}/epics/{EPIC}/tasks/implementation_reports/TASK-XXX_{backend|frontend}_implementation_report.md`

**Szekciók**:

- Implementation Summary
- Changed Files
- Key Design Decisions
- Testing (unit/integration tests written)
- Edge Cases Handled
- Known Limitations
- Next Steps (QA Handoff)

**Orchestrator ellenõrzés**: Task **NEM mehet QA-ba** implementation report nélkül!

---

### 3. QA Reports (QA Tester Dokumentum)

**Tulajdonos**: QA Tester
**Létrehozás**: QA Testing befejezése után
**Frissítés**: Testing során (egyszer)
**Formátum**: `src/agent-system/database/roles/engineering/qa_tester/templates/qa_signoff.template.md`

**Helye**: `docs/{project}/epics/{EPIC}/tasks/qa_reports/TASK-XXX_qa_signoff.md`

**Szekciók**:

- Testing Summary
- Test Cases Executed
- Test Results (Pass/Fail)
- Bug List (ha van)
- Performance/Security/Accessibility Notes
- QA Sign-off Decision (Approved/Conditional/Rejected)

**Orchestrator ellenõrzés**: Task **NEM zárható le** QA sign-off nélkül!

---

## ?? Dokumentum Tulajdonosi Mátrix (Role Mapping)

| Dokumentum | Tulajdonos | Létrehozás Fázisa | Frissítés Gyakoriság |
|:-----------|:-----------|:------------------|:---------------------|
| **Projekt szintû** ||||
| `goal.md` | Product Owner | Project Init | Ritkán |
| `state.md` | **Orchestrator** | Project Init | **Minden Epic/Task state change** |
| `dependency_map.md` | **Orchestrator** | Epic Planning | Epic Planning, Task Breakdown |
| `orchestrator_decision_log.md` | **Orchestrator** | Elsõ döntés | Minden jelentõs döntés |
| `backlog.md` | Product Owner / Orchestrator | Project Init | Ad-hoc |
| `standards/*.md` | Architect / Tech Lead | Project Init | Epic Closure (calibration) |
| `decisions/ADR-XXX.md` | Architect | Epic Planning | Epic Planning (új ADR) |
| **Epic szintû** ||||
| `plan.md` | **Architect** | Epic Planning | Epic Planning fázis |
| `epic_review.md` | **Tech Lead** | Epic Closure | Epic Closure (egyszer) |
| `architect_signoff.md` | **Architect** | Epic Closure | Epic Closure (egyszer) |
| `backlog.md` | Tech Lead | Ad-hoc | Ad-hoc |
| `state.md` | Orchestrator (opcionális) | Ad-hoc | Ad-hoc |
| **Task szintû** ||||
| `TASK-XXX.md` | **Tech Lead** | Task Planning | Task Planning fázis |
| `TASK-XXX_backend_implementation_report.md` | **Backend Developer** | Implementation | Implementation végén (egyszer) |
| `TASK-XXX_frontend_implementation_report.md` | **Frontend Developer** | Implementation | Implementation végén (egyszer) |
| `TASK-XXX_qa_signoff.md` | **QA Tester** | QA Testing | QA Testing végén (egyszer) |

---

## ?? Dokumentáció Karbantartási Protokollok

### 1. Orchestrator Felelõsségek (Folyamatos)

**State Tracking (KRITIKUS!):**

```markdown
MINDEN Epic/Task állapotváltozásnál frissítsd:
1. docs/{project}/state.md
   - Epic State Map táblázat
   - Task State Map táblázat
   - Blocker Register (ha van új)
   - Next Steps Queue
   - Project Timeline

2. docs/{project}/dependency_map.md (ha függõség változik)
   - Dependency Matrix
   - Critical Path Analysis

3. docs/{project}/orchestrator_decision_log.md (ha döntés történik)
   - Új Decision Entry
```

**Validation Protokoll (Epic Planning elõtt):**

```powershell
# 1. Project Goal létezik?
if (-not (Test-Path "docs/{project}/goal.md")) {
    ERROR: "goal.md hiányzik! Projekt NEM indítható!"
}

# 2. State.md létezik?
if (-not (Test-Path "docs/{project}/state.md")) {
    ACTION: "Hozd létre project_state_snapshot.message.md használatával!"
}

# 3. Epic mappa elnevezés helyes?
$epics = Get-ChildItem "docs/{project}/epics" -Directory
foreach ($epic in $epics) {
    if ($epic.Name -notmatch "^EPIC-\d{3}-[a-z0-9-]+$") {
        ERROR: "Epic mappa elnevezés helytelen: $($epic.Name)"
    }
}
```

---

### 2. Knowledge Steward Felelõsségek (Epic Closure után)

**Archiválás Protokoll:**

```markdown
Epic lezárás után (ha architect_signoff.md = "Approved"):

1. Ellenõrizd a teljes Epic dokumentációt:
   - [x] plan.md létezik
   - [x] epic_review.md létezik
   - [x] architect_signoff.md létezik
   - [x] Minden TASK-XXX.md alatt van implementation_report és qa_signoff

2. Ha teljes:
   - Archívba másolás: docs/{project}/archived/EPIC-XXX/
   - Original Epic mappa marad (történelmi referencia)

3. Calibration Instructions feldolgozása:
   - src/agent-system/database/roles/**/skills/ frissítések (ha javasolt)
   - src/agent-system/database/roles/**/templates/ frissítések (ha javasolt)
   - docs/{project}/standards/ frissítések (ha javasolt)
```

---

### 3. Architect/Tech Lead Felelõsségek (Epic Closure)

**Calibration Instructions Generálása:**

```markdown
Epic lezárásakor (epic_review.md és architect_signoff.md):

1. Lessons Learned alapján:
   - Mely global skill-eket kell frissíteni? (pl. backend_dotnet.knowledge.md)
   - Mely template-eket kell finomítani? (pl. task.template.md)
   - Mely standards-okat kell módosítani? (pl. testing_standards.md)

2. Calibration Instructions formátum:
   ---
   ## Calibration Instructions

   ### Global Skill Updates
   - **File**: src/agent-system/database/knowledge/engineering/backend_dotnet.knowledge.md
   - **Reason**: EF Core migration issue pattern encountered
   - **Suggested Addition**: "Minta X: Migration Conflict Resolution"

   ### Template Updates
   - **File**: src/agent-system/database/roles/discovery/tech_lead/templates/task.template.md
   - **Reason**: Dependencies section unclear
   - **Suggested Change**: Add "Dependency Type" field (Blocking/Optional)
   ---

3. Knowledge Steward feldolgozza ezeket a kalibrációs utasításokat
```

---

## ?? Gyakori Hibák és Megoldások

| Hiba | Ok | Megoldás |
|------|----|---------:|
| **Missing state.md** | Orchestrator nem hozta létre projekt indításkor | Használd a `project_state_snapshot.message.md`-t! |
| **Epic mappa elnevezés helytelen** | Nem követi a `EPIC-XXX-title` formátumot | Átnevezés + dependency_map.md frissítés |
| **Task file hiányzik** | Tech Lead nem készítette el TASK-XXX.md-t | Epic Planning nem fejezõdött be! Ne engedj Task-ot indítani! |
| **Implementation report hiányzik** | Developer nem készítette el | Task NEM mehet QA-ba! Blokkold a Task-ot! |
| **QA signoff hiányzik** | QA Tester nem készítette el | Task NEM zárható le! Blokkold a Task-ot! |
| **state.md elavult** | Orchestrator nem frissítette | **KRITIKUS**: Context loss! Azonnal frissítsd! |
| **Circular dependency** | Architect/Tech Lead rossz függõség definiálás | dependency_map.md ellenõrzés › CRITICAL WARNING! |
| **ADR hiányzik kritikus döntésnél** | Architect nem dokumentálta | Arch review során jelezd! |

---

## ?? Validációs Checklist (Orchestrator Pre-flight Check)

### Epic Planning elõtt

- [ ] `docs/{project}/goal.md` létezik
- [ ] `docs/{project}/state.md` létezik és naprakész
- [ ] `docs/{project}/dependency_map.md` létezik (ha van min. 1 Epic)
- [ ] Elõzõ Epic-ek lezártak (van `epic_review.md` és `architect_signoff.md`)
- [ ] Nincs Critical Blocker a `state.md` Blocker Register-ben

### Task Dispatching elõtt

- [ ] `docs/{project}/epics/{EPIC}/plan.md` létezik
- [ ] `docs/{project}/epics/{EPIC}/tasks/TASK-XXX.md` létezik
- [ ] Task dependencies teljesültek (függõ Task-ok Done státuszúak)
- [ ] Assigned Role available (nincs túlterhelt ágens)

### Epic Closure elõtt

- [ ] Minden TASK-XXX.md Done státuszú
- [ ] Minden Task-hoz van implementation_report
- [ ] Minden Task-hoz van qa_signoff (Approved)
- [ ] `docs/{project}/epics/{EPIC}/epic_review.md` létezik
- [ ] `docs/{project}/epics/{EPIC}/architect_signoff.md` létezik (Approved)
- [ ] `state.md` frissítve (Epic = Done)

---

## ?? Kapcsolódó Dokumentumok

- **[Orchestrator Workflow](../workflows/orchestrator.workflow.md)** - Orchestrator munkafolyamat
- **[Orchestrator Dispatching Skill](./orchestrator_dispatching.knowledge.md)** - Task kiosztási logika
- **[Project State Template](../templates/project_state.template.md)** - state.md sablon
- **[Epic Dependency Map Template](../templates/epic_dependency_map.template.md)** - dependency_map.md sablon
- **[Decision Log Template](../templates/decision_log.template.md)** - decision_log.md sablon
- **[Context Structure Management Skill](../../knowledge_steward/../../../knowledge/management/context_structure_management.knowledge.md)** - `src/agent-system/database/roles` struktúra (agent-specifikus dokumentáció)

---

## ?? Dokumentációs Statisztikák (Referencia)

**Tipikus projektnél** (5 Epic, 15 Task):

| Dokumentum Típus | Várható Darabszám | Tulajdonos | Létrehozási Fázis |
|:-----------------|:------------------|:-----------|:------------------|
| `goal.md` | 1 | Product Owner | Project Init |
| `state.md` | 1 | Orchestrator | Project Init |
| `dependency_map.md` | 1 | Orchestrator | Epic Planning |
| `decision_log.md` | 1 | Orchestrator | Elsõ döntés |
| Epic `plan.md` | 5 | Architect | Epic Planning |
| Epic `epic_review.md` | 5 | Tech Lead | Epic Closure |
| Epic `architect_signoff.md` | 5 | Architect | Epic Closure |
| `TASK-XXX.md` | 15 | Tech Lead | Task Planning |
| Implementation Reports | 15-30 | Developers | Implementation |
| QA Signoffs | 15 | QA Tester | QA Testing |
| ADR-XXX.md | 3-10 | Architect | Epic Planning |

**Összesen**: ~60-90 dokumentum (Epic/Task arány függvényében)

---

*Ez a skill az Orchestrator "Project Documentation Governance" funkciója. Használd projekt inicializáláskor, Epic Planning elõtt és Epic Closure-kor a dokumentációs integritás biztosításához!*
