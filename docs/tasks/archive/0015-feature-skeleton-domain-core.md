# Task ID: 0015
# Title: Fázis 6 — Skeleton Domain Core (in-memory + DWG XRecord persistence)
# Category: feature
# Milestone: 15 (Arch Vision v2 — Fázis 6)
# Status: active
# Source: CabinetBilder_Architecture_Vision_v2.md §7.2, §14, DB-11, CAD-ADR-019

## Szándék (Intent)

Implementálni a `Skeleton` aggregate-et a `CabinetBilder.Core.Skeleton` namespaceben.
A Skeleton a parametrikus bútortervezés központi domain-modellje — in-memory él a szerkesztés idején,
perzisztens forrása kizárólag a DWG XRecord + Extension Dictionary (nem SQLite!).
Ez az Arch Vision v2 egyik legfontosabb döntése (DB-11): **egyetlen igazságforrás = DWG**.

## Elfogadási kritérium (Acceptance Criteria)

### Core.Skeleton domain (§7.2, §14)
- [ ] `Skeleton` aggregate root osztály (DDD aggregate, nem EF entity)
- [ ] `SkeletonId` value object (Guid-alapú, immutable)
- [ ] `SkeletonComponent` domain objektum (panel, élelszalag, furatok, stb.)
- [ ] `SkeletonParameter` value object (kulcs-érték pár, típusos)
- [ ] `Skeleton.ApplyParameter(key, value)` — paraméter módosítás (validációval)
- [ ] `Skeleton.AddComponent(component)` — komponens hozzáadás
- [ ] `Skeleton.ComputeBom()` → `BomLine[]` — BOM kiszámítás (topológiai rendezés alapján, Kahn's algorithm jövőbeli)
- [ ] A `Skeleton` **NEM tartalmaz** SQLite-perzisztencia logikát

### DWG XRecord persist port (ICadSkeletonStore)
- [ ] `ICadSkeletonStore` port interfész (Core rétegben)
- [ ] `ICadSkeletonStore.ReadSkeletonAsync(objectHandle)` → `Skeleton?`
- [ ] `ICadSkeletonStore.WriteSkeletonAsync(objectHandle, skeleton)` → `Result`
- [ ] `AutoCadSkeletonStore : ICadSkeletonStore` implementáció az Adapter.AutoCAD rétegben
- [ ] XRecord írás/olvasás JSON-ban tárolva (Extension Dictionary alatt, saját app key)
- [ ] User AutoCAD-bezárás mentés nélkül → **változások elvesznek** (explicit dokumentálva, nem hiba)

### Skeleton lifecycle az AutoCAD-ban
- [ ] `CreateSkeletonCommand` — új Skeleton létrehozása SmartObjectra kötve
- [ ] `OpenSkeletonCommand` — SmartObject megnyitásakor Skeleton betöltése XRecord-ból
- [ ] `ApplySkeletonParameterCommand` — paraméter módosítás → in-memory + XRecord mentés
- [ ] UI integráció: Skeleton szerkesztő panel megjelenítése a palettán (alapszintű)

### Tesztek
- [ ] `SkeletonDomainTests` — pure unit: ApplyParameter, AddComponent, ComputeBom; AutoCAD-függőség nélkül
- [ ] `SkeletonPersistenceTests` — Mock ICadSkeletonStore-ral: round-trip read/write

## Tanúsítás (Evidence)

- Fájl: `CabinetBilder.Core/Skeleton/Skeleton.cs`
- Fájl: `CabinetBilder.Core/Skeleton/SkeletonId.cs`
- Fájl: `CabinetBilder.Core/Skeleton/SkeletonComponent.cs`
- Fájl: `CabinetBilder.Core/Ports/ICadSkeletonStore.cs`
- Fájl: `CabinetBilder.Adapter.AutoCAD/Persistence/AutoCadSkeletonStore.cs`
- Fájl: `CabinetBilder.Tests/Skeleton/SkeletonDomainTests.cs`
- Build + Teszt: `dotnet test` → All green
- Kézi: Skeleton létrehozása → DWG mentése → AutoCAD újraindítás → Skeleton visszatöltve XRecord-ból

## Megjegyzések (Notes)

- **Blokkoló:** 0010 (projekt-szerkezet) szükséges.
- **DB-11 döntés:** A Skeleton aggregate SOHA nem kerül SQLite-ba. Ez a döntés végleges (CAD-ADR-019).
- A `ComputeBom()` algoritmika (Kahn's sort) részletezése külön "Skeleton design deep-dive" sessionbe tartozik (§3.2).
- A Skeleton perzisztencia-döntés leegyszerűsíti a sync logikát: nincs 3-way merge (DWG + SQLite + szerver).
- Kapcsolódó ADR: CAD-ADR-019 (Skeleton = in-memory + DWG, nem SQLite)

---

**Started:** 2026-04-23
**Completed:**
**Duration:**
**Owner:** Antigravity
