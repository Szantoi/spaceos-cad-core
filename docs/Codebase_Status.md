# Codebase Status

## Source Of Truth Statement

- This file is the single source of truth for project status.
- Every meaningful code or architecture change must be reflected here.
- Status entries must be based on verified results (build/test/manual check).

## Current State (Verified: 2026-04-24)

- Runtime baseline: .NET 10 (`net10.0`) across all projects.
- **Architecture Evolution:** Transitioned to **CabinetBilder Architecture Vision v2**.
- **Current Milestone:** Milestone 2: Smart Objects & Persistence (COMPLETED)
- **Project Structure:**
  - `CabinetBilder.Core` (Domain, DTOs, Abstractions)
  - `CabinetBilder.SpaceOsBridge` (Persistence, Auth, Sync, Outbox)
  - `CabinetBilder.Adapter.AutoCAD` (AutoCAD Plugin, WPF UI, Overrules)
  - `CabinetBilder.Tests` (MSTest)
- **Skeleton Domain:** Task 0015 implemented. Parametric cabinets (Skeleton aggregate) now have robust AutoCAD XRecord persistence via JSON.
- **Dependency Injection:** Centralized `IServiceProvider` implemented.
- **BOM Push & GUID Integrity:** OutboxWorker processing implemented with DPAPI decryption. Drawing-level GUID collision detection and resolution active.
- **CLI Tooling:** `CabinetBilder.Cli` implemented with `diagnose` (JSON export + redaction) and `login` commands. Verified diagnostic report schema.
- **Status:** Build successful, core synchronization infrastructure finalized, domain persistence verified.

## Completed Milestones

- [x] 1-31. (Previous milestones including MediatR, Distributed Sync, Server Integration)
- [x] 32. **Project Refactor (Vision v2 Phase 0+1):** Renamed all projects to `CabinetBilder.*`, implemented three-layer separation, updated namespaces, and verified build stability.
- [x] 33. **Skeleton Domain Core (Task 0015):** Implemented parametric skeleton aggregate and AutoCAD persistence.
- [x] 34. **Milestone 2: Smart Objects & Geometry:** Implemented property palette integration (0018), 3D geometry generation (0019), and drilling patterns (0020).

## Verified Quality Gates

- Build: PASSED, 0 errors (2026-04-24, using net10.0)
- Namespace Consistency: Verified across all projects.
- Unit Tests: 99 tests PASSED (2026-04-24).
- SQLite Foundation: PASSED, migration runner (PRAGMA user_version) and V2 schema verified by tests.
- Outbox Concurrency: PASSED, Named Mutex (Global\CabinetBilder.OutboxLeader) implemented.

## Goals

### Short-Term Goals (Arch Vision v2 Roadmap)
- [x] 0010: Projekt-struktúra átnevezés (KÉSZ)
- [x] 0011: Auth Foundation (Device Code Flow + DPAPI) (KÉSZ)
- [x] 0012: Outbox and Local Storage Foundation (Vision v2 §7) (KÉSZ)
- [x] 0013: Read-only Pull (TemplateCache + MaterialCache ETag alapú, DPAPI ár-titkosítás) (KÉSZ)
- [x] 0014: BOM Push (Outbox submission logic) (KÉSZ)
- [x] 0015: Skeleton Domain Core (KÉSZ)
- [x] 0016: CLI Project + Diagnose Command (DB-12) (KÉSZ)
- [x] 0017: Skeleton Component Logic (Geometry/BOM compute) (KÉSZ)
- [x] 0018: AutoCAD UI Integration (Properties Palette binding) (KÉSZ)
- [x] 0019: Panel Geometria Generálás (Brep alapú alkatrészek) (KÉSZ)
- [x] 0020: Panel fúrási kép és megmunkálási logika (KÉSZ)
- [/] 0021: Machining Features (Grooving/Backpanel slot) (IN PROGRESS)
- [ ] 0022: Edgebanding Domain Model & BOM update (TERVEZETT)

### Long-Term Goals
- [ ] Fázis 12: MCP server expozíció (SpaceOsBridge.McpExposer)
- [ ] Fázis 13-17: Platform bővítés (BricsCAD, GstarCAD, ZWCAD, Inventor, SolidWorks)
- [ ] v3 security review: SEC-pre-01..05 finding-ek feldolgozása
- [ ] v4 backend review: SpaceOS backend endpoint-struktúra véglegesítése

## Risks And Constraints
- Architecture v2 finding DB-03 (CRITICAL): Client-side SQLite migration strategy must be implemented before any schema change — forward-only, no rollback.
- Architecture v2 finding DB-04 (HIGH): WAL mode + named mutex OutboxLeader required for multi-CAD-instance concurrency.

## Next Action
- Coordinate subtasks MSG-BACKEND-003 (domain model and validation) and MSG-BACKEND-004 (3D visualization) to complete Task 0021.
