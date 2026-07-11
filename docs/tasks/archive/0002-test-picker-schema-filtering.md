# Task 0002

**Title:** Add Tests for Type Picker and Schema Filtering

**Category:** test

**Milestone:** 10

**Status:** completed

## Szándék (Intent)

Növeljük a megbízhatóságot célzott tesztekkel a Type picker parsing logikára és a schema-aware szűrés viselkedésére.

## Elfogadási kritérium (Acceptance Criteria)

- [x] Type picker parsing logikára unit tesztek létrehozása
- [x] Érvénytelen inputokra (üres, whitespace, rossz index) tesztek létrehozása
- [x] Schema-aware filtering viselkedésre tesztek létrehozása
- [x] Build sikeres (`dotnet build -c Release`)
- [x] Tesztek sikeresek (`dotnet test -c Release`)

## Tanúsítás (Evidence)

- Kód: `App.AutoCadScripts.Core/FrontMatter/FrontMatterTypeInputParser.cs`
- Teszt: `App.AutoCadScripts.Tests/FrontMatter/FrontMatterTypeInputParserTests.cs`
- Kód: `App.AutoCadScripts.Core/SmartObjects/SmartObjectSchemaFilter.cs`
- Teszt: `App.AutoCadScripts.Tests/SmartObjects/SmartObjectSchemaFilterTests.cs`
- Build: `dotnet build .\CabinetBilder.AutoCadScripts.slnx -c Release -nologo -v:minimal` -> Success
- Teszt: `dotnet test .\App.AutoCadScripts.Tests\App.AutoCadScripts.Tests.csproj -c Release -nologo -v:minimal` -> All passed

## Megjegyzések (Notes)

A teszteknek deterministic módon kell futniuk, AutoCAD runtime-tól függetlenül ahol lehetséges.

---

**Started:** 2026-04-22
**Completed:** 2026-04-22
**Duration:** 1 day
**Owner:**
