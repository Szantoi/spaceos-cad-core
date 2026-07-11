# Goal Persistence és Goal Drift Patterns (2026)

> **Forrás:** 2026-os kutatás alapján (Zylos Research, CodeBridge, FutureAGI, Anthropic)
> **Létrehozva:** 2026-07-04
> **Releváns:** Conductor koordináció, Epic management, hosszú futású agent workflow-k

---

## A Probléma

A **Goal Drift** és **Goal Persistence** 2026-ban az egyik meghatározó engineering kihívás a production AI agent rendszereknél. Az agent hosszú workflow során "elfelejti" vagy "eltéved" az eredeti céltól.

---

## 5 Goal Drift Failure Mode

| Failure Mode | Leírás | Példa SpaceOS-ben |
|---|---|---|
| **Context Dilution** | Korai instrukciók elhalványulnak ahogy zaj gyűlik | Conductor 50 turn után elfelejti Phase 1 célokat |
| **Pattern Matching Override** | Friss kontextus felülírja az explicit direktívákat | Terminál DONE output felülírja az epic prioritást |
| **Inherited Drift** | Subagent outputok szennyezik a parent célokat | Backend DONE részletei eltérítik Conductor fókuszát |
| **Value Conflict Drift** | Modell értékeivel ütköző instrukciók erodálódnak | "Gyors implementáció" vs "biztonságos kód" konfliktus |
| **Subgoal Displacement** | Részfeladatok optimalizálása aláássa a fő célt | Egy task tökéletesítése blokkolja az epic haladást |

---

## Megoldási Minták

### 1. Durable External Storage (Goal Externalization)

**Elv:** Goals must be externalized — persistent markdown files that survive restarts and context resets.

**SpaceOS implementáció:**
- `EPICS.yaml` — epic és phase definíciók
- `terminals/conductor/STATUS.md` — current state snapshot
- Per-phase checkpoint fájlok

**Használat:**
```bash
# Conductor session indításkor MINDIG beolvasni:
cat docs/projects/EPICS.yaml
cat terminals/conductor/STATUS.md
```

### 2. Plan-and-Act Architecture Separation

**Elv:** Separate Planner (generates plans) from Executor (implements). Prevents goal dilution.

**SpaceOS implementáció:**
- **Conductor = Planner** — koordinál, nem ír kódot
- **Backend/Frontend = Executors** — implementálnak
- **Root = Strategic Planner** — epic-level döntések

**Szabály:** Planner soha ne implementáljon, Executor soha ne tervezzen.

### 3. Dense Milestone Rewards (MiRA Framework)

**Elv:** Per-subgoal signals instead of sparse terminal rewards. Research: 6.4% → 43.0% performance improvement.

**SpaceOS implementáció:**
```
Sparse (rossz):
  Epic START → ... sok task ... → Epic DONE

Dense (jó):
  Epic START → Task 1 DONE (feedback) → Task 2 DONE (feedback) → Phase 1 DONE (feedback) → ...
```

**Konkrét lépések:**
- Minden task DONE → immediate Conductor feedback
- Feedback formátum: "Task X complete. Epic progress: Phase 2 (65% → 68%)"
- Minden Phase completion → explicit acknowledgment

### 4. Goal Checkpointing at Boundaries

**Elv:** When context approaches saturation, trigger explicit goal re-anchoring.

**SpaceOS implementáció:**
```markdown
# Re-anchoring Template (minden 10-15 turn után)

## Current State
- Epic: EPIC-CUTTING-Q3
- Phase: 2 (Core Modules)
- Progress: 72%

## Remaining Milestones
1. Backend Week 2 completion (80% → 100%)
2. Frontend Wave 2 completion (60% → 100%)
3. Phase 3 dispatch

## Next Immediate Action
[konkrét következő lépés]
```

**Trigger-ek:**
- Session >50 turn
- Phase transition
- BLOCKED üzenet feldolgozás után
- Context window >60% telített

### 5. Hierarchical Credit Assignment (HiPER)

**Elv:** Planners receive credit at subgoal level, executors at action level.

**SpaceOS implementáció:**
- Conductor "credit" = Phase completion count
- Terminal "credit" = Task completion count
- Root "credit" = Epic completion count

**Tracking:**
```yaml
# terminals/conductor/METRICS.md
phases_completed: 3
tasks_coordinated: 47
epics_delivered: 1
```

### 6. Subagent Output Filtering

**Elv:** Filter subagent outputs for goal-relevant content before parent ingestion. Prevents inherited drift.

