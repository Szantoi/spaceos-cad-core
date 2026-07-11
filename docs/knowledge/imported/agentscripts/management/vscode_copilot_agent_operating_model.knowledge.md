---
name: vscode-copilot-agent-operating-model
description: 'VS Code Copilot single-LLM agent operating model and context management strategies. Use when understanding role-switching mechanisms, context window limits, or documentation persistence needs.'
domain: management
last_updated: 2026-02-24
---

# ?? VS Code Copilot: Agent Operating Model Skill

**Environment**: Visual Studio Code + GitHub Copilot (Claude 3.5 Sonnet backend)

**Summary:** Ez a skill tisztázza, hogyan mûködik a multi-agent workflow rendszer **VS Code Copilot környezetben**. Kritikus megérteni a mûködési modellt, mert ez határozza meg a context management stratégiát, a role switching mechanizmusokat és a dokumentáció persistence szükségességét.

---

## ??? VS Code Copilot Agent Architecture

### 1. Single LLM Instance Model

**VS Code Copilot mûködése:**

```
-¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¬
-  VS CODE COPILOT CHAT PANEL                            -
-  (Egyetlen conversation thread)                        -
-                                                        -
-  -¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¬         -
-  -  User Input                               -         -
-  -  "Orchestrator: Indítsd el EPIC-001-et"  -         -
-  L¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦-         -
-                    ¡                                   -
-  -¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¬         -
-  -  Claude 3.5 Sonnet Instance              -         -
-  -  (EGYETLEN LLM)                           -         -
-  -                                           -         -
-  -  Context:                                 -         -
-  -  - Conversation History (teljes)         -         -
-  -  - Workspace Files (read access)         -         -
-  -  - Tools (file edit, terminal, search)   -         -
-  -  - Active Role Prompt (injected)         -         -
-  L¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦-         -
-                    ¡                                   -
-  -¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¬         -
-  -  Role-based Response (Virtual Agent)     -         -
-  -  "Architect role activated..."           -         -
-  -  [plan.md created]                        -         -
-  L¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦-         -
-                    ¡                                   -
-  -¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¬         -
-  -  Role Switch (Prompt Injection)          -         -
-  -  "Tech Lead role activated..."           -         -
-  -  [UGYANAZ az LLM, közös history]         -         -
-  L¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦-         -
L¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦-
```

**Kulcspontok:**

- ? **Egyetlen LLM instance** (Claude 3.5 Sonnet)
- ? **Közös conversation history** (minden "ágens" látja az elõzõ interakciókat)
- ? **Szekvenciális végrehajtás** (nincs párhuzamos ágensek)
- ? **Virtual role-switching** (prompt injection alapú)
- ? **NINCS valós multi-agent system** (nincs külön Architect/Tech Lead/Developer process)
- ? **NINCS context isolation** (szerepek között nincs memória elválasztás)

---

## ?? Virtual Multi-Agent System

**Mit jelent a "Virtual Agent"?**

```markdown
# Workflow-ban így írod:
"Architect ágensnek átadod a vezérlést..."

# Ami VALÓJÁBAN történik VS Code-ban:
1. Orchestrator role prompt injected › LLM response
2. LLM output: "Architect role-ra váltok"
3. Architect role prompt injected › UGYANAZ az LLM response
4. LLM output: "plan.md created" (tool call: create_file)
5. Orchestrator role prompt re-injected › UGYANAZ az LLM response
```

**Tehát:**

- **"Agent dispatch"** = **Role prompt injection** (nincs valós process indítás)
- **"Handoff"** = **Role context switch** (prompt template csere)
- **"Agent communication"** = **Conversation history reference** (LLM visszaolvassa saját korábbi output-ját)

---

## ?? Context Management (Single Conversation Thread)

### Context Lifecycle

```
Session Start (User opens VS Code Chat)
        ¡
[Conversation Thread created - 0 tokens]
        ¡
User: "Orchestrator - Epic indítás"
        ¡
LLM Response (Orchestrator role) - 5k tokens
        ¡
[History: 5k tokens]
        ¡
LLM Response (Architect role) - 15k tokens
        ¡
[History: 20k tokens]
        ¡
LLM Response (Tech Lead role) - 25k tokens
        ¡
[History: 45k tokens]
        ¡
LLM Response (Backend Dev role) - 30k tokens
        ¡
[History: 75k tokens]
        ¡
... (további role switches)
        ¡
[History: 150k tokens - WARNING: 75% capacity]
        ¡
Session End (User closes VS Code or 24h timeout)
        ¡
[Conversation History LOST - nincs persistence!]
```

