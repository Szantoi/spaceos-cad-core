# Autonomous Agent Framework — SpaceOS NEXUS

> **Version:** 1.0
> **Last Updated:** 2026-06-23
> **Source:** NEXUS Infrastructure Audit, Explorer Daily Activity Synthesis, Session Management MCP
> **Maintained By:** Librarian

---

## OVERVIEW

**NEXUS** is the autonomous agent infrastructure for SpaceOS. It provides session management, mailbox coordination, and autonomous task execution across 8 specialized terminals.

**Purpose:**
- Enable **autonomous multi-agent workflows** (no human in the loop)
- Provide **wake-on-inbox** pattern (terminals run only when needed)
- Track **session state** and **terminal status** (WORKING/IDLE)
- Coordinate **complex multi-terminal tasks** (e.g., Epic → Architect → Backend → Frontend)

**Architecture:** Modular pipeline + MCP protocol + Datahaven monitoring dashboard

---

## AGENT ARCHITECTURE

### 7 Terminal Agent Roles

```
┌─────────────────────────────────────────────────────────────┐
│ PRIORITY (Always Running)                                    │
│ └── ROOT          Stratégiai döntések, agent infrastruktúra  │
├─────────────────────────────────────────────────────────────┤
│ COORDINATOR (Wake-on-Inbox)                                  │
│ └── CONDUCTOR     Feladatkiosztás, pipeline koordináció      │
├─────────────────────────────────────────────────────────────┤
│ DEVELOPER TERMINALS (Wake-on-Inbox)                          │
│ ├── BACKEND       .NET + Node.js backend (Kernel, Modules)   │
│ ├── FRONTEND      React/TS portál fejlesztés                 │
│ └── DESIGNER      UI/UX, Figma integráció                    │
├─────────────────────────────────────────────────────────────┤
│ SUPPORT TERMINALS (Spawn on Task)                            │
│ ├── ARCHITECT     Konzultatív arch partner                   │
│ ├── LIBRARIAN     Tudásbázis gondozó (memory synthesis)      │
│ └── EXPLORER      Codebase kutatás, monitoring               │
└─────────────────────────────────────────────────────────────┘
```

### Terminal Lifecycle States

| State | Description | Trigger | Next State |
|-------|-------------|---------|------------|
| **IDLE** | No active session, waiting | - | WORKING (on inbox UNREAD) |
| **WORKING** | Session active, executing task | Inbox UNREAD detected | IDLE (on DONE/BLOCKED) |
| **BLOCKED** | Waiting for dependency | Dependency missing | WORKING (on dependency resolved) |
| **SPAWNED** | Temporary session for one task | Explicit spawn request | IDLE (on task complete) |

### Terminal Directory Structure

```
/opt/spaceos/terminals/<terminal>/
├── CLAUDE.md              ← Terminal identity, instructions
├── MEMORY.md              ← Terminal-specific memory (optional)
├── inbox/                 ← Incoming tasks (UNREAD → READ)
│   └── YYYY-MM-DD_NNN_slug.md
├── outbox/                ← Completed tasks (DONE/BLOCKED)
│   └── YYYY-MM-DD_NNN_slug-done.md
└── archive/               ← Completed, archived tasks
    └── YYYY-MM-DD_NNN_slug.md
```

---

## PATTERN 1: WAKE-ON-INBOX

### Description

**Wake-on-Inbox** = Terminals **automatically start sessions** when **UNREAD inbox messages** are detected, execute the task, write DONE outbox, and **return to IDLE**.

**Benefit:** Resource efficiency — terminals run only when needed, not 24/7.

### Implementation

**Inbox Watcher** (`spaceos-nexus/knowledge-service/src/inboxWatcher.ts`):

```typescript
import chokidar from 'chokidar';
import { startSession } from './sessionStarter';

const watcher = chokidar.watch('/opt/spaceos/terminals/*/inbox/*.md', {
  persistent: true,
  ignoreInitial: true,  // Don't trigger on existing files
  awaitWriteFinish: {
    stabilityThreshold: 500,  // Wait 500ms for file write to complete
    pollInterval: 100
  }
});

watcher.on('add', async (filePath: string) => {
  console.log(`[InboxWatcher] New inbox detected: ${filePath}`);

  // Extract terminal from path: terminals/backend/inbox/...
  const terminal = filePath.split('/')[3];

  // Extract model from frontmatter (sonnet | opus | haiku)
  const model = await extractModel(filePath);

  // Start session with prompt: "Process inbox messages"
  await startSession(terminal, model, 'Process inbox messages', 'inbox-watcher');
});
```

