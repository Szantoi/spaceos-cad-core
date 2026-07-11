# SpaceOS Knowledge Base — INDEX

**Updated:** 2026-07-04 — JoineryTech Multi-Team Patterns + 8 New Skills

---

## 🎯 7 Terminál Roller — Gyors Links

| Role | Daily Doc | Kontextus | Prioritás |
|------|-----------|-----------|-----------|
| **Backend** | [backend-daily.md](by-role/backend-daily.md) | [KERNEL_CONTEXT.md](context/KERNEL_CONTEXT.md) | ORM, RLS, testing |
| **Frontend** | [frontend-daily.md](by-role/frontend-daily.md) | [PORTAL_CONTEXT.md](context/PORTAL_CONTEXT.md) | React 18, hooks, testing |
| **Architect** | [architect-daily.md](by-role/architect-daily.md) | [ADR_CATALOGUE.md](architecture/ADR_CATALOGUE.md) | Design decisions |
| **Librarian** | [librarian-daily.md](by-role/librarian-daily.md) | [INDEX.md](INDEX.md) | Knowledge curation |
| **Designer** | [designer-daily.md](by-role/designer-daily.md) | [VISION.md](context/VISION.md) | UI/UX patterns |
| **Explorer** | [explorer-daily.md](by-role/explorer-daily.md) | [CUTTING_CONTEXT.md](context/CUTTING_CONTEXT.md) | Codebase research |
| **Conductor** | [conductor-daily.md](by-role/conductor-daily.md) | [NEXUS_CONTEXT.md](context/NEXUS_CONTEXT.md) | Coordination |

---

## 📁 Terminal Domain Memory (ADR-048 Phase 3)

**Minden terminál saját knowledge/ mappával rendelkezik:**

```
terminals/<terminal>/knowledge/
  ├── domain.memory.md      — Session-specific context (hot, 48h TTL)
  ├── patterns.memory.md    — Recurring patterns & best practices (warm, 14d TTL)
  └── decisions.memory.md   — Architectural decisions cache (cold, 365d TTL)
```

### Domain Memory Struktúra

| Terminál | Domain Focus | Pattern Focus | Decision Focus |
|----------|--------------|---------------|----------------|
| **Architect** | Active consultations, pending reviews | Cross-module patterns, API design | ADR decisions, technology choices |
| **Backend** | Active modules, pending tests | .NET patterns, security, testing | Module decisions, security trade-offs |
| **Conductor** | Coordination state, blocked terminals | Task distribution, review workflow | Planning pipeline, escalation rules |
| **Designer** | Design focus, Figma status | UI/UX patterns, accessibility | Design system, multi-tenancy theming |
| **Explorer** | Research focus, discoveries | Codebase navigation, pattern recognition | Technology evaluation, conventions |
| **Frontend** | Sprint focus, API integration | React patterns, TypeScript, testing | Framework decisions, performance |
| **Librarian** | Knowledge tasks, memory updates | Knowledge management, review patterns | Memory architecture, synthesis workflow |
| **Root** | Strategic priorities, active epics | Coordination, escalation handling | Strategic decisions, 5 Golden Rules |

### Használat

**Session elején:**
1. Olvasd a `domain.memory.md`-t → aktuális kontextus
2. Olvasd a `patterns.memory.md`-t → bevált megoldások
3. Szükség esetén nézd a `decisions.memory.md`-t → döntési háttér

**Session végén:**
- Frissítsd a `domain.memory.md`-t az aktuális státusszal
- Ha új pattern-t találtál, add hozzá a `patterns.memory.md`-hez
- Ha ADR-t írtál, szintetizáld a `decisions.memory.md`-be

---

## 🔥 HOT Tier (48h) — Aktív Sprint Patterns

**Legfrissebb döntések és megoldások:**