### Context Limits

| Limit | Value | Implication |
|:------|:------|:------------|
| **Max Context Window** | 200k tokens | Teljes conversation + workspace files + tool outputs |
| **Practical Limit** | ~150k tokens | 75% után context overflow risk |
| **Session Timeout** | 24 óra | Auto-cleanup, history LOST |
| **Context Persistence** | **NINCS!** | Session után minden context elvész |

**?? KRITIKUS:** VS Code Copilot **NEM** tárolja a conversation history-t session-ök között! Ezért **KÖTELEZÕ** a state.md, dependency_map.md, decision_log.md persistence!

---

## ?? Role-Based Prompt Injection Mechanism

### Hogyan mûködik a "Role Activation"?

```markdown
# 1. Orchestrator kér Architect Planning-et
Orchestrator (LLM): "Architect role-ra váltok..."

# 2. VS Code Copilot System Prompt módosul:
System Prompt (injected):
---
You are a Senior Software Architect.
Your current task: Plan EPIC-001 (User Authentication).
Your workflow: src/agent-system/database/roles/discovery/architect/architect.workflow.md
Your skills: src/agent-system/database/roles/discovery/architect/skills/design_thinking.knowledge.md
Your output: docs/joinerytech-flow/epics/EPIC-001/plan.md
Context files: docs/joinerytech-flow/goal.md, docs/joinerytech-flow/state.md
---

# 3. LLM Response (Architect role szerint):
LLM: "Rendben, Architect vagyok. Elemzem a goal.md-t..."
[Fájlok olvasása, plan.md létrehozása]
LLM: "plan.md elkészült. Epic Planning complete."

# 4. Orchestrator role re-injection:
System Prompt (injected):
---
You are the Orchestrator.
Previous step: Architect created plan.md
Next step: Dispatch Tech Lead for Task Breakdown
---

# 5. LLM Response (Orchestrator role szerint):
LLM: "Rendben, Orchestrator vagyok. state.md frissítése..."
```

**Kulcs:** A "role" csak egy **prompt template**, amit az LLM-re ráveszünk. Az LLM **ugyanaz** marad, csak a context változik.

---

## ?? State Persistence Strategy (VS Code Copilot)

### Miért KÖTELEZÕ a State Tracking?

**Probléma:**

```
Session 1 (2026-02-16 10:00):
- Epic-001 Planning done
- Task-001, Task-002 created
- Conversation History: 80k tokens

[User bezárja VS Code-ot]

Session 2 (2026-02-17 09:00):
- Conversation History: 0 tokens (LOST!)
- LLM: "Nem tudom, hol tartunk a projektben..."
```

**Megoldás:**

```
Session 1 (2026-02-16 10:00):
- Epic-001 Planning done
- state.md FRISSÍTVE (Epic State Map: EPIC-001 = "Ready for Task Breakdown")
- dependency_map.md FRISSÍTVE (EPIC-001 dependencies)
- decision_log.md FRISSÍTVE (DEC-001: Epic Planning döntés)

[User bezárja VS Code-ot]

Session 2 (2026-02-17 09:00):
- Conversation History: 0 tokens
- Orchestrator: "Töltsd be state.md, dependency_map.md, decision_log.md"
- LLM: "Rendben, látom hogy EPIC-001 Ready for Task Breakdown státuszú..."
```

**?? STATE TRACKING = SESSION-OK KÖZÖTTI MEMÓRIA!**

---

## ??? Tool-based Subagent Execution (Advanced)

### `runSubagent` Tool (Ha elérhetõ VS Code Copilot-ban)

**Mikor használd:**

1. **Kritikus context isolation szükséges** (pl. security review, independent analysis)
2. **Context overflow veszély** (150k+ tokens, subagent tiszta context-tel indul)
3. **Idõigényes feladat** (subagent background-ban futhat)
4. **Cross-project analysis** (subagent más workspace-bõl olvas)

