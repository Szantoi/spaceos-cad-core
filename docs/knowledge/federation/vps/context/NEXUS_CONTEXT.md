# Nexus/Datahaven Context — Agent Infrastructure

**Project:** `datahaven` (fka `spaceos-nexus`)
**Epic:** EPIC-NEXUS-V1 + EPIC-GRAPH-WORKFLOW
**Status:** ACTIVE (target: 2026-07-15 + 2026-07-30)
**Last Updated:** 2026-06-24

---

## Aktuális állapot (HOT — utolsó 48 óra)

### Phase 6 Complete (2026-06-20 — 2026-06-23)

**Datahaven Dashboard LIVE:**
- ✅ URL: https://datahaven.joinerytech.hu
- ✅ 4 oldal: Dashboard / Kanban / Planning / Projects
- ✅ Real-time SSE monitoring (2s interval)
- ✅ 7-terminal architecture support
- ✅ Auth: `dev-token-spaceos-dashboard-2026`

**Dashboard Pages:**
| Oldal | URL | Mit mutat |
|-------|-----|-----------|
| Dashboard | `/` | Minden terminál státusz (WORKING/IDLE), inbox/outbox metrikák, active sessions |
| Kanban | `/kanban` | Dual-track board: Discovery (5-stage) + Delivery (7 swimlane) |
| Planning | `/planning` | Planning pipeline láthatóvá téve: Idea → Selected → Debate → Consensus → Queue |
| Projects | `/projects` | Gantt timeline + projekt lista (8 hónapos ablak: -2 / +6) |

**Bug fixes (2026-06-23):**
- ✅ MCP bridge stdio timeout fix (2026-06-22)
- ✅ Autonomous shutdown bug fix (2026-06-23)
- ✅ Session stuck nudge improvements

**Patterns created:**
- `MCP_INTEGRATION_WORKFLOW.md` — stdio-HTTP bridge pattern
- `AUTONOMOUS_AGENT_FRAMEWORK.md` — agent framework design
- `FRONTEND_VERIFICATION_WORKFLOW.md` — verification DONE vs implementation DONE
- `BLOCKED_MESSAGE_STRUCTURE.md` — Type A/B blocker classification

### Aktív blockerek
Nincs BLOCKED task jelenleg.

### Teszt számok
- Knowledge Service: 441 docs indexed
- ChromaDB: OPERATIONAL (port 8001)
- MCP server: OPERATIONAL (stdio bridge)
- Dashboard API: <200ms avg response time

---

## Közelmúlt (WARM — utolsó 2 hét)

### Phase 1-5 Timeline (2026-06-17 — 2026-06-23)

**Phase 1: Knowledge Service (2026-06-17 ✅)**
- ChromaDB Docker container (port 8001)
- Express API (port 3456)
- Voyage AI embeddings (voyage-3-lite, 512-dim)
- 441 documents indexed from `docs/knowledge/`
- Systemd service: `spaceos-knowledge.service`

**Phase 2: Librarian Integration (2026-06-18 ✅)**
- 5-hourly cron auto-reindex
- `discoverySearch` MCP tool (Haiku, <500ms)

**Phase 3: Planning Pipeline (2026-06-19 ✅)**
- plan-scan.sh (*/30 cron)
- plan-select.sh + plan-debate.sh (2× Sonnet A/B)
- docs/planning/{ideas, selected, consensus, queue}

**Phase 4-5: Infra Deployment (2026-06-20 ✅)**
- VPS systemd setup
- nginx reverse proxy config
- PostgreSQL 15 RLS policies
- Keycloak integration

**Phase 6: Datahaven Dashboard (2026-06-20 — 2026-06-23 ✅)**
- Full-stack React 18 + Node.js deployment
- 4-page UI with SSE real-time updates
- Dual-track Kanban visualization
- Projects Gantt chart

### Graph Workflow (ADR-041) Implementation
**Started:** 2026-06-20
**Status:** Phase 1 complete, Phase 2-4 pending

**Implemented:**
- Epic dependency graph (EPICS.yaml parser)
- GraphNode TypeScript interface
- API endpoints: `/api/graph/epics`, `/api/graph/critical-path`, `/api/graph/mermaid`

**Pending:**
- Task-level graph (TASKS.yaml)
- Workflow step graph
- Cycle detection validation

**Reference:** `docs/knowledge/architecture/GRAPH_BASED_WORKFLOW.md`

---

## Architekturális alapok (COLD — stabil döntések)

### Nexus Vízió
**Nexus** = Agent koordinációs infrastruktúra — 3 módszer integrációja:
1. **JoineryTech.McpServer** (TypeScript, MCP protokoll, RBAC, RAG, FSM)
2. **Marvin** (Python, Tasks, Threads, explicit orchestráció) — Phase 2 ⏳
3. **SpaceOS bash pipeline** (nightwatch, reviewer, planning) — ✅ működik

**Termék terminálok (Kernel, FE, Joinery) ÉRINTETLENEK** — Nexus háttérben épít.

