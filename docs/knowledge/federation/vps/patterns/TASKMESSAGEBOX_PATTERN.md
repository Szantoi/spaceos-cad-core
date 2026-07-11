# TaskMessageBox Pattern — DB-Backed Message System

**Created:** 2026-07-01
**Author:** Librarian
**Status:** Production (2026-06-24+)

---

## Overview

**TaskMessageBox (TMB)** is a DB-backed message system that replaces file-based inbox/outbox messaging with a SQLite database as the source of truth. Markdown files are rendered as readonly views for backward compatibility.

### Problem Solved

**Before TaskMessageBox:**
- Inbox/outbox messages stored as individual `.md` files
- Race conditions during concurrent file writes
- No atomic operations
- Difficult to query/filter messages
- Manual file parsing required

**After TaskMessageBox:**
- Single SQLite database (`task-message-box.db`) as source of truth
- Atomic operations with ACID guarantees
- SQL queries for filtering/reporting
- Auto-rendered `.md` files for human readability
- Message notes/progress tracking

---

## Architecture

### Database Schema

**4 Core Tables:**

1. **`messages`** — All tasks, questions, DONE, BLOCKED, info messages
2. **`message_notes`** — Appended notes (implementation, feedback, blockers, progress)
3. **`terminal_status`** — Current working/idle state per terminal
4. **`message_sequence`** — Auto-increment sequence per terminal

**4 Views:**

- `v_inbox` — Incoming messages sorted by priority
- `v_outbox` — DONE/BLOCKED messages chronologically
- `v_active_tasks` — In-progress tasks
- `v_blocked_tasks` — Blocked tasks ordered by recency

### Message Lifecycle

```
UNREAD → READ → IN_PROGRESS → (COMPLETED | BLOCKED) → ARCHIVED
```

**Status Transitions:**

| From | To | Trigger |
|------|----|---------|
| `unread` | `read` | Terminal reads message |
| `read` | `in_progress` | Terminal starts working |
| `in_progress` | `completed` | Task finished successfully |
| `in_progress` | `blocked` | Task blocked (dependencies, decisions) |
| `completed` | `archived` | After review/acceptance |
| `blocked` | `archived` | After resolution or rejection |

---

## Message Types

**5 message types:**

| Type | Direction | Purpose | Example |
|------|-----------|---------|---------|
| `task` | Root/Conductor → Terminal | Work assignment | MSG-BACKEND-042: Implement CRM module |
| `question` | Terminal → Root/Conductor | Clarification needed | MSG-ARCHITECT-015: Which API pattern? |
| `done` | Terminal → Root/Conductor | Task completion | MSG-BACKEND-042-DONE: CRM module complete |
| `blocked` | Terminal → Root/Conductor | Blocker escalation | MSG-FRONTEND-033-BLOCKED: API spec missing |
| `info` | Any → Any | Informational | MSG-CONDUCTOR-055: New skills available |

---

## Priority System

**4 priority levels:**

| Priority | SLA | Use Case | Color |
|----------|-----|----------|-------|
| `critical` | <2h | Production down, security breach | 🔴 Red |
| `high` | <24h | Feature blocking release, cross-module dependency | 🟠 Orange |
| `medium` | <3d | Standard feature work, refactoring | 🟡 Yellow |
| `low` | <7d | Nice-to-have, documentation, cleanup | ⚪ Gray |

**Priority reserve:** Critical tasks reserve 20-30% of daily token budget.

---

## MCP Tools

### 1. `tmb_create_task`

**Creates a new task message.**

**Parameters:**
```typescript
{
  from: string;              // "root", "conductor"
  to: string;                // "backend", "frontend", etc.
  title: string;             // Short task title
  description: string;       // Markdown task description
  priority: Priority;        // "critical" | "high" | "medium" | "low"
  acceptance_criteria?: string[];
  epic_id?: string;          // "EPIC-CRM-WAVE1"
  project_id?: string;       // "spaceos/crm"
  model?: Model;             // "haiku" | "sonnet" | "opus"
  context?: string;          // Additional markdown context
}
```

**Returns:**
```typescript
{
  success: boolean;
  id: string;                // "MSG-BACKEND-042"
  rendered_path: string;     // Path to .md file
}
```

**Example:**
```bash
mcp__spaceos-knowledge__tmb_create_task(
  from: "conductor",
  to: "backend",
  title: "CRM Module Wave 1 Implementation",
  description: "Implement Lead and Opportunity aggregates...",
  priority: "high",
  epic_id: "EPIC-CRM-WAVE1",
  model: "sonnet"
)
```

### 2. `tmb_read_message`

**Reads message by ID and marks as READ.**

**Parameters:**
```typescript
{
  message_id: string;        // "MSG-BACKEND-042"
}
```

**Returns:**
```typescript
{
  success: boolean;
  data: Message;             // Full message object
}
```

**Auto-updates:** Sets `status = 'read'`, `read_at = NOW()`

### 3. `tmb_complete_message`

**Completes task with DONE or BLOCKED status.**

