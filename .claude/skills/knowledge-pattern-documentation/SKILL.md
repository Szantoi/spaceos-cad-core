# Knowledge Pattern Documentation Skill

**Purpose:** Create comprehensive, production-ready knowledge pattern documentation following SpaceOS quality standards.

**Owner:** Librarian terminal
**Quality Standard:** 400-650 lines, 14+ sections, real-world examples

---

## When to Use

✅ **Use this skill when:**
- Documenting a new infrastructure component (TaskMessageBox, Dispatch Control, etc.)
- Synthesizing terminal learnings into reusable patterns
- Creating reference documentation for complex systems
- Root/Conductor requests knowledge base updates
- After implementing major features (ADR-049, ADR-050, etc.)
- Consolidating scattered knowledge from multiple sources

❌ **Do NOT use when:**
- Writing simple API documentation (use inline comments instead)
- Creating one-off task documentation (use DONE outbox instead)
- Documenting temporary workarounds (memory note is sufficient)
- Quick reference needed (use snippets/ instead)

---

## Prerequisites

- Access to source code (TypeScript, C#, SQL schema)
- Understanding of the system being documented
- Real-world usage examples (from terminal MEMORY.md or outbox)
- Related patterns identified (for cross-referencing)

---

## Procedure

### Step 1: Generate Template

Use the pattern template generator:

```bash
cd /opt/spaceos/scripts
./knowledge-pattern-template.sh "Pattern Name" "category"

# Example:
./knowledge-pattern-template.sh "TaskMessageBox" "patterns"
./knowledge-pattern-template.sh "Authentication Flow" "security"
```

### Step 2: Research Source Code

**Read key files:**
- TypeScript interfaces: `src/*/types.ts`
- Database schema: `src/*/schema.sql`
- Core logic: `src/*/[component].ts`

**Example research (TaskMessageBox):**
```bash
# Read types
cat spaceos-nexus/knowledge-service/src/task-message-box/types.ts

# Read schema
cat spaceos-nexus/knowledge-service/src/task-message-box/schema.sql

# Read implementation
cat spaceos-nexus/knowledge-service/src/task-message-box/index.ts
```

**Extract:**
- Core interfaces/types
- Database tables and views
- Workflow diagrams
- Integration points

### Step 3: Document Architecture

**Fill in template sections:**

1. **Overview:**
   - 2-3 sentence summary
   - Problem solved (before/after comparison)

2. **Architecture:**
   - Core tables/components (numbered list with descriptions)
   - Database schema (if applicable)
   - Workflow diagram (text-based flowchart)

3. **Configuration:**
   - Per-terminal settings (if applicable)
   - Default values with rationale
   - Configuration examples

**Quality check:**
- Use tables for structured data
- Include SQL/TypeScript code snippets
- Show real configuration examples

### Step 4: Integration Points

**Document how other systems use this pattern:**

```markdown
### 1. Session Starter

**Before starting session:**
```typescript
const check = canDispatch(terminal, estimatedTokens, priority);
if (!check.allowed) {
  log(`Cannot dispatch: ${check.reason}`);
  return;
}
```
```

**Include:**
- 3-5 integration points minimum
- Code examples for each
- Concrete file paths (e.g., `sessionStarter.ts:287`)

### Step 5: Best Practices

**Organize by role:**

```markdown
### For Terminals

**1. Estimate token usage accurately:**
```typescript
// Small task (< 2,000 tokens)
canDispatch('backend', 1500, 'medium');
```

### For Root/Conductor

**1. Set realistic limits:**
- Start conservative
- Adjust based on 7-day average
- Reserve 20-30% buffer
```

**Quality:**
- 2-3 practices per role
- Code examples for each
- Actionable, specific advice

### Step 6: Monitoring & Error Handling

**Monitoring section:**
- Real-time queries (SQL, API calls)
- Dashboard checks
- Health indicators

**Error handling:**
- 3-5 common errors
- Cause + fix for each
- Example error messages

**Example:**
```markdown
**1. Budget depleted:**
```
Error: Budget depleted for backend
```
**Cause:** Terminal used 10,000/10,000 tokens
**Fix:** Wait until midnight OR escalate to critical
```

### Step 7: Performance & Future Enhancements

**Performance characteristics:**
- Write performance (table with operation/time/notes)
- Read performance (table with query/time/notes)
- Database size estimates

**Future enhancements:**
- Planned (3-5 items with checkboxes)
- Under consideration (2-3 items)

### Step 8: Cross-References

**Related patterns:**
- Link 2-5 related patterns
- Brief description (1 line) for each
- Use relative paths

**Example:**
```markdown
- [TASKMESSAGEBOX_PATTERN.md](TASKMESSAGEBOX_PATTERN.md) — DB-backed messaging
- [COLD_MODE_SESSION_PATTERN.md](COLD_MODE_SESSION_PATTERN.md) — Session lifecycle
```

### Step 9: Quality Check

Run quality checker:

```bash
/opt/spaceos/scripts/pattern-quality-check.sh docs/knowledge/patterns/YOUR_PATTERN.md
```

**Fix any errors/warnings before finalizing.**

### Step 10: Update INDEX.md

Add pattern to appropriate tier:

```markdown
## 🔥 HOT Tier (48h)

- [YOUR_PATTERN.md](patterns/YOUR_PATTERN.md) — **Brief description** (ÚJ! YYYY-MM-DD)
```

### Step 11: Update PROCESSED_LOG.md

Record pattern creation:

```markdown
## YYYY-MM-DD

### Knowledge Base Updates (Completed)

**Pattern created:**
- `docs/knowledge/patterns/YOUR_PATTERN.md` (XXX lines)
  - [Brief summary of content]
```

---

## Quality Metrics

**EXCELLENT (Production-ready):**
- ✅ 400-650 lines
- ✅ All required sections present
- ✅ 6+ code examples
- ✅ 3+ tables
- ✅ 2+ related patterns linked
- ✅ Real-world examples included

**GOOD (Acceptable):**
- ✅ 200-400 lines
- ⚠️ Minor sections missing (Future Enhancements, etc.)
- ⚠️ 3-5 code examples
- ⚠️ 1-2 tables

**NEEDS WORK:**
- ❌ <200 lines
- ❌ Missing required sections (Overview, Architecture, etc.)
- ❌ <3 code examples
- ❌ No tables

---

## Success Metrics

**Pattern is successful if:**
1. Other terminals reference it (check grep usage in MEMORY.md files)
2. No questions in outbox about the documented topic (self-documenting)
3. Quality check passes with 0 errors, 0-2 warnings
4. INDEX.md link drives >3 views per month (track via logs)

---

## Real-World Example

**Task:** MSG-LIBRARIAN-008 — Document TaskMessageBox system

**Execution:**
1. Generated template: `knowledge-pattern-template.sh "TaskMessageBox" "patterns"`
2. Researched source code:
   - `task-message-box/types.ts` (158 lines, 4 interfaces)
   - `task-message-box/schema.sql` (169 lines, 4 tables + 4 views)
3. Documented architecture:
   - 4 tables, 4 views, message lifecycle diagram
   - Status transitions table (6 states)
   - 5 message types table with examples
4. Integration points:
   - Session Starter (code example)
   - Inbox Watcher (chokidar monitoring)
   - Epic Router (task routing)
   - Nightwatch Pipeline (automation)
5. Best practices:
   - Terminals: use MCP tools, append notes, acceptance criteria
   - Root/Conductor: set model, provide context, link epic_id
6. Monitoring:
   - SQL queries for active tasks, terminal status, blocked tasks
   - Audit trail query examples
7. Error handling:
   - 3 common errors with causes and fixes
8. Performance:
   - Write: 5ms create, 3ms append, 10ms complete
   - Read: 5ms inbox, 1ms by ID
9. Related patterns:
   - COLD_MODE_SESSION_PATTERN.md
   - TERMINAL_REVIEW_PATTERN.md
   - MCP_INTEGRATION_WORKFLOW.md
   - DISPATCH_CONTROL_PATTERN.md
10. Quality check: ✅ EXCELLENT (547 lines, 0 errors, 0 warnings)
11. Updated INDEX.md: Added to HOT tier
12. Result: **TASKMESSAGEBOX_PATTERN.md** (547 lines, production-ready)

**Time invested:** ~1 hour (research 20min, documentation 40min)

---

## Error Handling

**Error 1: Missing source code**
```
Error: Cannot read types.ts
```
**Fix:** Check file path, may be in different module

**Error 2: Quality check fails**
```
❌ NEEDS WORK — 2 errors, 3 warnings
```
**Fix:** Review missing sections, add code examples, expand content

**Error 3: Pattern already exists**
```
❌ File already exists: PATTERN.md
```
**Fix:** Check docs/knowledge/patterns/, may need to update existing instead

**Error 4: No real-world examples**
```
⚠️ No terminal MEMORY.md references
```
**Fix:** Search terminal memories for usage, or create example from scratch

---

## Maintenance Notes

**Frequency:** As needed (when new infrastructure added)

**Review cycle:** Quarterly (check pattern accuracy, update examples)

**Archive threshold:** If pattern unused for 6+ months → move to COLD tier or archive

---

## Related Skills

- [memory-cleanup](../memory-cleanup/SKILL.md) — Memory maintenance workflow
- [inbox-archival](../inbox-archival/SKILL.md) — Inbox cleanup automation
- [terminal-audit](../terminal-audit/SKILL.md) — Health check methodology

---

**Created:** 2026-07-01
**Last Updated:** 2026-07-01
**Success Rate:** 100% (3/3 patterns created today met EXCELLENT standard)
