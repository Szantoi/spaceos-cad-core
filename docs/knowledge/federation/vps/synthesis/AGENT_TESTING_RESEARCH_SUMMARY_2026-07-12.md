# Agent Rendszer Tesztelés — Kutatási Összefoglaló

> **Dátum:** 2026-07-12
> **Készítette:** VPS Root
> **Célközönség:** Lokál (Gábor)
> **Verzió:** 1.0

---

## Executive Summary

Ez a dokumentum összefoglalja az LLM agent rendszerek tesztelésével kapcsolatos kutatást, beleértve:
1. **Agent Behavior Testing** — hogyan teszteljük, hogy az agent azt csinálja amit szeretnénk
2. **Goal Drift Detection** — hogyan előzzük meg, hogy az agent "eltévedjen"
3. **SpaceOS Agent Framework inspirációs alapja** — milyen kutatásokra épül a rendszerünk

---

## 1. AGENT BEHAVIOR TESTING (2026)

### A Fő Kérdés

> "Azt lehet tesztelni, hogy egy agent azt csinálja amit szeretnénk?"

**Válasz:** Igen, három fő megközelítéssel.

### 1.1 Execution-Based Verification (Legmegbízhatóbb)

**Elv:** Futtasd az agent-et és ellenőrizd az eredményt determinisztikusan.

```
Agent Input → Agent Execution → Output/Side Effects → Deterministic Check
```

**Benchmark Példák:**

| Benchmark | Mit ellenőriz | Módszer |
|-----------|---------------|---------|
| **SWE-Bench** | Kód működik-e | Test suite futtatás (pass/fail) |
| **tau-bench** | Database művelet helyes-e | Database state ellenőrzés |
| **tau2-bench** | Megbízhatóság | pass^k (k próbálkozásból hány sikeres) |
| **WebArena** | Web task completion | DOM state + URL ellenőrzés |

**SpaceOS alkalmazás:**
```
Terminál Input (inbox task)
    ↓
Agent Execution (Claude session)
    ↓
Output (outbox DONE/BLOCKED)
    ↓
Deterministic Check:
  - DONE tartalmazza az elvárt deliverables-t?
  - Build/test pass?
  - Kód módosítások megfelelnek a spec-nek?
```

### 1.2 Három Metrika Osztály

A modern framework-ök (AgentEval, DeepEval, Phoenix) három metrika osztályt használnak:

#### TrajectoryAccuracy

**Mit mér:** Agent lépéssorozat vs. "golden path" (elvárt referencia)

```yaml
golden_path:
  - step: "Read inbox task"
  - step: "Run existing tests"
  - step: "Implement feature"
  - step: "Run tests again"
  - step: "Write DONE outbox"

actual_trajectory:
  - step: "Read inbox task"
  - step: "Implement feature"  # ⚠️ SKIPPED: Run existing tests
  - step: "Run tests"
  - step: "Write DONE outbox"

deviation_score: 0.8  # 80% match
```

#### ToolCorrectnessJudge

**Mit mér:** Tool/MCP hívások helyessége

**Ellenőrzött szempontok:**
- Helyes tool név
- Helyes paraméterek
- Helyes sorrend
- Graceful failure recovery

#### TaskCompletionJudge

**Mit mér:** Végső cél elérése (bináris vagy kategorikus)

**Kategóriák:**
- **COMPLETE:** Minden deliverable megvan
- **PARTIAL:** Néhány deliverable hiányzik
- **FAILED:** Kritikus hiba
- **BLOCKED:** External dependency

### 1.3 SWE-Bench 2026 Állapot

| Modell | Score | Dátum |
|--------|-------|-------|
| Claude Opus 4.7 | **87.6%** | 2026 április |
| Codex CLI + GPT-5.5 | **88.7%** | 2026 május |

> ⚠️ **Fontos:** Azonos modell 10-20 ponttal eltérő eredményt adhat különböző harness-ekben!

### 1.4 Evaluation Frameworks (2026)

