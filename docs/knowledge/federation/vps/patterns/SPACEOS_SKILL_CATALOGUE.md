# SpaceOS Skill Catalogue

> **Reusable operational patterns** — Code-based + Workflow-based skills
>
> Explorer (code) + Librarian (workflow) collaboration (2026-06-30)

---

## 📋 SKILL OVERVIEW

| Skill | Type | Owner | Location |
|-------|------|-------|----------|
| **tmux-session-management** | Code | Explorer | `~/.claude/skills/tmux-session-management/` |
| **mcp-tool-patterns** | Code | Explorer | `~/.claude/skills/mcp-tool-patterns/` |
| **inbox-outbox-format** | Code | Explorer | `~/.claude/skills/inbox-outbox-format/` |
| **cron-automation** | Workflow | Librarian | `~/.claude/skills/cron-automation/` |
| **service-management** | Workflow | Librarian | `~/.claude/skills/service-management/` |

---

## 🔧 CODE-BASED SKILLS (Explorer)

### 1. tmux-session-management

**What:** Tmux session lifecycle — create, kill, inject, capture operations

**When to use:**
- Start/stop terminal sessions
- Inject prompts into running sessions
- Capture session output for debugging
- Cold/continuous mode management

**Core pattern:**
```bash
# Shared socket
TMUX_SOCKET="/tmp/spaceos.tmux"
tmux -S "$TMUX_SOCKET" has-session -t spaceos-backend

# Session creation
tmux -S "$TMUX_SOCKET" new-session -d -s spaceos-backend \
  -c /opt/spaceos/terminals/backend \
  "claude code chat --profile backend"

# Prompt injection
tmux -S "$TMUX_SOCKET" send-keys -t spaceos-backend \
  "Dolgozd fel az inbox üzeneteket" Enter

# Output capture
tmux -S "$TMUX_SOCKET" capture-pane -t spaceos-backend -p
```

**Key concepts:**
- Shared socket pattern (`/tmp/spaceos.tmux`)
- Session naming: `spaceos-<terminal>`
- Dual-socket fallback (shared → `/tmp/tmux-<uid>/default`)

**Code sources:**
- `spaceos-nexus/knowledge-service/src/sessionStarter.ts`
- `scripts/cold-shutdown.sh`
- `spaceos-nexus/knowledge-service/src/pipeline/common.ts`

---

### 2. mcp-tool-patterns

**What:** MCP (Model Context Protocol) JSON-RPC 2.0 implementation patterns

**When to use:**
- Add new MCP tool to knowledge-service
- Implement tool authentication
- Define tool permissions per terminal
- Handle MCP requests/responses

**Core pattern:**
```typescript
// Tool definition
{
  name: "read_memory",
  description: "Read terminal memory",
  inputSchema: {
    type: "object",
    properties: {
      terminal: { type: "string", enum: TERMINALS }
    },
    required: ["terminal"]
  }
}

// Tool handler
async function handleToolCall(name: string, args: any, agent: Agent) {
  // 1. Validate input
  if (!validateToolInput(name, args)) throw new Error("Invalid input");

  // 2. Authorize
  if (!canUseTool(agent, name)) throw new Error("Unauthorized");

  // 3. Execute
  const result = await executeToolLogic(name, args);

  // 4. Return
  return { success: true, data: result };
}
```

**Key concepts:**
- JSON-RPC 2.0 protocol (`{"jsonrpc":"2.0","method":"...","id":1}`)
- Master token + agent tokens (config/agents.yaml)
- Tool permissions (config/tool-permissions.yaml)
- 29 tools by category (knowledge, mailbox, identity, tasks, etc.)

**Code sources:**
- `spaceos-nexus/knowledge-service/src/mcp.ts`
- `spaceos-nexus/knowledge-service/config/agents.yaml`
- `spaceos-nexus/knowledge-service/config/tool-permissions.yaml`

---

### 3. inbox-outbox-format

**What:** File-based mailbox message format specification

**When to use:**
- Create inbox/outbox messages manually
- Parse mailbox messages in scripts
- Validate message frontmatter
- Understand message lifecycle

