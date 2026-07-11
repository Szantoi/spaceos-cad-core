# Task ID: 0008

# Title: Integrate Overrule-based Property Surface

# Category: feature

# Milestone: 16

# Status: new

## Szándék (Intent)

Natívabb szerkesztési élményhez overrule alapú property surface integráció előkészítése és prototípus implementálása.

## Elfogadási kritérium (Acceptance Criteria)

- [ ] Overrule integráció technikai terv elkészítése
- [ ] Legalább egy prototípus objektumtípus támogatása
- [ ] Smart object mezők property-szerű szerkesztési útvonalának validálása
- [ ] Build sikeres (`dotnet build -c Release`)

## Tanúsítás (Evidence)

- Kód: AutoCAD overrule integration komponensek
- Manuális ellenőrzés: property surface szerkesztés működik prototípus szinten
- Build: `dotnet build .\CabinetBilder.AutoCadScripts.slnx -c Release -nologo -v:minimal` -> Success

## Megjegyzések (Notes)

Komplexitás gyorsan nőhet, ezért fokozatos, feature-flag jellegű bevezetés javasolt.

---

**Started:**
**Completed:**
**Duration:**
**Owner:**
