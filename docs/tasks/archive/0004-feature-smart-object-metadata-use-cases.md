# Task ID: 0004

# Title: Extend Smart Object Metadata Use-Cases

# Category: feature

# Milestone: 12

# Status: new

## Szándék (Intent)

A SchemaID mellett további smart object metaadat mezők olvasása/írása use-case szinten bevezetésre kerül.

## Elfogadási kritérium (Acceptance Criteria)

- [ ] Metaadat mezőlista definiálása (pl. domain-specifikus kulcsok)
- [ ] Read/write use-case-ek létrehozása alkalmazási szinten
- [ ] Infrastructure tároló bővítése új mezők kezelésére
- [ ] Build sikeres (`dotnet build -c Release`)
- [ ] Tesztek sikeresek (`dotnet test -c Release`)

## Tanúsítás (Evidence)

- Kód: `App.AutoCadScripts.Application/UseCases/`
- Kód: `App.AutoCadScripts/Infrastructure/ObjectMetadata/`
- Teszt: `App.AutoCadScripts.Tests/`
- Build: `dotnet build .\CabinetBilder.AutoCadScripts.slnx -c Release -nologo -v:minimal` -> Success
- Teszt: `dotnet test .\App.AutoCadScripts.Tests\App.AutoCadScripts.Tests.csproj -c Release -nologo -v:minimal` -> All passed

## Megjegyzések (Notes)

A tranzakciós határok maradjanak explicit módon a command execution környezetben.

---

**Started:**
**Completed:**
**Duration:**
**Owner:**
