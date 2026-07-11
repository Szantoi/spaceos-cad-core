# Task ID: 0006

# Title: Document Command to Use-Case Mapping

# Category: docs

# Milestone: 14

# Status: new

## Szándék (Intent)

Onboarding célból dokumentáljuk, hogy melyik command melyik use-case/handler komponenseket használja.

## Elfogadási kritérium (Acceptance Criteria)

- [ ] Mapping dokumentum létrehozása a docs mappában
- [ ] Minden aktív command felsorolása
- [ ] Minden commandhoz hozzárendelt use-case és infrastruktúra komponensek felsorolása
- [ ] Build sikeres (`dotnet build -c Release`)

## Tanúsítás (Evidence)

- Dokumentáció: `docs/`
- Build: `dotnet build .\CabinetBilder.AutoCadScripts.slnx -c Release -nologo -v:minimal` -> Success

## Megjegyzések (Notes)

A mapping segít a karbantarthatóságban és a gyors kontextusváltásban.

---

**Started:**
**Completed:**
**Duration:**
**Owner:**
