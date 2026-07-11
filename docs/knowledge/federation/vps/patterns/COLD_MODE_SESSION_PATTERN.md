# Cold Mode Session Pattern — Epic-Aware Task Routing

**Created:** 2026-06-25
**Status:** ACTIVE (production transition 2026-06-24)
**Tier:** HOT

---

## Overview

A Cold Mode Session Pattern a SpaceOS agent infrastruktúra **session lifecycle management** mechanizmusa. Minden terminal session **hideg indítással** (fresh context) kezdődik, és explicit task befejezéssel zárul.

**Lényegi változás (2026-06-24):**
- **Előtte:** Hot sessions (24/7 running, context accumulation)
- **Utána:** Cold sessions (task-triggered, context reset)

---

## Motivation

### Miért váltottunk cold mode-ra?

**1. Context bloat megelőzés**
- Hot session: 100k+ token context 8-12 óra után
- Cold session: Fresh start minden task-nál (<10k token)

**2. Determinisztikus session indítás**
- Hot session: Függő állapot (mi történt 2 órával ezelőtt?)
- Cold session: Reprodukálható kezdőállapot (CLAUDE.md + task)

**3. Memória konzisztencia**
- Hot session: MEMORY.md gyakran elavult
- Cold session: MEMORY.md minden session végén frissítve

**4. Epic-aware routing**
- Hot session: Task sorrendet ember határozta meg
- Cold session: Epic dependency graph automatikus routing

---

## Mechanizmus

### 1. Task Dispatch (Conductor → Terminal)

**Epic Router API:**
```bash
# Conductor dispatches next task in epic chain
POST /api/epic-router/task/<terminal>/dispatch
{
  "epicId": "EPIC-DATAHAVEN-UI",
  "taskId": "MSG-BACKEND-047",
  "messageId": "MSG-BACKEND-047"
}
```

**Task file creation:**
```
terminals/<terminal>/inbox/YYYY-MM-DD_NNN_<slug>.md
```

**Frontmatter:**
```yaml
---
id: MSG-<TERMINAL>-NNN
from: conductor
to: <terminal>
type: task
priority: high
status: UNREAD
epic_id: EPIC-DATAHAVEN-UI
created: YYYY-MM-DD
model: sonnet
---
```

### 2. Session Start (Cold Boot)

**Trigger:** Nightwatch detection (UNREAD inbox)
```bash
# scripts/nightwatch.sh → watch-inbox.sh
grep -rl "status: UNREAD" terminals/*/inbox/ | head -1
```

**Session starter invocation:**
```bash
# spaceos-nexus/knowledge-service/src/sessionStarter.ts
curl -X POST http://localhost:3456/api/session/start \
  -d '{
    "terminal": "backend",
    "model": "sonnet",
    "prompt": "[TASK ASSIGNED] MSG-BACKEND-047\n\nStart session ritual:\n1. mcp__spaceos-knowledge__fetch_task\n2. mcp__spaceos-knowledge__ack_task\n3. Work on task\n4. mcp__spaceos-knowledge__complete_task",
    "fromTerminal": "conductor"
  }'
```

**Session boot sequence:**
1. Fresh Claude Code session (no previous context)
2. CLAUDE.md loaded (identity + rules)
3. Task prompt injected
4. MCP tools available

### 3. Task Execution

**Terminal session ritual (example: backend):**
```markdown
1. **Fetch task content:**
   curl http://localhost:3456/api/epic-router/fetch/backend/MSG-BACKEND-047

2. **Acknowledge receipt:**
   curl -X POST http://localhost:3456/api/epic-router/ack/backend/MSG-BACKEND-047

3. **Work on task:**
   - Read files
   - Write code
   - Run tests
   - Update MEMORY.md

4. **Complete task:**
   curl -X POST http://localhost:3456/api/epic-router/task/backend/complete \
     -d '{"messageId":"MSG-BACKEND-047"}'
```

**Security:** Terminal can ONLY fetch/ack/complete tasks assigned to itself (403 Forbidden otherwise)

