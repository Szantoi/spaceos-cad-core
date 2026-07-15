---
id: MSG-BACKEND-OUT-2026-07-15-001
from: backend
to: conductor
type: status
priority: high
status: NEW
ref: MSG-BACKEND-004
created: 2026-07-15
---

# Task 0021 (MSG-BACKEND-004): GrooveOperation 3D vizualizáció + paraméterszerkesztés — KÉSZ (vizuális ellenőrzés Gáborra vár)

## Összefoglaló

Az 5 elfogadási kritériumból 4 teljesítve és build+teszt szinten igazolva;
az 5. (vizuális ellenőrzés valódi AutoCAD 2027-ben) emberi lépés.

- **3D geometria**: `SkeletonGeometryGenerator.CreateGrooveGraphics()` — Solid3d
  dobozok panel-lokális koordinátarendszerben, −Z irányú mélységgel (Z-sík szabály).
- **Megjelenítés**: `CB_Grooves` réteg (piros) + Transparency(127) — piros, félig
  átlátszó testek, Sync-enként újragenerálva.
- **Paraméterszerkesztés**: OPM/Properties Palette **helyett** `CB_SKELETON_PARAM`
  parancs — az AutoCAD 2027 már nem szállít AcPropServices.dll-t, a managed
  PropertyInspector API nem létezik. A halott OPM kód törölve; az útvonal:
  ApplyParameter → Rebuild → XRecord írás → SkeletonSyncService.Sync (újrarajzolás).
- **Build**: Adapter először fordul 0 hibával (az OPM-blokker 12 hibája megszűnt).
- **Teszt**: `CabinetBilder.Core.Tests` — 22/22 PASS (validáció, polimorf JSON
  round-trip `$type` diszkriminátorral, GroovingService/Rebuild viselkedés).

Részletes végrehajtási jegyzőkönyv a task fájlban:
`terminals/backend/inbox/2026-07-06_004_implement-autocad-3d-visualization-and-properties-.md`

## Gábor teendője (5. kritérium)

AutoCAD 2027: `NETLOAD` → `CB_SKELETON_CREATE` → `CB_SKELETON_PARAM` →
`BackGrooved` = `true` → a hornyok piros, félig átlátszó testként jelennek meg.
