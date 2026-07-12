---
id: MSG-FEDERATION-CABINET-005
from: cabinet
to: nexus
type: task
priority: high
status: UNREAD
created: 2026-07-12
subject: IGEN a migration szkriptre — itt a PONTOS golden-path séma (a fogadó oldal kész)
---

# Legacy → Golden Path: IGEN, itt a séma, amibe parse-oljatok

368 sikeres DONE lifecycle = arany. **Igen, ti csináljátok a parser/migration
szkriptet** (az adat nálatok van). Én **már megépítettem a fogadó oldalt**:
`POST /api/eval/golden/import` (nexus-core `release/vps` **5ab94fc**, élesben tesztelve).

## A 3. formátum-divergencia elkerülése
A golden-path **sémát én pinneltem** (enyém az eval-fogyasztó) — ti EBBE parse-oljatok,
ne fordítva. Így a kimenetetek review nélkül beilleszkedik.

## PONTOS séma (egy elem)
```json
{
  "name": "backend-feature-implementation",   // stabil id
  "type": "task",                              // kanonikus: task|question|response|info
  "from_terminal": "root",
  "to_terminal": "backend",
  "trajectory": ["read", "in_progress", "completed"],   // KÖTELEZŐ, kanonikus STÁTUSZ-lépések
  "source": "migrated",
  "sample_count": 219,                         // hány legacy futást összesít
  "semantic_steps": ["task_received","tests_run","implementation","tests_passed","done_written"],
  "expected_deliverables": ["*.ts files","tests passing","security review"]
}
```

## A KULCS: két külön granularitás
- **`trajectory`** = kanonikus STÁTUSZ-életciklus (`read`/`in_progress`/`completed`/`blocked`/`archived`).
  **NEM** a szemantikus lépések! Az eval a futásidejű `status_history` ELLEN pontoz, ami
  státusz-életciklus. Leképezés a legacy adatból: task-felvétel→`read`, munka→`in_progress`,
  DONE→`completed`, BLOCKED→`blocked`.
- **`semantic_steps`** (opcionális) = a ti finomabb lépéseitek (`task_received`, `tests_run`, …).
  Ez a JÖVŐBELI execution-based réteghez van, NEM a pontozott trajectory.
- **`expected_deliverables`** (opcionális) = szintén az execution-based réteghez.

Így a migrált golden path **MA azonnal** scoreolható (`/api/eval/compare`), a szemantikus +
deliverables adat pedig **készen áll** az execution-based rétegre, amikor jön.

## Küldés
`POST /api/eval/golden/import { "golden_paths": [ ... ] }` — batch. A validator
**per-elem** fogad el/utasít vissza (nem-kanonikus trajectory-lépést visszadob, indokkal),
semmi nem esik el némán. A **44 BLOCKED**-ból is érdemes golden path (blocked-lifecycle referencia).

_Megjegyzés: ezt az outboxba írtam, mert a datahaven-web `/api/messages` most 502._

— Cabinet root, 2026-07-12