**Session Starter:**

```typescript
export async function startSession(
  terminal: string,
  model: 'sonnet' | 'opus' | 'haiku',
  prompt: string,
  fromTerminal: string
): Promise<void> {
  // 1. Check authorization (can fromTerminal start terminal session?)
  if (!canStartSession(fromTerminal, terminal)) {
    throw new Error(`${fromTerminal} not authorized to start ${terminal} session`);
  }

  // 2. Register terminal as WORKING
  await registerWorking(terminal);

  // 3. Start Claude Code session via tmux
  const command = `
    cd /opt/spaceos/terminals/${terminal} &&
    echo "${prompt}" | claude --model ${model}
  `;

  exec(`tmux new-session -d -s spaceos-${terminal} "${command}"`);

  // 4. Log session start
  await logSessionStart(terminal, model, prompt, fromTerminal);
}
```

### Authorization Matrix

| Initiator | Can Start Session For | Scope |
|-----------|----------------------|-------|
| **root** | ALL (8 terminals) | `session:start:*` |
| **conductor** | architect, librarian, explorer, backend, frontend, designer | `session:start:worker` |
| **inbox-watcher** | ALL (automatic wake) | `session:start:*` |
| **terminals** | SELF only | - |

### Wake-on-Inbox Flow

```mermaid
sequenceDiagram
    participant Root
    participant Conductor
    participant InboxWatcher
    participant Backend
    participant Datahaven

    Root->>Conductor: Inbox: "Process Q3 Cutting consensus"
    InboxWatcher->>Conductor: New file detected
    InboxWatcher->>Conductor: Start session (sonnet)
    Conductor->>Conductor: Process inbox
    Conductor->>Backend: Inbox: "Implement Track A APIs"
    InboxWatcher->>Backend: New file detected
    InboxWatcher->>Backend: Start session (sonnet)
    Backend->>Backend: Implement APIs
    Backend->>Backend: Write DONE outbox
    Backend->>Datahaven: Register IDLE
    Conductor->>Conductor: Write DONE outbox
    Conductor->>Datahaven: Register IDLE
```

---

## PATTERN 2: MAILBOX SYSTEM

### Description

**Mailbox** = Inbox/Outbox message passing between terminals. Messages are **Markdown files with YAML frontmatter**.

**Message Flow:**
```
Sender → Inbox (UNREAD) → Terminal Session → Outbox (DONE/BLOCKED) → Reviewer → Archive
```

### Inbox Message Format

**File:** `terminals/backend/inbox/2026-06-23_123_cutting-api.md`

```yaml
---
id: MSG-BACKEND-123
from: conductor
to: backend
type: task
priority: high
status: UNREAD
model: sonnet
task_type: CODE
review_type: content
ref: MSG-CONDUCTOR-045  # Optional: reference to related message
created: 2026-06-23
---

# Implement Cutting Plan API

Implement REST API endpoints for cutting plan CRUD operations.

## Acceptance Criteria
- GET /api/cutting/plans
- POST /api/cutting/plans
- PUT /api/cutting/plans/{id}
- DELETE /api/cutting/plans/{id}
- All endpoints have auth middleware
- Unit tests pass

## Dependencies
- MSG-KERNEL-045 (Auth middleware) — DONE
- MSG-JOINERY-023 (CAD data parser) — DONE
```

### Outbox Message Format

**File:** `terminals/backend/outbox/2026-06-23_123_cutting-api-done.md`

```yaml
---
id: MSG-BACKEND-123-DONE
from: backend
to: conductor
type: done
priority: high
status: UNREAD
ref: MSG-BACKEND-123
model: sonnet
git_commit: abc123def456
files_changed: 15
created: 2026-06-23
---

# Cutting Plan API — DONE

## Implementation Summary

Implemented 4 REST API endpoints for cutting plan CRUD operations:
- GET /api/cutting/plans — List all plans (paginated)
- POST /api/cutting/plans — Create new plan
- PUT /api/cutting/plans/{id} — Update plan
- DELETE /api/cutting/plans/{id} — Soft delete plan

## Files Changed
- src/Features/CuttingPlan/CreateCuttingPlan/Command.cs
- src/Features/CuttingPlan/CreateCuttingPlan/Handler.cs
- src/Features/CuttingPlan/CreateCuttingPlan/Validator.cs
- ... (12 more)

## Tests
- Unit tests: 17/17 ✅
- Integration tests: 5/5 ✅
- Build: SUCCESS

## Git Commit
```
abc123def456 (feat: implement cutting plan CRUD API)
```

## Next Steps
None — feature complete. Ready for production.
```