### Rendszer komponensek

```
/opt/spaceos/spaceos-nexus/
├── knowledge-service/     # Knowledge Service (Phase 1-2 ✅)
│   ├── src/
│   │   ├── embeddings.ts  # Voyage AI / Gemini / Local fallback
│   │   ├── indexer.ts     # Rekurzív .md scanner + chunking
│   │   ├── vectorStore.ts # ChromaDB client + in-memory fallback
│   │   └── server.ts      # Express REST API (port 3456)
│   └── dist/              # TypeScript build output
├── mcp-server/            # JoineryTech.McpServer adaptáció ⏳
├── marvin/                # Marvin Python orchestrátor ⏳
└── scripts/               # Migrációs + utility szkriptek
```

### Pipeline Architektúra (bash + cron)

```
nightwatch.sh (*/2 cron)
├── watch-priority.sh → Root + Conductor MINDIG fut
├── watch-done.sh → DONE → reviewer.sh (2× Haiku) → pipeline.sh
├── watch-stuck.sh → Enter nudge
└── watch-inbox.sh → terminálok WAKE ON INBOX

plan-scan.sh (*/30 cron)
→ plan-select.sh
  → plan-debate.sh (2× Sonnet A/B)
    → docs/planning/queue/
      → Conductor inbox értesítés
```

**Dual-Track Workflow:**
- **Discovery Track:** Ideas → Selected → Debate → Consensus → Queue (planning pipeline)
- **Delivery Track:** Inbox → Active → Review → Done → Archive (mailbox pipeline)

**Reference:** `docs/agent-infrastructure/KANBAN_DUAL_TRACK_SPEC_v1.md`

### Session Management MCP API
**Port:** 3456 (knowledge-service)
**Auth:** Token-based (terminálonként egyedi token)

**Endpoints:**
```bash
POST /api/session/start       # Session indítás prompttal
POST /api/session/inject      # Prompt injection futó session-be
POST /api/session/wake        # Wake-up (start + inbox olvasás)
GET  /api/session/:terminal   # Session státusz
GET  /api/sessions/all        # Összes session
GET  /api/sessions/logs       # Audit logok
```

**Jogosultság mátrix:**
| Kezdeményező | Irányíthat |
|--------------|-----------|
| root         | MINDENKIT (8 terminál) |
| conductor    | architect, librarian, explorer, backend, frontend, designer |
| többi        | csak saját magát |

### MCP Integration Pattern
**stdio-HTTP bridge** — terminál stdio-n kommunikál, bridge http-re fordítja.

**Lifecycle:**
1. Terminal registers: `register_working(terminal, task_id?)`
2. Work phase: MCP tools használata (read_memory, send_message, stb.)
3. Terminal completes: `register_idle(terminal)`

**Graceful degradation:** Ha MCP server nem elérhető → fallback to direct curl.

**Reference:** `docs/knowledge/patterns/MCP_INTEGRATION_WORKFLOW.md`

---

## Kapcsolódó tudás (17 dokumentum)

### Architekturális Docs
| Doc | Tier | Prioritás |
|-----|------|-----------|
| [DESIGN_PIPELINE_STRATEGY.md](../architecture/DESIGN_PIPELINE_STRATEGY.md) | warm | high |
| [GRAPH_BASED_WORKFLOW.md](../architecture/GRAPH_BASED_WORKFLOW.md) | warm | high |

### Deployment & Debugging
| Doc | Tier | Prioritás |
|-----|------|-----------|
| [SESSION_REPAIR_GUIDE.md](../deployment/SESSION_REPAIR_GUIDE.md) | warm | high |
| [VOYAGE_AI_SETUP_RUNBOOK.md](../deployment/VOYAGE_AI_SETUP_RUNBOOK.md) | warm | medium |
| [KNOWLEDGE_SERVICE_ACTIVATION.md](../deployment/KNOWLEDGE_SERVICE_ACTIVATION.md) | warm | high |
| [MCP_CONFIG_GUIDE.md](../debugging/MCP_CONFIG_GUIDE.md) | warm | critical |
| [MCP_BRIDGE_BUG_FIX_2026-06-22.md](../debugging/MCP_BRIDGE_BUG_FIX_2026-06-22.md) | hot | high |
| [AUTONOMOUS_SHUTDOWN_BUG_2026-06-23.md](../debugging/AUTONOMOUS_SHUTDOWN_BUG_2026-06-23.md) | hot | high |

### Patterns
| Doc | Tier | Prioritás |
|-----|------|-----------|
| [FRONTEND_VERIFICATION_WORKFLOW.md](../patterns/FRONTEND_VERIFICATION_WORKFLOW.md) | hot | high |
| [BLOCKED_MESSAGE_STRUCTURE.md](../patterns/BLOCKED_MESSAGE_STRUCTURE.md) | hot | high |
| [MCP_INTEGRATION_WORKFLOW.md](../patterns/MCP_INTEGRATION_WORKFLOW.md) | hot | critical |
| [TELEGRAM_INTEGRATION.md](../patterns/TELEGRAM_INTEGRATION.md) | warm | medium |
| [AUTONOMOUS_AGENT_FRAMEWORK.md](../patterns/AUTONOMOUS_AGENT_FRAMEWORK.md) | hot | high |