- [TERMINAL_CONTEXT_PERSISTENCE_FILES.md](patterns/TERMINAL_CONTEXT_PERSISTENCE_FILES.md) — **Terminál fájl struktúra hosszú futású workflow-khoz (STATUS.md, .session-state.json, CHECKPOINTS.md templates + MCP integration)** (ÚJ! 2026-07-07)
- [INFRASTRUCTURE_BLOCKER_RESOLUTION.md](debugging/INFRASTRUCTURE_BLOCKER_RESOLUTION.md) — **Infrastructure blocker diagnosis + resolution (L1→L4 escalation, NuGet timeout, tmux hang, PostgreSQL)** (ÚJ! 2026-07-04)
- [TERMINAL_COLLABORATION_NEXUS_DEVELOPMENT.md](patterns/TERMINAL_COLLABORATION_NEXUS_DEVELOPMENT.md) — **Terminálok koordinálása + Checkpoint Coordination (multi-team epic, automated triggers)** (FRISSÍTVE 2026-07-04)
- [CONDUCTOR_CONTINUOUS_PROGRESS_PATTERN.md](patterns/CONDUCTOR_CONTINUOUS_PROGRESS_PATTERN.md) — **Monitor-alapú intelligens workflow trigger (projekt-aware priorizálás)** (ÚJ! 2026-07-02)
- [GOAL_PERSISTENCE_PATTERNS.md](patterns/GOAL_PERSISTENCE_PATTERNS.md) — **Goal Drift + Goal Persistence megoldási minták (2026 kutatás: Zylos, CodeBridge, Anthropic)** (ÚJ! 2026-07-04)
- [CONDUCTOR_SESSION_KILLER_ANALYSIS.md](debugging/CONDUCTOR_SESSION_KILLER_ANALYSIS.md) — **AutonomausDev kilövi a Conductor-t 20 percenként (root cause + fix)** (ÚJ! 2026-07-02)
- [TASKMESSAGEBOX_PATTERN.md](patterns/TASKMESSAGEBOX_PATTERN.md) — **DB-backed message system (SQLite source of truth, auto-rendered .md files)** (ÚJ! 2026-07-01)
- [DISPATCH_CONTROL_PATTERN.md](patterns/DISPATCH_CONTROL_PATTERN.md) — **Budget-aware dispatch (token tracking, daily limits, threshold alerts)** (ÚJ! 2026-07-01)
- [DATAHAVEN_UI_PATTERNS.md](patterns/DATAHAVEN_UI_PATTERNS.md) — **Dashboard KPI Cards, Kanban Drag-Drop, Dark-First Bento Grid** (ÚJ! 2026-06-30)
- [UX_DESIGN_PRINCIPLES.md](patterns/UX_DESIGN_PRINCIPLES.md) — **Mobile-First, Single-Screen Focus, Dark-First**
- [SECURITY_PATTERNS.md](patterns/SECURITY_PATTERNS.md) — JWT, RBAC, SSRF
- [TERMINAL_REVIEW_PATTERN.md](patterns/TERMINAL_REVIEW_PATTERN.md) — DONE review workflow
- [COLD_MODE_SESSION_PATTERN.md](patterns/COLD_MODE_SESSION_PATTERN.md) — Task injection & routing
- [TELEGRAM_INTEGRATION.md](patterns/TELEGRAM_INTEGRATION.md) — Webhook, MultiBotManager, 8 bots
- [EVENT_SOURCING_PATTERNS.md](patterns/EVENT_SOURCING_PATTERNS.md) — Event sourcing + idempotency
- [MCP_INTEGRATION_WORKFLOW.md](patterns/MCP_INTEGRATION_WORKFLOW.md) — stdio-HTTP bridge

---

## 🌡️ WARM Tier (2w) — Runbook & Deployment

**Szükséges napi munkához:**