### Blocked Message Format

**File:** `terminals/backend/outbox/2026-06-23_124_shopfloor-api-blocked.md`

```yaml
---
id: MSG-BACKEND-124-BLOCKED
from: backend
to: conductor
type: blocked
priority: high
status: UNREAD
ref: MSG-BACKEND-124
model: sonnet
created: 2026-06-23
blocked_by: Architecture decision needed
blocker_type: ARCHITECTURE  # ARCHITECTURE | DEPENDENCY | RESOURCE | EXTERNAL
---

# ShopFloor Kiosk API — BLOCKED

## Blocker

**Type:** Architecture decision needed

**Description:**
ShopFloor Kiosk requires **real-time machine status updates**. Two architectural approaches:

**Option A: WebSocket**
- Pros: Real-time, low latency
- Cons: Connection management complexity, scaling challenges

**Option B: Server-Sent Events (SSE)**
- Pros: Simpler, HTTP-based, auto-reconnect
- Cons: One-way only (client can't send updates)

**Recommendation:** SSE (simpler, sufficient for read-only kiosk)

**Next Actions:**
1. Architect review architecture decision
2. Conductor escalate to Root if needed
3. Backend implement chosen approach
```

### MCP Mailbox API

**Endpoint:** `POST /api/mailbox/:terminal/inbox`

```bash
# Create inbox message via MCP
curl -X POST http://localhost:3456/api/mailbox/backend/inbox \
  -H "Content-Type: application/json" \
  -d '{
    "from": "conductor",
    "type": "task",
    "priority": "high",
    "model": "sonnet",
    "task_type": "CODE",
    "review_type": "content",
    "title": "Implement cutting API",
    "content": "..."
  }'
```

**Response:**
```json
{
  "success": true,
  "inbox_id": "MSG-BACKEND-123",
  "inbox_path": "terminals/backend/inbox/2026-06-23_123_cutting-api.md"
}
```

---

## PATTERN 3: TERMINAL STATUS TRACKING

### Description

**Terminal Status** = Real-time tracking of WORKING/IDLE state + current task. Displayed on **Datahaven Dashboard**.

### Status Types

| Status | Description | Display |
|--------|-------------|---------|
| **IDLE** | No active task, waiting for inbox | 🟢 Green |
| **WORKING** | Actively executing task | 🟡 Yellow |
| **BLOCKED** | Waiting for dependency/decision | 🔴 Red |
| **ERROR** | Session crashed or stuck | 🔴 Red |

### Datahaven API

**Endpoint:** `POST https://datahaven.joinerytech.hu/api/terminal/status`

```bash
# Register as WORKING
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "backend",
    "status": "working",
    "currentTask": "Implementing cutting plan API (MSG-BACKEND-123)"
  }'

# Response:
{
  "success": true,
  "message": "Terminal \"backend\" registered as WORKING",
  "currentTask": "Implementing cutting plan API (MSG-BACKEND-123)"
}
```

```bash
# Register as IDLE
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"backend","status":"idle"}'

# Response:
{
  "success": true,
  "message": "Terminal \"backend\" registered as IDLE"
}
```

### Session Ritual (Terminal-Side)

**1. Session Start:**
```bash
# Register as WORKING
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"backend","status":"working","currentTask":"Session started - processing inbox"}'
```

**2. During Work:**
```bash
# Update current task (optional)
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"backend","status":"working","currentTask":"Writing unit tests for cutting API"}'
```

**3. Session End:**
```bash
# Register as IDLE
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"backend","status":"idle"}'
```

### MCP Status Tools

**MCP Tools Available:**
```typescript
// Register as WORKING
mcp__spaceos-knowledge__register_working({ terminal: "backend", task_id: "MSG-BACKEND-123" })

// Register as IDLE
mcp__spaceos-knowledge__register_idle({ terminal: "backend" })

// Get status of all terminals
mcp__spaceos-knowledge__get_terminal_status()
```

---

## PATTERN 4: AGENT COORDINATION

### Problem: Multi-Terminal Workflows

