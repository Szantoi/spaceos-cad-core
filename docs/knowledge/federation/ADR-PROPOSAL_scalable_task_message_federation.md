---
name: ADR-PROPOSAL — Scalable task-message federation
description: Egységes, skálázható, auditálható sziget-közi kommunikáció a task-message modellre építve, könyvtáros-visszacsatolással a tudástárba
status: PROPOSAL (VPS-egyeztetésre vár)
created: 2026-07-12
from: cabinet
---

# ADR-PROPOSAL: Skálázható task-message federáció

> Cél: **egy modell, egy transport, egy audit-log, egy könyvtáros-visszacsatolás.**
> Gábor követelménye (2026-07-12): "Skálázható megoldás kell a kommunikációhoz, ami
> logolható és követi a feladat-üzenet koncepciót... ne legyen fekete doboz a munka,
> és a tudástár is gyarapodhat a döntésekkel és a tapasztalatokkal."

## Probléma

Jelenleg 3 párhuzamos csatorna él, inkonzisztensen:
1. **Fájl-watcher** (`federation-watcher.sh`, outbox→inbox `cp` 30mp-enként) — nem skálázik, nincs központi log, nincs garantált dedup.
2. **MCP tool** (`send_message`) — session-höz kötött, nem tartós.
3. **REST** (`/api/messages`, `/api/federation/send`) — kettő is van, eltérő mezőkkel.

Emiatt a munka részben "fekete doboz", és a döntések nem kerülnek vissza a tudástárba.

## Döntés: a task-message a federáció EGYETLEN modellje

A `task-message-box` (knowledge-service, SQLite + .md render) már pont a kellő
gazdag, strukturált formátum. Minden sziget-közi üzenet EZ legyen — nem ad-hoc szöveg.

### 1. Modell — task-message (követi a feladat-üzenet koncepciót)
Minden üzenet strukturált rekord: `from_island/from_terminal`, `to_island/to_terminal`,
`type` (task | question | response | info | done | blocked), `priority`, `subject`,
`payload.body`, `status`, `ref` (szálazás), `content_hash` (dedup), timestamps, és a
lezáró mezők: `completion_summary`, **`decisions`**, `files_changed`, `next_steps`.
Ez a gazdagság az, amit a könyvtáros szűrni tud.

### 2. Transport — központi hub HTTP API (skálázható)
Minden sziget a központi `datahaven` PostgreSQL-backed API-t használja
(`POST /api/messages/send`, `GET /api/messages/inbox`). Postgres, nem fájl-másolás →
skálázik, sziget-enkénti token-auth, hálózaton át is megy (Cabinet is). **Ez váltja ki
a fájl-watchert.** A `federation.messages` tábla a single source of truth.

### 3. Audit-log — append-only (ne legyen fekete doboz)
Minden üzenet ÉS minden státusz-átmenet (UNREAD→READ→IN_PROGRESS→DONE/BLOCKED) bekerül
a Postgres logba, törölhetetlenül. Bármikor lekérdezhető: "mit döntött X sziget Y-ról,
és miért". A `content_hash` garantálja a dedupot.

### 4. Könyvtáros-visszacsatolás — a tudástár gyarapodása
Ütemezett `librarian` pipeline (a meglévő `extract_patterns` + knowledge-indexelés):
- olvassa a lezárt (`done`) és döntést tartalmazó üzeneteket az audit-logból,
- kinyeri a **döntéseket, tapasztalatokat, mintákat** (a `decisions`/`completion_summary` mezőkből),
- indexeli őket a megfelelő sziget RAG-jába (`docs/knowledge/federation/harvested/`).
Így minden lezárt feladat gazdagítja a keresésbe a jövőbeli döntésekhez.

## Skálázhatóság
- Központi Postgres store (nem N×N fájl-másolás).
- Sziget-enkénti poll VAGY subscribe; `content_hash` dedup; `priority` sor; `ref` szálazás.
- Új sziget felvétele = 1 token + 1 island-bejegyzés, nem watcher-scriptek bővítése.

## Migrációs terv (inkrementális, nem big-bang)
1. **Kanonizálás:** a `/api/messages` (payload.body) legyen az EGY REST-forma; a
   `/api/federation/send` és a fájl-watcher fokozatosan kivezetve.
