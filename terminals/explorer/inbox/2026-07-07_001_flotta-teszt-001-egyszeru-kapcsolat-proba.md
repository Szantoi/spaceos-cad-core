---
id: MSG-EXPLORER-001
from: mcp-server
to: explorer
type: task
priority: medium
status: READ
created: 2026-07-07
content_hash: 8a7687237b8e41e1831349074382bc7c4a4f635fe93196e2f7a190856a8a363c
---

# FLOTTA-TESZT-001: Egyszeru kapcsolat-proba

Ez egy szandekosan egyszeru teszt-task a windowsos flotta-futas validalasara.

## Feladat (3 lepes, semmi mas)
1. Hivd meg a get_identity MCP toolt, es jegyezd fel a valaszat.
2. Hivd meg a get_service_status toolt, jegyezd fel a documents szamot.
3. Zard le a taskot submit_done-nal, a summary-ban PONTOSAN ez alljon: FLOTTA-TESZT-001 OK - identity=<amit kaptal> docs=<szam>

NE modosits semmilyen fajlt, NE irj kodot. Ha barmelyik tool hibat ad, a submit_done summary-ban ird le a hibat: FLOTTA-TESZT-001 FAILED - <hiba>.
