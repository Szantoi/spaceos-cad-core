# Sziget-közi federáció — átadási dokumentum a VPS root részére

> **Küldő:** Cabinet_bilder lokális sziget, root terminál
> **Címzett:** SpaceOS VPS (spaceos-gabor) root terminál
> **Dátum:** 2026-07-06
> **Közvetítő:** Gábor (a lokális oldal nem nyúl a VPS-hez — minden VPS-oldali lépést ti végeztek)

---

## 1. Cél

A két csapat — a **lokális Cabinet_bilder flotta** (bútoripari gyártás-előkészítés) és a **VPS-en futó ERP-csapat** — elkezd egymással kommunikálni egy **egyértelműen dedikált, token-azonosított és naplózott** csatornán.

**Irányítási modell (Gábor döntése):**
1. Elsődlegesen a **két root terminál kommunikál egymással** a bridge-identitásokon keresztül.
2. A beérkező feladatkéréseket a fogadó oldal **conductora hagyja jóvá**, és ő osztja szét a terminálok között — de csak akkor, ha a kérés **belefér az aktuális célokba** (goals).
3. A bridge-identitás ezért **csak üzenetet küldhet** (`send_message`), taskot közvetlenül **nem** hozhat létre — a task-létrehozás a fogadó sziget conductorának belső döntése.

## 2. Bridge-identitások és tokenek

Mindkét sziget knowledge-service-e token→agent leképezéssel azonosít (agents.yaml). A federációhoz két dedikált híd-identitás készült:

| Identitás | Szerep | Hol kell regisztrálni |
|-----------|--------|----------------------|
| `cabinet-bridge` | a lokális csapat identitása a VPS-en | **a TI agents.yaml-otokban** (3. pont) |
| `vps-bridge` | a ti csapatotok identitása a lokális szigeten | nálunk ✅ már regisztrálva |

**Tokenek** (base64, 32 byte, openssl rand):

```
cabinet-bridge: <REDACTED — l. .mcp.json, rotálva 2026-07-11 incidens után>
vps-bridge:     <REDACTED — l. agents.yaml, rotálva 2026-07-11 incidens után>
```

A `vps-bridge` tokent őrizzétek meg: ezzel fogtok minket hívni, amint lesz VPS→lokál transport (7. pont).

## 3. Amit a VPS oldalon be kell állítani

### 3.1 Token-regisztráció (agents.yaml)

Az `agents:` szekcióba (hot-reload ~30 mp, restart nem kell):

```yaml
  # Federációs híd: a Cabinet_bilder lokális sziget identitása
  "<REDACTED — l. .mcp.json, rotálva 2026-07-11 incidens után>": "cabinet-bridge"
```

Ajánlott csoport is (mi ugyanígy csináltuk):

```yaml
groups:
  federation:
    - cabinet-bridge
```

### 3.2 Jogosultságok (tool-permissions.yaml)

A `cabinet-bridge` **csak** üzenetküldést kapjon a governance-modell szerint:

```yaml
  send_message:
    - root
    - conductor
    - cabinet-bridge   # federációs híd — üzenet a rootnak, task NEM
```

A `create_task` / `tmb_create_task` listákhoz **NE** adjátok hozzá. Az olvasó toolok (search_knowledge, get_identity, list_inbox stb.) a default "all" szerint elérhetők — ha ezt szűkíteni akarjátok, a ti döntésetek.

### 3.3 Federációs audit-log (dedikált naplózás)

Gábor követelménye: a sziget-közi forgalom **dedikáltan naplózott** legyen. Nálunk ez az MCP routerben van (`src/mcp.ts`): minden `-bridge` végű hívó minden hívása JSONL-ként a `logs/federation.jsonl`-be íródik. A minta (nálunk élesben tesztelve):

```typescript
// ─── Federation Audit Log ───────────────────────────────────────────────────
const FEDERATION_LOG_PATH = path.join(__dirname, '..', 'logs', 'federation.jsonl');

function isBridgeAgent(agent: string): boolean {
  return agent.endsWith('-bridge');
}

function logFederation(entry: Record<string, unknown>): void {
  const line = JSON.stringify({ ts: new Date().toISOString(), ...entry });
  fs.appendFile(FEDERATION_LOG_PATH, line + '\n', (err) => {
    if (err) console.error('[MCP] federation.jsonl write failed:', err.message);
  });
}
```

Hívási pontok a `router.post('/')` handlerben:
- `initialize` → `{agent, ip, method:'initialize', status:'ok'}`
- `tools/call` engedély-megtagadás → `status:'denied'`
- `tools/call` siker → `{tool, target, status:'ok', ms}`
- `tools/call` hiba → `{tool, status:'error', ms, error}`

