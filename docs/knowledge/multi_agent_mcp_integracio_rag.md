# Multi-Agent MCP Integráció: Topológia, Identitás és Tanulságok (2026-07-06)

Ez a dokumentum a SpaceOS knowledge-service többkliensű (Claude Code + Antigravity CLI) integrációja során szerzett tapasztalatokat, hibaelhárítási megoldásokat és architektúra-döntéseket rögzíti. Célja, hogy bármely terminál (humán vagy agent) visszakereshesse a topológia működését és a bejáratott mintákat.

---

## 1. Lokál vs VPS példány: a "láthatatlan" port-forward 🔀

### Kihívás
A `localhost:3456`-on elérhető knowledge-service-ről kiderült, hogy NEM a lokális `knowledge-service-0.0.01` példány, hanem a `spaceos-gabor` VPS-en futó szerver — a VS Code Remote-SSH **automatikus port-forwardján** keresztül. A lokális config-módosítások (pl. `agents.yaml` token-regisztráció) ezért látszólag "nem működtek": a hot-reload lefutott, csak épp egy másik gépen lévő fájlt kellett volna szerkeszteni.

### Diagnózis-módszer (újrafelhasználható)
1. `Get-NetTCPConnection -LocalPort 3456 -State Listen` → ha a tulajdonos folyamat `Code.exe` (NodeService utility), az forward, nem lokális szerver.
2. `Get-Process ssh` → aktív `spaceos-gabor` kapcsolatok = Remote-SSH session él.
3. Verziókülönbség-teszt: a futó szerver terminál-listája (`get_identity` hibaüzenete) `chat-root` és `reviewer` neveket tartalmazott, amelyek csak a VPS-buildben léteznek.

### Megoldás / szabály
- A forward legyen **explicit**: `~/.ssh/config` → `LocalForward 3456 localhost:3456` a `spaceos-gabor` hosthoz; így VS Code nélkül, `ssh -N spaceos-gabor`-ral is felhúzható.
- A lokális dev példány fusson **külön porton** (javasolt: 3457), hogy a két példány soha ne ütközzön.
- Minden példány `/health` válasza tartalmazzon `instance`, `project`, `environment` mezőt — enélkül két példány megkülönböztethetetlen.

---

## 2. Agent-identitás modell: token → név → jogosultság 🔑

### Működés
- `config/agents.yaml`: token → agent név mapping (30 mp-es mtime-alapú hot-reload, restart nélkül).
- `config/tool-permissions.yaml`: tool → engedélyezett terminálok (default: `all`; író/koordináló toolok root/conductor-only).
- Master token = root (mindenre jogosult). Env-felülírás: `MCP_AUTH_TOKEN`, `MCP_TOKEN_<NÉV>`.

### Bejáratott minta: minden kliensnek SAJÁT identitás
A Claude Code fő session 2026-07-06-tól saját identitással fut: **`claude-main`** — nem root, nem conductor. Indok: audit trail tisztasága + legkisebb jogosultság elve (effektíve read+report).

### Ismert adósság ⚠️
Az Antigravity központi configja (`~/.gemini/config/mcp_config.json`) a spaceos-knowledge-t a **master tokennel** köti be `mcp-remote` wrapperen át → minden `agy` session root-ként fut. Teendő: per-terminál token + natív `serverUrl`/`headers` forma (Antigravity 2.0 óta támogatott, a `terminals/*/.agents/mcp_config.json` fájlok már ezt a formát használják).

---

## 3. Kliens-bekötési receptek 📡

### Claude Code (natív HTTP, wrapper nélkül)
```json
// .mcp.json (projekt gyökér)
"spaceos-knowledge": {
  "type": "http",
  "url": "http://localhost:3456/mcp/",
  "headers": { "Authorization": "Bearer <terminál-token>" }
}
```

### Antigravity CLI + IDE (közös config, 2.0+)
```json
// ~/.gemini/config/mcp_config.json
"spaceos-knowledge": {
  "serverUrl": "http://localhost:3456/mcp/",
  "headers": { "Authorization": "Bearer <terminál-token>" }
}
```

### Kézi teszt (bármely kliens nélkül)
```powershell
$h = @{ Authorization = "Bearer <token>"; "Content-Type" = "application/json" }
Invoke-RestMethod -Uri http://localhost:3456/mcp/ -Method POST -Headers $h `
  -Body '{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"get_service_status","arguments":{}}}'
```

---

## 4. Sziget-architektúra elv 🏝️

Minden projekt/munkaegység saját knowledge-service példányt ("szigetet") kap, saját configgal, tokenekkel, ChromaDB-kollekcióval. Minden szereplő — Claude Code, Antigravity, a leendő CabinetBilder MCP-host, sőt egy másik sziget conductora is — **ugyanazon a token-alapú MCP HTTP interfészen** csatlakozik. Sziget-közi kommunikáció: a meglévő mailbox/task-routing MCP toolok, federation tokennel, SSH-tunnel vagy Tailscale/WireGuard fölött. Publikus port soha.

Előfeltételek (TODO): példány-identitás a /health-ben, port-séma vagy `islands.json` regiszter, federation tokenek.

---

## 5. CabinetBilder CLI → MCP host irány 🛠️

Nem a CLI-t alakítjuk át (az vékony: diagnose, login, sync-stub), hanem a **CabinetBilder.Core fölé** épül MCP host a hivatalos C# SDK-val (`ModelContextProtocol` NuGet, stdio + HTTP transport beépítve — nem kell kézzel HTTP API-t építeni). A meglévő CQRS requestek (`GetSmartObjectMetadataQuery`, `PushMetadataCommand`, `UpdateSmartObjectMetadataCommand`, `CheckSyncStatusQuery`) közel 1:1 MCP tool-definíciók.

**Humán–agent közös munka elve:** mindkettő ugyanazokat a use-case handlereket hívja — a humán az AutoCAD WPF UI-ból, az agent MCP toolon át; közös állapot a SmartObject metadata + lokális SQLite (`client.db`) + outbox; ütközéskezelés a Redis-es pesszimista lockkal (az agent is "CAD-felhasználó").

Első toolok (olvasó, alacsony kockázat): `list_smart_objects`, `get_smart_object_metadata`, `get_skeleton`, `get_sync_status`, `generate_bom`. Író toolok csak második körben, lock-kal.

---

## 6. Szervezeti elv: ERP-séma követés + Datahaven-first 📚

- A VPS-en futó csapat a **faipari ERP-t** fejleszti; a lokális (CabinetBilder) csapat az ottani **dokumentumkezelési és tervezési sémákat követi**, hogy a tudásmegosztás (dokumentum, terv, RAG) átjárható maradjon.
- **Datahaven-first**: minden projektnél először a tudásközpont épül ki. A keletkezett tudás fájlokban + adatbázisokban + RAG-ban tárolódik → a terminálok "az ősök vállán állva" egyre többet tudnak, és a csapat be tud segíteni egymás fejlesztésébe.
- Minden kutatási/hibaelhárítási eredmény strukturált knowledge-dokumentumba kerül (`docs/knowledge/`), hogy a semantic search megtalálja — ez a dokumentum is e szabály szerint készült.
