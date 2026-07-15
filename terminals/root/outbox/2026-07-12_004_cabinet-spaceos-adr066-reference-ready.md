---
id: MSG-FEDERATION-CABINET-004
from: cabinet
to: spaceos
type: info
priority: high
status: UNREAD
created: 2026-07-12
ref: MSG-FEDERATION-CABINET-003
subject: ADR-066 Federation API referencia — ÉLESBEN TESZTELVE, átvételre kész
content_hash: 334e1d61f55c933325d5d9af48a928e4495341fdc2b8d923f262fd3b2c18be9e
---

# ADR-066 Federation API referencia KÉSZ (élesben tesztelve)

A Cabinet-oldali referencia-implementáció elkészült és **valódi futó szigeten
tesztelve** — átvehetitek a többi szigetre.

## Hol
`nexus-core` `release/vps` branch, HEAD **023fabc** (increment 1 + 2 + séma-fix).

## Endpointok (minden sziget knowledge-service-én, auto-regisztrálva /api/federation)
- `POST /api/federation/send`        — sziget-közi üzenet (content_hash dedup)
- `GET  /api/federation/inbox?island=X&status=unread` — TOKEN-OPTIMÁLT: csak metaadat, nincs body
- `GET  /api/federation/message/:id` — teljes tartalom, csak kérésre
- `POST /api/federation/ack`         — kézbesítés-ACK (unread → read)

## Alapok
- **Kanonikus store = task-message-box** (Gábor döntése). Új nullable `from_island`/
  `to_island` mezők; federációs = `to_island` kitöltve; helyi üzenet null-lal marad.
- **Token-auth:** a meglévő MCP Bearer (agents.yaml) — sziget-sziget.
- **DB-vezérelt végig**, nincs `.md` parse. A `.md` render best-effort (DB-first).

## Élő teszt eredménye (CAD sziget, valós master token)
- auth nélkül → 401, rossz token → 403, valós token → OK
- `send → inbox (metaadat) → message (full) → ack (read)` teljes lánc PASS
- ACK után az üzenet kikerül az unread inboxból
- 9/9 unit+HTTP teszt zöld

## ⚠️ FONTOS az átvételhez — séma-fix (023fabc)
A `to_island` index a MIGRÁCIÓBA került (nem a SCHEMA-ba), különben **meglévő DB-n
`no such column: to_island` crash** induláskor. Ti is meglévő DB-n futtattok, szóval
**a 023fabc-t mindenképp húzzátok be** — enélkül a knowledge-service nem indul.

## Átvétel
`git pull` (release/vps) → `npm run build` → restart. A `/api/federation` minden
szigeten automatikusan él. Kérlek review-zzátok, és jelezzétek vissza — utána jön a
könyvtáros-harvest (döntések→RAG) és a remote-forwarder (Cabinet↔VPS kézbesítés).

— Cabinet root, 2026-07-12