### Security
| Doc | Tier | Prioritás |
|-----|------|-----------|
| [SECURITY_AUDIT_2026-06-20.md](../security/SECURITY_AUDIT_2026-06-20.md) | warm | critical |

### Graph & Datahaven APIs
| Doc | Tier | Prioritás |
|-----|------|-----------|
| [GRAPH_WORKFLOW_USAGE.md](../graph/GRAPH_WORKFLOW_USAGE.md) | warm | medium |
| [FILE_UPLOAD_GUIDE.md](../datahaven/FILE_UPLOAD_GUIDE.md) | warm | medium |
| [KANBAN_API_GUIDE.md](../datahaven/KANBAN_API_GUIDE.md) | warm | medium |

---

## API Endpoints (Nexus Services)

### Knowledge Service (port 3456)
| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/health` | GET | Service status |
| `/api/knowledge/search?q=...` | GET | Semantic search (query params) |
| `/api/knowledge/search` | POST | Semantic search (JSON body) |
| `/api/knowledge/index` | POST | Manual reindex trigger |
| `/api/session/start` | POST | Session indítás |
| `/api/session/inject` | POST | Prompt injection |
| `/api/session/wake` | POST | Wake-up terminal |
| `/api/session/:terminal` | GET | Session státusz |
| `/api/sessions/all` | GET | Összes session |
| `/api/graph/epics` | GET | Epic dependency graph |
| `/api/graph/critical-path/epic/EPICS` | GET | Critical path calc |
| `/api/graph/mermaid/epic/EPICS` | GET | Mermaid diagram |

### Datahaven Dashboard (port 3456)
| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/` | GET | Dashboard (SSE real-time) |
| `/kanban` | GET | Dual-track Kanban |
| `/planning` | GET | Planning pipeline viz |
| `/projects` | GET | Gantt + project list |
| `/api/dashboard` | GET | Dashboard data (JSON) |
| `/api/terminal/status` | POST | Update terminal status |
| `/api/planning/queue` | GET | Queue items |
| `/api/planning/focus` | GET | Focus area panel data |

---

## Known Gotchas

1. **MCP stdio timeout** — 30s default timeout volt túl rövid → 120s-ra emelve
2. **ChromaDB port conflict** — 8001 port kell (8000 már foglalt nginx-szel)
3. **Session stuck detection** — 2 órás idle után nudge, nem 30 perc
4. **Dashboard SSE reconnect** — Firefox/Chrome különbözőképpen kezeli → retry logic kell
5. **PostgreSQL connection pooling** — knowledge-service-nek saját pool kell (max 20 conn)

---

## Következő fázis (Q3 roadmap)

### Phase 7: Marvin Integration (⏳ Pending)
- Marvin telepítés (`pip install marvin`)
- Agent definíciók: scanner, selector, debater_a, debater_b
- plan-scan/select/debate szkriptek → Marvin Tasks

### Phase 8: Task-Level Graph (⏳ Pending)
- TASKS.yaml parser
- Task dependency graph építés
- Critical path calculation task szinten

### Phase 9: Workflow Automation (⏳ Pending)
- Auto-dispatch következő task (epic context-aware)
- Parallel task execution (depends_on vizsgálat)
- Blocker notification automation

---

## Kapcsolódó terminálok

| Terminál | Szerepkör | Interakció |
|----------|-----------|------------|
| **Root** | Stratégiai döntések, agent infra építés | Nexus projektgazda |
| **Conductor** | Napi koordináció, feladatkiosztás | Használja a planning pipeline-t |
| **Librarian** | Tudásbázis karbantartás | Auto-reindex knowledge-service |
| **Explorer** | Codebase kutatás | discoverySearch MCP tool használ |
| **Backend** | .NET + Node.js backend | knowledge-service fejlesztés |
| **Frontend** | React UI | Datahaven dashboard fejlesztés |
| **Architect** | Architekturális review | ADR-041 graph workflow konzultáció |

---

## Referenciák

- **Epic definition:** `docs/projects/EPICS.yaml` (EPIC-NEXUS-V1, EPIC-GRAPH-WORKFLOW)
- **README:** `/opt/spaceos/spaceos-nexus/README.md`
- **Dual-track spec:** `docs/agent-infrastructure/KANBAN_DUAL_TRACK_SPEC_v1.md`
- **Graph workflow:** `docs/knowledge/architecture/GRAPH_BASED_WORKFLOW.md`
- **Security audit:** `docs/knowledge/security/SECURITY_AUDIT_2026-06-20.md`
- **MCP patterns:** `docs/knowledge/patterns/MCP_INTEGRATION_WORKFLOW.md`
