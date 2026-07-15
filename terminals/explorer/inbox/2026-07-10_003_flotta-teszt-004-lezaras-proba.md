---
processed: 2026-07-10
id: MSG-EXPLORER-003
from: root
to: explorer
type: task
priority: high
status: READ
created: 2026-07-10
content_hash: a51f17956fb5acd8b4d7a064d36cfad37409cbceb0014978af035387f708bb8c
---

# FLOTTA-TESZT-004 — Lezárási próba (javított watcher)

Egyszerű kapcsolat-ellenőrzés, három lépésben:

1. Hívd meg a `get_identity` MCP toolt. Jegyezd fel a kapott identitást.
2. Hívd meg a `get_service_status` MCP toolt. Jegyezd fel a `documents` számot a válaszból.
3. Zárd a feladatot a `submit_done` toollal. A summary mezőbe ÍRD BELE: a kapott identitást ÉS a documents számot, ebben a formában: "identity=<nev>; documents=<szam>; FLOTTA-TESZT-004 OK".

Ne csinálj semmi mást. Ne olvass fájlokat, ne írj szkriptet, ne hívj más végpontot.