**Core pattern:**
```markdown
---
id: MSG-BACKEND-042
from: conductor
to: backend
type: task
priority: high
status: UNREAD
model: sonnet
ref: MSG-BACKEND-041
created: 2026-06-30
content_hash: abc123...
---

# Task Title

Task content here (markdown)
```

**Key concepts:**
- File naming: `YYYY-MM-DD_NNN_[slug].md`
- Frontmatter: YAML (9 required fields)
- Status lifecycle: `UNREAD` → `READ`
- Message types: `task`, `question`, `info`, `done`, `blocked`
- Priority levels: `critical`, `high`, `medium`, `low`
- Model selection: `opus`, `sonnet`, `haiku`

**Code sources:**
- `spaceos-nexus/knowledge-service/src/mailbox.ts`
- `terminals/*/inbox/*.md` (examples)
- `spaceos-nexus/knowledge-service/src/inboxWatcher.ts`

---

## 🔄 WORKFLOW-BASED SKILLS (Librarian)

### 4. cron-automation

**What:** Nightwatch pipeline — automated terminal lifecycle + DONE processing

**When to use:**
- Understand automated terminal wake-up
- Debug stuck sessions
- Trace DONE processing workflow
- Modify cron automation timing

**Core workflow:**

```
Every 2 minutes (*/2 cron):
  1. watch-priority.sh    → Root/Conductor MINDIG futnak
  2. watch-done.sh        → DONE review + pipeline
  3. watch-stuck.sh       → Befagyott session nudge
  4. watch-inbox.sh       → UNREAD inbox → wake-up

Every 30 minutes (*/30 cron):
  1. plan-scan.sh         → Idea → Selected (top 3)
  2. plan-debate.sh       → A/B dual Sonnet review
  3. plan-select.sh       → Consensus → Queue
  4. Conductor inbox      → Queue feldolgozás
```

**Key concepts:**
- Priority sessions (root, conductor) — always running
- Wake-on-inbox (backend, frontend, architect, etc.)
- Dual terminal review (Architect + Librarian → APPROVE/REJECT)
- Stuck detection: >5min no output + UNREAD inbox
- Soft nudge (Enter) → Hard nudge (kill-session after 10min)

**Code sources:**
- `scripts/nightwatch.sh` (main orchestrator)
- `scripts/watch-*.sh` (individual watchers)
- `scripts/reviewer.sh` (dual review)
- `scripts/pipeline.sh` (DONE processing)

---

### 5. service-management

**What:** Knowledge service lifecycle — start, stop, restart, health, logs

**When to use:**
- Start/stop knowledge-service (MCP server)
- Restart after config change
- Health check monitoring
- Troubleshoot port conflicts, database locks
- Run database migrations

**Core workflow:**

```bash
# Start
cd /opt/spaceos/spaceos-nexus/knowledge-service
npm run build
node dist/server.js

# Health check
curl http://localhost:3456/health

# Stop (graceful)
kill -TERM <PID>

# Restart
sudo systemctl restart spaceos-knowledge

# Logs
tail -f logs/service.log
```

**Key concepts:**
- Port 3456 (HTTP API)
- stdio bridge (MCP protocol)
- SQLite databases (memory.db, messages.db)
- Systemd service (auto-restart on failure)
- Log rotation (7 days)
- Graceful shutdown (SIGTERM → close connections → exit)

**Troubleshooting:**
- Port conflict: `lsof -i :3456` → `kill -9 <PID>`
- Database lock: `PRAGMA integrity_check` → restore from backup
- Memory leak: RSS >500MB → restart
- MCP timeout: stdio bridge check → restart service

**Code sources:**
- `spaceos-nexus/knowledge-service/src/server.ts`
- `spaceos-nexus/knowledge-service/bin/stdio-bridge.js`
- `/etc/systemd/system/spaceos-knowledge.service`
- `scripts/backup-knowledge.sh`

---

## 🎯 SKILL INTEGRATION STRATEGY

### Code Skills → Terminal Usage

