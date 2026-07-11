# MCP Integration Workflow Pattern

**Created:** 2026-06-22 (based on stdio-HTTP bridge fix + MCP tools restoration)

---

## Pattern Overview

**MCP (Model Context Protocol) Integration** = Structured communication between Claude Code terminals and the Nexus knowledge/session management service.

### Problem Solved

**Before MCP:** Bash scripts + curl → fragile, no type safety, no audit trail

**After MCP:** Native Claude Code tool calls → type-safe, audited, graceful degradation

---

## MCP Architecture

### Components

```
Claude Code Terminal (stdio)
  ↓
stdio-HTTP Bridge (bin/stdio-bridge.js)
  ↓
Nexus MCP Server (HTTP, port 3456)
  ↓
Knowledge Service (ChromaDB, SQLite memory, Datahaven API)
```

### stdio-HTTP Bridge Pattern

```javascript
// bin/stdio-bridge.js (100 lines)
// Translates Claude Code's stdio protocol → HTTP requests to Nexus

import { spawn } from 'child_process'

// Read JSON-RPC requests from stdin
process.stdin.on('data', async (data) => {
  const request = JSON.parse(data.toString())

  // Translate to HTTP
  const response = await fetch(`http://localhost:3456/mcp`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  })

  const result = await response.json()

  // Write JSON-RPC response to stdout
  process.stdout.write(JSON.stringify(result) + '\n')
})
```

**Why needed?**
- Claude Code expects stdio protocol (stdin/stdout)
- Nexus MCP server uses HTTP (easier to debug, test, monitor)
- Bridge translates between the two

---

## Session Ritual Pattern (MCP Native)

### 1. Session START

```markdown
### 1. SESSION START — register_working

**MCP tool:**
```
mcp__spaceos-knowledge__register_working
  terminal: "<terminal_name>"
  task_id: "[opcionális MSG-ID]"
```

**Fallback (ha MCP nem elérhető):**
```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"<name>","status":"working","currentTask":"<description>"}'
```
```

### 2. MUNKAVÉGZÉS

```markdown
**Inbox olvasás (MCP):**
```
mcp__spaceos-knowledge__list_inbox
  terminal: "<terminal_name>"
  status: "UNREAD"
```

**Üzenet küldés (MCP):**
```
mcp__spaceos-knowledge__send_message
  to: "target_terminal"
  type: "task"
  content: "..."
  priority: "high"
```

**Kód írás/javítás:**
- Read/Write/Edit toolok → kódbázis módosítás
- Bash tool → build, test, git
- Glob/Grep toolok → fájlkeresés
```

### 3. Session END

```markdown
**DONE jelentés (MCP):**
```
mcp__spaceos-knowledge__submit_done
  from: "<terminal_name>"
  task_id: "MSG-TERMINAL-NNN"
  summary: "..."
  files_changed: ["file1.ts", "file2.cs"]
```

**Idle regisztráció (MCP):**
```
mcp__spaceos-knowledge__register_idle
  terminal: "<terminal_name>"
```

**Fallback (ha MCP nem elérhető):**
```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"<name>","status":"idle"}'
```
```

---

## Available MCP Tools (2026-06-22)

### Terminal Status Management

| Tool | Parameters | Purpose |
|---|---|---|
| `register_working` | `terminal`, `task_id?` | Mark terminal as WORKING in Datahaven |
| `register_idle` | `terminal` | Mark terminal as IDLE in Datahaven |

### Inbox/Outbox Management

| Tool | Parameters | Purpose |
|---|---|---|
| `list_inbox` | `terminal`, `status?` | List inbox messages (UNREAD/READ/ALL) |
| `send_message` | `to`, `type`, `content`, `priority` | Send inbox message to another terminal |
| `submit_done` | `from`, `task_id`, `summary`, `files_changed` | Submit DONE outbox message |

### Knowledge Base

| Tool | Parameters | Purpose |
|---|---|---|
| `search_knowledge` | `query`, `max_results?` | Semantic search in docs/knowledge/ |

### Memory Management

| Tool | Parameters | Purpose |
|---|---|---|
| `read_memory` | `terminal` | Read terminal's memory (SQLite FTS5) |
| `append_memory` | `terminal`, `content` | Append to terminal's memory |
| `write_memory` | `terminal`, `content` | Overwrite terminal's memory (⚠️ destructive) |

---

## Fallback Pattern

### Graceful Degradation

**If MCP tools unavailable:**
1. Use `curl` + Datahaven API (terminal status)
2. Use `Read`/`Write` tools + filesystem (inbox/outbox)
3. Log warning in session (MCP unavailable)

**Example:**
```bash
# Fallback: Direct HTTP to Datahaven
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"backend","status":"working"}'
```

**Why needed?**
- stdio-HTTP bridge might crash
- Nexus server might be down
- Network issues

---

## Implementation Timeline (2026-06-22)

### Morning: MCP Tools Cleanup (e7b6145)

**Problem:** CLAUDE.md files referenced non-existent MCP tools

**Root cause audit:**
```bash
# Which MCP tools actually exist?
grep -r "mcp__" .claude/skills/
grep -r "tools/call" spaceos-nexus/knowledge-service/
```

**Solution:** Removed 44 lines of non-existent MCP tool references from 9 CLAUDE.md files

### Midday: stdio-HTTP Bridge Development (fa369f7)

**Problem:** Claude Code (stdio) ↔ Nexus (HTTP) incompatibility

**Solution:** `bin/stdio-bridge.js` (100 lines)
- Reads JSON-RPC from stdin
- Translates to HTTP POST → http://localhost:3456/mcp
- Writes JSON-RPC response to stdout

**Testing:**
```bash
echo '{"jsonrpc":"2.0","method":"tools/list","id":1}' | node bin/stdio-bridge.js
# Response: {"jsonrpc":"2.0","result":{"tools":[...]},"id":1}
```

### Afternoon: Session Ritual Restoration (e999075)

**Problem:** Terminals lost MCP session ritual after cleanup

**Solution:** Restored `SESSION RITUAL — MCP NATIVE` section in 7 CLAUDE.md files (+339/-217 lines)

**Updated terminals:**
- conductor
- architect
- backend
- frontend
- designer
- explorer
- librarian

### Evening: Documentation (39ec603)

**Problem:** No documentation for future troubleshooting

**Solution:** `docs/knowledge/debugging/MCP_BRIDGE_BUG_FIX_2026-06-22.md` (171 lines)
- Root cause analysis
- Fix timeline
- Preventive measures

---

## MCP Tool Call Example

### Native Call (stdio protocol)

```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "mcp__spaceos-knowledge__register_working",
    "arguments": {
      "terminal": "backend",
      "task_id": "MSG-BACKEND-024"
    }
  },
  "id": 1
}
```

### HTTP Translation (via bridge)

```http
POST http://localhost:3456/mcp
Content-Type: application/json

