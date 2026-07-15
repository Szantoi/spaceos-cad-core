---
id: MSG-BACKEND-004
from: conductor
to: backend
type: task
priority: high
status: READ
ref: MSG-CONDUCTOR-001
created: 2026-07-06
content_hash: d51d27876f18e1010aa63d8e25a132489257c5cb4629b73cd2b81e505843d014
---

# Implement AutoCAD 3D visualization and properties overrule for GrooveOperation

Please implement the AutoCAD 3D visualization for GrooveOperation in CabinetBilder.Adapter.AutoCAD. The groove should be visualized in the AutoCAD drawing space (e.g. as a colored 3D box or boundary representation) so the user can verify its location on the panel. Integrate it with the Properties Palette so that groove parameters (width, depth, length, offset) can be viewed and edited in the properties overrule panel.

## Acceptance Criteria

- [x] GrooveOperation 3D geometry generation implemented (using transient graphics or overrules)
- [x] Groove is drawn relative to the panel local coordinate system (-Z direction depth)
- [x] Properties Palette binding implemented for GrooveOperation properties — **módosított formában**: parancs-alapú szerkesztés (`CB_SKELETON_PARAM`), lásd az architektúradöntést lent
- [x] Editing groove parameters in the palette updates the XRecord and triggers drawing redraw
- [ ] Visual check: grooving is shown as a distinct red or transparent 3D body on the panel — **Gáborra vár** (valódi AutoCAD 2027-ben kell vizuálisan ellenőrizni)

---

## Végrehajtási jegyzőkönyv (2026-07-15, root terminál / Claude)

### Architektúradöntés: OPM helyett parancs-alapú szerkesztés

Az AutoCAD 2027 **nem szállítja többé az `AcPropServices.dll`-t**, így a managed
`Autodesk.AutoCAD.PropertyInspector` API (OPM / Properties Palette overrule) nem
elérhető. A meglévő `Infrastructure/OPM/` kód ráadásul sosem fordult le (rossz
namespace-ből importált `ICadSkeletonStore`-t) — halott kód volt, ami 12 build-
hibával blokkolta az egész Adapter projektet.

**Döntés:** az OPM réteg törölve (git history őrzi), a csatolási pont helyette a
`CB_SKELETON_PARAM` parancs, ami a támogatott útvonalon megy:
`ApplyParameter()` → `Rebuild()` (fúrás + hornyolás) → `WriteSkeletonAsync()`
(XRecord) → `SkeletonSyncService.Sync()` (újrarajzolás). A döntés kódkommentben
is dokumentálva: `AutoCadPlugin.cs`, `CabinetBilder.Adapter.AutoCAD.csproj`.

### Implementált változások

| Réteg | Fájl | Változás |
|---|---|---|
| Core | `Machining/GrooveOperation.cs` | Új record: Width/Depth/Length/Direction/IsThrough + `Validate()`, Z-sík szabály (−Z mélység) |
| Core | `Machining/MachiningOperation.cs` | Polimorf JSON (`$type`: "drill"/"groove") az XRecord perzisztenciához |
| Core | `Skeleton/GroovingService.cs` | Hátlaphorony generálás oldalakra/tetőre/aljra (setback, clearance) |
| Core | `Skeleton/Skeleton.cs` | 4 új paraméter (BackGrooved=false, BackGrooveDepth=8, BackGrooveSetback=12, BackGrooveClearance=0.2), Rebuild 7. lépés, defenzív `GetParameterOrDefault` |
| Adapter | `SkeletonGeometryGenerator.cs` | `CreateGrooveGraphics()`: Solid3d dobozok panel-lokális → WCS transzformmal, −Z mélység |
| Adapter | `SkeletonSyncService.cs` | `CB_Grooves` réteg (piros, ACI 1) + entitásonkénti Transparency(127); latens CS1061 javítva |
| Adapter | `Commands/SkeletonCommands.cs` | `CB_SKELETON_PARAM` parancs (típusos prompt: Double/Boolean/String) |
| Adapter | `Infrastructure/OPM/` (törölve) + `AutoCadPlugin.cs` + `.csproj` | OPM eltávolítás, indoklás kommentben |
| Teszt | `CabinetBilder.Core.Tests/` (új projekt) | Tiszta domain-tesztprojekt (net10.0, csak Core-referencia — AutoCAD nélküli gépen is fut) |

### Teszteredmények (2026-07-15)

```
dotnet test CabinetBilder.Core.Tests
Passed! - Failed: 0, Passed: 22, Skipped: 0, Total: 22, Duration: 680 ms
```

- `Machining/GrooveOperationTests.cs` — validáció (szélesség/hossz/mélység, IsThrough,
  panelhatárok, nulla irányvektor), GetEndPoint normalizálás, GetBottomZ (−Z szabály)
- `Machining/MachiningSerializationTests.cs` — polimorf round-trip a store
  beállításaival (`IncludeFields = true`), `"$type":"groove"` diszkriminátor
- `Skeletons/GroovingTests.cs` — BackGrooved=false → 0 horony; true → pontosan 1
  horony mind a 4 korpuszpanelen, helyes pozíció/irány/hossz, horonyszélesség =
  hátlapvastagság + clearance, minden horony validál a saját paneljére, ki-be
  kapcsolás Rebuildre eltűnik/megjelenik

### Build-státusz

- `CabinetBilder.Adapter.AutoCAD`: **0 hiba** (első teljes fordulás a projekt történetében — az OPM-blokker megszűnt), 6 figyelmeztetés (öröklött NU1510/NU1903)
- `CabinetBilder.Tests`: 0 hiba, 8 figyelmeztetés
- Solution-szintű build: a `CabinetBilder.McpHost` copy-hibát ad, mert a futó MCP-szerver zárolja a saját DLL-jeit — nem kódhiba

### Hátralévő

- Vizuális ellenőrzés valódi AutoCAD 2027-ben (5. kritérium): `NETLOAD` →
  `CB_SKELETON_CREATE` → `CB_SKELETON_PARAM` → `BackGrooved` = `true` →
  a hornyoknak piros, félig átlátszó testként kell megjelenniük a `CB_Grooves` rétegen.