**Parameters:**
```typescript
{
  message_id: string;
  status: "completed" | "blocked";
  summary: string;           // 1-2 sentence summary
  details?: string;          // Detailed markdown implementation notes
  files_changed?: string[];  // List of modified files
  blocked_reason?: string;   // Why blocked (for BLOCKED status)
  next_steps?: string;       // Recommended next steps
}
```

**Returns:**
```typescript
{
  success: boolean;
  rendered_path: string;     // Path to DONE/BLOCKED .md file
}
```

**Effect:**
- Appends completion report to original inbox message
- Creates DONE/BLOCKED outbox message
- Updates terminal status to `idle`

### 4. `tmb_append_note`

**Appends progress note to message.**

**Parameters:**
```typescript
{
  message_id: string;
  section: NoteSection;      // "notes" | "implementation" | "feedback" | "blockers" | "progress"
  content: string;           // Markdown note content
  author?: string;           // Author name (default: terminal name)
}
```

**Use cases:**
- **`notes`:** General observations, decisions
- **`implementation`:** Technical details, code snippets
- **`feedback`:** Review comments, suggestions
- **`blockers`:** Dependencies, issues
- **`progress`:** Incremental updates during long tasks

### 5. `tmb_get_inbox` / `tmb_get_outbox`

**Retrieves inbox/outbox messages for a terminal.**

**Parameters:**
```typescript
{
  terminal: string;
  status?: "unread" | "read" | "all";
}
```

**Returns:**
```typescript
{
  success: boolean;
  data: Message[];
  count: number;
}
```

---

## Rendered Markdown Files

### File Naming Convention

**Inbox:**
```
terminals/{terminal}/inbox/YYYY-MM-DD_NNN_{slug}.md
```
Example: `terminals/backend/inbox/2026-07-01_042_crm-module-wave1.md`

**Outbox:**
```
terminals/{terminal}/outbox/YYYY-MM-DD_NNN_{slug}-{status}.md
```
Example: `terminals/backend/outbox/2026-07-01_042_crm-module-wave1-done.md`

### Auto-Rendering

**Trigger:** Every DB update automatically re-renders `.md` files.

**Frontmatter:**
```yaml
---
id: MSG-BACKEND-042
from: conductor
to: backend
type: task
status: in_progress
priority: high
model: sonnet
epic_id: EPIC-CRM-WAVE1
created: 2026-07-01T10:30:00Z
updated: 2026-07-01T14:25:00Z
read_at: 2026-07-01T10:35:00Z
started_at: 2026-07-01T10:40:00Z
content_hash: a7f3b2...
---
```

**Body:** Markdown content from `description` field + appended notes.

---

## Integration Points

### 1. Session Starter

**Cold start task injection:**
```typescript
// sessionStarter.ts
const task = await readMessage(messageId);
const prompt = buildTaskAssignmentPrompt(task);
await injectToTmuxSession(terminal, prompt);
```

**Task assignment workflow:**
1. MCP `create_task` → DB insert → `.md` render
2. InboxWatcher detects file change
3. SessionStarter fetches task via `read_message`
4. Prompt injected into terminal session

### 2. Inbox Watcher

**File-based trigger (backward compatibility):**
```bash
# InboxWatcher monitors .md files
chokidar terminals/*/inbox/*.md
  → UNREAD file detected
  → SessionStarter.startTerminalSession(terminal, messageId)
```

### 3. Epic Router

**Task routing with epic context:**
```typescript
// epicRouter.ts
const task = await createTask({
  from: 'conductor',
  to: determineTerminal(epicId),
  epic_id: epicId,
  ...
});
```

### 4. Nightwatch Pipeline

**Automated monitoring:**
- `watch-done.sh` → Scans outbox for DONE/BLOCKED
- `watch-inbox.sh` → Checks for UNREAD tasks
- `watch-stuck.sh` → Detects in_progress >24h tasks

---

## Best Practices

### For Terminals

**1. Always use MCP tools (never edit .md files directly):**
```typescript
// ✅ CORRECT
await mcp__spaceos-knowledge__tmb_complete_message({
  message_id: "MSG-BACKEND-042",
  status: "completed",
  summary: "CRM module implemented...",
  files_changed: ["Crm/Domain/Lead.cs", "Crm/Api/LeadEndpoints.cs"]
});

// ❌ WRONG
// Manually editing outbox/2026-07-01_042_...-done.md
```

**2. Append notes during long tasks:**
```typescript
// Progress update after 2 hours
await mcp__spaceos-knowledge__tmb_append_note({
  message_id: "MSG-BACKEND-042",
  section: "progress",
  content: "## 2h Update\n- Domain layer complete (33 files)\n- Application layer 60% (9/15 handlers)\n- ETA: 2 more hours"
});
```

**3. Use acceptance criteria as checklist:**
```markdown
## Acceptance Criteria
- [x] Lead aggregate with FSM
- [x] Opportunity aggregate with FSM
- [ ] Unit tests (pending)
- [x] 19 API endpoints
```

### For Root/Conductor