### 4. Task Completion & Next Task Routing

**Epic-aware routing:**
```typescript
// spaceos-nexus/knowledge-service/src/pipeline/epicRouter.ts
async function findNextTask(completedTaskId: string): Promise<string | null> {
  const epic = await loadEpic(completedTaskId);
  const dependentTasks = epic.tasks.filter(t =>
    t.depends_on.includes(completedTaskId) &&
    t.status === 'pending'
  );

  if (dependentTasks.length === 0) return null; // Epic complete

  // Priority: critical > high > medium > low
  const nextTask = dependentTasks.sort((a, b) =>
    priorityWeight(b.priority) - priorityWeight(a.priority)
  )[0];

  return nextTask.id;
}
```

**Auto-dispatch:**
```bash
# If next task exists in same epic → auto-inject to same terminal
POST /api/session/inject
{
  "terminal": "backend",
  "prompt": "[TASK ASSIGNED] MSG-BACKEND-048\n\n..."
}

# If next task in different terminal → new session start
POST /api/session/start
{
  "terminal": "frontend",
  "model": "sonnet",
  "prompt": "[TASK ASSIGNED] MSG-FRONTEND-047\n\n..."
}
```

### 5. Session Shutdown (Graceful Exit)

**Trigger:** Task complete + no next task in epic
```bash
# Epic-router response
{
  "success": true,
  "nextTask": null,  # No more tasks
  "epicStatus": "done"
}
```

**Shutdown ritual (terminal責任):**
```markdown
1. **Save memory:**
   mcp__spaceos-knowledge__save_tiered_memory
   terminal: "backend"
   tier: "warm"
   content: "Session 2026-06-25: MSG-BACKEND-047 done. Flow editor API enhanced."

2. **Register idle:**
   mcp__spaceos-knowledge__register_idle
   terminal: "backend"

3. **Exit:**
   /exit  (or manual Ctrl+C)
```

**Session lifecycle:**
```
[UNREAD inbox] → [Session start] → [Task work] → [DONE outbox] → [Review APPROVE] → [Next task?] → [Shutdown]
                                                                                     ↓ (yes)
                                                                              [Inject task] → [Loop]
```

---

## Pattern Strengths

### ✅ Predictable Context
- Minden session ugyanabból az állapotból indul (CLAUDE.md)
- Debugging egyszerűbb (nincs hidden state)

### ✅ Memory Freshness
- MEMORY.md minden session végén frissítve
- Elavult információ kizárva (warm tier 14 nap TTL)

### ✅ Epic Coordination
- Task dependency graph automatikusan követve
- Manual sequencing error elkerülve

### ✅ Resource Optimization
- Session csak task ideje alatt él (2-4 óra átlag)
- Idle terminálok nem fogyasztanak resourcet

### ✅ Isolation
- Egy task hibája nem hatja át a következőt
- Fresh context = fresh start

---

## Pattern Weaknesses & Mitigations

### ⚠️ Context Loss
**Problem:** Previous session tudás elvész

**Mitigation:**
1. **Tiered Memory:** hot/warm/cold tier (ADR-046)
   - Hot tier: 48h TTL (legutóbbi task-ok)
   - Warm tier: 14d TTL (közelmúlt patterns)
   - Cold tier: 365d TTL (archív döntések)
2. **MEMORY.md mandatory update:** Every session must append
3. **Knowledge Base:** Librarian szintetizál patterns → docs/knowledge/

### ⚠️ Session Startup Latency
**Problem:** Cold start ~10-15s (session init + MCP connection)

**Mitigation:**
- **Acceptable trade-off:** 15s delay << context bloat cost
- **Future optimization:** Pre-warm session pool (Phase 2)

### ⚠️ Epic Dependency Complexity
**Problem:** Circular dependencies → infinite loop

**Mitigation:**
- **Cycle detection:** Graph validator (ADR-041)
- **Manual override:** Root can break loops
- **Max depth limit:** 10 tasks/epic (hard limit)

---

## Usage Patterns

