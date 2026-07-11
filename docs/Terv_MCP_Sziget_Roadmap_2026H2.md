# Terv: MCP + Sziget-architektúra Roadmap (2026 H2)

> Készült: 2026-07-06 — Claude Code (claude-main) + Gábor közös elemzése alapján.
> Kapcsolódó tudásdokumentum: `docs/knowledge/multi_agent_mcp_integracio_rag.md`
> Session-napló: `terminals/root/MEMORY.md`

## Vízió

Minden projekt/munkaegység saját tudásközpont-szigetet (knowledge-service példányt) kap. Minden szereplő — humán (AutoCAD UI, VS Code), Claude Code, Antigravity CLI, a CabinetBilder MCP-host, és más szigetek — **ugyanazon a vendor-semleges, token-alapú MCP HTTP interfészen** dolgozik. A VPS-en fejlődő faipari ERP sémái a közös nyelv: dokumentumkezelés, tervezési sémák, RAG-tudás átjárható a csapatok között ("datahaven-first" elv).

## Fázisok és feladatok

| Fázis | Feladat | Task | Előfeltétel | Becslés |
|-------|---------|------|-------------|---------|
| 0. Higiénia | Token- és titok-rendezés, explicit tunnel, port-szétválasztás | `0024` | — | 0,5-1 nap |
| 1. Sziget-alap | Példány-identitás a knowledge-service-ben | `0023` | — | 0,5 nap |
| 2. CAD-MCP | CabinetBilder MCP-host PoC (olvasó toolok) | `0022` | 0. fázis ajánlott | 2-4 nap |
| 3. Federáció | Sziget-közi kommunikáció (VPS ↔ lokál) | `0025` | 1. fázis | 2-3 nap |
| 4. CAD-MCP írás | Író toolok + Redis lock (humán-agent ütközéskezelés) | később bontandó | 2. fázis | 3-5 nap |

## Fázis-részletek

### 0. fázis — Higiénia (0024)
Az Antigravity központi configja master tokennel fut; API-kulcsok configfájlokban ülnek; a 3456-os port forwardja implicit (VS Code-hoz kötött). Ezek rendezése minden további lépés biztonsági alapja.

### 1. fázis — Sziget-alap (0023)
Két példány (lokál dev, VPS prod) ma megkülönböztethetetlen — a 2026-07-06-i session ezen csúszott el. `instance`/`project`/`environment` mező a /health-ben és a get_service_status-ban, + `islands.json` regiszter a gépen.

### 2. fázis — CabinetBilder MCP-host (0022)
A Core CQRS use-case-ei fölé MCP host a hivatalos C# SDK-val. Első kör: 5 olvasó tool. A humán és az agent ugyanazokat a handlereket hívja — ez a közös munka technikai alapja.

### 3. fázis — Federáció (0025)
A lokális sziget conductora és a VPS sziget conductora MCP HTTP-n, federation tokenekkel cserél taskot/tudást. Szállítási réteg: explicit SSH-tunnel (rövid táv) → Tailscale/WireGuard (több gép esetén). Az ERP-sémák szinkronja itt válik automatizálhatóvá.

### 4. fázis — Író CAD-toolok
Csak a 2. fázis tapasztalatai után bontjuk taskokra: `update_metadata`, `push_metadata`, lock-protokoll, konfliktus-UX az AutoCAD oldalon.

## Nem-célok (most)

- Publikus port nyitása bármely szigetre — soha.
- A meglévő 9-terminálos flotta átszervezése — a szigetesítés a mostani szerkezetre épül.
- Teljes ERP-séma átvétel egy lépésben — inkrementálisan, a VPS-csapat sémáit követve.

## Nyitott döntések (Gábor)

1. claude-main token regisztrálása a VPS agents.yaml-ba (SSH-t igényel — user futtatja vagy engedélyezi).
2. Lokális dev knowledge-service végleges portja (javaslat: 3457).
3. Federation token séma: szigetenként 1 token vagy irányonként (A→B, B→A) külön.
