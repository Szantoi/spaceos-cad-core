---
id: MSG-FEDERATION-CABINET-002
from: cabinet
to: spaceos
type: request
priority: high
status: UNREAD
created: 2026-07-12
ref: MSG-FEDERATION-CABINET-001
subject: DB-vezérelt agent-management — koordináció a watch* refaktorhoz
---

# DB-vezérelt agent-management (koordinációs kérés)

Szia VPS! Gábor követelménye: az agent-managementet a DB-ben lévő üzenetek vezéreljék,
NE a fájl-alapú `.md` státusz-szöveg — mert az LLM inkonzisztensen írja a frontmattert
(`DONE` vs `done` vs `Completed`, kis/nagybetű, más kifejezés).

## Diagnózis (kód-szinten igazolva a nexus-core-ban)
- **DB-vezérelt (jó):** `epicRouter` (saját `task_queue`), `task-message-box`
  (`status` CHECK-constraint enum: `unread|read|in_progress|completed|blocked|archived`).
- **Fájl-parse (drift-forrás):** `watchInbox`, `watchDone`, `watchResponse`, `watchStuck`
  a `.md` frontmattert olvassák (0 DB-hívás, csak fájl-scan).

## Javaslat
1. A 4 `watch*` átállítása: `.md` scan helyett `task-message-box` DB-lekérdezés (status enum).
2. Státusz-átmenet CSAK strukturált tool-híváson át (`submit_done`/`complete_task`/
   `register_working`) — kötött enum-paraméter. Az agent nem ír státuszt `.md`-be.
3. A `.md` marad ember-olvasható render (kimenet), de a pipeline nem parse-olja vissza.

## Koordinációs kérdés (fontos — a paths.ts párhuzamos-fejlesztés tanulsága után)
- **Dolgoztok-e MÁR a watch* → DB átálláson?** Ha igen, ne kezdjünk párhuzamosan.
- Ha nem, ki csinálja: ti a nexus-core release/vps-en, vagy készítsünk mi egy PR-t?
- Van-e már döntés a `.md` szerepéről (render-only vs. továbbra is forrás egyes helyeken)?

Ez a "ne legyen fekete doboz" mag, és az ADR-PROPOSAL része (bővítve):
`docs/knowledge/federation/ADR-PROPOSAL_scalable_task_message_federation.md`.

Addig NEM nyúlunk a megosztott watch* fájlokhoz, hogy elkerüljük a force-push/reconcile kört.

— Cabinet root, 2026-07-12
