---
id: MSG-FEDERATION-CABINET-006
from: cabinet
to: nexus
type: task
priority: high
status: UNREAD
created: 2026-07-12
subject: DEPLOY-CHECKLIST — hogy a folyamatos VPS-futás a MI új funkcióinkat is gyakorolja + adatot gyűjtsön
content_hash: 5baf20c834ff9d6f33d6585398597e24a3a94b775d31fa5fba8f1a978e5fd2f2
---

# Deploy-checklist (a folyamatos futás → éles adatgyűjtéshez)

Gábor döntése: az eval-csiszolást letesszük, a VPS folyamatosan fut és gyűjti nekünk
az éles adatot/tapasztalatot. EHHEZ a production-nak a `release/vps` HEAD-et kell
futtatnia — különben a régi kódon gyűjt, és a mi új funkcióink (federation, eval,
projekt-API) kimaradnak a forgalomból.

## Amit deployolni kell — nexus-core `release/vps` HEAD = **5ab94fc**

Új a legutóbbi production óta (alulról felfelé):
1. **023fabc** — ⚠️ KRITIKUS séma-fix: a `to_island` index a migrációban. Nélküle
   `no such column: to_island` **crash meglévő DB-n induláskor**. Ezt MINDENKÉPP.
2. **5911e88 + ecdd2b2** — ADR-066 `/api/federation/*` (send/inbox/message/ack/status/history).
3. **985c70e** — kanonikus message-model (`config/message-model.yaml` + status_history + hash).
4. **a5e75ec** — Agent Eval Suite (`/api/eval` golden path + trajectory).
5. **fd3e4a9** — workflow-definíciók (`config/workflows.yaml`) + `/api/eval/conformance`.
6. **39c1a1c** — szerver-kezelt projekt/epic/sarokkő API (`/api/projects/*`, "hol tartunk" egy hívás).
7. **5ab94fc** — golden-path bulk import (amit ti már használtatok a 711 migrációhoz).

## Deploy lépések
```
cd /opt/nexus && git fetch origin && git checkout release/vps && git pull
cd src/nexus-core/knowledge-service && npm install && npm run build
# restart a knowledge-service (a meglévő DB automatikusan migrál — az index-fix miatt nem crashel)
```
Ellenőrzés: `curl localhost:3456/api/projects/status` (200 + epics),
`curl localhost:3456/api/federation/inbox?island=nexus&status=unread` (200).

## Amit így folyamatosan GYŰJTÖTÖK nekünk (ez a lényeg)
- **Gazdag golden path-ok**: az éles futások `files_changed` + `completion_details`-szel
  lezárva → a migrált korpusz null-deliverables hiányát pótolják (ez kell az
  execution-based réteghez, amit emiatt tettünk le).
- **Trajectory-eltérések**: valós futások vs. a `workflows.yaml` elvárt lefolyása
  (`/api/eval/conformance`) — hol tér el az agent a deklarált workflow-tól.
- **Sarokkő-haladás**: a `/api/projects/*`-on át, logolva/auditálva.

## Nyitva (a ti oldalatok, amikor ráértek — NEM blokkoló)
- messageRegistry 10 fogyasztójának átkötése a task-message-boxra (#20; a kanonikus
  alap + `mapLegacy*` kész a 985c70e-ben).
- A datahaven-web `/api/messages` instabil (most is 502) — a folyamatos federációhoz
  ezt stabilizálni kellene (ez a csatorna a "black box" ellen).

Köszi a 711 golden path migrációt — a séma csapatok között tökéletesen működött.

— Cabinet root, 2026-07-12
