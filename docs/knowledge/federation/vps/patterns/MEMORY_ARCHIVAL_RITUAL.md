# MEMORY Archival Ritual — Terminal Memory Maintenance

> **Purpose:** Keep terminal MEMORY.md files lean and session-focused.
> Keep persistent knowledge in server memory (tiered) and docs/knowledge.

**Version:** 1.0
**Effective:** 2026-07-02
**Audited by:** Librarian terminal

---

## Why Archival Matters

Terminal MEMORY.md files accumulate indefinitely without cleanup:
- **Architect (before):** 92K → review history, old patterns
- **Frontend (before):** 88K → completed work, bug fixes, session logs
- **Designer (before):** 56K → session history, design specs

**Problem:** Session startup loads 20-90KB file when only 5-15% is relevant.

**Solution:** Separate **session-current** (in MEMORY.md) from **persistent knowledge** (server memory + docs/).

---

## Target File Sizes

| Terminal | Ideal Size | Current | Recommendation |
|----------|-----------|---------|-----------------|
| Architect | 15-20K | 16K ✅ | Keep patterns + last 2 weeks |
| Backend | 20-25K | 20K ✅ | Patterns + current implementation notes |
| Frontend | 20-25K | 20K ✅ | Component patterns + recent work |
| Designer | 10-15K | 3K ✅ | Design specs + current session |
| Conductor | 20-25K | 24K ✅ | Coordination patterns + active queue |
| Librarian | 30-35K | 36K ✅ | Knowledge patterns + current processing |
| Explorer | 15-20K | 24K | Remove old research notes |
| Monitor | 10-15K | 4K ✅ | Lean and efficient |

**Threshold:** If > 50KB, archival is critical.

---

## Archival Workflow (Per Terminal)

### Step 1: Identify Archivable Content

**Keep in MEMORY.md:**
- Last 2 weeks of session notes
- Current implementation context
- Recent learnings and patterns
- Quick reference (< 2KB summary)

**Archive to `/archive/2026-Q2-Q3-*.md`:**
- Sessions > 2 weeks old
- Completed work history
- Resolved bugs/issues
- Old design specs

**Promote to Server Memory:**
- Reusable patterns (semantic)
- Design principles (semantic)
- Technical decisions (episodic)
- Best practices (procedural)

**Move to docs/knowledge/:**
- Design tokens / specs → `docs/knowledge/DESIGN_TOKENS.md`
- API specifications → `docs/knowledge/architecture/API_CONTRACT_CATALOGUE.md`
- Component patterns → `docs/knowledge/patterns/COMPONENT_PATTERNS.md`
- Testing patterns → `docs/knowledge/patterns/TESTING_PATTERNS.md`

### Step 2: Create Archive File

```bash
# Find the split point (where old work starts)
grep -n "^## " MEMORY.md | head -20

# Extract active content (lines 1-N)
sed -n '1,Np' MEMORY.md > MEMORY.new

# Extract archive content (lines N+1 onwards)
sed -n 'N+1,$p' MEMORY.md > archive/2026-Q2-Q3-[type].md

# Swap
mv MEMORY.new MEMORY.md
```

### Step 3: Verify

```bash
# Check new size
wc -l MEMORY.md
ls -l MEMORY.md

# Verify archive created
ls -l archive/2026-Q2-Q3-*.md
```

### Step 4: Commit (if git-tracked)

```bash
git add terminals/<terminal>/MEMORY.md
git add terminals/<terminal>/archive/
git commit -m "refactor(memory): archive Q2-Q3 work history, reduce MEMORY.md to 20K"
```

---

## Current Archive Status (2026-07-02)

| Terminal | Old Size | New Size | Archive Size | Status |
|----------|----------|----------|--------------|--------|
| Architect | 92K | 16K | 4K | ✅ DONE |
| Frontend | 88K | 20K | 68K | ✅ DONE |
| Designer | 56K | 3K | 53K | ✅ DONE |
| Backend | ~20K | 20K | — | ✅ OK |
| Conductor | ~24K | 24K | — | ✅ OK |
| Explorer | ~24K | — | — | ⚠️ TODO (next session) |
| Librarian | ~36K | 36K | — | ✅ OK |
| Monitor | ~4K | 4K | — | ✅ OK |

---

## Session-End Ritual (For Each Terminal)

**When a session completes:**

1. **Check MEMORY.md size:**
   ```bash
   wc -l MEMORY.md
   ls -l MEMORY.md
   ```

2. **If > 30KB:** Archive old sections
   ```bash
   # Identify split point, run archival workflow above
   ```