Példa a mi élő naplónkból:

```json
{"ts":"2026-07-06T19:01:30.163Z","agent":"vps-bridge","ip":"::1","method":"initialize","status":"ok"}
{"ts":"2026-07-06T19:01:30.226Z","agent":"vps-bridge","ip":"::1","method":"tools/call","tool":"send_message","target":"root","status":"ok","ms":7}
{"ts":"2026-07-06T19:01:30.287Z","agent":"vps-bridge","ip":"::1","method":"tools/call","tool":"write_memory","status":"denied"}
```

## 4. Kommunikációs protokoll (root ↔ root)

- **Cím:** minden sziget-közi üzenet a fogadó oldal **root** terminálnak megy: `send_message { to: "root", type: "info" | "question" | "task", content: "..." }`
  - Figyelem: a paraméter **`to`** és **`content`** (nem `terminal`/`body`)!
- **Jelölés:** a content eleje jelezze az irányt és a szándékot, pl.:
  - `[CABINET→VPS] ...` illetve `[VPS→CABINET] ...`
  - feladatkérésnél: `[VPS→CABINET][TASK-REQUEST] <leírás, elvárt eredmény, határidő>`
- **Feldolgozás a fogadó oldalon:** root elolvassa → továbbítja a conductornak → a conductor a célok (goals) tükrében jóváhagyja vagy elutasítja → jóváhagyás esetén Ő hozza létre a belső taskot és osztja szét. Az elutasításról a root válaszüzenetet küld vissza a hídon.
- **Napló:** minden híd-hívás mindkét oldalon a `logs/federation.jsonl`-ben visszakereshető.

## 5. Hogyan hívjuk MI a TI szervereteket (lokál → VPS)

A regisztráció (3.1) után a lokális root a `cabinet-bridge` tokennel hívja a VPS MCP endpointját:

```bash
curl -X POST <VPS-MCP-URL>/mcp/ \
  -H "Authorization: Bearer <REDACTED — l. .mcp.json, rotálva 2026-07-11 incidens után>" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"send_message","arguments":{"to":"root","type":"info","content":"[CABINET→VPS] Kapcsolat-teszt a Cabinet_bilder szigetről."}}}'
```

Átmenetileg a `<VPS-MCP-URL>` = `http://localhost:3456` (SSH-forward). **De** a dedikált csatornához a publikus domaint kérjük (6. pont).

## 6. Amit KÉRÜNK a VPS roottól (válasz ugyanezen a közvetítőn)

1. ✅/❌ visszajelzés: `cabinet-bridge` token regisztrálva, send_message engedélyezve
2. **A publikus domain**, amin a Telegram-webhook is fut (nginx `server_name`) — a dedikált csatorna HTTPS-en, ezen a domainen menjen (`https://<domain>/mcp/`), ne a törékeny VS Code-os SSH-forwardon. Kérjük azt is jelezzétek, ha a `/mcp/` útvonal nincs kipublikálva az nginx-ben, és milyen path-on érhető el.
3. A federációs log-patch alkalmazásának visszaigazolása (3.3)
4. Az ERP-oldali **dokumentumkezelési és tervezési sémák** hivatkozásai, amiket a lokális csapatnak követnie kell (Gábor korábbi döntése szerint a tudásmegosztáshoz mi igazodunk a ti sémáitokhoz)
5. Opcionális: milyen célokat (goals) tartotok számon, amikbe a tőlünk érkező kérések illeszkedhetnek

## 7. Ismert korlát: VPS → lokál irány

A lokális gép nem érhető el kívülről, ezért **a VPS a "közös postahivatal"**: a nektek szánt üzeneteinket ti a saját root inboxotokban kapjátok; a nekünk szánt üzeneteket pedig a `cabinet-bridge` identitás inboxába küldjétek (`send_message { to: "cabinet-bridge", ... }`), amit a mi rootunk rendszeresen polloz a hídon keresztül. Ha később kétirányú, valós idejű kapcsolat kell, dedikált reverse SSH-tunnel vagy Tailscale jöhet szóba — az külön egyeztetés.

## 8. A lokális oldal állapota (referencia)

| Elem | Állapot |
|------|---------|
| Lokális knowledge-service | fut, port **13457** (a 3457-et a TI szervereteknek foglalja a VS Code forward!) |
| `vps-bridge` regisztrálva + `federation` group | ✅ |
| vps-bridge jogosultság: send_message igen, create_task NEM | ✅ tesztelve (write_memory → denied) |
| Federációs audit-log (`logs/federation.jsonl`) | ✅ élesben tesztelve |
| End-to-end üzenet vps-bridge → lokál root inbox | ✅ megérkezett |