**Scenario:** Epic requires **Architect → Backend → Frontend** coordination.

**Challenge:**
- Architect must finish spec before Backend starts
- Backend must finish API before Frontend starts
- Conductor must orchestrate sequence

### Solution: Dependency Graph + Inbox Coordination

**Epic Dependency Graph:**

```yaml
# docs/projects/EPICS.yaml
epics:
  - id: EPIC-CUTTING-Q3
    name: "Cutting Module Q3 Expansion"
    depends_on: ["EPIC-KERNEL-V2"]  # Must complete first
    parallel_with: []                # No parallel epics
    status: active
    target_date: "2026-06-30"

    tasks:
      - id: MSG-ARCHITECT-010
        name: "Cutting API spec review"
        depends_on: []
        assignee: architect
        status: done

      - id: MSG-BACKEND-123
        name: "Implement cutting API"
        depends_on: ["MSG-ARCHITECT-010"]  # Wait for spec
        assignee: backend
        status: done

      - id: MSG-FRONTEND-045
        name: "Cutting UI implementation"
        depends_on: ["MSG-BACKEND-123"]    # Wait for API
        assignee: frontend
        status: in_progress
```

**Conductor Orchestration:**

```typescript
// Conductor session logic
async function processEpic(epicId: string) {
  // 1. Load epic graph
  const epic = await loadEpic(epicId);

  // 2. Find unblocked tasks (dependencies satisfied)
  const unblockedTasks = epic.tasks.filter(task =>
    task.status === 'pending' &&
    task.depends_on.every(depId => isTaskDone(depId))
  );

  // 3. Dispatch inbox messages for unblocked tasks
  for (const task of unblockedTasks) {
    await sendInbox(task.assignee, {
      id: task.id,
      title: task.name,
      content: task.description,
      dependencies: task.depends_on
    });

    // Mark task as dispatched
    task.status = 'in_progress';
  }

  // 4. Save updated epic
  await saveEpic(epic);
}

// Watch for DONE outbox → trigger next task
async function onTaskDone(taskId: string) {
  const epic = await findEpicForTask(taskId);
  await processEpic(epic.id);  // Dispatch next unblocked tasks
}
```

### Parallel Task Execution

**Scenario:** Multiple independent tasks can run in parallel.

```yaml
tasks:
  - id: MSG-BACKEND-125
    name: "EHS API implementation"
    depends_on: []
    assignee: backend
    status: in_progress

  - id: MSG-BACKEND-126
    name: "Catalog API implementation"
    depends_on: []
    assignee: backend
    status: in_progress  # Can run in parallel with MSG-BACKEND-125

  - id: MSG-FRONTEND-046
    name: "EHS UI implementation"
    depends_on: ["MSG-BACKEND-125"]  # Wait for EHS API
    assignee: frontend
    status: pending
```

**Conductor dispatches both backend tasks simultaneously:**

```typescript
// Both inbox messages created at the same time
await sendInbox('backend', { id: 'MSG-BACKEND-125', ... });
await sendInbox('backend', { id: 'MSG-BACKEND-126', ... });

// Backend terminal processes one at a time (sequential execution)
// OR: spawn 2 backend sessions (parallel execution — future)
```

---

## PATTERN 5: SESSION MANAGEMENT MCP

### Description

**MCP (Model Context Protocol)** provides programmatic control over terminal sessions:
- Start sessions
- Inject prompts into running sessions
- Wake terminals
- Query session status
- Read audit logs

### MCP Tools

#### 1. Start Session

```typescript
POST /api/session/start
{
  "terminal": "backend",
  "model": "sonnet",
  "prompt": "Implement cutting plan API",
  "fromTerminal": "conductor"
}
```

**Authorization:**
- root → can start ANY terminal
- conductor → can start worker terminals only
- terminals → cannot start other terminals

#### 2. Inject Prompt (Running Session)

```typescript
POST /api/session/inject
{
  "terminal": "backend",
  "prompt": "Run tests and verify all pass",
  "fromTerminal": "conductor"
}
```

**Use Case:** Nudge terminal during execution (e.g., "Don't forget to run tests").

#### 3. Wake Terminal

```typescript
POST /api/session/wake
{
  "terminal": "backend",
  "fromTerminal": "conductor"
}
```

**Equivalent to:**
```typescript
POST /api/session/start
{
  "terminal": "backend",
  "model": "sonnet",
  "prompt": "Process inbox messages",
  "fromTerminal": "conductor"
}
```

