# MCP Tool Adoption Tracking

> **Purpose:** Track usage, success rates, and ROI of MCP tools across terminals.
> **Owner:** Librarian
> **Update Frequency:** Weekly (every Monday)

---

## Week 1 (2026-07-07 → 2026-07-14)

### Phase 1 Infrastructure Tools

#### Terminal Status Aggregator

- **Tool:** `get_terminal_status_aggregate`
- **Calls:** 0 *(tracking starts Monday 2026-07-08)*
- **Terminals:** conductor, root, monitor
- **Success Rate:** N/A
- **Average Response Time:** Target <100ms
- **ROI Target:** 15 min/day saved (105 min/week)
- **Status:** 🟡 Deployment complete, adoption pending

#### Dependency Resolver

- **Tool:** `resolve_dependencies`
- **Calls:** 0 *(tracking starts Monday 2026-07-08)*
- **Terminals:** conductor, root
- **Success Rate:** N/A
- **Average Response Time:** Target <150ms
- **ROI Target:** 20-30 min/phase saved (75 min/week)
- **Status:** 🟡 Deployment complete, adoption pending

#### Session Context Transfer

- **Tool:** `transfer_session_context`
- **Calls:** 0 *(tracking starts Monday 2026-07-08)*
- **Terminals:** explorer, librarian
- **Success Rate:** N/A
- **Average Response Time:** Target <200ms
- **ROI Target:** 30 min/handoff saved (2-3 hours/week)
- **Status:** 🟡 Deployment complete, adoption pending

#### Component Scaffold

- **Tool:** `scaffold_component`
- **Calls:** 0 *(tracking starts Monday 2026-07-08)*
- **Terminals:** frontend
- **Success Rate:** N/A
- **Average Response Time:** Target <500ms
- **ROI Target:** 2-3 hours/week saved
- **Status:** 🟡 Deployment complete, adoption pending

#### Domain Pattern Matcher

- **Tool:** `match_domain_pattern`
- **Calls:** 0 *(tracking starts Monday 2026-07-08)*
- **Terminals:** architect, backend, frontend
- **Success Rate:** N/A
- **Average Response Time:** Target <300ms
- **ROI Target:** 2-3 hours/week saved
- **Status:** 🟡 Deployment complete, adoption pending

---

### Context Persistence Tools

#### build_session_start_context

- **Tool:** `build_session_start_context`
- **Calls:** 0 *(tracking starts Monday 2026-07-08)*
- **Terminals:** ALL (mandatory)
- **Success Rate:** N/A
- **Average Response Time:** Target <50ms
- **Adoption:** **MANDATORY** for all terminals at session start
- **Status:** 🟡 Deployment complete, adoption pending

#### get_context_saturation

- **Tool:** `get_context_saturation`
- **Calls:** 0 *(tracking starts Monday 2026-07-08)*
- **Terminals:** ALL (mandatory every 10-15 turns)
- **Success Rate:** N/A
- **Average Response Time:** Target <30ms
- **Adoption:** **MANDATORY** periodic check
- **Status:** 🟡 Deployment complete, adoption pending

#### write_session_state & write_terminal_status_md

- **Tool:** `write_session_state` + `write_terminal_status_md`
- **Calls:** 0 *(tracking starts Monday 2026-07-08)*
- **Terminals:** ALL (mandatory at session end)
- **Success Rate:** N/A
- **Average Response Time:** Target <30ms each
- **Adoption:** **MANDATORY** for session end
- **Status:** 🟡 Deployment complete, adoption pending

---

### Week 1 Summary

**Status:** All tools deployed, documentation complete, adoption starts Monday 2026-07-08.

**Next Steps:**
1. Monitor tool usage in session logs (`/opt/spaceos/logs/sessions/mcp-*.log`)
2. Track terminal DONE outbox mentions of tool usage
3. Manual survey: Ask terminals "Did you use tool X? Was it helpful?"
4. Update this file every Monday with real data

---

## Issues & Feedback (Week 1)

*No issues reported yet. Tracking starts Monday 2026-07-08.*

**Feedback Collection:**
- Session logs: `/opt/spaceos/logs/sessions/`
- Terminal outbox DONE messages: count tool mentions
- Manual survey (Week 2): Ask terminals about tool usage

---

## Adoption Checklist

### Conductor
- [ ] Use `get_terminal_status_aggregate` in morning routine
- [ ] Use `resolve_dependencies` before epic dispatch
- [ ] Use `transfer_session_context` for cross-terminal coordination
- [ ] Use Context Persistence tools (session start/end)

### Frontend
- [ ] Use `scaffold_component` for new hooks/components
- [ ] Use Context Persistence tools (session start/end)

### Architect
- [ ] Use `match_domain_pattern` for feature planning
- [ ] Use Context Persistence tools (session start/end)

### Explorer
- [ ] Use `transfer_session_context` for research handoffs
- [ ] Use Context Persistence tools (session start/end)

### Librarian
- [ ] Use Context Persistence tools (session start/end)
- [ ] Update this adoption tracking file weekly

### Backend
- [ ] Use `match_domain_pattern` (optional) for pattern reuse
- [ ] Use Context Persistence tools (session start/end)

### Root / Monitor
- [ ] Use `get_terminal_status_aggregate` for health checks
- [ ] Use `get_all_context_files_status` for system diagnostics

---

## Data Sources

1. **MCP Logs:** `/opt/spaceos/logs/sessions/mcp-*.log`
2. **Terminal DONE Outbox:** `grep -r "mcp__spaceos" terminals/*/outbox/`
3. **Knowledge Service Metrics:** `curl localhost:3456/api/metrics` (TODO: implement)
4. **Manual Survey:** Weekly check-in with terminals

---

## ROI Calculation (Week 1 Targets)

| Tool Category | Target ROI | Metric |
|--------------|-----------|--------|
| **Terminal Status Aggregator** | 105 min/week | 15 min/day × 7 days |
| **Dependency Resolver** | 75 min/week | 25 min/phase × 3 phases |
| **Context Transfer** | 120-180 min/week | 30 min/handoff × 4-6 handoffs |
| **Component Scaffold** | 120-180 min/week | 2-3 hours direct savings |
| **Pattern Matcher** | 120-180 min/week | 2-3 hours pattern search |
| **Context Persistence** | **CRITICAL** | Goal drift prevention (unmeasurable) |

**Total Target ROI (Week 1):** 540-720 min/week (9-12 hours)

**Measurement:**
- Week 2: Compare manual vs tool-based workflows
- Week 3: Validate ROI targets with actual usage data
- Week 4: Adjust targets based on reality

---

## Future Enhancements

### Phase 2 Tools (Week 3-5)

- **Skill Factory** — Automated terminal skill generation
- **Code Generator Suite** — Full CRUD generation from schemas
- **Parallel Task Dispatch** — Coordinate multi-terminal parallel work
- **Epic Progress Tracker** — Real-time epic completion visualization
- **Memory Archival Automation** — Compress + archive old session data

**Est. ROI Phase 2:** Additional 20-30 hours/week saved

---

**Last Updated:** 2026-07-07 (Week 1 initialization)
**Next Update:** 2026-07-14 (Week 2 real data)
**Maintainer:** Librarian