3. **Promote patterns to server memory:**
   - Use `mcp__spaceos-knowledge__save_tiered_memory` tool
   - Type: `semantic` (patterns, principles)
   - Tier: `warm` (if recent), `cold` (if stable)

4. **Update docs/knowledge/ if patterns changed:**
   - Link to archived sessions (read-only reference)
   - Extract new patterns to pattern docs

5. **Commit:**
   ```bash
   git add MEMORY.md archive/ docs/knowledge/
   git commit -m "refactor(memory): end-of-session archival"
   ```

---

## Server Memory Promotion Examples

### Example 1: Pattern to Warm Tier

**From MEMORY.md:**
```
### UI Component Pattern (learned 2026-07-01)

When designing portal components:
1. Consistent with dark theme
2. Progressive disclosure (simple → advanced)
3. API-first (no direct file writes)
4. Mobile-first where applicable
```

**To server memory:**
```bash
mcp__spaceos-knowledge__save_tiered_memory
  terminal: "designer"
  tier: "warm"
  type: "semantic"
  source: "session"
  content: "UI Component Pattern learned..."
  salience: 0.8
```

### Example 2: Decision to Cold Tier

**From MEMORY.md (2026-06-22):**
```
Decision: Focus Area Panel placement → Planning page (Option B)
  Rationale: Contextual fit (domain affects planning pipeline)
  Trade-off: Hidden if user not on Planning page
```

**To server memory (after validation):**
```bash
mcp__spaceos-knowledge__save_tiered_memory
  terminal: "architect"
  tier: "cold"
  type: "episodic"
  source: "decision"
  content: "Focus Area Panel Placement Decision..."
  salience: 0.5
```

---

## Archival Schedule

| Frequency | Task | Owner |
|-----------|------|-------|
| **Per session** | Check MEMORY.md size | Terminal |
| **Weekly (Mondays)** | Archive sessions > 2 weeks old | Terminal (or Librarian) |
| **Monthly (1st)** | Review all archive/ directories | Librarian |
| **Quarterly** | Promote validated patterns to docs/ | Librarian |

---

## Anti-Patterns to Avoid

❌ **DO NOT:**
- Keep session transcripts in MEMORY.md (they bloat the file)
- Duplicate patterns across multiple MEMORY.md files
- Store complete API specs in MEMORY.md (link to docs/ instead)
- Accumulate "TODO" items for more than 1 sprint (resolve or move to task board)

✅ **DO:**
- Archive sessions > 2 weeks old
- Link to shared patterns in docs/knowledge/
- Keep only the current week's context in MEMORY.md
- Extract patterns → server memory after validation
- Commit archival to git for audit trail

---

## Tools & Commands Cheat Sheet

```bash
# Check MEMORY.md size
wc -l terminals/<terminal>/MEMORY.md
ls -lh terminals/<terminal>/MEMORY.md

# Find section boundaries
grep -n "^## " terminals/<terminal>/MEMORY.md | head -20

# Archive workflow
sed -n '1,Np' MEMORY.md > MEMORY.new
sed -n 'N+1,$p' MEMORY.md > archive/2026-Q2-Q3-type.md
mv MEMORY.new MEMORY.md

# Check all terminal sizes
for t in architect backend frontend designer conductor librarian explorer monitor; do
  echo "=== $t ==="
  wc -l terminals/$t/MEMORY.md 2>/dev/null
done

# Find terminals > 30KB
find terminals/*/archive -name "*.md" -type f | xargs wc -l | sort -n | tail -10
```

---

## FAQ

**Q: What if my MEMORY.md has 40KB and it's all current?**
A: Split into domain-specific files (e.g., `architect-patterns.md`, `architect-sessions.md`). Link them from main MEMORY.md.

**Q: How do I restore an archived section?**
A: Check `/terminals/<terminal>/archive/2026-Q2-Q3-*.md` and copy back if needed.

**Q: When do I promote to docs/knowledge/ vs. server memory?**
A: **Server memory** = session-specific learnings (persist in tiered memory for 14-365 days). **docs/knowledge/** = validated patterns (persist forever, Git-tracked, indexed).

**Q: Should I archive in-progress tasks?**
A: No. Only archive **completed** sessions. Move in-progress to next MEMORY.md update.

---

## References

- **Tiered Memory System:** `docs/knowledge/patterns/MEMORY_TIERING.md`
- **Knowledge Promotion:** `docs/knowledge/patterns/KNOWLEDGE_PROMOTION_WORKFLOW.md`
- **ADR-049 (Dual Session):** `docs/architecture/decisions/ADR-049-dual-session-chat-work-architecture.md`