**Példa:**

```markdown
# Opció A: Virtual Role Switch (default, közös context)
Orchestrator: "Architect role-ra váltok..."
[UGYANAZ az LLM, látja az összes elõzõ Epic history-t]

# Opció B: Subagent Tool (ha elérhetõ, isolated context)
Orchestrator: runSubagent(
  prompt: "
    Role: Senior Architect
    Task: Plan EPIC-001 (User Authentication)
    Workflow: src/agent-system/database/roles/discovery/architect/architect.workflow.md
    Context: docs/joinerytech-flow/goal.md, docs/joinerytech-flow/state.md
    Output: docs/joinerytech-flow/epics/EPIC-001/plan.md
  ",
  description: "Architect Epic Planning (isolated context)"
)

# Subagent:
# - ÚJ LLM instance (tiszta context, 0 tokens)
# - NEM látja az elõzõ Epic-ek history-ját
# - Végrehajt, majd visszatér egy report-tal
# - Report bekerül a main conversation history-ba

Orchestrator: "Subagent visszatért. plan.md elkészült. state.md frissítés..."
```

**?? FIGYELEM:** `runSubagent` tool **NEM STANDARD** VS Code Copilot-ban! Ellenõrizd a tool_search_tool_regex-szel, hogy elérhetõ-e!

---

## ?? Best Practices (VS Code Copilot Environment)

### 1. **Explicit Role Declaration (Minden lépésnél)**

```markdown
? ROSSZ:
"Készítsd el a plan.md-t..."
[LLM nem tudja, milyen role-ban van]

? JÓ:
**Role**: Architect
**Task**: Készítsd el a plan.md-t az EPIC-001-hez
**Workflow**: src/agent-system/database/roles/discovery/architect/architect.workflow.md
```

### 2. **Context Loading (Session kezdetekor MINDIG)**

```markdown
? ROSSZ:
User: "Folytasd az Epic-et"
[LLM nem tudja, hol tartunk]

? JÓ:
User: "Orchestrator: Folytasd az Epic-et"
Orchestrator: "Töltsd be state.md, dependency_map.md, decision_log.md"
[LLM kontextusba kerül]
```

### 3. **State Tracking (MINDEN role switch után)**

```markdown
? ROSSZ:
Architect: "plan.md elkészült"
[Orchestrator azonnal Tech Lead-et hív]
[state.md NEM frissült › következõ session-ben context loss]

? JÓ:
Architect: "plan.md elkészült"
Orchestrator: "state.md frissítés (Epic State: Planning › Ready for Task Breakdown)"
[State persisted › következõ session-ben helyreállítható]
```

### 4. **Context Hygiene (Token management)**

```markdown
?? WARNING: Context > 150k tokens

Orchestrator: "Knowledge Steward role aktiválása"
Knowledge Steward: "Lezárt Epic-ek archiválása, context cleanup"
Orchestrator: "Token usage: 80k tokens (visszaesett)"
```

### 5. **Session Continuity Check (Új session kezdetekor)**

```markdown
# Minden új VS Code session-nél:

Orchestrator Pre-flight Check:
1. state.md utolsó frissítés: 2026-02-16 18:30
2. Conversation History: 0 tokens (új session)
3. Context Loading: state.md, dependency_map.md, decision_log.md
4. Aktuális állapot: EPIC-001 = "In Progress", 3/5 Task Done
5. Next Step: Task-004 Implementation (Backend Developer)
```

---

## ?? Terminológiai Tisztázás (VS Code Copilot Context)

| Workflow Terminus | VS Code Copilot Valóság | Helyes Interpretáció |
|:------------------|:------------------------|:---------------------|
| **"Agent dispatch"** | **Role prompt injection** | System prompt módosítás az LLM számára |
| **"Handoff"** | **Role context switch** | Új role prompt injecting, elõzõ role context megõrzése |
| **"Agent communication"** | **Conversation history reference** | LLM visszaolvassa saját korábbi output-ját |
| **"Subagent execution"** | **`runSubagent` tool call** (ha elérhetõ) | ÚJ LLM instance, isolated context, report visszaküldése |
| **"Context isolation"** | **File-based state tracking** (state.md, stb.) | Conversation history NEM isolated, persist to files! |
| **"Multi-agent system"** | **Virtual multi-agent (single LLM)** | Role-based prompt engineering |
| **"Agent running in background"** | **Szekvenciális végrehajtás** (nincs background) | LLM response után vezérlés visszaáll User-hez |