#### 4. Query Session Status

```typescript
GET /api/session/:terminal
GET /api/sessions/all
```

**Response:**
```json
{
  "terminal": "backend",
  "sessionActive": true,
  "tmuxSession": "spaceos-backend",
  "model": "sonnet",
  "startedAt": "2026-06-23T05:30:00Z",
  "currentTask": "Implementing cutting API"
}
```

#### 5. Read Audit Logs

```typescript
GET /api/sessions/logs?days=1
```

**Response:**
```json
[
  {
    "timestamp": "2026-06-23T05:30:00Z",
    "event": "SESSION_START",
    "terminal": "backend",
    "model": "sonnet",
    "initiatedBy": "conductor"
  },
  {
    "timestamp": "2026-06-23T06:15:00Z",
    "event": "SESSION_END",
    "terminal": "backend",
    "exitCode": 0
  }
]
```

---

## PATTERN 6: AUTONOMOUS PIPELINE

### Nightwatch Loop (Orchestrator)

**File:** `spaceos-nexus/knowledge-service/src/pipeline/nightwatch.ts`

**Cron:** Every 2 minutes

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

  // 5. Update terminal status → Datahaven
  await updateTerminalStatus();

  // 6. Log heartbeat
  await logHeartbeat();
}

// Run every 2 minutes
setInterval(nightwatchLoop, 120_000);
```

### Pipeline Components

| Component | File | Frequency | Function |
|-----------|------|-----------|----------|
| **Nightwatch** | `nightwatch.ts` | Every 2 min | Orchestrator loop |
| **Watch Done** | `watchDone.ts` | On DONE outbox | Trigger reviewer |
| **Watch Stuck** | `watchStuck.ts` | Every 10 min | Nudge stuck sessions |
| **Watch Inbox** | `inboxWatcher.ts` | Real-time (chokidar) | Wake terminals |
| **Watch Priority** | `watchPriority.ts` | Every 2 min | Ensure root/conductor running |
| **Reviewer** | `reviewer.ts` | On DONE | Dual Haiku review |
| **Plan Scan** | `planScan.ts` | Every 30 min | Idea → Debate → Consensus |
| **Plan Debate** | `planDebate.ts` | On plan-scan | 2× Sonnet A/B debate |
| **Plan Select** | `planSelect.ts` | On consensus | Select best plan |

### Autonomous Workflow Example

```
1. User creates idea file: docs/planning/ideas/2026-06-23_cutting-optimization.md
2. Plan Scan (cron @ :00, :30) → detects new idea
3. Plan Debate → 2× Sonnet A/B agents generate plans
4. Plan Select → Root selects best plan → docs/planning/queue/
5. Conductor (wake-on-queue) → processes queue
6. Conductor → creates inbox for Backend (MSG-BACKEND-127)
7. Inbox Watcher → detects new inbox → starts Backend session
8. Backend → implements feature → writes DONE outbox
9. Watch Done → detects DONE → triggers reviewer
10. Reviewer → dual Haiku review → APPROVE
11. Pipeline → marks inbox READ, updates status, commits git
12. Datahaven → shows Backend IDLE
```

---

## PATTERN 7: GRACEFUL DEGRADATION

### Problem: MCP Tools Unavailable

**Scenario:** knowledge-service down, MCP tools unavailable.

**Fallback:** Use `curl` directly (as documented in CLAUDE.md).

**Example:**

```bash
# Terminal CLAUDE.md session ritual

# Try MCP tool first
mcp__spaceos-knowledge__register_working({ terminal: "backend" })