| Framework | Újdonság |
|-----------|----------|
| **Phoenix v16.0.0** | Sandboxed Code Evaluators, LLM-jury |
| **DeepEval v4.0.3** | Decision Graph Logic, agentic eval harness |
| **Harbor, Exgentic** | Cross-environment agent assessment |

---

## 2. GOAL DRIFT DETECTION (SpaceOS Implementáció)

### 2.1 A Probléma

A **Goal Drift** (cél eltérés) 2026-ban az egyik meghatározó engineering kihívás. Az agent hosszú workflow során "elfelejti" vagy "eltéved" az eredeti céltól.

### 2.2 Az 5 Failure Mode

| Failure Mode | Leírás | SpaceOS Példa |
|--------------|--------|---------------|
| **Context Dilution** | Korai instrukciók elhalványulnak | Conductor 50 turn után elfelejti Phase 1 célokat |
| **Pattern Matching Override** | Friss kontextus felülírja az explicit direktívákat | DONE output felülírja az epic prioritást |
| **Inherited Drift** | Subagent outputok szennyezik a parent célokat | Backend részletei eltérítik Conductor fókuszát |
| **Value Conflict Drift** | Modell értékeivel ütköző instrukciók erodálódnak | "Gyors implementáció" vs "biztonságos kód" |
| **Subgoal Displacement** | Részfeladat tökéletesítése blokkolja az epic haladást | Refactoring instead of delivery |

### 2.3 SpaceOS Megoldási Minták (Implementálva!)

#### 1. Durable External Storage (Goal Externalization)

**Elv:** Goals must be externalized — persistent markdown files.

**SpaceOS implementáció:**
- `EPICS.yaml` — epic és phase definíciók
- `terminals/conductor/STATUS.md` — current state snapshot
- `.session-state.json` — cross-session goal recovery

#### 2. Plan-and-Act Architecture Separation

**Elv:** Separate Planner from Executor.

**SpaceOS implementáció:**
- **Conductor = Planner** — koordinál, nem ír kódot
- **Backend/Frontend = Executors** — implementálnak
- **Root = Strategic Planner** — epic-level döntések

#### 3. Dense Milestone Rewards (MiRA Framework)

**Elv:** Per-subgoal signals. Research: 6.4% → 43.0% performance improvement!

**SpaceOS implementáció (`watchDone.ts`):**
```
Sparse (rossz):
  Epic START → ... sok task ... → Epic DONE

Dense (jó):
  Epic START → Task 1 DONE (feedback) → Task 2 DONE (feedback) → Phase 1 DONE (feedback) → ...
```

#### 4. Goal Checkpointing at Boundaries

**Trigger-ek:**
- Session >50 turn
- Phase transition
- BLOCKED üzenet feldolgozás után
- Context window >60% telített

**Implementáció (`contextSaturation.ts`):**
```typescript
const WARNING_THRESHOLD = 30;
const CRITICAL_THRESHOLD = 50;
const AUTO_REANCHOR_THRESHOLD = 50;
```

#### 5. Subagent Output Filtering

**Elv:** Filter subagent outputs for goal-relevant content.

**Implementáció (`outputFiltering.ts`):**
- DONE outbox >1500 char → automatikus szűrés
- Csak goal-relevant summary jut el Conductor-hoz
- Prevents inherited drift

### 2.4 SpaceOS Goal Persistence Architektúra