**SpaceOS implementáció:**
```
Terminal DONE outbox:
  ├── Full technical details (archive)
  └── Goal-relevant summary (Conductor ingests)

Conductor látja:
  "✅ MSG-BACKEND-042 DONE: Kernel FSM implementálva. Phase 2 progress: +5%"

Conductor NEM látja:
  [500 sor technikai részlet ami eltérítheti a fókuszt]
```

---

## Session Hooks (Implementálandó)

### 1. Session Start Hook

```bash
# Conductor session indításkor automatikusan:
1. EPICS.yaml beolvasás
2. Current phase + progress kiírás
3. Next milestone explicit megnevezése
4. Remaining tasks lista
```

### 2. Context Saturation Detection

```bash
# Ha session >50 turn VAGY context >60%:
1. Trigger goal re-anchoring
2. Summarize completed work
3. Re-state current objective
4. Continue with fresh context
```

### 3. Task Completion Feedback

```bash
# Terminal DONE feldolgozás után:
1. Update epic progress %
2. Send Conductor feedback: "Task X done, progress Y%"
3. Check phase completion threshold
4. If >90% → trigger phase transition
```

---

## SpaceOS Specifikus Alkalmazás

### Már Meglévő Komponensek ✅

| Komponens | Goal Persistence Role |
|---|---|
| `EPICS.yaml` | Durable goal storage |
| `Conductor CLAUDE.md` | Planner identity separation |
| `inbox/outbox` mailbox | Structured communication |
| `DONE/BLOCKED` protocol | Task completion signals |
| `Monitor terminal` | Idle detection + ösztönzés |

### Hiányzó Komponensek ⏳

| Komponens | Megvalósítandó |
|---|---|
| Session start goal re-anchoring | Hook a session indításkor |
| Dense milestone feedback | Task-level progress updates |
| Context saturation detection | Turn counter + re-anchoring |
| Subagent output filtering | DONE summary extraction |
| Goal re-statement template | Standardizált formátum |

---

## Monitor Protocol Értékelés

A Monitor ösztönzési protokoll **hasznos komponens**, de **nem teljes megoldás**:

| Mit old meg | Mit nem old meg |
|---|---|
| ✅ Phase progress tracking | ❌ Context dilution |
| ✅ Idle detection | ❌ Inherited drift |
| ✅ Ösztönzés küldés | ❌ Subgoal displacement |
| ✅ Root escalation | ❌ Goal re-anchoring |

**Következtetés:** Monitor + Goal Persistence hooks együtt = robust solution.

---

## Implementációs Roadmap

### Phase 1: Monitor Integration (DONE)
- ✅ MONITOR-CONFIG.yaml
- ✅ Ösztönzés protocol
- ✅ Phase progress tracking

### Phase 2: Goal Persistence Hooks (DONE — 2026-07-04)
- ✅ Session start re-anchoring (`sessionStarter.ts:buildModeAwarenessContext()`)
- ✅ Dense milestone feedback (`watchDone.ts:generateDenseMilestoneFeedback()`)
- ✅ Task completion progress updates (auto-injected after DONE review)

### Phase 3: Advanced Patterns (DONE — 2026-07-04)
- ✅ Context saturation detection (Auto detector: contextSaturation.ts)
- ✅ Subagent output filtering (outputFiltering.ts)
- ✅ Cross-session goal recovery (sessionState.ts)

---

## Implementált Komponensek (2026-07-04)

### 1. Goal Re-Anchoring Hook (sessionStarter.ts)

**Lokáció:** `spaceos-nexus/knowledge-service/src/sessionStarter.ts:107-241`

**Működés:**
- Conductor session indításkor explicit goal statement injection
- Epic ID, progress %, checkpoint státusz megjelenítése
- "A TE EGYETLEN FELADATOD" explicit re-anchoring

**Kód részlet:**
```typescript
context += `### 🎯 GOAL RE-ANCHORING

**⚠️ A TE EGYETLEN FELADATOD:** Az \`${activeEpic.id}\` epic végigvitele!

| Metrika | Érték |
|---------|-------|
| **Progress** | **${progress}%** (${doneCount}/${totalCount} checkpoint) |
...
```

### 2. Dense Milestone Feedback (watchDone.ts)

**Lokáció:** `spaceos-nexus/knowledge-service/src/pipeline/watchDone.ts:24-97`

**Működés:**
- Minden approved DONE után automatikus feedback injection
- Epic progress % frissítés
- Következő milestone megjelenítése
- tmux send-keys a Conductor session-be

