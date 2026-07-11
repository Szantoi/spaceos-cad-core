# Task ID: 0024
# Title: Agent token- és titok-higiénia + explicit tunnel
# Category: refactor
# Milestone: 13
# Status: new

## Szándék (Intent)

A multi-agent hozzáférés biztonsági alapjainak rendezése: az Antigravity ne master tokennel fusson, a titkok kerüljenek ki a configfájlokból, a VPS-tunnel legyen explicit és VS Code-független.

## Elfogadási kritérium (Acceptance Criteria)

- [ ] `~/.gemini/config/mcp_config.json`: spaceos-knowledge bejegyzés master token → dedikált terminál-token (pl. `antigravity-main`), `mcp-remote` wrapper → natív `serverUrl` + `headers`
- [ ] Új agent-tokenek regisztrálva: `antigravity-main`, `claude-main` (lokál agents.yaml ✅ 2026-07-06, VPS ⏳)
- [ ] API-kulcsok (Brave, context7, Stitch) configfájlokból env-változóba vagy secret-tárolóba
- [ ] `~/.ssh/config` spaceos-gabor: `LocalForward 3456 localhost:3456` — tunnel `ssh -N spaceos-gabor`-ral, VS Code nélkül is él
- [ ] master_token kikerül az agents.yaml-ból → `MCP_AUTH_TOKEN` env (a kód már támogatja)
- [ ] Dokumentálva: melyik kliens melyik identitással fut (tábla a `docs/knowledge/multi_agent_mcp_integracio_rag.md`-ben frissítve)

## Tanúsítás (Evidence)

- Kézi teszt: `agy` session `get_identity` → NEM root
- Kézi teszt: VS Code bezárva, `ssh -N spaceos-gabor` mellett `curl localhost:3456/health` → OK
- Grep: nincs API-kulcs a repo-configokban

## Megjegyzések (Notes)

- A VPS-oldali agents.yaml módosításhoz SSH kell — Gábor futtatja vagy engedélyezi.
- Kapcsolódó terv: `docs/Terv_MCP_Sziget_Roadmap_2026H2.md` (0. fázis).

---

**Started:**
**Completed:**
**Duration:**
**Owner:**
