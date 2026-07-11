# SpaceOS Memory Index — Projekt & Modul Struktúra

> **Hideg indítás segítség** — Melyik terminálnak melyik memória kell
>
> Rendszerezett tudás projekt/modul szintenként

**Last updated:** 2026-06-30

---

## 🎯 GYORS NAVIGÁCIÓ

| Projekt | Terminálok | Memory Fájlok |
|---------|-----------|---------------|
| **SpaceOS Core** | backend | kernel.md, identity.md |
| **Orchestrator** | backend | orchestrator.md, orch.md |
| **Joinery Module** | backend | joinery.md |
| **Cutting Module** | backend | cutting.md |
| **Portal** | frontend | fe.md, PORTAL_CONTEXT.md |
| **Procurement** | backend | procurement.md |
| **Sales** | backend | sales.md |
| **Inventory** | backend | inventory.md |
| **Datahaven/Nexus** | root, conductor | nexus.md, NEXUS_CONTEXT.md |
| **Infrastructure** | root, conductor | infra.md, INFRA_CONTEXT.md |
| **E2E Testing** | backend, frontend | e2e.md |
| **Abstractions** | backend, architect | abstractions.md |

---

## 📦 SPACEOS CORE (Kernel, Identity, Audit)

### Fájlok

**docs/memory:**
- `kernel.md` — Kernel modul memória (FSM, RLS, Audit)
- `identity.md` — Identity modul memória (Keycloak, JWT, RBAC)

**docs/knowledge/context:**
- `KERNEL_CONTEXT.md` — Kernel architektúra deep-dive

**terminals:**
- `backend/MEMORY.md` — Backend terminál session memória

### Összefoglaló

SpaceOS Kernel: Identity & Access Management (Keycloak OIDC), Audit trail (event sourcing), FSM engine (state machine core), RLS (Row-Level Security multi-tenancy). Clean Architecture + DDD alapok.

### Releváns termináloknak

- **backend** — .NET 8 Kernel fejlesztés
- **architect** — Kernel architektúra döntések

---

## 🌐 ORCHESTRATOR (BFF, API Gateway)

### Fájlok

**docs/memory:**
- `orchestrator.md` — Orchestrator memória (Tool Calling, Function Registry)
- `orch.md` — Orch modul memória (alias)

**docs/knowledge/context:**
- (jelenleg nincs ORCHESTRATOR_CONTEXT.md, de KERNEL_CONTEXT említi)

**terminals:**
- `backend/MEMORY.md` — Backend terminál session memória

### Összefoglaló

Node.js 22 BFF layer, LLM Tool Calling (Anthropic Claude), Function Registry (MCP tool pattern), API Gateway (reverse proxy Kernel/Joinery/Cutting), Session management.

### Releváns termináloknak

- **backend** — Node.js Orchestrator fejlesztés
- **architect** — BFF architektúra döntések

---

## 🪵 JOINERY MODULE (Ajtó/Szekrény Üzleti Logika)

### Fájlok

**docs/memory:**
- `joinery.md` — Joinery modul memória (Parametric Product, CAD logic)

**docs/knowledge/context:**
- `JOINERY_CONTEXT.md` — Joinery domain deep-dive

**terminals:**
- `backend/MEMORY.md` — Backend terminál session memória

### Összefoglaló

Ajtó és szekrény üzleti logika, Parametric Product interface (IParametricProduct), CAD geometry rules, Price calculation, Material BOM, Provider pattern (modularity).

### Releváns termináloknak

- **backend** — .NET 8 Joinery fejlesztés
- **architect** — Joinery domain design

---

## ✂️ CUTTING MODULE (Szabászat)

### Fájlok

**docs/memory:**
- `cutting.md` — Cutting modul memória (Nesting, Optimization)

**docs/knowledge/context:**
- `CUTTING_CONTEXT.md` — Cutting domain deep-dive

**terminals:**
- `backend/MEMORY.md` — Backend terminál session memória

### Összefoglaló

Lapszabászat modul, Nesting algorithm, Sheet optimization, Cut list generation, Material waste calculation, Q3 2026 roadmap priority (Doorstar Soft Launch).

### Releváns termináloknak

- **backend** — .NET 8 Cutting fejlesztés
- **frontend** — Cutting UI (nesting visualization)
- **architect** — Cutting algorithm design

---

## 🖥️ PORTAL (React Frontend)

### Fájlok

**docs/memory:**
- `fe.md` — Frontend modul memória (React 18, TypeScript)

**docs/knowledge/context:**
- `PORTAL_CONTEXT.md` — Portal architektúra

**terminals:**
- `frontend/MEMORY.md` — Frontend terminál session memória

