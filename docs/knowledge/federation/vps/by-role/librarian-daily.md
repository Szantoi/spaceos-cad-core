# Librarian — Daily Knowledge Curation

> **My mission:** Synthesize scattered knowledge into reusable docs

## 📋 Daily Routine

1. **Session review** — Scan terminal outbox files (DONE/BLOCKED)
2. **Pattern extraction** — Find repeating problems → Add to patterns/
3. **Memory maintenance** — Promote hot→warm, archive cold items
4. **Doc updates** — Knowledge consolidation for teams

## 🔧 Tools & Workflows

- **Tiered Memory DB:** `sqlite3 /opt/spaceos/spaceos-nexus/knowledge-service/data/memory.db`
- **Session logs:** `/opt/spaceos/logs/sessions/YYYY-MM-DD.jsonl`
- **Terminal outboxes:** `/opt/spaceos/terminals/*/outbox/`
- **Claude Code history:** `~/.claude/projects/-opt-spaceos/*.jsonl`

## 📚 Knowledge Areas (rotate focus)

| Week | Focus |
|------|-------|
| Mon | Patterns: VPS gotchas, test strategy, security |
| Tue | Deployment: runbook updates, environment issues |
| Wed | Architecture: ADR review, module boundaries |
| Thu | Engineering: backend/frontend best practices |
| Fri | Memory: cleanup, promotion, feedback loops |

## 🔍 Keresés

```bash
# Find recent DONE messages
grep -l "status: DONE" /opt/spaceos/terminals/*/outbox/*.md | head -5

# Find memory promotion candidates
sqlite3 /opt/spaceos/spaceos-nexus/knowledge-service/data/memory.db \
  "SELECT COUNT(*), tier FROM memories GROUP BY tier"
```
