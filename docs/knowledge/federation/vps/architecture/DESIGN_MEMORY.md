# DESIGN_MEMORY.md

> Memory-system replacement for the `spaceos-design` VPS session.
> Read at every session start. Append (never silently overwrite) when a decision/learning emerges.
> Single persistence mechanism — if it isn't here, the design session does not "remember" it.

---

## 1. Locked architectural decisions (one line each)

| ADR | Decision |
|-----|----------|
| ADR-010 | Orchestrator-Mediated FlowEpic — Island Architecture: modules never know Kernel directly |
| ADR-014 | Product Graph Engine replaces all offset-table approaches; Modules.Joinery v4.2 DEPRECATED → Modules.Abstractions (parametric graph, 3 derived views: configurator / CNC / process plan) |
| ADR-018/019/020 | T-shape ecosystem · 6 actor types (Manufacturer, Supplier, Dealer, Installer, Designer, Client) · context-based UI separation |
| ADR-024 | Background Worker Privilege Pattern — dedicated PostgreSQL roles with BYPASSRLS |
| ADR-039 | Platform-wide cross-module integration: read = sync loopback HTTP; write = outbox + integration worker + idempotent receiver; Kernel-mediated FlowEpic excluded |
| 2026-04-30 topology | Orchestrator demoted to AI gateway only; modules communicate via direct HTTP loopback; API proxy on nginx |

## 2. Key principles (non-negotiable)

- **Framework-first over feature-first.** "We do not create technical debt. We implement now and immediately if we know it will be needed."
- **Data → Rules → Geometry.** LLM only passes parameters; C# layer owns all business rules; frontend never computes measurements.
- **Physical data separation is non-negotiable.** RLS FORCE on all tenant-scoped tables; cross-tenant ops require explicit dedicated roles.
- **Data sovereignty axiom.** Only bounding-box contracts (AABB + metadata) may cross server boundaries — never implementation details (materials, cut lists, pricing).
- **B2B Relativity.** Shared functionality (cutting, fabrication) lives in independent modules because professional requirements are identical whether internal or external.
- **Switch exhaustiveness over runtime defaults** (D-03, Cabinet 0.2) — compile-time exhaustiveness over default arms.
- **Strict v1→v4 review pipeline** — DB → security → backend passes before IMPLEMENTÁCIÓRA KÉSZ. No shortcuts.
- **Frozen template at submit-time** (not add-time) for part identity in manufacturing flows.
- **Part identity immutability.** `PartInstance` UUIDs at submit time, never merged even when physically identical.
- **Revenue as consequence, not goal.** Blender Foundation model; no advertising, no data sales.

## 3. Proven security patterns

- `CryptographicOperations.FixedTimeEquals` for shared secrets.
- `FORCE RLS` on all new tenant-scoped tables.
- Catch PostgreSQL `23505` for concurrent duplicate inserts.
- PII-free error responses enforced.

## 4. Working conventions

- **Hungarian prose + English identifiers** in all architecture docs.
- **Terse approval signals:** "Mehet" = proceed; single-letter (A/B/C) = option selection → proceed autonomously through pipeline stages once direction is set.
- **Pre-decisions before drafting:** ≥3 key architectural decisions resolved (one sentence each) before any v1 draft.
- **One session = one well-scoped task.** Deepen only when prereqs met.
- **Implementation context block mandatory in v4 docs:** shell discovery commands, grep patterns, Claude Code agent instruction blocks, track breakdown, DoD checklist.
- **WSJF prioritization** for all backlog decisions.
- **v1→v4 versioning:** v1 Draft → v2 DB Review → v3 Security Review → v4 Backend Review; findings carry severity-prefixed IDs (DB-P, SEC-P, BE-P).

## 5. Current state snapshot (verify against newest Codebase_Status_*.md each session)

**Deployed/running modules:** Kernel · Orchestrator · Modules.Cutting (Core→Adapters all phases) · Modules.Joinery · Inventory Core (164 tests) · Procurement Core (53 tests) · Manufacturing Phase 1 · Cabinet 0.1–0.3 · Identity (port 5008) · Sales v4 (impl status to confirm).

**Recent architecture work:** Sales v4 · Procurement v2 (IMPLEMENTÁCIÓRA KÉSZ) · Kernel `GET /api/internal/tenants/{id}` · Joinery `POST /joinery/internal/orders/from-quote` · Portal World Architecture v4-final (58 findings).

**On the horizon:** Procurement v2 impl · Modules.Abstractions / Graph Engine full impl (~46 days) · FreeTier anonymous workspace (Q3) · PartnerTier (Q4) · second paying customer (Q2–Q3 2027) · Portal screen-by-screen specs (~29 task files) · Nagyfaalföld ERP Walking Skeleton · OSS pinned-version mini-ADR.

## 6. Decision log (append-only, dated)

- **2026-06-16** — Design pipeline migrated Claude.ai → VPS (`spaceos-design` persistent interactive session, WD `~/spaceos-docs/`). Option A chosen after initially deferring. Accepted 🔴 losses: semantic Project Knowledge search (→ ripgrep), automatic memory (→ this file, manual), auto-skill-triggering (→ explicit invocation). Gained: ground-truth read access to all repos (drift eliminated). `Design_Pipeline_Strategy_v1` R4 to be overridden — see migration runbook §6.
