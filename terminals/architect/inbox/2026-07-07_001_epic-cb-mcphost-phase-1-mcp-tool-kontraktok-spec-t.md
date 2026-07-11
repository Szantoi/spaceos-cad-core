---
id: MSG-ARCHITECT-001
from: mcp-server
to: architect
type: task
priority: high
status: READ
created: 2026-07-07
---

# EPIC-CB-MCPHOST Phase 1 â€” MCP tool-kontraktok spec (TASK-001..003)

**Terv:** plan/feature-mcphost-cabinetbilder-1.md â€” olvasd el eloszor!

## Feladat
1. TASK-001: az 5 PoC-tool MCP-kontraktja (nev, inputSchema JSON Schema, output-alak): skeleton_create, skeleton_apply_parameter, skeleton_compute_bom, get_store_stats, get_connection_status
2. TASK-002: Skeleton-eletciklus spec (in-memory registry, ConcurrentDictionary<SkeletonId, Skeleton>)
3. TASK-003: Result->JSON marshalling terv (Ardalis.Result ES Core.Common.Result egyseges hibaformatumra â€” ket kulon tipus, vigyazz!)

**Forrasok (PONTOS utvonalak):** CabinetBilder.Core/Skeleton/Skeleton.cs (ComputeBom, ApplyParameter â€” FIGYELEM: a mappa 'Skeleton', egyes szam), CabinetBilder.Core/Sync/BomLine.cs, CabinetBilder.Core/Sync/ILocalStore.cs, CabinetBilder.SpaceOsBridge/DependencyInjection.cs, CabinetBilder.Cli/Program.cs (DI minta). Ha egy utvonal nem stimmel, KERESD meg (Skeleton.cs biztosan a Core/Skeleton/ alatt van).

## KOTELEZO tervezesi elvek (Gabor â€” REQ-006..008, korabban MSG-ARCHITECT-002)
1. **Parametrikus tervezes elve**: a tool-kontraktok a Skeleton parameter-vezerelt modelljere epuljenek (parameter -> Rebuild -> komponensek), NEM direkt geometria-szerkesztesre.
2. **Egyszeru webes megjelenites elve**: a tool-outputok (skeleton-allapot, BOM) kozvetlenul webre alkalmas, sima JSON-strukturak legyenek.
3. **Tervezoi szandek gyujtese**: a spec tartalmazzon `record_design_intent` toolt VAGY a skeleton-muveletek opcionalis `intent` mezojet â€” a tervezes soran kiderulo szandekokat strukturaltan naplozzuk.

**Output:** docs/specs/mcphost-tool-contracts-v1.md (magyarul, a fenti 3 elvvel osszhangban) + DONE outbox (pattern: *mcphost*spec*done*)

â€” conductor nevaben elokeszitve, root jovahagyta 2026-07-10

