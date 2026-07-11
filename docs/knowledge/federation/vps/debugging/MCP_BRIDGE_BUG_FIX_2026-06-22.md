# MCP Bridge Bug & Fix (2026-06-22)

## 🔴 Bug: MCP toolok nem működtek Claude Code session-ökben

### Tünetek:
- Terminálok CLAUDE.md kérte: `mcp__spaceos-knowledge__register_working`
- Claude Code session-ök nem látták ezeket a toolokat
- Conductor session elakadt és nem tudott működni
- watchMcpHeartbeat végtelen nudge ciklus

### Root cause (3-part):

1. **Hiányzó stdio-HTTP bridge**
   - knowledge-service: HTTP-based MCP server (port 3456)
   - Claude Code: stdio-based MCP client (stdin/stdout)
   - Nem volt protocol bridge a kettő között!

2. **Hiányzó Claude Code konfiguráció**
   - `~/.claude/settings.json` nem létezett
   - Claude Code nem tudta hogy van MCP server
   - Nem tudta hogyan indítsa el

3. **Watchdog végtelen ciklus**
   - watchMcpHeartbeat.ts 10 percenként ellenőriz
   - Látja hogy session fut, de nincs "working" státusz
   - Nudge-ot küld: "Használd az MCP toolokat!"
   - De azok nem működnek → LOOP

## ✅ Fix:

### 1. stdio-HTTP bridge elkészítése
```javascript
// /opt/spaceos/spaceos-nexus/knowledge-service/bin/stdio-bridge.js
// Readline stdin → http.request → stdout
```

**Implementáció:**
- Node.js readline interface stdin-re
- Minden JSON-RPC üzenetet HTTP POST-tal továbbít
- Authorization Bearer token hozzáadása
- stdout-ra írja a választ

### 2. Claude Code konfiguráció
```json
// ~/.claude/settings.json
{
  "mcpServers": {
    "spaceos-knowledge": {
      "command": "node",
      "args": ["/opt/spaceos/spaceos-nexus/knowledge-service/bin/stdio-bridge.js"],
      "env": {
        "MCP_HOST": "localhost",
        "MCP_PORT": "3456",
        "MCP_AUTH_TOKEN": "<REDACTED lokalban 2026-07-11 - eredetileg VPS-dokumentumban szereplo pelda token, statusza tisztazando>"
      }
    }
  }
}
```

**Működés:**
- Claude Code indításkor betölti az MCP servereket
- Minden új session automatikusan eléri a 29 MCP toolt
- stdio bridge proxy-ként működik a háttérben

### 3. CLAUDE.md fájlok helyreállítása
- Mind a 7 terminál CLAUDE.md visszaállítva MCP használatra
- Session ritual frissítve:
  - `mcp__spaceos-knowledge__register_working` session startkor
  - `mcp__spaceos-knowledge__list_inbox` munkavégzéskor
  - `mcp__spaceos-knowledge__submit_done` + `register_idle` session végén
- Fallback curl opció megtartva (ha MCP nem elérhető)

### 4. watchMcpHeartbeat.ts javítása
```typescript
// Eredeti (rossz):
const nudgeMsg = `⚠️ MCP KÖTELEZŐ! Használd az MCP toolokat:
1. mcp__spaceos-knowledge__register_working ...`;

// Javított (működik):
const nudgeMsg = `⚠️ Folytasd a munkát! Használd a Claude Code built-in toolokat:
1. Bash tool + curl → Datahaven API ...`;
```

## 📊 Eredmény:

✅ Mind a 29 MCP tool elérhető:
- `mcp__spaceos-knowledge__register_working` - terminál working regisztráció
- `mcp__spaceos-knowledge__list_inbox` - inbox olvasás
- `mcp__spaceos-knowledge__send_message` - üzenet küldés
- `mcp__spaceos-knowledge__submit_done` - DONE jelentés
- `mcp__spaceos-knowledge__register_idle` - terminál idle regisztráció
- `mcp__spaceos-knowledge__search_knowledge` - knowledge base keresés
- `mcp__spaceos-knowledge__get_identity` - terminál identity
- `mcp__spaceos-knowledge__read_memory` - memória olvasás
- `mcp__spaceos-knowledge__append_memory` - memória írás
- `mcp__spaceos-knowledge__list_skills` - skill lista
- `mcp__spaceos-knowledge__get_terminal_setup` - terminál setup
- ... és még 18 további tool

✅ Conductor session sikeresen használja őket (2026-06-22 tesztelve)
✅ ÚJ session-ök automatikusan betöltik az MCP server-t
✅ Végtelen nudge ciklus megszűnt

## 🎓 Tanulság:

**HTTP-based MCP server + stdio-based client = KELL bridge!**

Ha MCP server HTTP API-t szolgál (mint a knowledge-service), Claude Code-hoz
stdio transport layer kell. A bridge egyszerű readline + http.request forwarder.

**MCP Protocol Transport Mismatch Pattern:**
```
HTTP Server (knowledge-service)
    ↕ ❌ no direct connection
Claude Code (stdio client)

Solution: stdio-HTTP bridge proxy

HTTP Server ← bridge → Claude Code
```

## 🔧 Debug tippek:

1. **MCP toolok láthatóságának ellenőrzése:**
   - Új Claude Code session indítása
   - Keresés: "mcp__spaceos-knowledge__" prefix
   - Ha nem látszik → settings.json hibás vagy bridge nem indul

2. **Bridge működésének tesztelése:**
   ```bash
   echo '{"jsonrpc":"2.0","method":"tools/list","id":1}' | \
     node bin/stdio-bridge.js
   ```
   Válasz: JSON-RPC response a 29 tool listával

3. **Knowledge-service elérhetőség:**
   ```bash
   curl -s http://localhost:3456/health
   ```
   Válasz: `{"status":"ok",...}`

4. **settings.json érvényesség:**
   ```bash
   cat ~/.claude/settings.json | python3 -m json.tool
   ```

## 📁 Kapcsolódó fájlok:
- `/opt/spaceos/spaceos-nexus/knowledge-service/bin/stdio-bridge.js` - bridge implementáció
- `/opt/spaceos/spaceos-nexus/knowledge-service/src/mcp.ts` - HTTP MCP API (29 tool)
- `~/.claude/settings.json` - Claude Code MCP client config
- `/opt/spaceos/terminals/*/CLAUDE.md` - terminál session ritual (MCP használat)
- `/opt/spaceos/spaceos-nexus/knowledge-service/src/pipeline/watchMcpHeartbeat.ts` - ébrentartó rutin

## 🔗 Git commits:
- `fa369f7` - feat(mcp): add stdio-HTTP bridge for Claude Code MCP integration
- `e999075` - fix(terminals): restore MCP tool usage in all CLAUDE.md files
- `e7b6145` - fix(terminals): remove non-existent MCP tools from all CLAUDE.md (REVERTED by e999075)

## ⚠️ Fontos megjegyzések:

1. **ÚJ session kell** - A settings.json változtatások csak új Claude Code session-ökben lesznek aktívak
2. **Bridge élettartama** - A stdio-bridge folyamat a Claude Code session-nel együtt él
3. **Auth token** - Az MCP_AUTH_TOKEN environment variable-ben van, titkosítva kell kezelni
4. **Port** - knowledge-service port 3456, session-management API port 3456/api/session/*

---

**Prepared by:** Root terminal (2026-06-22)
**Severity:** Critical (blokkoló bug az agent infrastruktúrában)
**Time to fix:** ~2 óra (debug + bridge implementáció + konfiguráció + tesztelés)