**?? FONTOS:** A workflow dokumentációban használt "agent dispatch", "handoff" terminusok **METAPHORÁK**. VS Code Copilot-ban ez **NEM valós multi-process system**, hanem **prompt engineering pattern**!

---

## ?? Context Inspection (Debugging)

### Hogyan ellenõrizd, melyik "role" aktív?

```markdown
# Kérdezd meg az LLM-et:
User: "Milyen role-ban vagy most?"

# LLM válasza (conversation history alapján):
LLM: "Jelenleg Orchestrator vagyok. Legutóbbi role switch:
- 10:00 - Orchestrator (Epic indítás)
- 10:05 - Architect (Epic Planning)
- 10:30 - Orchestrator (state.md frissítés)
- 11:00 - Tech Lead (Task Breakdown)
- 11:45 - Orchestrator (current)"
```

### Hogyan ellenõrizd a context usage-t?

```markdown
# VS Code Copilot nem mutatja direkt, de becsülhetõ:
User: "Hány token a conversation history?"

# LLM válasza (hozzávetõleges):
LLM: "A jelenlegi conversation ~120k token körül lehet (75 prompt-response pair)."

# ?? Ha > 150k › Context cleanup ajánlott!
```

---

## ?? Gyakori Buktatók (VS Code Copilot Specifikus)

| Buktató | Ok | Megoldás |
|:--------|:---|:---------|
| **Role confusion** | Túl sok role switch, LLM "elfelejti" aktív role | Explicit role declaration minden lépésnél |
| **Context overflow** | 200k token limit elérése | Knowledge Steward cleanup (Fázis C workflow) |
| **State loss (session után)** | Conversation history nem persistent | **state.md frissítés KÖTELEZÕ!** |
| **"Agent" nem válaszol** | LLM várakozik User input-ra (vezérlés visszaállt) | Explicit "Continue" prompt vagy következõ role activation |
| **Circular role loop** | LLM rossz role-t aktivál | Workflow ellenõrzés, explicit "Next role: XYZ" instruction |
| **File access error** | LLM nem tudja olvasni a workspace file-okat | Ellenõrizd a file path-ot, használj absolute path-ot |

---

## ? Operating Model Checklist (Orchestrator)

**Minden Epic/Task execution elõtt ellenõrizd:**

- [ ] **Context Load**: state.md, dependency_map.md, decision_log.md betöltve?
- [ ] **Token Usage**: < 150k tokens? (ha nem › cleanup)
- [ ] **Active Role Clear**: Explicit role declaration a prompt-ban?
- [ ] **State Tracking Plan**: Melyik fájlokat kell frissíteni a fázis után?
- [ ] **Session Continuity**: Új session? › Pre-flight check (state.md last update check)

**Minden Role Switch után:**

- [ ] **Role Confirmation**: LLM válaszol "Architect vagyok..." formában?
- [ ] **Output Validation**: Elvárt file (plan.md, TASK-XXX.md, stb.) létrejött?
- [ ] **State Persist**: state.md frissítve?
- [ ] **Next Role Prepared**: Következõ role prompt ready?

---

## ?? Advanced Pattern: Multi-Workspace Parallel Execution

### 2. Multi-Workspace Model (Részleges Párhuzamosítás)

**Innováció:** Minden ágenshez külön VS Code workspace/ablak + File-based Communication Hub.

**Architektúra:**

