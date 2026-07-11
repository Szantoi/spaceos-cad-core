# Task ID: 0022
# Title: CabinetBilder MCP Host (olvasó toolok PoC)
# Category: feature
# Milestone: 13
# Status: new

## Szándék (Intent)

A CabinetBilder.Core use-case-ei fölé MCP szerver-host épül (`CabinetBilder.McpHost` projekt), hogy az agentek (Claude Code, Antigravity) és a humán (AutoCAD UI) ugyanazokat a domain-műveleteket használhassák. Nem a CLI átalakítása: új host a Core + SpaceOsBridge fölött.

## Elfogadási kritérium (Acceptance Criteria)

- [ ] Új projekt: `CabinetBilder.McpHost` (.NET 10, `ModelContextProtocol` NuGet, stdio + HTTP transport)
- [ ] 5 olvasó tool működik: `list_smart_objects`, `get_smart_object_metadata`, `get_skeleton`, `get_sync_status`, `generate_bom`
- [ ] A toolok a meglévő CQRS requesteket hívják (GetSmartObjectMetadataQuery, CheckSyncStatusQuery, …), nem duplikálnak logikát
- [ ] Bearer token auth a HTTP transporton (a knowledge-service token-mintája szerint)
- [ ] Claude Code `.mcp.json`-ból és Antigravity `mcp_config.json`-ból is bekötve, kézi teszttel igazolva
- [ ] Író tool NINCS ebben a körben (az a 4. fázis, külön taskban)

## Tanúsítás (Evidence)

- Kód: `CabinetBilder.McpHost/`
- Teszt: `CabinetBilder.Tests/McpHost/` (tool-hívás → use-case handler integrációs teszt)
- Build: `dotnet build -c Release` → Success
- Kézi teszt: `tools/list` + `tools/call generate_bom` JSON-RPC hívás dokumentálva

## Megjegyzések (Notes)

- A C# MCP SDK friss dokumentációját implementáció előtt ellenőrizni (Context7/ref: `ModelContextProtocol` NuGet, Microsoft–Anthropic).
- A `client.db` (SQLite) csak olvasás — konkurencia-kérdés így minimális; írásnál jön a Redis lock (későbbi task).
- Kapcsolódó terv: `docs/Terv_MCP_Sziget_Roadmap_2026H2.md` (2. fázis).

---

**Started:**
**Completed:**
**Duration:**
**Owner:**