**Trigger:** `watchDone()` → `runPipeline()` → `generateDenseMilestoneFeedback()`

### 3. Context Saturation Detection (Conductor CLAUDE.md)

**Lokáció:** `terminals/conductor/CLAUDE.md:11-66`

**Működés:**
- Turn count figyelmeztetés utasítások
- >50 turn → kérj re-anchoring/új session
- Monitor-nak küldhet request-et

### 4. Epic Progress Calculation (epicManager.ts)

**Lokáció:** `spaceos-nexus/knowledge-service/src/conductor/epicManager.ts:62-69`

**Használt függvény:** `getEpicProgress(epic)` — checkpoint completion %

### 5. Cross-session Goal Recovery (sessionState.ts) — 2026-07-04

**Lokáció:** `spaceos-nexus/knowledge-service/src/conductor/sessionState.ts`

**Működés:**
- Session indításkor betölti a korábbi goal state-et (`.session-state.json`)
- Recovery context injection Conductor session-be
- State tartalmaz: epicId, progress, checkpoints, lastTurnCount, lastActiveTask

**Kód részlet:**
```typescript
export function buildRecoveryContext(): string | null {
  const state = loadGoalState();
  if (!state) return null;

  return `🔄 [CROSS-SESSION GOAL RECOVERY]

## ⚠️ Korábbi Session Context
- **Epic:** \`${state.epicId}\`
- **Progress:** **${state.progress}%**
- **Last Task:** \`${state.lastActiveTask || 'N/A'}\`

FOLYTASD AZ EPIC MUNKÁT!`;
}
```

### 6. Auto Context Saturation Detector (contextSaturation.ts) — 2026-07-04

**Lokáció:** `spaceos-nexus/knowledge-service/src/conductor/contextSaturation.ts`

**Működés:**
- Turn count persisted `.turn-count` fájlban
- Nightwatch ciklusonként increment (1 cycle ≈ 2-3 turn)
- WARNING: >30 turn, CRITICAL: >50 turn
- Auto re-anchoring trigger >50 turn → tmux injection + reset

**Threshold-ök:**
```typescript
const WARNING_THRESHOLD = 30;
const CRITICAL_THRESHOLD = 50;
const AUTO_REANCHOR_THRESHOLD = 50;
```

**Integration:** `nightwatch.ts` hívja `incrementTurnCount()` és `checkContextSaturation()`

### 7. Subagent Output Filtering (outputFiltering.ts) — 2026-07-04

**Lokáció:** `spaceos-nexus/knowledge-service/src/conductor/outputFiltering.ts`

**Működés:**
- DONE outbox tartalom szűrése goal-relevant summary-ra
- Heuristics: >500 char + code blocks VAGY >5 file paths VAGY >1500 char
- Summary extraction: max 200 char összefoglaló, file count, next steps

**Trigger:** `watchDone.ts:generateDenseMilestoneFeedback()` alkalmazza

**Példa:**
```
Input (793 chars technikai részlet):
  ## Summary
  Implementation complete with 7 files changed...
  ```typescript
  const x = 1;
  ```

Output (288 chars filtered):
  ## ✅ DONE Summary (Filtered)
  **Task:** MSG-BACKEND-042
  **Epic:** EPIC-CUTTING-Q3 (65%)
  ### Mi történt
  Implementation complete.
  ### Változások
  7 fájl módosítva
```

---

## Architektúra Összefoglaló (2026-07-04)

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
│                                │                    │           │
│                                │ - Goal-relevant    │           │
│                                │   summary only     │           │
│                                │ - Prevent inherit  │           │
│                                │   drift            │           │
│                                └────────────────────┘           │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Referenciák

- [Goal Persistence and Drift in Long-Horizon AI Agents - Zylos Research](https://zylos.ai/research/2026-04-03-goal-persistence-drift-long-horizon-ai-agents/)
- [Multi-Agent Orchestration Guide - CodeBridge](https://www.codebridge.tech/articles/mastering-multi-agent-orchestration-coordination-is-the-new-scale-frontier)
- [LLM Agent Architectures - FutureAGI](https://futureagi.com/blog/llm-agent-architectures-core-components/)
- [Anthropic Multi-Agent Sessions](https://platform.claude.com/docs/en/managed-agents/multi-agent)
- [AI Agent Orchestration 2026 - Viston](https://viston.tech/ai-agent-orchestration-in-2026-moving-from-pilots-to-enterprise-wide-execution/)

---

**Maintainer:** Root + Librarian
**Last Updated:** 2026-07-04