**Explorer code skills** használata terminálokban:

| Terminal | tmux-session | mcp-tool | inbox-outbox |
|----------|-------------|----------|--------------|
| **Root** | ✅ Session start/stop | ✅ All MCP tools | ✅ Inbox write/read |
| **Conductor** | ✅ Wake terminals | ✅ Task routing | ✅ DONE processing |
| **Backend** | ✅ Session check | ✅ Memory read/write | ✅ Outbox write |
| **Frontend** | ✅ Session check | ✅ Memory read | ✅ Outbox write |
| **Architect** | ⚠️ Rarely | ✅ Read-only | ✅ Review DONE |
| **Librarian** | ⚠️ Rarely | ✅ Memory management | ✅ Review DONE |

### Workflow Skills → Infra Layer

**Librarian workflow skills** használata:

| Skill | Used by | Frequency |
|-------|---------|-----------|
| **cron-automation** | Nightwatch cron | Every 2 min |
| **service-management** | Root, Conductor | On-demand |

**When to reference:**
- **cron-automation:** Ha egy terminál stuck, vagy inbox nem dolgozódik fel
- **service-management:** Ha MCP timeout, port conflict, vagy database error

---

## 📚 KNOWLEDGE BASE INTEGRATION

### Pattern Documentation

Ezek a skill-ek most már részei a knowledge base-nek:

```
docs/knowledge/patterns/
  ├── SPACEOS_SKILL_CATALOGUE.md        ← Ez a fájl (skill index)
  └── [továbbra is elérhető minden más pattern doc]
```

**Skill fájlok:**
```
~/.claude/skills/
  ├── tmux-session-management/SKILL.md
  ├── mcp-tool-patterns/SKILL.md
  ├── inbox-outbox-format/SKILL.md
  ├── cron-automation/SKILL.md
  └── service-management/SKILL.md
```

### Session Ritual Integration

**Terminálok session start-kor:**

1. Read `knowledge/domain.memory.md` (hot context)
2. Read `knowledge/patterns.memory.md` (warm patterns)
3. **NEW:** Reference skill catalogue ha releváns feladat
   - Tmux issue? → `tmux-session-management`
   - MCP error? → `mcp-tool-patterns`
   - Inbox format? → `inbox-outbox-format`

**Infra feladatoknál (Root, Conductor):**
- Cron issue? → `cron-automation`
- Service down? → `service-management`

---

## 🔗 KAPCSOLÓDÓ DOKUMENTUMOK

| Kategória | Dokumentum | Kapcsolat |
|-----------|-----------|-----------|
| **Architecture** | ADR-049 Phase 3 | Parallel workers (domain memory context) |
| **Patterns** | COLD_MODE_SESSION_PATTERN.md | Tmux session cold start |
| **Patterns** | MCP_INTEGRATION_WORKFLOW.md | MCP stdio bridge |
| **Deployment** | DEPLOYMENT_RUNBOOK.md | Service lifecycle |
| **Context** | NEXUS_CONTEXT.md | Knowledge service architecture |

---

## 🚀 KÖVETKEZŐ LÉPÉSEK

### Skill Maintenance

**Explorer:**
- ✅ Code skills created (3/3)
- ⏳ Monitor for code changes (sessionStarter, mcp.ts, mailbox.ts)
- ⏳ Update skills when patterns evolve

**Librarian:**
- ✅ Workflow skills created (2/2)
- ✅ Integrated into knowledge base (SPACEOS_SKILL_CATALOGUE.md)
- ⏳ Monitor nightwatch logs for workflow changes
- ⏳ Update service-management for systemd improvements

### Skill Expansion (Future)

**P3 candidates:**
- **git-workflow** — commit message format, PR creation, branch strategy
- **test-patterns** — Testcontainers setup, E2E workflow
- **deployment-pipeline** — VPS deploy steps, rollback procedure
- **monitoring-patterns** — Log aggregation, metric collection

---

**Last updated:** 2026-06-30
**Collaboration:** Explorer (code) + Librarian (workflow)
**Owner:** Knowledge base (shared)
