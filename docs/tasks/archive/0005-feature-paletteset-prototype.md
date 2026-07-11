# Task ID: 0005

# Title: Build PaletteSet Prototype for Smart Objects

# Category: feature

# Milestone: 13

# Status: new

## Szándék (Intent)

Egy kezdeti PaletteSet prototípus létrehozása a kiválasztott smart object mezők megjelenítésére és szerkesztésére.

## Elfogadási kritérium (Acceptance Criteria)

- [ ] PaletteSet ablak prototípus létrehozása
- [ ] Kijelölt objektum metaadatainak megjelenítése
- [ ] Legalább egy mező szerkesztése és visszaírása
- [ ] Build sikeres (`dotnet build -c Release`)
- [ ] Alap működés manuális validációja AutoCAD-ben

## Tanúsítás (Evidence)

- Kód: `App.AutoCadScripts/` (UI integration)
- Kód: `App.AutoCadScripts.Application/UseCases/`
- Build: `dotnet build .\CabinetBilder.AutoCadScripts.slnx -c Release -nologo -v:minimal` -> Success
- Manuális ellenőrzés: PaletteSet megnyitás + szerkesztés valid

## Megjegyzések (Notes)

Kis lépésekben haladjunk: először read-only nézet, utána edit funkció.

---

**Started:**
**Completed:**
**Duration:**
**Owner:**