2. **Cabinet referencia:** a Cabinet minden kimenő federációja a task-message modellen
   + központi API-n megy (már részben így van).
3. **Könyvtáros harvest:** először a mi CAD/Doorstar szigetünkön állítjuk fel a
   döntés-harvestet a `docs/knowledge/federation/harvested/`-ba, mint referencia.
4. **Belső szigetek:** a VPS a fájl-watchert lecseréli a központi API-poll-ra.

## RÉTEG-MODELL: mi helyi (sziget) és mi federációs?

A federáció CSAK a sziget-határt átlépő üzenet. Minden más helyi marad. Két réteg,
tiszta határral — a diszkriminátor az `island` mező (a helyi task-message-ben NINCS is).

### 1. réteg — HELYI (sziget-belső) — MÁR KÉSZ a nexusban
- **Helyi tudás:** a sziget saját RAG-collectionje (CAD=`cabinetbilder-cad`,
  Doorstar=`cabinetbilder-doorstar`). Alapból SOHA nem hagyja el a szigetet.
- **Helyi feladatok:** `task-message-box` terminál↔terminál EGY szigeten belül
  (nincs `island` mező) → `terminals/<név>/inbox`-ba renderel. Nem érinti a központi hubot.
- **Terminál-ébresztés helyi szinten (megvan!):** `epicRouter` (idle terminálnak
  dispatch), `watchInbox`/`watchIdle`/`watchQueue`/`watchPriority` (figyel + trigger),
  `spawn_work_session`/`spawn_parallel_workers`/`register_working`/`register_idle`,
  `projectDispatcher`, `nightwatch`. Ez mind sziget-belső, nem federációs.

### 2. réteg — FEDERÁCIÓS (sziget-közi)
- Csak akkor, ha `to_island != from_island`. A task-message-re rákerül az `island` cím.
- **Tudás-szeparáció:** alapból semmi nem federálódik. Csak a könyvtáros által
  `shared`-nek jelölt, kiharvestelt DÖNTÉSEK kerülnek a közös/federált tudás-rétegbe.
- Áthalad a központi `datahaven` API-n + audit-logon.

### Rootok kommunikációja = a federáció gateway-ei
- Minden sziget **root**-ja a helyi hub ÉS a federációs kapu egyszerre.
- **Root→root** = egy federációs üzenet, ahol mindkét oldalon `to_terminal: root`.
- Egy root: fogad federációs üzenetet (`to_island: én`) → **helyben dispatch-eli**
  egy worker-terminálnak (1. réteg ébresztés) → küld kifelé federációs üzenetet.
- Két-szintű hub-and-spoke: a **központ a rootokat** köti össze; **minden root a saját
  termináljainak helyi hubja**. Így a federáció nem ismeri a belső terminálokat — csak
  a rootokat; a belső ébresztés/dispatch teljesen helyi marad.

### Egy üzenet útja (példa: VPS taskot ad a Cabinet backendnek)
1. `spaceos/root` → federációs task, `to_island: cabinet, to_terminal: root`.
2. Központi API + audit-log rögzíti; `cabinet/root` inboxába kézbesül.
3. `cabinet/root` HELYBEN dispatch-eli: `task-message-box`, `to_terminal: backend`,
   `epicRouter`/`spawn_work_session` felébreszti a backendet — ez már 1. réteg, a
   federáció nem látja.
4. backend lezárja (`done` + `decisions`) → `cabinet/root` federációs `response`-t küld vissza.
5. Könyvtáros a `decisions`-t a RAG-ba harvesteli (`shared`, ha releváns más szigeteknek).

## Nyitott kérdések (VPS-egyeztetés)
- OD-A: poll vs. push (webhook/SSE) a sziget-oldali kézbesítéshez?
- OD-B: a `decisions` mező formátuma (szabad szöveg vs. strukturált: {döntés, indok, alternatívák})?
- OD-C: harvest gyakoriság és a "mi minősül tudástárba-emelendő döntésnek" szűrő.

_Cabinet root — proposal, 2026-07-12. Egyeztetésre a VPS ADR-066-tal._
