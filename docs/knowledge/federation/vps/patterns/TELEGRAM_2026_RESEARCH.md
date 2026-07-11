# Telegram + AI Agent Integráció 2026 — Kutatás Eredménye

> **Dátum:** 2026-07-04
> **Kutató:** Root terminál
> **Cél:** Telegram chat megoldások feltérképezése, SpaceOS integráció újragondolása

---

## Összefoglaló

A 2026-os Telegram + AI agent ökoszisztéma jelentősen fejlődött. A Claude Code hivatalos Channels plugin-ja és a Telegram Bot API bot-to-bot kommunikációja megoldja azokat a problémákat, amiket korábban egyedi fejlesztéssel próbáltunk kezelni.

**Fő tanulság:** Nem kell külön chat session — a hivatalos megoldások elegánsabbak és karbantarthatóbbak.

---

## 1. Hivatalos Claude Code Channels

**Forrás:** [Claude Code Docs - Channels](https://code.claude.com/docs/en/channels)

### Mi ez?

A channel egy **MCP szerver**, ami push-olja az eventeket a futó Claude Code sessionbe. Claude reagálhat a külső eseményekre (Telegram üzenet, Discord ping, stb.) miközben dolgozik.

### Használat

```bash
# Telegram channel indítása
claude --channels plugin:telegram@claude-plugins-official
```

### Setup lépések

1. BotFather-ben `/newbot` → token másolása
2. Token beállítása környezeti változóként vagy config-ban
3. Claude Code újraindítása a `--channels` flag-gel
4. Párosítás a bot-tal (chat_id lock)

### Jellemzők

| Feature | Leírás |
|---------|--------|
| **Kétirányú** | Claude olvassa ÉS válaszol ugyanazon a csatornán |
| **Push-based** | Nem polling — event-driven |
| **Session-aware** | A futó sessionbe injektál |
| **Multi-platform** | Telegram, Discord, iMessage (research preview) |

### Limitációk

- Offline üzenetek elvesznek (nincs queue)
- Rate limit esetén üzenetek droppolódhatnak
- Reconnect nem mindig automatikus

**Forrás:** [Claude Code Telegram Plugin Guide](https://dev.to/czmilo/claude-code-telegram-plugin-complete-setup-guide-2026-3j0p)

---

## 2. Telegram Bot-to-Bot Communication (2026 május)

**Forrás:** [TechTimes - Telegram Bot API](https://www.techtimes.com/articles/316790/20260518/telegrams-bot-api-now-lets-autonomous-ai-agents-coordinate-directly-no-federal-multi-agent.htm)

### Mi ez?

A Telegram Bot API 2026 májusi frissítése lehetővé teszi, hogy **botok közvetlenül kommunikáljanak egymással** @username referenciával.

### Működés

```
@sarkany_bot → @maestro_bot: "Frontend task kész, review kell"
@maestro_bot → @arnyek_bot: "Review this PR: #123"
```

### Feltételek

- **Mutual opt-in:** Mindkét bot explicit engedélyezi
- Spam-lánc megelőzése a cél
- Manager bot pattern támogatása

### Managed Bots (2026 március)

Egy "manager bot" létrehozhat és konfigurálhat személyre szabott agent botokat két kattintással — nincs manuális BotFather setup.

**SpaceOS használat:** A Conductor lehet a manager bot, ami koordinálja a terminál botokat.

---

## 3. A2A Protocol (Google-backed)

**Forrás:** [kagent + A2A Kubernetes](https://maniak.io/articles/2026-03-13-telegram-bot-kagent-a2a-kubernetes/)

### Architektúra

```
Telegram Bot (transport layer)
       │
       ▼
A2A Endpoint (JSON-RPC 2.0 over HTTP)
       │
       ▼
Agent Controller (kagent)
       │
       ▼
LLM + Tools
```

### Kulcs koncepciók

| Koncepció | Leírás |
|-----------|--------|
| **contextId** | Session tracking azonosító |
| **Transport layer** | A bot CSAK transport, nem logika |
| **Agent interop** | Bármely A2A-kompatibilis kliens kommunikálhat |

**SpaceOS relevancia:** Ha több external agent-tel akarunk integrálni, A2A lehet a protokoll.

---

## 4. Gateway Pattern (ZeroClaw, OpenClaw)

**Forrás:** [ZeroClaw Blog](https://zeroclaws.io/blog/telegram-bot-api-2026-ai-agent-developers-guide/)

### Minimális konfig

```yaml
channel: telegram
token: ${TELEGRAM_TOKEN}
```

A gateway automatikusan kezeli:
- Message polling/webhook
- Response routing
- Reconnection

### Anti-pattern

> "Blocking operations are problematic because Telegram has timeout limits. Instead: Acknowledge immediately, process in background, send update when done."

---

## 5. Session Management Best Practices

**Forrás:** [LLM Agent Architectures 2026](https://futureagi.com/blog/llm-agent-architectures-core-components/)

### 6 rétegű agent architektúra

1. **Model core** — reasoning
2. **Memory** — working, episodic, procedural
3. **Tools** — MCP, plugins
4. **Planner** — goal decomposition
5. **Runtime** — state, retries, handoffs
6. **Observability** — tracing, scoring, guardrails

### Session műveletek

| Művelet | Leírás |
|---------|--------|
| **Start new** | Fresh chat ID |
| **Resume by description** | Lookup chat ID |
| **Resume by ID** | Explicit session select |

### Persistence

> "The agent persists tasks and results to a local database. When a new task arrives, the agent loads all prior tasks as numbered context entries."

---

## 6. Security Considerations

**Forrás:** [MindStudio Guide](https://www.mindstudio.ai/blog/telegram-claude-code-mobile-ai-agent-access)

### Token kezelés

- Bot token = credential → jelszóként kezelni
- Ne commitolni publikus repoba
- IP allowlist szerver szinten
- Telegram privacy settings

### Attack surface

> "Granting an agent shell access and connecting it to messaging apps expands your attack surface exponentially."

### Ajánlások

- Chat ID allowlist (csak engedélyezett user-ek)
- Rate limiting
- Audit logging minden Telegram interakcióra

---

## SpaceOS Tanulságok

### Mi működött

1. **Intent parsing** — @terminal mention alapú routing
2. **Conversation tracking** — chat_id + conversation_id
3. **Multi-bot setup** — terminálonként dedikált bot

### Mi nem működött

1. **Külön chat session** — felesleges komplexitás
2. **Dupla naming convention** — `chat-root` vs `root-chat` zavar
3. **Polling minden bothoz** — resource waste

### Mit használjunk helyette

1. **Claude Code Channels** — hivatalos plugin a sessionbe
2. **Bot-to-bot communication** — multi-agent koordináció
3. **Single gateway** — egy belépési pont, routing mögötte

---

## ⚠️ SpaceOS VPS Korlátozás

**Tesztelve:** 2026-07-04

| Verzió | Státusz |
|--------|---------|
| `2.0.62` | ✅ Működik |
| `2.1.201` | ❌ Timeout (CPU inkompatibilis) |

**Probléma:** A VPS QEMU Virtual CPU (2.5+) nem támogatja a Claude Code 2.1.x binárisokat. Valószínűleg AVX vagy más modern CPU instrukció hiányzik.

**Következmény:** A `--channels` flag nem elérhető — saját Telegram megoldás kell.

---

## Következő lépések

### Ha VPS upgrade történik:
1. [ ] Claude Code 2.1.80+ telepítése
2. [ ] Claude Code Channels plugin kipróbálása
3. [ ] ADR-059 implementálása

### Jelenlegi VPS-sel (2.0.62):
1. [ ] Saját Telegram megoldás egyszerűsítése
2. [ ] Chat session-ök eltávolítása (duplikáció)
3. [ ] Single session + injection pattern megtartása
4. [ ] Bot-to-bot kommunikáció saját implementációval

---

## Források

- [Claude Code Docs - Channels](https://code.claude.com/docs/en/channels)
- [Claude Code Telegram Plugin Guide](https://dev.to/czmilo/claude-code-telegram-plugin-complete-setup-guide-2026-3j0p)
- [MindStudio - Claude Code Channels](https://www.mindstudio.ai/blog/claude-code-channels-telegram-discord-setup)
- [TechTimes - Bot-to-Bot Communication](https://www.techtimes.com/articles/316790/20260518/telegrams-bot-api-now-lets-autonomous-ai-agents-coordinate-directly-no-federal-multi-agent.htm)
- [kagent + A2A](https://maniak.io/articles/2026-03-13-telegram-bot-kagent-a2a-kubernetes/)
- [ZeroClaw Blog](https://zeroclaws.io/blog/telegram-bot-api-2026-ai-agent-developers-guide/)
- [LLM Agent Architectures](https://futureagi.com/blog/llm-agent-architectures-core-components/)
- [Telegram Bot Framework](https://github.com/yomazini/telegram-automation-bot-framework)