- [CONTRACT_FIRST_DEVELOPMENT.md](patterns/CONTRACT_FIRST_DEVELOPMENT.md) — **Week 0 OpenAPI spec workflow (4-day process, Orval/NSwag code-gen, $4k → $14k ROI)** (ÚJ! 2026-07-04)
- [REVIEW_REDUNDANCY_ARCHITECTURE.md](patterns/REVIEW_REDUNDANCY_ARCHITECTURE.md) — **Dual-reviewer pattern (Architect + Librarian, 98% success rate, zero blockages)** (ÚJ! 2026-07-04)
- [BACKEND_PATTERNS.md](engineering/BACKEND_PATTERNS.md) — **.NET patterns + FSM Aggregates (Lead, Opportunity, HR, QA, Work Order templates)** (FRISSÍTVE 2026-07-04)
- [JOINERYTECH_MIGRATION_PATTERNS.md](patterns/JOINERYTECH_MIGRATION_PATTERNS.md) — **Prototype → Production migration (localStorage → PostgreSQL, 8 worlds, 1520h)** (ÚJ! 2026-07-01)
- [DEPLOYMENT_RUNBOOK.md](deployment/DEPLOYMENT_RUNBOOK.md) — VPS deploy lépésről lépésre
- [KNOWN_GOTCHAS.md](deployment/KNOWN_GOTCHAS.md) — 10 VPS csapda
- [DOTNET_8_CLEAN_ARCHITECTURE_2026.md](architecture/DOTNET_8_CLEAN_ARCHITECTURE_2026.md) — Clean Arch 4-layer
- [DATABASE_PATTERNS.md](patterns/DATABASE_PATTERNS.md) — EF Core, RLS, Testcontainers
- [REACT_18_TYPESCRIPT_MODERNIZATION.md](patterns/REACT_18_TYPESCRIPT_MODERNIZATION.md) — React best practices

---

## ❄️ COLD Tier (3m+) — Archív Foundation

**Stabil alapok:**

- [VISION.md](context/VISION.md) — SpaceOS vízió & roadmap
- [MULTI_TENANT_RLS_ARCHITECTURE_2026.md](architecture/MULTI_TENANT_RLS_ARCHITECTURE_2026.md) — RLS deep-dive
- [GRAPH_BASED_WORKFLOW.md](architecture/GRAPH_BASED_WORKFLOW.md) — ADR-041 dependency graph
- [CODEGEN_ARCHITECTURE.md](../architecture/CODEGEN_ARCHITECTURE.md) — ADR-050 Code Generator Toolchain (4 fázis: Orval/NSwag → Scripts → CLI → MCP)

---

## 📋 Copy-Paste Snippets — `snippets/` mappa

Hamarabb dolgozni, kevesebb keresés:

- [jwt-pattern.md](snippets/jwt-pattern.md) — JWT RS256 setup
- [rls-template.md](snippets/rls-template.md) — RLS policy template
- [testcontainers-setup.md](snippets/testcontainers-setup.md) — .NET testing setup
- [efcore-migration.md](snippets/efcore-migration.md) — Migration workflow
- [react-hook.md](snippets/react-hook.md) — Custom hook template
- [zustand-store.md](snippets/zustand-store.md) — Store boilerplate

---

## 🛠️ Claude Code Skills — `~/.claude/skills/`

**8 reusable workflow skills (2026-07-04):**

### Priority 1: Core Workflows
- **contract-first-development-workflow** — Week 0 OpenAPI spec (4-day process, $4k → $14k ROI)
- **fsm-aggregate-generator** — FSM template (Lead, Opportunity, HR, QA, Work Order → 60-70% time savings)
- **mock-api-parallel-development** — MSW setup (Frontend independence, 2-4 weeks earlier delivery)

### Priority 2: Advanced Coordination
- **checkpoint-coordination-workflow** — Multi-team epic orchestration (8 weeks → 5 weeks delivery)
- **infrastructure-blocker-resolution-guide** — L1→L4 escalation (NuGet timeout, tmux hang, PostgreSQL)

### Priority 3: Templates & Patterns
- **adr-decision-template** — ADR writing (30-minute structured decision capture)
- **review-redundancy-architecture** — Dual-reviewer pattern (98% success rate implementation)
- **multi-module-delivery-roadmap-template** — Kernel → Orchestrator → Portal pipeline (37.5% faster)

**Használat:** `claude-code` session során automatikusan használhatóak. Példa: Contract-first workflow indításakor az OpenAPI spec írás 4 napra lebontva, automata code-gen setup-pal.

---

## 🔍 Keresés

- **Semantic:** `curl "http://localhost:3456/api/knowledge/search?q=RLS+tenant"`
- **By-role:** Start with `by-role/<role>-daily.md`
- **Snippets:** `docs/knowledge/snippets/`

---

## 📝 Processed Log

**Librarian PROCESSED_LOG.md:** `/opt/spaceos/terminals/librarian/PROCESSED_LOG.md`
