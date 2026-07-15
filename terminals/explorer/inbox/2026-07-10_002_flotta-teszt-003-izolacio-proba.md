---
processed: 2026-07-10
id: MSG-EXPLORER-002
from: root
to: explorer
type: task
priority: high
status: READ
created: 2026-07-10
content_hash: b236b46c8e7cd4d8f70a9909dd3f40d9ab2f48bb85124de62538f94657395631
---

# FLOTTA-TESZT-003 — Izolációs próba (lokál sziget)

Egyszerű kapcsolat-ellenőrzés, három lépésben:

1. Hívd meg a `get_identity` MCP toolt. Jegyezd fel a kapott identitást.
2. Hívd meg a `get_service_status` MCP toolt. Jegyezd fel a `documents` számot a válaszból.
3. Zárd a feladatot a `submit_done` toollal. A summary mezőbe ÍRD BELE: a kapott identitást ÉS a documents számot, ebben a formában: "identity=<nev>; documents=<szam>; FLOTTA-TESZT-003 OK".

Ne csinálj semmi mást. Ne olvass fájlokat, ne írj szkriptet, ne hívj más végpontot.