### For Terminals

**Cold start checklist (session ritual):**
```markdown
1. **Context load:**
   - Read CLAUDE.md (identity)
   - Read MEMORY.md (session history)
   - Read inbox task (current work)

2. **Fetch task:**
   mcp__spaceos-knowledge__fetch_task
   terminal: "<terminal>"
   messageId: "<MSG-ID>"

3. **Acknowledge:**
   mcp__spaceos-knowledge__ack_task
   terminal: "<terminal>"
   messageId: "<MSG-ID>"

4. **Work:**
   [Implementation logic]

5. **Complete:**
   mcp__spaceos-knowledge__complete_task
   terminal: "<terminal>"
   messageId: "<MSG-ID>"
   summary: "[1-2 mondat összefoglaló]"

6. **Idle:**
   mcp__spaceos-knowledge__register_idle
   terminal: "<terminal>"
```

### For Conductor

**Epic dispatch strategy:**
```markdown
1. **Check epic status:**
   GET /api/epic-router/epic/<EPIC-ID>/status

2. **Dispatch first task:**
   POST /api/epic-router/task/<terminal>/dispatch
   {"epicId": "...", "taskId": "...", "messageId": "..."}

3. **Wait for DONE:**
   [Nightwatch review pipeline]

4. **Auto-route next task:**
   [Epic-router handles this automatically]

5. **Monitor completion:**
   GET /api/epic-router/epic/<EPIC-ID>/status
   → {"status": "done", "completedTasks": 5, "totalTasks": 5}
```

---

## Metrics & Performance

**2026-06-24 Cold Mode Transition Statistics:**
- **Sessions converted:** 8 terminals (100% coverage)
- **Average session duration:** 2.5 hours (down from 12h hot mode)
- **Context size:** 8-12k tokens (down from 80-100k)
- **Memory freshness:** 100% (vs 40% hot mode)
- **Epic routing success:** 95% (1 manual override due cycle)
- **Session startup latency:** 12 seconds average

**Token savings:**
- Hot mode: ~100k tokens/session × 8 terminals = 800k tokens
- Cold mode: ~10k tokens/session × 8 terminals = 80k tokens
- **Savings:** 90% reduction in context token usage

---

## Evolution & Future

### Phase 1 (CURRENT — 2026-06-24)
- Cold start sessions (task-triggered)
- Epic-aware routing
- Manual MEMORY.md update
- Tiered memory (ADR-046)

### Phase 2 (Proposed)
- **Session pool:** Pre-warmed sessions (5s startup)
- **Auto-memory sync:** Automatic MEMORY.md append on completion
- **Multi-epic support:** Parallel epic execution (backend + frontend epics)

### Phase 3 (Future)
- **Context caching:** Claude API context caching (reduce cold start latency)
- **Smart context injection:** Only load relevant MEMORY.md entries (not all warm tier)
- **Epic visualization:** Datahaven UI epic progress dashboard

---

## Related Patterns

- **[TERMINAL_REVIEW_PATTERN.md](TERMINAL_REVIEW_PATTERN.md)** — DONE review mechanism (triggers next task routing)
- **[MEMORY_CLEANUP_PATTERN.md](MEMORY_CLEANUP_PATTERN.md)** — Memory tier cleanup (ensures fresh context)
- **[MCP_INTEGRATION_WORKFLOW.md](MCP_INTEGRATION_WORKFLOW.md)** — MCP tools (fetch_task, ack_task, complete_task)

---

## References

- **Epic Router:** `spaceos-nexus/knowledge-service/src/pipeline/epicRouter.ts`
- **Session Starter:** `spaceos-nexus/knowledge-service/src/sessionStarter.ts`
- **Nightwatch:** `scripts/nightwatch.sh` → `watch-inbox.sh`
- **ADR-046:** Tiered Memory (hot/warm/cold tier definitions)
- **ADR-041:** Graph-Based Workflow (epic dependency graph)

---

**Last updated:** 2026-06-25 (Librarian memory cleanup session)
