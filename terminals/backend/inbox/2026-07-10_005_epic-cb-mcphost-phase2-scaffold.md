---
id: MSG-BACKEND-005
from: root
to: backend
type: task
priority: high
status: READ
created: 2026-07-10
---

# EPIC-CB-MCPHOST Phase 2 (RESZ) â€” McpHost PROJEKT-VAZ (TASK-004 + TASK-005 SCAFFOLD)

FONTOS: Ez CSAK a projekt-vaz. A root (Claude) fejezi be az 5 tool logikajat es a smoke-tesztet. NE implementald a toolok uzleti logikajat â€” hagyd stubkent. A cel: forduljon a build egy MCP stdio-hoszttal es EGY stub-toollal.

**Olvasd el eloszor:** docs/specs/mcphost-tool-contracts-v1.md (a kontraktok) es plan/feature-mcphost-cabinetbilder-1.md (REQ-001..005, CON-001..003).

## TASK-004 â€” Projekt letrehozasa (KOTELEZO, ez a fontos)
1. Uj projekt: `CabinetBilder.McpHost/CabinetBilder.McpHost.csproj`, mintaja a CabinetBilder.Cli.csproj:
   - `<OutputType>Exe</OutputType>`, `<TargetFramework>net10.0</TargetFramework>`, ImplicitUsings enable, Nullable enable, AssemblyName `cabinetbilder-mcphost`
   - ProjectReference: `..\CabinetBilder.Core\CabinetBilder.Core.csproj` ES `..\CabinetBilder.SpaceOsBridge\CabinetBilder.SpaceOsBridge.csproj`
   - CON-001: TILOS az Adapter.AutoCAD referencia!
2. NuGet csomagok:
   - `ModelContextProtocol` (hivatalos C# MCP SDK, preview â€” hasznald a legfrissebb stabil preview verziot; ha kell, nezz utana a context7/ref MCP-vel: "ModelContextProtocol C# SDK stdio server")
   - `Microsoft.Extensions.Hosting` (9.0.0)
3. Vedd be a solutionbe: a `CabinetBilder.AutoCadScripts.slnx` egy egyszeru XML â€” adj hozza egy sort a tobbi `<Project Path=... />` melle:
   `<Project Path="CabinetBilder.McpHost/CabinetBilder.McpHost.csproj" />`

## TASK-005 â€” Program.cs VAZ (best-effort; ha az SDK API-ban elakadsz, hagyd fordu+stubbal es jelezd)
- `Host.CreateApplicationBuilder(args)` mintaju host.
- MCP szerver regisztralasa STDIO transporttal a ModelContextProtocol SDK szerint (`AddMcpServer().WithStdioServerTransport()` vagy az aktualis API â€” ellenorizd a context7-tel).
- Regisztralj EGYETLEN stub toolt (pl. `ping` ami visszaad egy `{"pong": true}`-t) hogy a `tools/list` mukodjon. TOBB toolt NE.
- A DI-hez hasznald az `AddSpaceOsBridge()`-t a Cli Program.cs mintaja szerint (BaseDirectory = %AppData%\CabinetBilder).
- FIGYELEM RISK-002: az AddSpaceOsBridge berakja az OutboxWorker hosted service-t, ami a VPS-t hivna. NE oldd meg â€” csak IRD LE a DONE-ban, hogy ez nyitott; a root kezeli.

## Ellenorzes / DONE
1. `dotnet build CabinetBilder.McpHost/CabinetBilder.McpHost.csproj` â€” FORDULJON. Ha az MCP SDK API miatt nem fordul, csokkentsd a Program.cs-t a minimumra (csak a host builder + build), es a DONE-ban ird le pontosan hol akadtal el.
2. submit_done: terminal="backend", task_id="MSG-BACKEND-005", summary: mit hoztal letre, fordul-e (`dotnet build` kimenet lenyege), es mi maradt a rootra (5 tool logika, OutboxWorker RISK-002, smoke-teszt).

KAPCSOLATI SZABALY: kizarolag a lokal sziget (13457) MCP-szerveret hasznald a .agents/mcp_config.json-bol; mas portot/domaint/talalt tokent TILOS.

â€” Cabinet root (Phase 1 spec verifikalva, jovahagyva 2026-07-10)