**1. Set appropriate model:**
```typescript
// Simple task → haiku (10x cheaper)
model: "haiku"  // Codegen, simple feature

// Complex task → sonnet
model: "sonnet" // Domain design, cross-module

// Architecture → opus (rare!)
model: "opus"   // Major architectural decisions
```

**2. Provide context for complex tasks:**
```typescript
context: `
## Background
CRM module follows ADR-054 design...

## Related Work
- MSG-BACKEND-039: Database RLS setup (done)
- MSG-FRONTEND-088: CRM UI components (in progress)

## Integration Points
- Kernel: IUserValidationService
- Identity: JWT token validation
`
```

**3. Link tasks with epic_id:**
```typescript
epic_id: "EPIC-CRM-WAVE1"  // Enables epic-aware routing
```

---

## Monitoring & Debugging

### View Active Tasks

```bash
# SQL query
sqlite3 data/task-message-box.db "SELECT * FROM v_active_tasks;"

# MCP tool
mcp__spaceos-knowledge__tmb_get_inbox(terminal: "backend", status: "all")
```

### Check Terminal Status

```bash
sqlite3 data/task-message-box.db "SELECT * FROM terminal_status;"
```

**Output:**
```
terminal  | status    | current_task_id  | last_activity_at
----------|-----------|------------------|------------------
backend   | working   | MSG-BACKEND-042  | 2026-07-01 14:30
frontend  | idle      | NULL             | 2026-07-01 12:15
```

### Find Blocked Tasks

```bash
sqlite3 data/task-message-box.db "SELECT * FROM v_blocked_tasks;"
```

### Audit Trail

```bash
# All notes for a message
sqlite3 data/task-message-box.db \
  "SELECT section, author, created_at, substr(content, 1, 50)
   FROM message_notes
   WHERE message_id = 'MSG-BACKEND-042'
   ORDER BY created_at;"
```

---

## Migration from File-Based System

### Backward Compatibility

**Phase 1: Dual-write (2026-06-24)**
- DB write → auto-render `.md`
- File watchers still trigger session starts
- No breaking changes

**Phase 2: DB-first (future)**
- InboxWatcher reads DB directly
- `.md` files optional (human-readable only)

### Migration Script

**NOT NEEDED** — TaskMessageBox auto-renders existing messages.

---

## Performance Characteristics

### Write Performance

| Operation | Time | Notes |
|-----------|------|-------|
| Create task | ~5ms | Single INSERT + file render |
| Append note | ~3ms | INSERT into message_notes |
| Complete task | ~10ms | UPDATE + INSERT outbox + render |
| Render .md | ~2ms | Markdown template fill |

### Read Performance

| Query | Time | Notes |
|-------|------|-------|
| Get inbox | ~5ms | Indexed query on `to_terminal` + `status` |
| Get by ID | ~1ms | Primary key lookup |
| Filter by epic | ~8ms | Indexed query on `epic_id` |
| Count by status | ~2ms | Indexed aggregate |

**Database size:** ~50 KB per 100 messages (negligible).

---

## Error Handling

### Common Errors

**1. Message ID collision:**
```
Error: UNIQUE constraint failed: messages.id
```
**Cause:** Duplicate MSG-BACKEND-042
**Fix:** Auto-increment sequence ensures uniqueness (should not happen)

**2. Rendered path not found:**
```
Warning: Rendered file missing at terminals/backend/inbox/...md
```
**Cause:** File system lag or permission issue
**Fix:** Auto-rerender triggered by next DB write

**3. Invalid status transition:**
```
Error: Cannot transition from 'completed' to 'in_progress'
```
**Cause:** Attempting to reopen completed task
**Fix:** Create new task referencing original with `ref_id`

---

## Future Enhancements

**Planned:**
- [ ] Full-text search across all messages (FTS5)
- [ ] Message threading (reply-to chains)
- [ ] Attachment support (files, images)
- [ ] Email integration (message → email)
- [ ] Slack integration (message → Slack thread)

**Under consideration:**
- [ ] Multi-tenant message isolation (PostgreSQL migration)
- [ ] Message expiration/archival (auto-archive >90 days)
- [ ] GraphQL API (for Portal UI integration)

---

## Related Patterns

- [COLD_MODE_SESSION_PATTERN.md](COLD_MODE_SESSION_PATTERN.md) — Task injection workflow
- [TERMINAL_REVIEW_PATTERN.md](TERMINAL_REVIEW_PATTERN.md) — DONE message review
- [MCP_INTEGRATION_WORKFLOW.md](MCP_INTEGRATION_WORKFLOW.md) — MCP tool design
- [DISPATCH_CONTROL_PATTERN.md](DISPATCH_CONTROL_PATTERN.md) — Budget-aware task dispatch

---

**Last Updated:** 2026-07-01
**Source Code:** `spaceos-nexus/knowledge-service/src/task-message-box/`
**Database:** `spaceos-nexus/knowledge-service/data/task-message-box.db`
**Tests:** `src/__tests__/unit/taskMessageBox.test.ts` (TODO)