### Összefoglaló

React 18 + TypeScript + Vite, Customer Portal (Doorstar), Catalog UI, Quote Request flow, Authentication (AuthOverlay), Lazy loading, Code splitting, API integration (Orchestrator).

### Releváns termináloknak

- **frontend** — React/TS fejlesztés
- **designer** — UI/UX design

---

## 📊 PROCUREMENT MODULE

### Fájlok

**docs/memory:**
- `procurement.md` — Procurement modul memória (Beszerzés, RFQ)

**terminals:**
- `backend/MEMORY.md` — Backend terminál session memória

### Összefoglaló

Beszerzési modul, RFQ (Request for Quote), Supplier management, Order tracking, CQRS handlers (18 handler: 13 query, 5 command), MediatR + Ardalis.Result pattern.

### Releváns termináloknak

- **backend** — .NET 8 Procurement fejlesztés

---

## 💰 SALES MODULE

### Fájlok

**docs/memory:**
- `sales.md` — Sales modul memória (Értékesítés, Quote)

**terminals:**
- `backend/MEMORY.md` — Backend terminál session memória

### Összefoglaló

Értékesítési modul, Quote management, Customer tracking, Price calculation integration (Joinery provider), Order lifecycle.

### Releváns termináloknak

- **backend** — .NET 8 Sales fejlesztés

---

## 📦 INVENTORY MODULE

### Fájlok

**docs/memory:**
- `inventory.md` — Inventory modul memória (Készletkezelés)

**terminals:**
- `backend/MEMORY.md` — Backend terminál session memória

### Összefoglaló

Készletkezelés, Material stock, Warehouse management, Stock alerts, Batch tracking.

### Releváns termináloknak

- **backend** — .NET 8 Inventory fejlesztés

---

## 🤖 DATAHAVEN / NEXUS (Agent Infrastructure)

### Fájlok

**docs/memory:**
- `nexus.md` — Nexus memória (Knowledge Service, MCP)

**docs/knowledge/context:**
- `NEXUS_CONTEXT.md` — Knowledge Service architektúra

**terminals:**
- `root/MEMORY.md` — Root terminál session memória
- `conductor/MEMORY.md` — Conductor terminál session memória
- `librarian/MEMORY.md` — Librarian terminál session memória

### Összefoglaló

**Datahaven Dashboard:** Terminal monitoring, Kanban board, Planning pipeline, Projects Gantt.

**Nexus Knowledge Service:** MCP server (port 3456), Tiered memory (hot/warm/cold), Task Message Box (TMB), Event Bus, Session management, Nightwatch automation.

### Releváns termináloknak

- **root** — Stratégiai döntések, agent infra
- **conductor** — Feladatkiosztás, pipeline koordináció
- **librarian** — Tudásbázis gondozó
- **architect** — MCP tool design

---

## 🏗️ INFRASTRUCTURE

### Fájlok

**docs/memory:**
- `infra.md` — Infra memória (VPS, Nginx, PostgreSQL, Keycloak)

**docs/knowledge/context:**
- `INFRA_CONTEXT.md` — Infrastructure deep-dive

**terminals:**
- `root/MEMORY.md` — Root terminál session memória
- `conductor/MEMORY.md` — Conductor terminál session memória

### Összefoglaló

VPS setup (4× vCPU, 8GB RAM), Nginx reverse proxy, PostgreSQL 16 (RLS), Keycloak 26+ (OIDC), systemd services, Logrotate, Backup scripts, Cold/Continuous mode session management.

### Releváns termináloknak

- **root** — VPS deploy, infra döntések
- **conductor** — Service monitoring

---

## 🧪 E2E TESTING

### Fájlok

**docs/memory:**
- `e2e.md` — E2E testing memória (Playwright, Testcontainers)

**terminals:**
- `backend/MEMORY.md` — Backend terminál session memória
- `frontend/MEMORY.md` — Frontend terminál session memória

### Összefoglaló

End-to-End testing, Playwright (frontend), Testcontainers (backend integration tests), Probe-and-skip pattern, 401/200 auth tests, Real PostgreSQL in tests (production parity).

### Releváns termináloknak

- **backend** — .NET integration tests
- **frontend** — Playwright E2E tests

---

## 🧩 ABSTRACTIONS (Shared Contracts)

### Fájlok

**docs/memory:**
- `abstractions.md` — Abstractions memória (Contracts, Interfaces)

**terminals:**
- `backend/MEMORY.md` — Backend terminál session memória

### Összefoglaló

Shared contracts NuGet package, Provider interfaces (IParametricProduct, IPricingProvider), Event contracts, Cross-module communication, Module boundaries (DDD bounded context).

### Releváns termináloknak

