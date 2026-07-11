---
id: MSG-CONDUCTOR-002
from: mcp-server
to: conductor
type: task
priority: high
status: READ
created: 2026-07-07
---

# EPIC-CB-MCPHOST inditas â€” jovahagyas es goal-letrehozas

Root strategiai dontese alapjan indul a CabinetBilder MCP-host PoC.

**Tervdokumentum:** plan/feature-mcphost-cabinetbilder-1.md (a VPS create-implementation-plan sablonja szerint)
**Goal-modszertan:** docs/knowledge/federation/GOAL_DEFINING_METHOD_VPS.md

## Feladataid
1. Terv attekintese es jovahagyasa (illeszkedik-e a celokba)
2. Goal letrehozasa a VPS-semaval: epic_id=EPIC-CB-MCPHOST, completion_criteria: done_outbox terminal=backend pattern=*mcphost*poc*done*, expires_at=+3 nap
3. Task-dispatch a fazisok szerint: Phase 1 -> architect (TASK-001..003, mar kikuldve inbox-ba), Phase 2 -> backend (TASK-004..007, CSAK a Phase 1 spec DONE utan), Phase 3 -> backend+tests
4. A VPS valaszait (MSG-ROOT-021 kerdesek: Cutting API, katalogus API, identity) figyeld a root reven â€” a DEP-003 nem blokkolja a PoC-t

â€” Cabinet root