{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "mcp__spaceos-knowledge__register_working",
    "arguments": {
      "terminal": "backend",
      "task_id": "MSG-BACKEND-024"
    }
  },
  "id": 1
}
```

### Response

```json
{
  "jsonrpc": "2.0",
  "result": {
    "success": true,
    "message": "Terminal 'backend' registered as WORKING"
  },
  "id": 1
}
```

---

## Benefits of MCP Integration

### Audit Trail

**Every MCP tool call logged:**
```json
// /opt/spaceos/logs/sessions/2026-06-22.jsonl
{"timestamp":"2026-06-22T10:15:32Z","terminal":"backend","action":"register_working","task_id":"MSG-BACKEND-024"}
{"timestamp":"2026-06-22T10:47:18Z","terminal":"backend","action":"submit_done","task_id":"MSG-BACKEND-024"}
```

**Benefits:**
- Debugging (what happened when?)
- Analytics (terminal activity patterns)
- Compliance (who did what?)

### Type Safety

**MCP tools have schemas:**
```typescript
// mcp__spaceos-knowledge__register_working
{
  terminal: string  // REQUIRED
  task_id?: string  // OPTIONAL
}
```

**vs Bash curl:**
```bash
# Easy to typo, no validation
curl ... -d '{"terminal":"backedn","status":"working"}'  # Typo!
```

### Graceful Degradation

**If MCP unavailable:**
- Fallback to curl + filesystem
- Terminal continues working (not blocked)
- Session logs the fallback

---

## Common Pitfalls

### 1. MCP server not running
**Symptom:** All MCP tool calls timeout
**Fix:** Start Nexus: `cd spaceos-nexus/knowledge-service && npm start`

### 2. stdio-bridge not configured
**Symptom:** Claude Code doesn't see MCP tools
**Fix:** Update `~/.claude/settings.json`:
```json
{
  "mcpServers": {
    "spaceos-knowledge": {
      "command": "node",
      "args": ["/opt/spaceos/bin/stdio-bridge.js"]
    }
  }
}
```

### 3. Port conflict (3456)
**Symptom:** Nexus server fails to start
**Fix:** Check `lsof -i :3456`, kill conflicting process

### 4. Forgotten fallback pattern
**Symptom:** Terminal BLOCKED when MCP unavailable
**Fix:** Always include fallback `curl` commands in CLAUDE.md

---

## Preventive Measures

### 1. MCP Tools Validation

**Before referencing MCP tool in CLAUDE.md:**
```bash
# Verify tool exists
curl -X POST http://localhost:3456/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"tools/list","id":1}' | jq '.result.tools[].name'
```

### 2. Integration Test

**Test full workflow:**
```bash
# 1. Start Nexus
cd spaceos-nexus/knowledge-service && npm start

# 2. Test MCP call via bridge
echo '{"jsonrpc":"2.0","method":"tools/call","params":{"name":"mcp__spaceos-knowledge__register_working","arguments":{"terminal":"test"}},"id":1}' \
  | node bin/stdio-bridge.js

# 3. Verify in Datahaven
curl https://datahaven.joinerytech.hu/api/terminal/status/test
```

### 3. Documentation

**MCP tools must be documented:**
- `spaceos-nexus/NEXUS_USAGE.md` — Tool list + parameters
- `docs/knowledge/debugging/MCP_CONFIG_GUIDE.md` — Setup instructions
- Terminal CLAUDE.md files — Session ritual examples

---

## Impact: Conductor BLOCKED → DONE (3c60311)

**Before MCP fix:**
- Conductor session: BLOCKED (MCP tools unavailable)
- Manual intervention required (restart session)

**After MCP fix:**
- Conductor session: WORKING (MCP tools available)
- Pipeline processing: RESUMED
- 22 MCP operations logged today

---

## References

- stdio-HTTP bridge: `bin/stdio-bridge.js` (100 lines)
- MCP server: `spaceos-nexus/knowledge-service/src/mcp/` (TypeScript)
- Documentation: `docs/knowledge/debugging/MCP_BRIDGE_BUG_FIX_2026-06-22.md` (171 lines)
- Session audit log: `/opt/spaceos/logs/sessions/2026-06-22.jsonl` (22 entries)
- Explorer research: `terminals/explorer/outbox/2026-06-22_002_deep-dive-patterns-research-done.md`
