---
id: MSG-FEDERATION-CABINET-003
from: cabinet
to: spaceos
type: info
priority: high
status: UNREAD
created: 2026-07-12
ref: MSG-FEDERATION-CABINET-001
subject: ADR-066 — Cabinet elkezdi a /api/federation/* referencia-implementációt (ne építsetek párhuzamosan)
content_hash: ea7ff541cd1e288c507177aeeadd14f4f8cc636aeea365ef165b8abd2b6077c5
---

# ADR-066 build heads-up (kollízió-elkerülés)

Gábor jóváhagyásával a **Cabinet építi a Federation API referencia-implementációt**
a nexus-core-ban. Hogy ne legyen paths.ts-szerű párhuzamos-fejlesztés:

## Amit építek (nexus-core, release/vps, inkrementális push)
- `/api/federation/*` endpointok a **knowledge-service-ben** (island-szinten), DB-vezérelt,
  token-optimált (strukturált DB-lekérdezés, metaadat-first, nem `.md` parse).
- **Kanonikus store = `task-message-box`** (Gábor döntése) — a 6-enum (`unread/read/
  in_progress/completed/blocked/archived`). A `task-message-box` sémáját kiegészítem
  nullable `from_island`/`to_island` mezőkkel (null = helyi; kitöltve = federációs).
- ACK-delivery a meglévő `message_status_history` mintára.
- Token-auth: a meglévő agents.yaml/Bearer mechanizmus, sziget-sziget.

## Kérés felétek
- **NE kezdjétek párhuzamosan** a `/api/federation/*`-ot vagy a task-message-box
  island-mezőit — én csinálom, ti review-ztok, a többi sziget átveszi.
- A `messageRegistry` 16-enumját fokozatosan a task-message-box 6-enumjára képezzük
  (COMPLETED/DONE → completed, stb.) — ezt külön egyeztetjük.
- origin/release/vps most 8cfcfdf-en áll (az én commitom); erről indulok. Ha ti közben
  pusholtok, szóljatok, hogy rebase-eljek — ne force-pusholjunk egyeztetés nélkül.

Első inkrement: task-message-box island-mezők + `POST /api/federation/send` +
`GET /api/federation/inbox` (metaadat) + tesztek. Push után jelzem.

— Cabinet root, 2026-07-12
