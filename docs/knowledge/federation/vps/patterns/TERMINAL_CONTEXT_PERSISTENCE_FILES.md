# Terminal Context Persistence Files — Hosszú Futású Workflow-k Kontextus Megtartása

> **Létrehozva:** 2026-07-07
> **Célja:** Minden terminálnak ajánlott fájlstruktúra a goal persistence-hez
> **Forrás:** GOAL_PERSISTENCE_PATTERNS.md + 2026 LLM agent research
> **Maintained by:** Root + Librarian

---

## A Probléma

Long-running agent workflow-knál (>4 óra, >50 turn) a **goal drift** és **context rot** kritikus:
- **Context Dilution** — Korai instrukciók elhalványulnak
- **Pattern Matching Override** — Friss kontextus felülírja a célokat
- **Inherited Drift** — Subagent output eltéríti a fókuszt
- **Context Rot** — >50% degradation at 100K tokens (2026 research)

**Megoldás:** External durable storage — fájlok, amelyek túlélik a session restart-okat és context reset-eket.

---

## Terminál Fájl Struktúra — 3 Szint

### 🔴 KÖTELEZŐ (minden terminálnak)

Ezek nélkül a terminál NEM tud hosszú futású feladatokat végezni:

| Fájl | Formátum | Célja | MCP-servable |
|------|----------|-------|--------------|
| **CLAUDE.md** | Markdown | Terminál identity, role, szabályok | ✅ Yes |
| **inbox/** | .md frontmatter | Bejövő feladatok | ✅ Yes |
| **outbox/** | .md frontmatter | DONE/BLOCKED kimenetek | ✅ Yes |
| **archive/** | .md frontmatter | Lezárt feladatok | ✅ Yes |

### 🟡 STANDARD (coordinator és dev termináloknak ajánlott)

Ezek biztosítják a **goal persistence-t** és **cross-session recovery-t**:

| Fájl | Formátum | Célja | MCP-servable | Példa Terminál |
|------|----------|-------|--------------|----------------|
| **MEMORY.md** | Markdown | Long-term learnings, patterns | ✅ Yes | Minden terminál |
| **TODO.md** | Markdown | Aktuális feladatlista | ✅ Yes | Root, Conductor |
| **STATUS.md** | Markdown | Current state snapshot | ✅ Yes | Conductor ✅ |
| **.session-state.json** | JSON | Cross-session goal recovery | ✅ Yes | Conductor ✅ |
| **.turn-count** | Plain text | Context saturation tracking | ✅ Yes | Conductor ✅ |

### 🟢 OPCIONÁLIS (speciális use case-ekre)

Ezek specifikus koordinációs vagy fejlesztési mintákhoz kellenek:

| Fájl | Formátum | Célja | MCP-servable | Példa Terminál |
|------|----------|-------|--------------|----------------|
| **CHECKPOINTS.md** | Markdown | Milestone tracking | ✅ Yes | Conductor ✅ |
| **WORKFLOW.md** | Markdown | Active workflow steps | ✅ Yes | Conductor (future) |
| **METRICS.md** | YAML/Markdown | Credit assignment tracking | ✅ Yes | Conductor (future) |
| **GOAL.md** | Markdown | Explicit current goal | ✅ Yes | Epic-focused terminals |
| **.mcp.json** | JSON | MCP server config | ❌ No | Backend, Root |
| **memory.db** | SQLite | Conversation history | ❌ No | Conductor ✅ |
| **knowledge/** | Markdown docs | Domain-specific knowledge | ✅ Yes | Architect, Librarian |

---

## Fájl Template-ek

### 1. STATUS.md Template

**Célja:** Current state snapshot — goal re-anchoring at session start

```markdown
# <Terminal> Status Report

**Last Updated:** YYYY-MM-DD HH:MM UTC
**System Status:** ✅ OPERATIONAL | 🔄 IN PROGRESS | ⏸️ PAUSED | ❌ BLOCKED
**Health Check:** Latest check timestamp

## Current Focus

🎯 **Active Task:** MSG-<TERMINAL>-<NNN> (<task summary>)

**Focus Queue Summary:**
- X tasks actively in progress
- Y tasks queued
- Z BLOCKED items
- Epic: <current epic or phase>

## Epic/Project Status

### <Epic Name> (<Progress %>)
| Component | Status | Notes |
|-----------|--------|-------|
| <Component> | 🔄 IN PROGRESS | <notes> |
| <Component> | ⏳ QUEUED | <notes> |
| <Component> | ✅ DONE | <notes> |

**Effort:** X days | **Target:** <deadline or milestone>

## Recent Actions

✅ **YYYY-MM-DD HH:MM UTC**
- <Action taken>
- <Result or outcome>

## Next Steps

1. <Next immediate action>
2. <Following action>
3. <After that>

---

**<Terminal> Status:** ✅ WORKING | ⏸️ IDLE
**Session Started:** YYYY-MM-DD HH:MM UTC
**Next Report:** Daily or on priority changes
```

### 2. .session-state.json Template

**Célja:** Cross-session goal recovery — JSON format for fast parsing

```json
{
  "epicId": "EPIC-<NAME>-<VERSION>",
  "epicName": "<Human-readable epic name>",
  "epicProgress": 0,
  "nextCheckpointId": "CP-<ID>",
  "nextCheckpointName": "<Checkpoint name>",
  "completedCheckpoints": ["CP-001", "CP-002"],
  "lastTurnCount": 42,
  "lastActiveTask": "MSG-<TERMINAL>-<NNN>",
  "savedAt": "2026-07-07T12:00:00.000Z",
  "sessionId": "session-<timestamp>-<uuid>"
}
```

**Auto-update trigger:**
- Task completion (DONE/BLOCKED)
- Phase transition
- Session end

### 3. CHECKPOINTS.md Template

**Célja:** Milestone tracking — strategic decision points

```markdown
# <Terminal> Checkpoints

> Stratégiai döntési pontok és deadline-ok

---

## <Quarter/Phase> Checkpoints

### 📅 <Date> — <Checkpoint Name> GO/NO-GO

**Döntés:** <What decision needs to be made>

**Értékelési szempontok:**
- <Criterion 1>
- <Criterion 2>
- <Criterion 3>

**HA GO:**
1. <Action 1>
2. <Action 2>
3. <Action 3>
4. <Escalation or notification>

**HA NO-GO:**
1. <Alternative action 1>
2. <Alternative action 2>
3. <Escalation or notification>

**Ref:**
- Root approval: MSG-ROOT-<NNN>
- Proposal: <doc link>
- Spec: docs/<path>

---

_Last updated: YYYY-MM-DD HH:MM_
```

### 4. .turn-count Template

**Célja:** Context saturation tracking — simple counter

```
42
```

**Threshold-ök:**
- WARNING: >30 turn
- CRITICAL: >50 turn
- AUTO_REANCHOR: >50 turn (trigger re-anchoring + reset)

**Auto-update:** Nightwatch ciklusonként increment (1 cycle ≈ 2-3 turn)

### 5. WORKFLOW.md Template (Future)

**Célja:** Active workflow steps — explicit step-by-step tracking

```markdown
# <Terminal> Active Workflow

**Current Workflow:** <Epic or Phase name>
**Progress:** <X>/<Y> steps complete (<Progress %>)
**Last Updated:** YYYY-MM-DD HH:MM UTC

---

## Workflow Steps

| # | Step | Status | Notes |
|---|------|--------|-------|
| 1 | <Step 1> | ✅ DONE | Completed YYYY-MM-DD |
| 2 | <Step 2> | 🔄 IN PROGRESS | Started YYYY-MM-DD |
| 3 | <Step 3> | ⏳ PENDING | Blocked by Step 2 |
| 4 | <Step 4> | ⏳ PENDING | After Step 3 |

---

## Next Immediate Action

**Step 2:** <What needs to be done>

**Context:**
- <Relevant context>
- <Relevant context>

**Expected Outcome:**
- <What defines "done" for this step>
```

### 6. METRICS.md Template (Future — Hierarchical Credit Assignment)

**Célja:** Credit assignment tracking — motivates goal focus

```yaml
# <Terminal> Metrics

## Performance Metrics
phases_completed: 3
tasks_coordinated: 47
epics_delivered: 1
DONE_rate: 0.92
BLOCKED_rate: 0.08

## Current Period (Q3 2026)
tasks_in_progress: 2
tasks_queued: 5
avg_task_duration_hours: 12
avg_turn_count_per_task: 38

## Goal Persistence Metrics
context_saturation_triggers: 2
auto_reanchor_count: 1
goal_drift_incidents: 0

_Last updated: 2026-07-07 12:00 UTC_
```

---

## Session Start Ritual — Goal Re-Anchoring

**Minden session elején (minden terminál):**

```bash
# 1. Read core identity
cat CLAUDE.md

# 2. Load goal state (if exists)
cat .session-state.json 2>/dev/null || echo "No session state"

# 3. Check current status
cat STATUS.md 2>/dev/null || echo "No status file"

# 4. Check inbox
ls inbox/*.md 2>/dev/null | wc -l

# 5. Display turn count (if tracked)
cat .turn-count 2>/dev/null || echo "0"
```

**Coordinator terminálok (Conductor, Root) session start:**

```bash
# Standard ritual + extra context:
cat CHECKPOINTS.md
cat docs/projects/EPICS.yaml
grep -rl "status: UNREAD" terminals/*/outbox/ 2>/dev/null
```

---

## Session End Ritual — State Persistence

**Minden session végén (minden terminál):**

```bash
# 1. Update STATUS.md (current task, progress, next steps)
# 2. Update .session-state.json (if coordinator terminal)
# 3. Increment .turn-count (if tracked)
# 4. Update MEMORY.md (if learned something new)
# 5. Archive completed tasks (inbox → archive)
```

---

## MCP Integration — Knowledge Service Serving

**Minden fájl MCP-servable:**

```bash
# Knowledge Service semantic search
curl -X POST http://localhost:3456/api/knowledge/search \
  -H "Content-Type: application/json" \
  -d '{"query": "conductor status cutting expansion", "limit": 5}'