```
┌─────────────────────────────────────────────────────────────────┐
│                    GOAL PERSISTENCE SYSTEM                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  SESSION START                  SESSION KÖZBEN                   │
│  ─────────────                  ──────────────                   │
│  ┌────────────────────┐        ┌────────────────────┐           │
│  │ Cross-session      │        │ Auto Context       │           │
│  │ Goal Recovery      │        │ Saturation         │           │
│  │ (sessionState.ts)  │        │ (contextSaturation │           │
│  │                    │        │  .ts)              │           │
│  │ - Load prev state  │        │ - Turn counting    │           │
│  │ - Inject recovery  │        │ - Auto re-anchor   │           │
│  │   context          │        │   at >50 turns     │           │
│  └────────────────────┘        └────────────────────┘           │
│           │                             │                        │
│           ▼                             ▼                        │
│  ┌────────────────────┐        ┌────────────────────┐           │
│  │ Mode Awareness     │        │ Dense Milestone    │           │
│  │ Context            │        │ Feedback           │           │
│  │ (sessionStarter.ts)│        │ (watchDone.ts)     │           │
│  │                    │        │                    │           │
│  │ - EPICS.yaml read  │        │ - Per-DONE inject  │           │
│  │ - Goal re-anchor   │        │ - Progress update  │           │
│  │ - Checkpoint state │        │ - Next milestone   │           │
│  └────────────────────┘        └────────────────────┘           │
│                                         │                        │
│                                         ▼                        │
│                                ┌────────────────────┐           │
│                                │ Subagent Output    │           │
│                                │ Filtering          │           │
│                                │ (outputFiltering   │           │
│                                │  .ts)              │           │
│                                └────────────────────┘           │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 3. SPACEOS AGENT FRAMEWORK — INSPIRÁCIÓS ALAPOK

### 3.1 Architektúra Inspirációk

| Forrás | Mit vettünk át |
|--------|----------------|
| **MiRA Framework** | Dense milestone rewards (per-task feedback) |
| **HiPER Pattern** | Hierarchical credit assignment (planner vs executor) |
| **Zylos Research (2026)** | Goal externalization, durable storage |
| **Anthropic Multi-Agent Sessions** | Session coordination, context management |
| **CodeBridge** | Multi-agent orchestration patterns |

### 3.2 SpaceOS Egyedi Innovációk

| Innováció | Leírás |
|-----------|--------|
| **Wake-on-Inbox** | Terminálok csak feladattal indulnak (resource efficiency) |
| **Mailbox Protocol** | Strukturált inbox/outbox markdown communication |
| **EPICS.yaml Dependency Graph** | Epic-level koordináció checkpoint-okkal |
| **Dual Review System** | 2× Haiku reviewer DONE validálás |
| **Monitor Terminal** | Autonomous health monitoring, goal drift detection |
| **Datahaven Dashboard** | Real-time terminal status + Kanban visualization |

### 3.3 Terminál Architektúra (7 Agent Role)

```
PRIORITY (Always Running)
  └── ROOT          Stratégiai döntések, agent infrastruktúra

COORDINATOR (Wake-on-Inbox)
  └── CONDUCTOR     Feladatkiosztás, pipeline koordináció

DEVELOPER TERMINALS (Wake-on-Inbox)
  ├── BACKEND       .NET + Node.js backend
  ├── FRONTEND      React/TS portál
  └── DESIGNER      UI/UX, Figma

SUPPORT TERMINALS (Spawn on Task)
  ├── ARCHITECT     Konzultatív arch partner
  ├── LIBRARIAN     Tudásbázis gondozó
  └── EXPLORER      Codebase kutatás
```

### 3.4 Autonomous Pipeline (Nightwatch Loop)

```typescript
async function nightwatchLoop() {
  // 1. Priority terminals (always running)
  await ensureRunning(['root', 'conductor']);

  // 2. Watch for DONE outbox → trigger reviewer
  await watchDone();

  // 3. Watch for stuck sessions → nudge
  await watchStuck();

  // 4. Watch for UNREAD inbox → wake terminals
  await watchInbox();

  // 5. Context saturation detection
  await checkContextSaturation();

  // 6. Dense milestone feedback injection
  await injectMilestoneFeedback();
}