```
-¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¬
-  PROJECT ROOT                                                        -
-  docs/{project}/communication_hub/                                   -
-                                                                      -
-  -¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¬      -
-  -  message_queue.md (Központi Kommunikációs Pult)          -      -
-  -  ¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦        -      -
-  -  [2026-02-16 10:00] Orchestrator › Architect            -      -
-  -  Epic-001 tervezés kérése. Goal.md: ...                 -      -
-  -                                                           -      -
-  -  [2026-02-16 10:30] Architect › Orchestrator            -      -
-  -  Epic-001 plan.md elkészült. ADR-001 létrehozva.        -      -
-  -                                                           -      -
-  -  [2026-02-16 10:35] Orchestrator › Tech Lead            -      -
-  -  Task breakdown kérése Epic-001-hez.                     -      -
-  -  ...                                                      -      -
-  L¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦-      -
-                                                                      -
-  -¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¬  -¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¬  -¦¦¦¦¦¦¦¦¦¦¦¦¦¦¬   -
-  - VS Code Window #1 -  - VS Code Window #2 -  - VS Code #3   -   -
-  - Orchestrator      -  - Architect         -  - Tech Lead    -   -
-  - (Main)            -  - (Parallel)        -  - (Parallel)   -   -
-  -                   -  -                   -  -              -   -
-  - Claude Instance 1 -  - Claude Instance 2 -  - Claude Inst 3-   -
-  - Context: History1 -  - Context: History2 -  - Context: H3  -   -
-  - (Isolated!)       -  - (Isolated!)       -  - (Isolated!)  -   -
-  L¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦-  L¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦-  L¦¦¦¦¦¦¦¦¦¦¦¦¦¦-   -
-           ¡                      ¡                      ¡           -
-  -¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¬  -
-  -  Shared Workspace Files (Read/Write Access)                  -  -
-  -  - docs/{project}/state.md                                   -  -
-  -  - docs/{project}/epics/EPIC-001/plan.md                     -  -
-  -  - docs/{project}/communication_hub/message_queue.md         -  -
-  L¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦-  -
L¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦-
```

**Kulcspontok:**

- ? **Valós context isolation** (külön VS Code instance = külön conversation thread)
- ? **Részleges párhuzamosítás** (Architect, Tech Lead, Developer párhuzamosan dolgozhatnak)
- ? **Async file-based communication** (message_queue.md = message passing)
- ? **Shared visibility** (minden ágens látja a teljes kommunikációs history-t)
- ? **State tracking közös** (state.md, dependency_map.md közös access)
- ?? **Manuális koordináció szükséges** (nincs automatikus message routing)

---

### File-based Message Queue Protocol

**Communication Hub struktúra:**

```
docs/{project}/communication_hub/
+¦¦ message_queue.md              # Központi üzenetsor (idõrendben)
+¦¦ orchestrator_inbox.md         # Orchestrator-nek címzett üzenetek
+¦¦ architect_inbox.md            # Architect-nek címzett üzenetek
+¦¦ tech_lead_inbox.md            # Tech Lead-nek címzett üzenetek
+¦¦ backend_developer_inbox.md    # Backend Dev-nek címzett üzenetek
+¦¦ frontend_developer_inbox.md   # Frontend Dev-nek címzett üzenetek
L¦¦ qa_tester_inbox.md            # QA-nek címzett üzenetek
```

**Message Format (message_queue.md):**

```markdown
---
id: msg-001
timestamp: 2026-02-16T10:00:00Z
from: orchestrator
to: architect
priority: high
status: pending
---

# Message MSG-001: Epic Planning Request

**From**: Orchestrator
**To**: Architect
**Timestamp**: 2026-02-16 10:00:00
**Priority**: High
**Status**: Pending

## Request

Tervezd meg az EPIC-001 (User Authentication System) architektúráját.

**Context Files**:
- `docs/joinerytech-flow/goal.md`
- `docs/joinerytech-flow/state.md`

**Deliverable**:
- `docs/joinerytech-flow/epics/EPIC-001/plan.md`
- ADR-ek (ha kritikus döntés)

**Deadline**: 2026-02-16 12:00

---

---
id: msg-002
timestamp: 2026-02-16T10:30:00Z
from: architect
to: orchestrator
priority: normal
status: completed
reply_to: msg-001
---

# Message MSG-002: Epic Planning Completed

**From**: Architect
**To**: Orchestrator
**Timestamp**: 2026-02-16 10:30:00
**Priority**: Normal
**Status**: Completed
**Reply To**: MSG-001

## Response

Epic-001 plan.md elkészült.

**Deliverables**:
- ? `docs/joinerytech-flow/epics/EPIC-001/plan.md` (created)
- ? `docs/joinerytech-flow/decisions/ADR-001-jwt-authentication.md` (created)

**Summary**: ASP.NET Core Identity + JWT Bearer auth választva. 3 alternatíva értékelve (Identity, IdentityServer, Custom JWT). Clean Architecture, DDD követve.

**Next Steps**: Tech Lead task breakdown kérhetõ.

---
```

