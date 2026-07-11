# Task ID: 0017
# Title: Skeleton Component Logic (Geometry/BOM compute)
# Category: feature
# Milestone: 2
# Status: active

## Szándék (Intent)

A `Skeleton` aggregátum kiterjesztése olyan logikával, amely lehetővé teszi fizikai alkatrészek (panelek) automatikus származtatását a paraméterek alapján, valamint a BOM (anyagjegyzék) pontos kiszámítását.

## Elfogadási kritérium (Acceptance Criteria)

- [ ] `Skeleton` aggregátum kiterjesztése `Rebuild()` vagy hasonló metódussal, amely frissíti a `Components` listát a paraméterek alapján.
- [ ] Alapvető korpusz-logika implementálása (tető, fenék, oldalak, hátfal méretezése).
- [ ] Anyagvastagság (Thickness) paraméter kezelése a számítások során.
- [ ] `ComputeBom()` metódus implementálása, amely `BomLine` objektumokat ad vissza.
- [ ] Unit tesztek a méret-számítási helyesség ellenőrzésére.

## Tanúsítás (Evidence)

- Kód: `CabinetBilder.Core/Skeletons/Skeleton.cs`
- Teszt: `CabinetBilder.Tests/Skeletons/SkeletonLogicTests.cs`
- Build: `dotnet build` → Success
- Teszt: `dotnet test` → All green

## Megjegyzések (Notes)

A geometria számítás során figyelembe kell venni a szerkezeti irányelveket (pl. "oldalak közé zárt tető" vs "tető az oldalakon"). Kezdetben egy fix, konfigurálható stratégiát alkalmazunk.

---

**Started:** 2026-04-24
**Completed:**
**Duration:**
**Owner:** Antigravity