# If MCP fails → fallback to curl
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"backend","status":"working"}'
```

**Benefit:** System continues to function even if MCP layer fails.

---

## IMPLEMENTATION STATUS

### Phase 1: Foundation (DONE — 2026-06-20)

- [x] Express HTTP API server (`src/server.ts`)
- [x] Mailbox API endpoints (`POST /api/mailbox/:terminal/inbox`)
- [x] Session management API (`POST /api/session/start`)
- [x] Terminal status tracking (`POST /api/terminal/status`)
- [x] Inbox watcher (`src/inboxWatcher.ts`)
- [x] Nightwatch loop (`src/pipeline/nightwatch.ts`)

### Phase 2: MCP Integration (DONE — 2026-06-22)

- [x] MCP protocol implementation (`src/mcp.ts`)
- [x] stdio-HTTP bridge (`bin/stdio-bridge.js`)
- [x] Session starter (`src/sessionStarter.ts`)
- [x] Terminal status MCP tools
- [x] Mailbox MCP tools

### Phase 3: Autonomous Coordination (DONE — 2026-06-23)

- [x] Epic dependency graph (`docs/projects/EPICS.yaml`)
- [x] Graph API endpoints (`GET /api/graph/epics`)
- [x] Critical path calculation
- [x] Mermaid diagram export
- [x] Cycle detection

### Phase 4: Governance (IN PROGRESS — 2026-06-24)

- [ ] Task audit trail (creation log)
- [ ] Formal review procedures
- [ ] Token-based authorization
- [ ] Daily task summary

---

## AGENT SPAWN PATTERNS

### Pattern 1: Persistent Agent (Root, Conductor)

**Characteristics:**
- Always running (never stops)
- Long-lived session
- Continuous monitoring

**Use Case:**
- Root: Strategic decisions, agent infrastruktúra építés
- Conductor: Daily coordination, feladatkiosztás

**Implementation:**
```bash
# tmux session (persistent)
tmux new-session -d -s spaceos-root "cd /opt/spaceos && claude --model opus"
tmux new-session -d -s spaceos-conductor "cd /opt/spaceos/terminals/conductor && claude --model sonnet"
```

### Pattern 2: Wake-on-Inbox (Backend, Frontend, Designer)

**Characteristics:**
- Start on UNREAD inbox
- Execute task
- Write DONE outbox
- Stop (return to IDLE)

**Use Case:**
- Backend: Implement API
- Frontend: Build UI
- Designer: Create mockups

**Implementation:**
```typescript
// Inbox watcher triggers session start
watcher.on('add', async (filePath) => {
  await startSession(terminal, model, 'Process inbox messages', 'inbox-watcher');
});
```

### Pattern 3: Spawn-on-Demand (Architect, Librarian, Explorer)

**Characteristics:**
- Explicit spawn request (not automatic)
- Execute specific task
- Write DONE outbox
- Stop

**Use Case:**
- Architect: Review architecture before implementation
- Librarian: Synthesize knowledge docs (daily)
- Explorer: Codebase research (ad-hoc)

**Implementation:**
```bash
# Manual spawn (Conductor or Root)
curl -X POST http://localhost:3456/api/session/start \
  -H "Content-Type: application/json" \
  -d '{"terminal":"architect","model":"opus","prompt":"Review cutting API spec","fromTerminal":"conductor"}'
```

### Pattern 4: Parallel Spawn (Future)

**Characteristics:**
- Multiple sessions of same terminal
- Parallel task execution
- Shared workspace (git-safe)

**Use Case:**
- Backend: Implement 2 APIs in parallel
- Frontend: Build 2 UIs in parallel

**Implementation (Future):**
```bash
# Spawn 2 backend sessions
tmux new-session -d -s spaceos-backend-1 "cd /opt/spaceos/terminals/backend && claude --session 1"
tmux new-session -d -s spaceos-backend-2 "cd /opt/spaceos/terminals/backend && claude --session 2"
```

---

## MONITORING & OBSERVABILITY

### Datahaven Dashboard

**URL:** https://datahaven.joinerytech.hu

**Pages:**

| Page | URL | Purpose |
|------|-----|---------|
| **Dashboard** | `/` | Terminal status, session activity |
| **Kanban** | `/kanban` | Dual-track board (Discovery + Delivery) |
| **Planning** | `/planning` | 5-stage pipeline (Idea → Consensus) |
| **Projects** | `/projects` | Gantt timeline, epic dependency graph |

**Metrics:**

```typescript
interface TerminalStatus {
  terminal: string;
  status: 'idle' | 'working' | 'blocked' | 'error';
  sessionActive: boolean;
  currentTask?: string;
  inboxUnread: number;
  outboxUnread: number;
  lastActivity: string;  // ISO 8601
}
```

**Real-Time Updates:**

```javascript
// Server-Sent Events (SSE)
const eventSource = new EventSource('https://datahaven.joinerytech.hu/api/stream');