- **backend** — Abstractions library fejlesztés
- **architect** — Contract design, module boundaries

---

## 🎭 TERMINÁL MEMÓRIÁK (Session-specific)

### Fájlok

**terminals/*/MEMORY.md:**
- `architect/MEMORY.md` — Architect session memória
- `backend/MEMORY.md` — Backend session memória
- `conductor/MEMORY.md` — Conductor session memória
- `designer/MEMORY.md` — Designer session memória
- `explorer/MEMORY.md` — Explorer session memória
- `frontend/MEMORY.md` — Frontend session memória
- `librarian/MEMORY.md` — Librarian session memória
- `monitor/MEMORY.md` — Monitor session memória (legacy)
- `root/MEMORY.md` — Root session memória

### Összefoglaló

Session-specifikus tudás (aktuális sprint, DONE review history, stuck session recovery, inbox/outbox státusz). Hot memory (48h TTL).

### Releváns termináloknak

Minden terminál saját MEMORY.md-jét olvassa session indításkor.

---

## 📖 MEMORY vs KNOWLEDGE vs DOMAIN MEMORY

### docs/memory/ (Legacy)

**Mi:** Régi terminál-specifikus memória fájlok (19+ fájl)
**Formátum:** Strukturálatlan markdown
**Használat:** Átmenet alatt → fokozatosan migrálva `terminals/*/MEMORY.md`-be

### docs/knowledge/

**Mi:** Szintetizált, strukturált tudásbázis
**Formátum:** INDEX.md hierarchia, context/, patterns/, architecture/
**Használat:** Cold start session ritual (minden terminál olvassa)

### terminals/*/knowledge/ (NEW — ADR-048)

**Mi:** Terminál-specifikus domain memory (3-tier: hot/warm/cold)
**Formátum:** domain.memory.md, patterns.memory.md, decisions.memory.md
**Használat:** Session start → domain.memory.md olvasás

---

## 🔄 SESSION RITUAL — MEMORY HASZNÁLAT

### Hideg indításkor (cold start)

```bash
# 1. Tudásbázis (közös)
cat docs/knowledge/INDEX.md
cat docs/knowledge/context/<TERMINAL>_CONTEXT.md

# 2. Domain memory (új rendszer, ADR-048)
cat terminals/<terminal>/knowledge/domain.memory.md
cat terminals/<terminal>/knowledge/patterns.memory.md

# 3. Session memória (session-specifikus)
cat terminals/<terminal>/MEMORY.md

# 4. Projekt-specifikus (ha releváns)
cat docs/memory/<module>.md
```

### Session végén (warm shutdown)

```bash
# 1. Frissítsd domain.memory.md (hot, 48h TTL)
# 2. Add új pattern-t patterns.memory.md-hez (ha van)
# 3. Frissítsd terminals/<terminal>/MEMORY.md-t
```

---

## 🎯 TERMINÁL → MEMORY MAPPING

| Terminál | Releváns Projects | Memory Fájlok |
|----------|------------------|---------------|
| **root** | Nexus, Infra | nexus.md, infra.md, root/MEMORY.md |
| **conductor** | Nexus, Infra | nexus.md, conductor/MEMORY.md |
| **backend** | Kernel, Orch, Joinery, Cutting, Procurement, Sales, Inventory | kernel.md, orchestrator.md, joinery.md, cutting.md, procurement.md, sales.md, inventory.md, backend/MEMORY.md |
| **frontend** | Portal | fe.md, PORTAL_CONTEXT.md, frontend/MEMORY.md |
| **architect** | Összes | abstractions.md, architect/MEMORY.md |
| **librarian** | Nexus, Knowledge | nexus.md, librarian/MEMORY.md |
| **designer** | Portal, UX | fe.md, designer/MEMORY.md |
| **explorer** | Codebase research | explorer/MEMORY.md |

---

## 📌 KÖVETKEZŐ LÉPÉSEK (Librarian maintenance)

### Memory Cleanup (2026 Q3)

- [ ] Migráld `docs/memory/*.md` → `terminals/*/MEMORY.md`
- [ ] Archíváld elavult memory fájlokat (>90 nap inaktív)
- [ ] Konszolidáld duplikált tudást (pl. orch.md vs orchestrator.md)

### Memory Index Maintenance

- [ ] Frissítsd MEMORY_INDEX.md ha új projekt jön (pl. Assembly module)
- [ ] Add hozzá új context fájlokat knowledge/context/-ba
- [ ] Monitorozd memory file növekedést (>5MB → cleanup szükséges)

---

**Last updated:** 2026-06-30
**Maintained by:** Librarian
**Next review:** 2026-07-31 (monthly)