// Run every 2 minutes
setInterval(nightwatchLoop, 120_000);
```

---

## 4. JAVASLATOK — KÖVETKEZŐ LÉPÉSEK

### 4.1 Agent Testing Infrastruktúra (Q3 2026)

| Feladat | Prioritás | Leírás |
|---------|-----------|--------|
| **Golden Path Recording** | HIGH | Sikeres session-ök rögzítése mint referencia |
| **Trajectory Comparison** | MEDIUM | Actual vs golden path deviation scoring |
| **Regression Test Suite** | HIGH | 10-20 historical task replay |
| **Datahaven Agent Health Dashboard** | MEDIUM | Metrics vizualizáció |

### 4.2 Goal Drift Prevention (DONE — 2026-07-04)

- ✅ Cross-session Goal Recovery (`sessionState.ts`)
- ✅ Auto Context Saturation Detection (`contextSaturation.ts`)
- ✅ Subagent Output Filtering (`outputFiltering.ts`)
- ✅ Dense Milestone Feedback (`watchDone.ts`)
- ✅ Monitor Goal Drift Protocol (`MONITOR_GOAL_DRIFT_DETECTION.md`)

### 4.3 Future Enhancements (Q4 2026)

| Feladat | Leírás |
|---------|--------|
| **Phoenix/DeepEval Integration** | External framework adoption |
| **Automated Deviation Alerts** | Telegram notification |
| **Predictive Monitoring** | Trend analysis, anomaly detection |
| **Parallel Session Execution** | Multiple backend sessions |

---

## 5. REFERENCIÁK

### External Sources (2026)

- [AI Agent Evaluation (2026): Metrics, Frameworks](https://www.morphllm.com/ai-agent-evaluation)
- [LLM Agent Evaluation Metrics 2026 - Confident AI](https://www.confident-ai.com/blog/llm-agent-evaluation-complete-guide)
- [AI Agent Eval Frameworks 2026](https://www.digitalapplied.com/blog/ai-agent-eval-frameworks-testing-guide-2026)
- [A Survey on Evaluation of LLM-based Agents (arXiv)](https://arxiv.org/html/2503.16416v2)
- [A Unified Framework for LLM Agentic Capabilities (arXiv)](https://arxiv.org/html/2605.27898v1)
- [Goal Persistence and Drift - Zylos Research](https://zylos.ai/research/2026-04-03-goal-persistence-drift-long-horizon-ai-agents/)
- [Multi-Agent Orchestration Guide - CodeBridge](https://www.codebridge.tech/articles/mastering-multi-agent-orchestration-coordination-is-the-new-scale-frontier)
- [LLM Agent Architectures - FutureAGI](https://futureagi.com/blog/llm-agent-architectures-core-components/)

### SpaceOS Internal Docs

| Fájl | Tartalom |
|------|----------|
| `docs/knowledge/patterns/AGENT_BEHAVIOR_TESTING_2026.md` | Részletes agent testing kutatás |
| `docs/knowledge/patterns/GOAL_PERSISTENCE_PATTERNS.md` | Goal drift megoldási minták |
| `docs/knowledge/patterns/MONITOR_GOAL_DRIFT_DETECTION.md` | Monitor protokoll |
| `docs/knowledge/patterns/AUTONOMOUS_AGENT_FRAMEWORK.md` | SpaceOS NEXUS architektúra |
| `docs/knowledge/patterns/TESTING_STRATEGIES.md` | Unit/Integration/E2E testing |

### Implementációs Fájlok

| Fájl | Funkció |
|------|---------|
| `spaceos-nexus/knowledge-service/src/sessionStarter.ts` | Goal re-anchoring hook |
| `spaceos-nexus/knowledge-service/src/pipeline/watchDone.ts` | Dense milestone feedback |
| `spaceos-nexus/knowledge-service/src/conductor/contextSaturation.ts` | Auto context detection |
| `spaceos-nexus/knowledge-service/src/conductor/outputFiltering.ts` | Subagent output filtering |
| `spaceos-nexus/knowledge-service/src/conductor/sessionState.ts` | Cross-session goal recovery |

---

## Összefoglaló

A SpaceOS agent rendszer **kutatás-alapú architektúrára** épül:

1. **Agent Behavior Testing:** Execution-based verification + trajectory analysis + tool correctness
2. **Goal Drift Prevention:** Dense milestone rewards + context saturation detection + output filtering
3. **Inspirációs alapok:** MiRA, HiPER, Zylos Research, Anthropic, CodeBridge

A rendszer 2026 júliusában **production-ready** a goal persistence komponensekkel, és Q3-ban bővül az agent testing infrastruktúrával.

---

**Document Status:** ✅ COMPLETE
**Készítette:** VPS Root
**Dátum:** 2026-07-12
