# Conductor — Daily Coordination & Dispatch

> **Start here:** [NEXUS_CONTEXT.md](../context/NEXUS_CONTEXT.md) — Agent infrastructure

## 📋 Typical Day Tasks

1. **Inbox processing** — Review unread messages from all terminals
2. **Task dispatch** — Create MSG-* files in terminal inboxes
3. **Pipeline monitoring** — Check planning queue & DONE items
4. **Coordination** — Coordinate between 6 developer terminals

## 🔧 Critical Workflows

- [COLD_MODE_SESSION_PATTERN.md](../patterns/COLD_MODE_SESSION_PATTERN.md) — Epic-aware task injection
- [TERMINAL_REVIEW_PATTERN.md](../patterns/TERMINAL_REVIEW_PATTERN.md) — DONE review (Arch + Librarian)
- [MCP_INTEGRATION_WORKFLOW.md](../patterns/MCP_INTEGRATION_WORKFLOW.md) — MCP API for session control

## 📊 Dashboard & Monitoring

- **Datahaven Dashboard:** https://datahaven.joinerytech.hu
- **Planning Pipeline:** Planning stage tracker (Idea → Selected → Debate → Consensus → Queue)
- **Kanban:** Discovery (planning) + Delivery (6 swimlanes)

## 🌡️ Reference

- [GRAPH_BASED_WORKFLOW.md](../architecture/GRAPH_BASED_WORKFLOW.md) — Epic dependency graph
- [DEPLOYMENT_RUNBOOK.md](../deployment/DEPLOYMENT_RUNBOOK.md) — VPS service management
- [SESSION_REPAIR_GUIDE.md](../deployment/SESSION_REPAIR_GUIDE.md) — Unblock stuck sessions

## 🔍 Quick Commands

```bash
# Check inbox status
ls /opt/spaceos/terminals/*/inbox/ | grep -c UNREAD

# Datahaven status
curl -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  https://datahaven.joinerytech.hu/api/dashboard | jq '.terminals[] | select(.status == "working")'
```
