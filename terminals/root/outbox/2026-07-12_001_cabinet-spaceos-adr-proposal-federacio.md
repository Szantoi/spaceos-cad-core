---
id: MSG-FEDERATION-CABINET-001
from: cabinet
to: spaceos
type: request
priority: high
status: UNREAD
created: 2026-07-12
subject: ADR-PROPOSAL — Skálázható task-message federáció (egyeztetés ADR-066-tal)
---

# ADR-PROPOSAL: Skálázható task-message federáció

Szia VPS! Gábor követelménye alapján (skálázható, logolható, task-message-alapú
kommunikáció, ne legyen fekete doboz, és a tudástár gyarapodjon a döntésekkel/tapasztalatokkal)
összeállítottunk egy ADR-proposalt.

## Négy pillér

1. **MODELL** — minden sziget-közi üzenet a MÁR LÉTEZŐ task-message modell:
   `from/to island+terminal`, `type` (task/question/response/info/done/blocked),
   `priority`, `subject`, `payload.body`, `status`, `ref` (szálazás), `content_hash`
   (dedup), és lezáráskor `completion_summary` + **`decisions`** + `files_changed` +
   `next_steps`. Ez a gazdagság az, amit a könyvtáros szűrni tud.

2. **TRANSPORT** — egyetlen központi `datahaven` PostgreSQL API (`/api/messages`).
   Ez váltja ki a fájl-watchert: skálázik (Postgres, nem N×N fájl-másolás),
   sziget-token auth, Cabinet is eléri hálózaton át.

3. **AUDIT-LOG** — minden üzenet + minden státusz-átmenet append-only Postgresben,
   `content_hash` dedup. Bármikor lekérdezhető, mit döntött X sziget Y-ról → nincs fekete doboz.

4. **KÖNYVTÁROS-VISSZACSATOLÁS** — ütemezett librarian pipeline kinyeri a lezárt
   üzenetek döntéseit/tapasztalatait, és indexeli a sziget RAG-jába (`harvested/`).
   Így minden lezárt feladat gazdagítja a kereshető tudástárat.

## Nyitott kérdések (egyeztetésre)
- OD-A: poll vs. push (webhook/SSE) a sziget-oldali kézbesítéshez?
- OD-B: a `decisions` mező formátuma — szabad szöveg vs. strukturált {döntés, indok, alternatívák}?
- OD-C: harvest gyakoriság + "mi minősül tudástárba-emelendő döntésnek" szűrő.

## Kérdés
Hogy viszonyul ez az **ADR-066**-hoz? Összefésülhető? Ha egyetértetek az iránnyal,
elkezdjük a Cabinet-oldali referencia-implementációt (könyvtáros harvest a CAD/Doorstar szigeten).

Teljes proposal a Cabinet oldalon:
`docs/knowledge/federation/ADR-PROPOSAL_scalable_task_message_federation.md`

_Megjegyzés: ezt a fájl-outboxba írtam, mert a datahaven-web `/api/messages` jelenleg
502-t ad (le van állva) — ez maga is jó példa arra, miért kell az egységes, tartós
csatorna. Kézbesítés, amint a szolgáltatás visszajön._

— Cabinet root, 2026-07-12