---

### Multi-Workspace Workflow

#### 1. Setup (Projekt Indítás)

**User (Main Orchestrator Window):**

```powershell
# 1. Main Orchestrator VS Code indítása
code c:\Projects\JoineryTech.Flow

# 2. Orchestrator Chat-ben:
"Orchestrator role: Inicializáld a communication hub-ot"

# Orchestrator létrehozza:
docs/joinerytech-flow/communication_hub/
  +¦¦ message_queue.md
  +¦¦ orchestrator_inbox.md
  +¦¦ architect_inbox.md
  L¦¦ ...

# 3. Egyéb ágensek VS Code indítása (külön ablakokban)
code c:\Projects\JoineryTech.Flow --new-window  # Architect
code c:\Projects\JoineryTech.Flow --new-window  # Tech Lead
code c:\Projects\JoineryTech.Flow --new-window  # Backend Dev
code c:\Projects\JoineryTech.Flow --new-window  # Frontend Dev
```

#### 2. Message Sending (Orchestrator › Architect)

**Orchestrator Window (Chat):**

```markdown
**Task**: Üzenet küldése Architect-nek

1. Töltsd be: docs/joinerytech-flow/communication_hub/message_queue.md
2. Append új üzenet (MSG-XXX formátumban):
   - Timestamp: mostani idõ
   - From: orchestrator
   - To: architect
   - Request: Epic-001 planning
3. Mentsd: message_queue.md
4. Frissítsd: architect_inbox.md (ugyanaz az üzenet, de csak architect látja)
```

#### 3. Message Reading (Architect Window)

**Architect Window (Chat):**

```markdown
**Role**: Architect

**Startup Protocol**:
1. Töltsd be: docs/joinerytech-flow/communication_hub/architect_inbox.md
2. Keress: status: pending üzeneteket
3. Legrégebbi pending üzenet feldolgozása (FIFO)

**Found**: MSG-001 (Epic-001 Planning Request)

**Action**:
1. Végrehajtás: architect.workflow.md szerint
2. Output: plan.md létrehozása
3. Response üzenet küldése: message_queue.md-ba (MSG-002)
4. Status frissítés: MSG-001 status: pending › status: completed
5. Saját inbox cleanup: architect_inbox.md MSG-001 törlése
```

#### 4. Parallel Execution (Orchestrator koordináció)

**Példa: 3 Task párhuzamos implementáció**

**Orchestrator Window:**

```markdown
**Decision**: Task-001 (Backend), Task-002 (Frontend), Task-003 (Backend) párhuzamos indítása

**Actions**:
1. MSG-010: Backend Developer › Task-001 implementálás
2. MSG-011: Frontend Developer › Task-002 implementálás
3. MSG-012: Backend Developer › Task-003 implementálás (dependency met)

**Send Time**: 11:00 (mindhárom egyszerre)

**Monitoring**:
- 11:05: Check message_queue.md status field
- 11:10: Check message_queue.md status field
- 11:15: Backend Dev completed MSG-010 (Task-001)
- 11:20: Frontend Dev completed MSG-011 (Task-002)
- 11:25: Backend Dev completed MSG-012 (Task-003)

**Result**: 3 Task 25 perc alatt (szekvenciálisan ~60 perc lett volna!)
```

---

### Elõnyök (vs Single-Thread Virtual Model)

| Feature | Single-Thread Virtual | Multi-Workspace Parallel |
|:--------|:---------------------|:-------------------------|
| **Context Isolation** | ? (közös history) | ? (külön conversation threads) |
| **Parallel Execution** | ? (szekvenciális) | ? (valós párhuzamosság) |
| **Token Usage** | ?? (egy thread, gyorsan full) | ? (elosztott, ágensek kisebb context-ek) |
| **Role Confusion** | ?? (sok role switch › confusion) | ? (minden ágens fix role) |
| **Debugging** | ?? (nehéz elkülöníteni a role-okat) | ? (külön VS Code ablak = külön log) |
| **Scalability** | ?? (10+ Epic › context overflow) | ? (ágensek párhuzamosan dolgozhatnak) |
| **Setup Complexity** | ? (egyszerû - 1 VS Code) | ?? (komplexebb - N VS Code ablak) |
| **Communication Overhead** | ? (nincs - conversation history) | ?? (file-based message queue kezelés) |

