# Task ID: 0003

# Title: Introduce Structured Command Result Model

# Category: refactor

# Milestone: 11

# Status: archived

## Szándék (Intent)

Egységes, típusos eredménymodellt vezetünk be a command műveletekhez, hogy a hiba- és státuszkezelés átláthatóbb legyen.

## Elfogadási kritérium (Acceptance Criteria)

- [x] `CommandResult` (vagy `Result<T>`) modell létrehozása
- [x] Legalább egy command útvonal refaktorálása új eredménymodellre
- [x] Hibák és figyelmeztetések egységes reprezentációja
- [x] Build sikeres (`dotnet build -c Release`)
- [x] Tesztek sikeresek (`dotnet test -c Release`)

## Tanúsítás (Evidence)

- Kód: `App.AutoCadScripts.Core/` vagy `App.AutoCadScripts.Application/`
- Refaktor: `App.AutoCadScripts/Commands/DimensionCommands.cs`
- Build: `dotnet build .\CabinetBilder.AutoCadScripts.slnx -c Release -nologo -v:minimal` -> Success
- Teszt: `dotnet test .\App.AutoCadScripts.Tests\App.AutoCadScripts.Tests.csproj -c Release -nologo -v:minimal` -> All passed

## Megjegyzések (Notes)

Cél a felhasználó felé egységes és lokalizálható visszajelzési mechanizmus előkészítése.

---

**Started:** 2026-04-22
**Completed:** 2026-04-22
**Duration:** ~1 óra
**Owner:** agent
