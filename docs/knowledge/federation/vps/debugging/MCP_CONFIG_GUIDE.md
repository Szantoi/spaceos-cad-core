# Claude Code MCP Server Konfiguráció

> **KRITIKUS TUDÁS** - 2026-06-22 debug session eredménye
> Minden terminálnak (daemon) szüksége van erre az információra.

## TL;DR

**MCP szerverek konfigurációja a `~/.claude.json` fájlba kell, NEM a `~/.claude/settings.json`-ba!**

## Probléma

Az MCP szerverek (pl. spaceos-knowledge) nem töltődtek be Claude Code indításkor, annak ellenére, hogy a stdio-HTTP bridge működött.

## Konfiguráció Helyek

| Fájl | Mire való | MCP szerver ide? |
|------|-----------|------------------|
| `~/.claude/settings.json` | Általános beállítások (permissions, plugins, marketplaces) | **NEM** |
| `~/.claude.json` | Fő Claude Code konfig (state + mcpServers) | **IGEN** |
| `/project/.mcp.json` | Projekt-szintű MCP szerverek | Igen, projekt scope-ra |
| `~/.mcp.json` | User-szintű MCP szerverek | Igen, de ~/.claude.json is működik |

## Helyes Konfiguráció

### ~/.claude.json példa

```json
{
  "mcpServers": {
    "spaceos-knowledge": {
      "type": "stdio",
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

### Projekt-szintű konfig: /opt/spaceos/.mcp.json

```json
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

## Meglévő ~/.claude.json módosítása

Ha már van ~/.claude.json fájl (Claude Code state-tel), így add hozzá az mcpServers szekciót:

```bash
cat ~/.claude.json | python3 -c "
import json, sys
data = json.load(sys.stdin)
data['mcpServers'] = {
    'spaceos-knowledge': {
        'type': 'stdio',
        'command': 'node',
        'args': ['/opt/spaceos/spaceos-nexus/knowledge-service/bin/stdio-bridge.js'],
        'env': {
            'MCP_HOST': 'localhost',
            'MCP_PORT': '3456',
            'MCP_AUTH_TOKEN': '<REDACTED lokalban 2026-07-11 - eredetileg VPS-dokumentumban szereplo pelda token, statusza tisztazando>'
        }
    }
}
print(json.dumps(data, indent=2))
" > /tmp/claude.json.new && mv /tmp/claude.json.new ~/.claude.json
```

## stdio-HTTP Bridge

A `/opt/spaceos/spaceos-nexus/knowledge-service/bin/stdio-bridge.js` bridge szükséges, mert:
- Claude Code stdio-alapú MCP protokollt használ
- A knowledge-service HTTP API-t biztosít

### Bridge kritikus részlet

```javascript
// FONTOS: readline NEM szabad stdout-ra írjon
const rl = readline.createInterface({
  input: process.stdin,
  terminal: false  // NE add hozzá: output: process.stdout
});

// Response írás MINDIG console.log()-gal
console.log(responseData.trim());
```

## Debug Ellenőrzés

### 1. MCP debug log

```bash
ls -la ~/.claude/debug/
cat ~/.claude/debug/mcp-*.txt | grep -i "spaceos-knowledge"
```

Ha nincs "spaceos-knowledge" bejegyzés → konfig rossz helyen van.

### 2. Bridge közvetlen teszt

```bash
curl -X POST http://localhost:3456/mcp \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <REDACTED lokalban 2026-07-11 - eredetileg VPS-dokumentumban szereplo pelda token, statusza tisztazando>" \
  -d '{"jsonrpc":"2.0","method":"tools/list","id":1}'
```

### 3. Bridge spawn teszt

```javascript
const { spawn } = require('child_process');
const child = spawn('node', ['/opt/spaceos/spaceos-nexus/knowledge-service/bin/stdio-bridge.js'], {
  env: { ...process.env, MCP_HOST: 'localhost', MCP_PORT: '3456', MCP_AUTH_TOKEN: '...' }
});
child.stdout.on('data', (data) => console.log('STDOUT:', data.toString()));
child.stdin.write(JSON.stringify({jsonrpc: '2.0', method: 'initialize', id: 1}) + '\n');
```

## Terminálok Beállítása

Minden terminál (daemon) ami MCP-t akar használni:

1. **Egyszerű megoldás:** A terminál a `/opt/spaceos` könyvtárból induljon → `.mcp.json` automatikusan betöltődik

2. **User-scope megoldás:** A terminál user-ének `~/.claude.json` fájljába mcpServers szekció

3. **Központi megoldás:** Minden terminál ugyanazt a user-t használja (pl. `gabor`) → egyszer kell konfigurálni

## Gyakori Hibák

| Tünet | Ok | Megoldás |
|-------|-----|----------|
| MCP tool nem található | Konfig rossz fájlban | Mozgasd ~/.claude.json-ba |
| Bridge nem válaszol | readline output binding | Távolítsd el `output: process.stdout`-ot |
| "connection refused" | knowledge-service nem fut | Indítsd: `node knowledge-service/server.js` |
| Auth hiba | Rossz token | Ellenőrizd MCP_AUTH_TOKEN-t |

## Kapcsolódó Fájlok

- `/opt/spaceos/spaceos-nexus/knowledge-service/bin/stdio-bridge.js` - stdio-HTTP bridge
- `/opt/spaceos/spaceos-nexus/knowledge-service/server.js` - HTTP MCP server
- `~/.claude.json` - Claude Code fő konfig (MCP ide!)
- `~/.claude/settings.json` - Claude Code beállítások (NEM MCP!)
- `/opt/spaceos/.mcp.json` - Projekt-szintű MCP konfig