---

### Hátrányok és Kihívások

| Kihívás | Probléma | Megoldás |
|:--------|:---------|:---------|
| **Manual Message Routing** | Nincs automatikus message delivery | Orchestrator koordinálja (dispatcher role) |
| **Message Queue Conflict** | 2 ágens egyszerre írja a message_queue.md-t | Git-like conflict resolution vagy file lock |
| **Inbox Sync** | Ágens nem veszi észre az új üzenetet | Polling (5-10 percenként check) vagy webhook |
| **Context Load** | Minden ágens betölti a teljes workspace-t | Selective file loading (csak inbox + project docs) |
| **Cost** | N VS Code Copilot session = N subscription? | Ellenõrizd a licensing model-t (workspace vs user) |

---

### Best Practices (Multi-Workspace Pattern)

#### 1. **Orchestrator = Dispatcher**

```markdown
Orchestrator felelõsségei:
- [ ] Message queue monitoring (újabb üzenetek küldése)
- [ ] Response collection (ágensek válaszainak olvasása)
- [ ] State tracking (state.md frissítés minden response után)
- [ ] Dependency coordination (Task dependencies betartása)
- [ ] Blocker handling (ha ágens stuck, escalation)
```

#### 2. **Ágensek = Worker Role**

```markdown
Minden ágens (Architect, Tech Lead, Dev, QA):
- [ ] Inbox check (startup + periodic, pl. 10 percenként)
- [ ] FIFO processing (legrégebbi pending üzenet elõször)
- [ ] Work execution (workflow szerint)
- [ ] Response posting (message_queue.md-ba válasz)
- [ ] Status update (pending › in-progress › completed)
- [ ] Inbox cleanup (processed messages törlése)
```

#### 3. **Message Priority Handling**

```markdown
Priority Levels:
- **critical**: Blocker, Epic Planning (azonnal feldolgozás)
- **high**: Task Implementation, QA Testing (1-2 órán belül)
- **normal**: Bug fix, minor refactor (4-6 órán belül)
- **low**: Documentation update, cleanup (1-2 napon belül)

Ágensek CRITICAL és HIGH priority-t first process-elik!
```

#### 4. **Conflict Resolution**

```markdown
Ha 2 ágens egyszerre módosítja message_queue.md:

Megoldás 1 (Pessimistic Lock):
- Ágens létrehoz: .message_queue.lock fájlt
- Ha létezik › wait 5s › retry
- Írás után: .lock törlése

Megoldás 2 (Optimistic - Append Only):
- Minden ágens append (nem overwrite!)
- Chronological ordering (timestamp alapján)
- Conflict ritkán történik (csak append)
```

---

### Mikor használd a Multi-Workspace Pattern-t?

**? AJÁNLOTT:**

- Nagy projektekben (10+ Epic, 50+ Task)
- Context token usage kritikus (single-thread 150k+ token)
- Valós párhuzamosság szükséges (3+ developer párhuzamos work)
- Long-running Epic-ek (1+ hét implementáció)
- Tiszta role separation kell (debugging, audit)

**? NEM AJÁNLOTT:**

- Kis projektekben (1-3 Epic, 10-15 Task)
- Egyetlen developer (nincs kit párhuzamosítani)
- Gyors prototyping (setup overhead nagy)
- Communication overhead > parallelization benefit

---

### Implementation Checklist

**Setup Phase:**

- [ ] Communication hub létrehozása (`docs/{project}/communication_hub/`)
- [ ] Message queue template definiálása (message format standardizálás)
- [ ] Inbox fájlok létrehozása (minden ágenshez)
- [ ] VS Code workspace-ek indítása (N ablak)
- [ ] Orchestrator dispatcher role aktiválása

**Runtime Phase:**