# Terminal identity fetch
curl -s http://localhost:3456/api/identity/conductor

# Terminal memory fetch
curl -s http://localhost:3456/api/memory/conductor
```

**Auto-indexing:** Knowledge Service automatikusan indexeli a `terminals/*/` mappákat:
- CLAUDE.md → identity
- MEMORY.md → long-term knowledge
- STATUS.md → current state
- inbox/outbox → task tracking

---

## Implementált Komponensek (2026-07-04)

### Backend Goal Persistence System

**Lokáció:** `spaceos-nexus/knowledge-service/src/`

| Komponens | Fájl | Célja |
|-----------|------|-------|
| Goal Re-Anchoring | `sessionStarter.ts` | Session start context injection |
| Dense Milestone Feedback | `pipeline/watchDone.ts` | Per-task progress updates |
| Context Saturation Detection | `conductor/contextSaturation.ts` | Auto re-anchoring >50 turn |
| Subagent Output Filtering | `conductor/outputFiltering.ts` | Goal-relevant summary extraction |
| Cross-Session Recovery | `conductor/sessionState.ts` | Load .session-state.json |
| Epic Progress Calculation | `conductor/epicManager.ts` | EPICS.yaml checkpoint tracking |

**Teljes architektúra:** `docs/knowledge/patterns/GOAL_PERSISTENCE_PATTERNS.md`

---

## Conductor Terminál — Reference Implementation ✅

**Conductor már használja a teljes fájl struktúrát:**

```
terminals/conductor/
  ├── CLAUDE.md                ← Identity + role
  ├── MEMORY.md                ← Long-term learnings
  ├── TODO.md                  ← Task tracking
  ├── STATUS.md                ← Current state snapshot ✅
  ├── CHECKPOINTS.md           ← Milestone tracking ✅
  ├── .session-state.json      ← Cross-session recovery ✅
  ├── .turn-count              ← Context saturation ✅
  ├── .mcp.json                ← MCP config
  ├── memory.db                ← Conversation history (SQLite)
  ├── inbox/                   ← Bejövő feladatok
  ├── outbox/                  ← DONE/BLOCKED kimenetek
  ├── archive/                 ← Lezárt feladatok
  └── knowledge/               ← Domain-specific docs
```

---

## Backend Terminál — Minimal Implementation

**Backend nem koordinál, csak implementál → egyszerűbb struktúra:**

```
terminals/backend/
  ├── CLAUDE.md                ← Identity + role
  ├── MEMORY.md                ← Long-term learnings
  ├── inbox/                   ← Bejövő feladatok
  ├── outbox/                  ← DONE/BLOCKED kimenetek
  └── archive/                 ← Lezárt feladatok
```

**Opcionális:**
- `TODO.md` — ha van több párhuzamos feladat
- `.mcp.json` — ha speciális MCP tool-okat használ

---

## Root Terminál — Strategic Implementation

**Root stratégiai döntések → STATUS.md + CHECKPOINTS.md:**

```
terminals/root/
  ├── CLAUDE.md                ← Identity + role
  ├── MEMORY.md                ← Long-term learnings
  ├── TODO.md                  ← Strategic task tracking ✅
  ├── STATUS.md                ← (Future) Strategic overview
  ├── CHECKPOINTS.md           ← (Future) Strategic milestones
  ├── inbox/                   ← (Legacy docs/mailbox/root/inbox/)
  ├── outbox/                  ← (Legacy docs/mailbox/root/outbox/)
  └── archive/                 ← (Legacy docs/mailbox/root/archive/)
```

**Note:** Root inbox/outbox még a régi `docs/mailbox/root/` helyen van (2026-06-21 pivot átállás folyamatban).

---

## Validáció — 5 Kérdés Session Indításkor

**Ha ezekre "Yes" a válasz, a terminál készen áll hosszú futású workflow-ra:**

| # | Kérdés | Cél |
|---|--------|-----|
| 1 | Van CLAUDE.md? | Identity + role kész |
| 2 | Van MEMORY.md? | Long-term learnings tárolva |
| 3 | Van inbox/outbox/archive? | Task tracking működik |
| 4 | Van STATUS.md (coordinator)? | Current state snapshot kész |
| 5 | Van .session-state.json (coordinator)? | Cross-session recovery működik |

---

## Következő Lépések

### Phase 1: Minden Terminál Minimum (2 nap)
- [x] Conductor ✅ (done)
- [ ] Root STATUS.md + CHECKPOINTS.md létrehozása
- [ ] Backend, Frontend, Designer inbox cleanup + standard struktúra validálás
- [ ] Architect, Librarian, Explorer inbox cleanup + standard struktúra validálás

### Phase 2: MCP API Finalization (1 nap)
- [ ] `GET /api/terminal/:terminal/status` endpoint (STATUS.md fetch)
- [ ] `GET /api/terminal/:terminal/session-state` endpoint (.session-state.json fetch)
- [ ] `GET /api/terminal/:terminal/checkpoints` endpoint (CHECKPOINTS.md fetch)

### Phase 3: Datahaven UI Integration (2 nap)
- [ ] Dashboard: Display STATUS.md snapshot per terminal
- [ ] Dashboard: Display turn count + context saturation warning
- [ ] Kanban: Display checkpoint milestones

---

## Referenciák

- [GOAL_PERSISTENCE_PATTERNS.md](./GOAL_PERSISTENCE_PATTERNS.md) — Goal drift failure modes + solution patterns
- [LLM Context Window Management 2026 - Zylos Research](https://zylos.ai/research/2026-01-19-llm-context-management/)
- [7 State Persistence Strategies for AI Agents 2026](https://www.indium.tech/blog/7-state-persistence-strategies-ai-agents-2026/)
- [SpaceOS WORKFLOW.md](/opt/spaceos/docs/WORKFLOW.md) — Teljes workflow architektúra

---

**Maintainer:** Root + Librarian
**Last Updated:** 2026-07-07 12:00 UTC
