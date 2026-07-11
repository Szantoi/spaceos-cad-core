# Task ID: 0010
# Title: Fázis 0+1 — Handler Layer Extraction & Project Rename / Layer Split
# Category: refactor
# Milestone: 10 (Arch Vision v2 — Fázis 0 + Fázis 1)
# Status: new
# Source: CabinetBilder_Architecture_Vision_v2.md §6.2, §16.1 (Fázis 0, Fázis 1)

## Szándék (Intent)

A jelenlegi `App.AutoCadScripts.*` névtér-struktúrát átnevezni és a rétegeket a `CabinetBilder.*` architektúra vízióra igazítani.
A handler logikát kivonni a jelenlegi projektből és az architektúra-dokumentumban definiált rétegekre (Core / SpaceOsBridge / Adapter.AutoCAD) szétválasztani.

## Elfogadási kritérium (Acceptance Criteria)

- [ ] Új projekt-struktúra létrehozva: `CabinetBilder.Core`, `CabinetBilder.SpaceOsBridge`, `CabinetBilder.Adapter.AutoCAD`
- [ ] Meglévő SmartObject doménelemek (`SmartObjectMetadata`, `ISmartObjectMetadataService`, stb.) átkerülnek a `CabinetBilder.Core` projektbe
- [ ] A `CabinetBilder.Adapter.AutoCAD` az egyetlen AutoCAD-függő réteg (AutoCAD API referenciák csak itt)
- [ ] Az `App.AutoCadScripts.Core` → `CabinetBilder.Core` névtér-csomag átnevezve
- [ ] Az `App.AutoCadScripts` → `CabinetBilder.Adapter.AutoCAD` névtér átnevezve
- [ ] A `CabinetBilder.SpaceOsBridge` projekt létrejön (üres, de hivatkozások konfigurálva)
- [ ] `CabinetBilder.Tests` projekt az összes meglévő tesztet tartalmazza
- [ ] `dotnet build` → 0 error, 0 warning (NU1603 kivételek dokumentálva)
- [ ] `dotnet test` → minden teszt zöld

## Tanúsítás (Evidence)

- Fájl: `CabinetBilder.slnx` (solution fájl az új projekt-névvel)
- Fájl: `CabinetBilder.Core/CabinetBilder.Core.csproj`
- Fájl: `CabinetBilder.SpaceOsBridge/CabinetBilder.SpaceOsBridge.csproj`
- Fájl: `CabinetBilder.Adapter.AutoCAD/CabinetBilder.Adapter.AutoCAD.csproj`
- Build: `dotnet build` → Success
- Teszt: `dotnet test` → All green

## Megjegyzések (Notes)

- **Blokkoló fázis:** Ez az alap — minden következő feladat erre épül.
- Az architektúra dokumentum §6.2 adja a célstruktúrát.
- A névtér-átnevezésnél `global using` aliasokat érdemes először bevezetni a törések elkerülése érdekében.
- Az AutoCAD plugin csak `CabinetBilder.Adapter.AutoCAD`-ből töltődik be (`.bundle` csomagolás változatlan).
- Kapcsolódó ADR: CAD-ADR-001 (v1-ből)

---

**Started:**
**Completed:**
**Duration:**
**Owner:**