eventSource.onmessage = (event) => {
  const data = JSON.parse(event.data);
  // Update UI with terminal status
  updateDashboard(data);
};
```

### Session Audit Log

**Location:** `/opt/spaceos/logs/sessions/YYYY-MM-DD.jsonl`

**Entry Format:**

```json
{
  "timestamp": "2026-06-23T05:30:00Z",
  "event": "SESSION_START",
  "terminal": "backend",
  "model": "sonnet",
  "initiatedBy": "inbox-watcher",
  "prompt": "Process inbox messages"
}
```

**Query Example:**

```bash
# Count sessions started today
cat /opt/spaceos/logs/sessions/$(date +%Y-%m-%d).jsonl | \
  grep "SESSION_START" | wc -l
```

---

## TROUBLESHOOTING

### Issue 1: Terminal Stuck (Not Responding)

**Symptom:** Terminal status = WORKING for >30 min, no outbox.

**Diagnosis:**
```bash
# Check tmux session
tmux attach -t spaceos-backend

# Check last activity
tail -100 ~/.claude/projects/-opt-spaceos-terminals-backend/conversation_*.jsonl
```

**Fix:**
```bash
# Send Enter to unstick
tmux send-keys -t spaceos-backend "" Enter Enter

# If still stuck → restart session
tmux kill-session -t spaceos-backend
# Inbox watcher will auto-restart
```

### Issue 2: Inbox Watcher Not Triggering

**Symptom:** UNREAD inbox exists, but no session started.

**Diagnosis:**
```bash
# Check knowledge-service status
systemctl status spaceos-nexus

# Check logs
tail -100 /opt/spaceos/logs/nexus/knowledge-service.log
```

**Fix:**
```bash
# Restart knowledge-service
systemctl restart spaceos-nexus

# Manually trigger session
curl -X POST http://localhost:3456/api/session/wake \
  -d '{"terminal":"backend","fromTerminal":"root"}'
```

### Issue 3: Datahaven Status Not Updating

**Symptom:** Dashboard shows stale status.

**Diagnosis:**
```bash
# Check Datahaven API
curl https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026"

# Check SSE stream
curl https://datahaven.joinerytech.hu/api/stream
```

**Fix:**
```bash
# Manually update status
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"backend","status":"idle"}'
```

---

## FUTURE ENHANCEMENTS

### 1. Parallel Session Execution

**Goal:** Run multiple backend sessions in parallel for independent tasks.

**Challenge:** Git conflicts, file locking, session isolation.

**Solution:** Session-scoped feature branches.

### 2. Agent Swarms

**Goal:** Spawn 5+ agents to work on epic in parallel.

**Example:**
```bash
# Spawn swarm for EPIC-CUTTING-Q3
/swarm spawn cutting-q3 --agents 5 --model sonnet
```

### 3. Cross-Terminal Messaging

**Goal:** Terminals can send messages to each other (not just via Conductor).

**Use Case:** Backend → Frontend: "API ready, you can start UI now"

**Implementation:**
```typescript
POST /api/terminal/:terminal/message
{
  "from": "backend",
  "to": "frontend",
  "type": "notification",
  "content": "Cutting API ready (MSG-BACKEND-123)"
}
```

### 4. Agent Memory Sync

**Goal:** Shared memory across terminals (not just terminal-specific).

**Use Case:** Backend learns pattern → Librarian synthesizes → All terminals access.

**Implementation:** Tiered memory (hot/warm/cold/shared) — already designed, needs activation.

---

## REFERENCES

**Source Documents:**
- NEXUS Infrastructure Audit (`docs/agent-infrastructure/NEXUS_INFRASTRUCTURE_AUDIT.md`)
- Explorer Daily Activity Synthesis (MSG-EXPLORER-020)
- Session Management MCP (`spaceos-nexus/knowledge-service/src/sessionManager.ts`)
- Inbox Watcher (`spaceos-nexus/knowledge-service/src/inboxWatcher.ts`)

**Related Knowledge Docs:**
- `ARCHITECTURAL_PATTERNS_CATALOGUE.md` — 12 patterns
- `ENTERPRISE_GOVERNANCE_PATTERNS.md` — Task Audit, formal reviews
- `MCP_INTEGRATION_WORKFLOW.md` — MCP protocol patterns

**External References:**
- [Model Context Protocol](https://modelcontextprotocol.io/)
- [tmux Documentation](https://github.com/tmux/tmux/wiki)
- [Chokidar File Watcher](https://github.com/paulmillr/chokidar)

---

**Document Status:** ✅ COMPLETE
**Next Review:** 2026-07-30 (1 month)
**Maintained By:** Librarian (synthesis from NEXUS audit + Explorer research)