- [ ] Orchestrator message sending (prioritás szerint)
- [ ] Ágensek inbox polling (5-10 perc)
- [ ] Work execution (workflow szerint)
- [ ] Response posting (message_queue.md)
- [ ] State tracking (state.md minden response után)

**Monitoring Phase:**

- [ ] Token usage tracking (ágensenként)
- [ ] Message queue backlog (pending messages count)
- [ ] Blocker detection (stuck messages > 2 óra)
- [ ] Performance metrics (task completion time)

---

## ?? Kapcsolódó Dokumentumok

- **[Orchestrator Epic Execution Workflow](../workflows/orchestrator_epic_execution.workflow.md)** - Epic lifecycle (7 fázis)
- **[Orchestrator Workflow (Task Dispatching)](../workflows/orchestrator.workflow.md)** - Task dispatch + context hygiene
- **[Project Documentation Structure Skill](./project_documentation_structure.knowledge.md)** - State persistence stratégia
- **[Orchestrator Dispatching Skill](./orchestrator_dispatching.knowledge.md)** - Role selection logic
- **[Prompt Engineering Skill](../../../../knowledge/core/prompt_engineering.knowledge.md)** - Mega-Prompt construction

---

## ?? Összehasonlítás: VS Code Copilot vs Más Környezetek

| Feature | VS Code Copilot<br/>(Single Workspace) | VS Code Multi-Workspace<br/>(Advanced Pattern) | AutoGPT / AgentGPT | LangChain Multi-Agent | CrewAI |
|:--------|:---------------------------------------|:-----------------------------------------------|:--------------------|:----------------------|:-------|
| **Agent Model** | Virtual (single LLM) | Hybrid (N LLMs, shared files) | Virtual (single LLM) | Real (multi-process) | Real (multi-process) |
| **Context Isolation** | ? (közös history) | ? (workspace isolation) | ? (közös history) | ? (process isolation) | ? (process isolation) |
| **Parallel Execution** | ? (szekvenciális) | ? (N workspace párhuzamos) | ? (szekvenciális) | ? (multi-process) | ? (multi-process) |
| **Inter-agent Comm** | Conversation history | File-based message queue | Conversation history | Message passing (async) | Message passing (async) |
| **Session Persistence** | ? (24h timeout) | ? (24h timeout, workspace-enként) | ? (DB backed) | ? (DB backed) | ? (DB backed) |
| **Subagent Support** | ?? (tool: runSubagent) | ?? (tool: runSubagent per workspace) | ? (native) | ? (native) | ? (native) |
| **Setup Complexity** | ? (1 VS Code) | ?? (N VS Code + message hub) | ?? (Python + config) | ?? (Python + infra) | ?? (Python + infra) |
| **State Tracking** | File-based (state.md) | File-based (state.md, shared) | ?? (memory/DB) | DB-backed | DB-backed |
| **Token Efficiency** | ?? (1 thread › token waste) | ? (N threads › distributed) | ?? (1 thread › token waste) | ? (distributed) | ? (distributed) |
| **Best For** | Kis-közepes projektek | Nagy projektek (10+ Epic) | Autonomous tasks | Production multi-agent | Production multi-agent |
| **Our Workflow Compatibility** | ? (100% compatible) | ? (100% compatible) | ? (compatible) | ?? (needs adaptation) | ?? (needs adaptation) |

**? KÖVETKEZTETÉS:**

- **Single Workspace (Default)**: Kis-közepes projektek (< 10 Epic, < 50 Task), gyors prototyping, 1-2 developer.
- **Multi-Workspace (Advanced)**: Nagy projektek (10+ Epic, 50+ Task), 3+ developer párhuzamos munka, token efficiency kritikus.
- **LangChain / CrewAI**: Production-grade, distributed systems, complex agent orchestration, DB persistence szükséges.

**?? FONTOS:** A workflow-ok mindkét VS Code pattern-nel mûködnek (Single vs Multi-Workspace). Multi-Workspace csak akkor ajánlott, ha valós párhuzamosításra van szükség!

---

*Ez a skill a VS Code Copilot specifikus mûködési modellt dokumentálja. A workflow-ok általánosak maradnak, de ez a skill segít megérteni, mi történik "a háttérben" VS Code környezetben.*
