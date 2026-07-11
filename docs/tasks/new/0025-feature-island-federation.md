# Task ID: 0025
# Title: Sziget-federáció (VPS ↔ lokál MCP kommunikáció)
# Category: feature
# Milestone: 14
# Status: new

## Szándék (Intent)

A VPS-en futó ERP-fejlesztő sziget és a lokális CabinetBilder sziget szervereken keresztül kommunikáljon: task-átadás, tudás-szinkron (ERP dokumentum- és tervezési sémák követése), RAG-tartalom megosztás — mindezt a meglévő MCP HTTP + token mechanizmussal, új protokoll nélkül.

## Elfogadási kritérium (Acceptance Criteria)

- [ ] Federation tokenek: a lokális sziget agents.yaml-jában `vps-conductor` bejegyzés, a VPS-ében `local-conductor` (irány-szintű audit)
- [ ] A lokális conductor MCP-kliensként tud taskot küldeni a VPS `/mcp` endpointjára (create_task / tmb_create_task) és fordítva
- [ ] ERP-séma szinkron: a VPS `docs/` sémadokumentumai (dokumentumkezelés, tervezési sémák) lokálisan indexelve a RAG-ba, forrás-megjelöléssel
- [ ] Szállítás: explicit SSH-tunnel (LocalForward, 0024-ből); Tailscale/WireGuard kiértékelés dokumentálva
- [ ] Sziget-regiszter (`islands.json`, 0023-ból) mindkét oldalon naprakész
- [ ] Hibatűrés: ha a tunnel nem él, a federációs hívás értelmes hibával áll le (nem akad be a pipeline)

## Tanúsítás (Evidence)

- Kézi teszt: lokál → VPS task-küldés + VPS → lokál válasz (DONE outbox) végigkövetve
- RAG-teszt: `search_knowledge` lokálisan visszaad VPS-eredetű ERP-sémadokumentumot
- Napló: mindkét oldal MCP logjában a federation agent-név látszik

## Megjegyzések (Notes)

- Előfeltétel: 0023 (példány-identitás), 0024 (explicit tunnel).
- A "sziget" hozzárendelhető projekthez VAGY munkaegységhez — a regiszter ezt címkével kezelje.
- Kapcsolódó terv: `docs/Terv_MCP_Sziget_Roadmap_2026H2.md` (3. fázis).

---

**Started:**
**Completed:**
**Duration:**
**Owner:**
