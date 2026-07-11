# Quality Development Workflow

Ez a dokumentum a projektben használt minőségi fejlesztési munkafolyamatot rögzíti a jelenleg elérhető eszközökkel.

## 1. Cél

A fejlesztés minden iterációban legyen:

- üzletileg releváns,
- technikailag helyes,
- tesztelt,
- biztonsági szempontból ellenőrzött,
- dokumentált és visszakövethető.

## 2. Források és Eszközök

- Feladatkezelés: `docs/tasks/` (`new/`, `active/`, `archive/`)
- Projekt státusz és roadmap: `Codebase_Status.md`
- Architektúra minta referencia (ajánlott): `docs/Architecture_Patterns_Collection.md`
- CAD minta referencia (ajánlott): `docs/CAD Fejlesztési Architektúra és Alapelvek.md`
- Build/Test: `dotnet build`, `dotnet test`
- Statikus biztonsági ellenőrzés: Semgrep (`semgrep.exe`, semgrep MCP)
- Dokumentáció lookup: Context7 MCP
- Kiegészítő kutatás: Brave Search MCP

## 3. End-to-End Fejlesztési Ciklus

### 3.1 Task előkészítés

1. A roadmap alapján új feladat létrehozása a `docs/tasks/new/` mappában.
2. Feladat aktiválása: áthelyezés `docs/tasks/active/` mappába.
3. Elfogadási kritériumok ellenőrzése, scope tisztázása.

Kimenet:

- Egyértelműen definiált task, mérhető acceptance criteria listával.

### 3.2 Technikai tervezés és kontextusfrissítés

1. Érintett fájlok és rétegek feltérképezése (Core, Infrastructure, Commands, Tests).
2. Szükséges minták kiválasztása (SRP, DIP, Repository, Use-case handler).
3. Ismeretlen library/API esetén Context7 használata dokumentációhoz.

Kimenet:

- Rövid, validált technikai megközelítés és módosítási lista.

### 3.3 Implementáció

1. Kódmódosítás kis, célzott lépésekben.
2. Réteghatárok betartása:
   - Core: domain logika, AutoCAD-független
   - Infrastructure: AutoCAD specifikus perzisztencia/integráció
   - Commands: orchestráció, vékony belépési pont
3. Null-check, specifikus exception, minimális mellékhatás.

Kimenet:

- Fordítható, konzisztens, clean code stílusú változtatás.

### 3.4 Unit tesztelés

1. Új/érintett viselkedéshez célzott MSTest tesztek írása.
2. AAA minta követése (Arrange, Act, Assert).
3. Determinisztikus tesztek, mellékhatások minimalizálása.

Kimenet:

- Regressziót fogó, olvasható tesztcsomag.

### 3.5 Lokális quality gate futtatás

Futtatandó minimum parancsok:

```powershell
dotnet build .\CabinetBilder.AutoCadScripts.slnx -c Release -nologo -v:minimal
dotnet test .\App.AutoCadScripts.Tests\App.AutoCadScripts.Tests.csproj -c Release -nologo -v:minimal
```

Kimenet:

- Build zöld, tesztek zöldek.

### 3.6 Security és code audit

1. Semgrep futtatás biztonsági és code quality szabályokkal.
2. Találatok triage: true positive, false positive, accepted risk.
3. Kritikus és magas találatok javítása még merge előtt.

Példa:

```powershell
&"C:\Users\szant\.local\bin\semgrep.exe" --config=p/csharp --config=p/security-audit --config=p/owasp-top-ten .
```

Kimenet:

- Ellenőrzött biztonsági státusz, dokumentált döntésekkel.

### 3.7 Dokumentáció és zárás

1. Frissíteni a releváns dokumentációt (`Codebase_Status.md`, task fájl).
2. Task áthelyezése `active/` -> `archive/` ha minden acceptance criterion teljesült.
3. Rövid changelog jellegű összegzés készítése (mi változott, miért, mivel validáltuk).

Kimenet:

- Visszakövethető, auditálható lezárás.

## 4. Definition of Done

Egy feladat akkor tekinthető késznek, ha:

- [ ] Minden acceptance criterion teljesült
- [ ] Build sikeres Release módban
- [ ] Tesztcsomag sikeres
- [ ] Semgrep ellenőrzés lefutott és eredmény triage megtörtént
- [ ] Dokumentáció frissítve
- [ ] Task archiválva

## 5. Rövid Működési Szabályok

- Kis, inkrementális változtatások.
- Egyszerre egy aktív fókusz feladat.
- Minden döntés legyen technikailag indokolt és visszakereshető.
- A quality gate kihagyása nem megengedett.
- Ha bizonytalanság van, előbb mérés/ellenőrzés, utána implementáció.

## 6. Ajánlott Heti Ritmus

1. Tervezés és priorizálás (`Codebase_Status.md` + `docs/tasks/new/`).
2. Kivitelezés és review (`docs/tasks/active/`).
3. Quality gate + audit (build/test/semgrep).
4. Lezárás és dokumentálás (`docs/tasks/archive/`).

## 7. Napi Munkafolyamat

### 7.1 Napi indítás (15-20 perc)

1. Átnézni a `docs/tasks/active/` feladatokat.
2. Ellenőrizni a `Codebase_Status.md` rövid távú célait.
3. Kiválasztani a napi fókuszfeladatot (1 fő fókusz).
4. Meghatározni a napi "kész" kritériumot.

Kimenet:

- Egyértelmű napi fókusz és mérhető napi eredmény.

### 7.2 Napi kivitelezés (2-3 fókuszblokk)

1. Implementáció kis lépésekben.
2. Folyamatos önellenőrzés: compile, logika, edge case.
3. Unit tesztek frissítése az érintett viselkedésre.

Kimenet:

- Stabil, részben vagy teljesen lezárt változtatás.

### 7.3 Napi zárás (20-30 perc)

1. Build + test futtatás.
2. Szükség esetén Semgrep gyorsellenőrzés.
3. Task állapot frissítése a `docs/tasks/` alatt.
4. Rövid státuszfrissítés a `Codebase_Status.md` fájlban, ha mérföldkőhaladás történt.

Kimenet:

- Napi állapot visszakövethetően dokumentálva.

## 8. Sprint Munkafolyamat

### 8.1 Sprint tervezés

1. A `Codebase_Status.md` alapján sprint cél kiválasztása.
2. A cél felbontása konkrét taskokra a `docs/tasks/new/` mappában.
3. Sprint scope rögzítése: mi fér bele, mi nem.

Kimenet:

- Priorizált, reális sprint backlog.

### 8.2 Sprint végrehajtás

1. Taskok mozgatása `new/` -> `active/` állapotba.
2. Minden tasknál kötelező quality gate (build/test), szükség esetén Semgrep.
3. Folyamatos dokumentációfrissítés a releváns fájlokban.

Kimenet:

- Inkrementálisan szállított, validált eredmények.

### 8.3 Sprint zárás

1. Minden kész task mozgatása `active/` -> `archive/`.
2. Sprint eredmények összegzése a `Codebase_Status.md` fájlban.
3. Következő sprinthez kockázatok és tanulságok rögzítése.

Kimenet:

- Zárt sprint, tiszta kiinduló állapot a következő iterációhoz.

---

Utolsó frissítés: 2026-04-22
