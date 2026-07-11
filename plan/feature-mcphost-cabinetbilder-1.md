---
goal: CabinetBilder MCP-host PoC — Core use-case-ek MCP toolként (AutoCAD-mentes)
version: 1.0
date_created: 2026-07-07
owner: Cabinet root (stratégia) / conductor (végrehajtás-koordináció)
status: 'Planned'
tags: [feature, architecture, mcp, cabinetbilder]
---

# Introduction

![Status: Planned](https://img.shields.io/badge/status-Planned-blue)

MCP-host konzolalkalmazás a CabinetBilder.Core fölé a hivatalos C# SDK-val (ModelContextProtocol NuGet), stdio transporttal. Cél: a CQRS use-case-ek (skeleton-tervezés, BOM, sync-státusz, diagnosztika) MCP toolként — agent és ember ugyanazokat a handlereket hívja. A PoC szigorúan AutoCAD-mentes.

## 1. Requirements & Constraints

- **REQ-001**: Új projekt: `CabinetBilder.McpHost` (net10.0, Exe), minta: `CabinetBilder.Cli` felépítése
- **REQ-002**: Csak `CabinetBilder.Core` + `CabinetBilder.SpaceOsBridge` referencia
- **REQ-003**: NuGet: `ModelContextProtocol` (hivatalos SDK) + `Microsoft.Extensions.Hosting`
- **REQ-004**: DI: `AddSpaceOsBridge(options)` a Cli Program.cs mintájára (BaseDirectory=%AppData%\CabinetBilder)
- **REQ-005**: Stdio transport (a lokál Claude Code / Antigravity kliensek így kötik be)
- **CON-001**: TILOS a `CabinetBilder.Adapter.AutoCAD` referencia (AutoCAD 2027 DLL-ek + ValidateAutoCadPath build-gate + Postgres RemoteDbContext)
- **CON-002**: Két Result-típus él: Ardalis.Result (MediatR handlerek) és Core.Common.Result (Skeleton domain) — a tool-outputnál mindkettőt unwrappelni kell JSON-ná
- **CON-003**: A solution .slnx formátumú; a Cli NINCS benne a solutionben — a McpHost kerüljön BE a solutionbe
- **GUD-001**: Teszt-minta: `SkeletonDomainTests.cs` (tiszta domain) és `ReadSmartObjectMetadataUseCaseTests.cs` (port-mock, MSTest + Moq)
- **PAT-001**: A VPS create-implementation-plan és project-setup skillek konvenciói (ez a doksi is azt követi)
- **REQ-006** (Gábor, 2026-07-07): A **parametrikus tervezés elve** érvényes marad a modellezésnél — a tool-kontraktok a Skeleton paraméter-vezérelt modelljére épüljenek (paraméter → Rebuild → komponensek), NEM direkt geometria-szerkesztésre
- **REQ-007** (Gábor, 2026-07-07): Az **egyszerű webes megjelenítés elve** érvényes marad — a tool-outputok (skeleton-állapot, BOM) legyenek közvetlenül webes megjelenítésre alkalmas, sima JSON-struktúrák (nincs bináris, nincs CAD-specifikus formátum a válaszban)
- **REQ-008** (Gábor, 2026-07-07): **Tervezői szándék gyűjtése**: a tervezés során kiderülő szándékokat (miért ez a paraméter, milyen megfontolás áll a döntés mögött) strukturáltan gyűjteni kell a dokumentáció minőségének javítására — a tool-készlet kapjon `record_design_intent` (vagy hasonló) eszközt / a skeleton-műveletek opcionális `intent` mezőt, ami naplózódik

## 2. Implementation Steps

### Implementation Phase 1 — Architect: spec és tool-kontraktok

- GOAL-001: Az 5 PoC-tool pontos MCP-kontraktja (név, inputSchema, output JSON-alak) + projekt-struktúra spec

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-001 | Tool-kontraktok specifikálása: `skeleton_create` (SkeletonId → default paraméterek), `skeleton_apply_parameter` (id, key, value → Result), `skeleton_compute_bom` (id → BomLine[]), `get_store_stats` (ILocalStore.GetStoreStatsAsync), `get_connection_status` (IConnectionState) | ✅ | 2026-07-10 |
| TASK-002 | Skeleton-példányok élettartam-kezelésének spec-je (in-memory registry a host processben; ConcurrentDictionary<SkeletonId, Skeleton>) | ✅ | 2026-07-10 |
| TASK-003 | Result→JSON marshalling terv (Ardalis.Result és Core.Common.Result egységes hibaformátumra) | ✅ | 2026-07-10 |

**Phase 1 output:** `docs/specs/mcphost-tool-contracts-v1.md` (541 sor). Az architect készítette (MSG-ARCHITECT-001), root verifikálta a Core-kód ellen: ApplyParameter/Rebuild/ComputeBom, Core.Common.Result mezők, ILocalStore.GetStoreStatsAsync mind valós — nincs hallucináció.

### Implementation Phase 2 — Backend: implementáció

- GOAL-002: Működő MCP-host, ami stdio-n kiszolgálja az 5 toolt

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-004 | `CabinetBilder.McpHost` projekt létrehozása + solution-be vétel (`dotnet sln ... add` a .slnx-hez) | ✅ (backend scaffold) | 2026-07-10 |
| TASK-005 | Program.cs: Host builder + AddSpaceOsBridge + MCP server (stdio) + tool-regisztráció | ✅ (backend scaffold + root: OutboxWorker RISK-002 eltávolítva, SkeletonRegistry regisztrálva) | 2026-07-10 |
| TASK-006 | Az 5 tool implementálása a TASK-001 kontraktok szerint | ✅ (root: valós Skeleton-lánc + Diagnostics DI-injektálással, stubok lecserélve) | 2026-07-10 |
| TASK-007 | Smoke-teszt: MCP initialize + tools/list + skeleton_create→compute_bom lánc stdio-n | ✅ (root: `smoke-test.py` PASS — 7 tool, Width→800 Rebuild, 5 BOM-sor, store-stats lokál SQLite-ból) | 2026-07-10 |

**Phase 2 megjegyzés:** hibrid végrehajtás — a backend agy-agent scaffoldolta a projektet (csproj + .slnx + Program.cs váz + stub toolok, `ModelContextProtocol` 1.4.1), a root (Claude) fejezte be: valós tool-logika (`Skeletons/SkeletonRegistry.cs` thread-safe in-memory registry + intent-napló, `Tools/SkeletonTools.cs`, `Tools/DiagnosticsTools.cs` DI-injektált `ILocalStore`/`IConnectionState`), RISK-002 megoldva (OutboxWorker hosted service eltávolítva → nincs kimenő VPS-hívás). Build: 0 error. Smoke: PASS.

### Implementation Phase 3 — Tests + verifikáció

- GOAL-003: MSTest lefedettség a tool-rétegre + kézi E2E Claude Code klienssel

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-008 | Unit tesztek: tool handler-ek (Skeleton-lánc tisztán; store/connection Moq-kal) a Tests projektben | ✅ (új AutoCAD-mentes `CabinetBilder.McpHost.Tests` projekt, 16 teszt PASS) | 2026-07-10 |
| TASK-009 | E2E: a host bekötése a lokál Claude Code `.mcp.json`-ba, élő tool-hívások ellenőrzése | ✅ (stdio smoke-teszt PASS + `.mcp.json`-ba bekötve `cabinetbilder-mcphost` néven) | 2026-07-10 |
| TASK-010 | DONE outbox jelentés a conductornak (goal completion: *mcphost*poc*done*) | ✅ | 2026-07-10 |

## 3. Alternatives

- **ALT-001**: A CLI kibővítése MCP-móddal — elvetve: a CLI System.CommandLine-ra épül, a host életciklusa más; tiszta külön projekt olcsóbb
- **ALT-002**: A MediatR SmartObject handlerek azonnali kiszolgálása — elvetve a PoC-ből: a handlerek az AutoCAD-assemblyben élnek (CON-001); később külön AutoCAD-mentes Application libbe emelhetők

## 4. Dependencies

- **DEP-001**: ModelContextProtocol NuGet (hivatalos C# MCP SDK)
- **DEP-002**: .NET SDK 10.0.102 (telepítve)
- **DEP-003**: VPS Cutting/katalógus API kontraktok (MSG-ROOT-021-ben kikérve) — csak a Phase 2 utáni bővítéshez, a PoC-t nem blokkolja

## 5. Files

- **FILE-001**: `CabinetBilder.McpHost/CabinetBilder.McpHost.csproj` (új)
- **FILE-002**: `CabinetBilder.McpHost/Program.cs` (új)
- **FILE-003**: `CabinetBilder.McpHost/Tools/SkeletonTools.cs`, `DiagnosticsTools.cs` (új)
- **FILE-004**: `CabinetBilder.AutoCadScripts.slnx` (módosul: +McpHost)
- **FILE-005**: `CabinetBilder.Tests/McpHost/*` (új tesztek)

## 6. Testing

- **TEST-001**: SkeletonTools: create→apply_parameter→compute_bom lánc, érvénytelen paraméter → hibaformátum
- **TEST-002**: DiagnosticsTools: ILocalStore/IConnectionState Moq-kal, Result unwrap mindkét típusra
- **TEST-003**: E2E smoke: initialize + tools/list stdio-n (subprocess)

## 7. Risks & Assumptions

- **RISK-001**: ModelContextProtocol SDK API-változékonyság (preview) — verziót pinneljük
- **RISK-002**: SpaceOsBridge OutboxWorker hosted service-ként elindul a hostban és a VPS API-t hívná — a PoC-ben kikapcsolható/no-op konfiggal kezelendő
- **ASSUMPTION-001**: A Skeleton domain in-memory működése elegendő a PoC-hez (nincs perzisztencia-igény)

## 8. Related Specifications / Further Reading

- `docs/knowledge/federation/GOAL_DEFINING_METHOD_VPS.md` (goal-módszertan)
- `.claude/skills/project-setup/SKILL.md`, `.claude/skills/create-implementation-plan/SKILL.md`
- Scoping report: a root terminál 2026-07-07-i felmérése (CQRS inventory, AutoCAD-függőségi mátrix)
- MSG-ROOT-021: integrációs kérdések a VPS-hez
