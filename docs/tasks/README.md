# Task Workflow

Feladatkezelési munkafolyamat a Cabinet Bilder projekthez. Az itt található dokumentumok az aktuális fejlesztési lépéseket, az in-progress munkafolyamatokat és az archivált feladatokat követik nyomon.

## Munkafolyamat szerkezete

```
new/        → active/       → archive/
(bejövő)      (aktuális)      (lezárult)
```

### 📋 new/ — Új feladatok

Az új feladatok ide kerülnek, amikor:

- A `Codebase_Status.md`-ben új mérföldkővet definiálunk
- Egy nagyobb epik lebontásra kerül részfeladatokra
- Váratlan hibát azonosítunk

**Intézmény:** Dokumentálás, felhasználók, érdekelt felek

### 🚀 active/ — Aktív feladatok

Az aktuális fejlesztési ciklus feladatai. Ezek azok az elemek, amelyeken jelenleg dolgozunk vagy hamarosan kezdjük.

**Max. 3-4 aktív feladat** ajánlott párhuzamos munka elkerülésére.

### ✅ archive/ — Lezárult feladatok

Befejezett vagy érvénytelenített feladatok. Hivatkozási érték a történethez.

## Feladatfájl szerkezete

Minden feladat egy Markdown fájl az alábbi elnevezéssel:

```
NNNN-{kategória}-{rövid-leírás}.md
```

Ahol:

- `NNNN` — sorszám (0001, 0002, ..., 0009, ...)
- `{kategória}` — `feature`, `bugfix`, `refactor`, `docs`, `test`
- `{rövid-leírás}` — 2-3 szavas azonosság (kötőjel-szeparált)

### Példák

- `0001-feature-application-handlers.md`
- `0002-test-picker-logic.md`
- `0003-refactor-command-results.md`
- `0004-docs-api-reference.md`

## Feladatfájl formátuma

```markdown
# Task ID: 0001
# Title: Create Application Service Layer (Handlers)
# Category: feature
# Milestone: 9
# Status: active

## Szándék (Intent)

Mit szeretnénk elérni? (1-2 mondat)

## Elfogadási kritérium (Acceptance Criteria)

- [ ] Subcriteria 1
- [ ] Subcriteria 2
- [ ] Subcriteria 3

## Tanúsítás (Evidence)

Miként ellenőrizzük, hogy kész?

- Kód: `App.AutoCadScripts.Application/UseCases/`
- Teszt: `App.AutoCadScripts.Tests/Application/`
- Build: `dotnet build -c Release` → Success
- Teszt: `dotnet test -c Release` → All green

## Megjegyzések (Notes)

Bármilyen kontextus, függőség, vagy korlát.

---

**Started:** 2026-04-22
**Completed:**
**Duration:**
**Owner:** (opcionális)
```

## Munkafolyamat lépések

### 1. Új feladat létrehozása

```bash
# new/ könyvtárban
# Név: NNNN-{kategória}-{leírás}.md
cat > 0005-feature-palette-set-ui.md << 'EOF'
# Task ID: 0005
# Title: Implement PaletteSet UI Prototype
# Category: feature
# Milestone: 13
# Status: new

## Szándék

...
EOF
```

### 2. Feladat aktiválása

Amikor egy feladat megkezdésre kerül:

1. Fájl átmozgatása `new/` → `active/` könyvtárba
2. `Status:` mező frissítése `active`-re
3. `Started:` dátum beállítása (YYYY-MM-DD)

```bash
mv docs/tasks/new/0005-feature-palette-set-ui.md \
   docs/tasks/active/0005-feature-palette-set-ui.md
```

Szerkesztés:

```markdown
# Status: active
**Started:** 2026-04-22
```

### 3. Feladat lezárása

Amikor a feladat kész (összes elfogadási kritérium teljesült):

1. Fájl átmozgatása `active/` → `archive/` könyvtárba
2. `Status:` mező frissítése `completed`-re (vagy `cancelled` ha érvénytelen)
3. `Completed:` dátum beállítása
4. `Duration:` kiszámítása (nem kötelező, de javasolt)

```bash
mv docs/tasks/active/0005-feature-palette-set-ui.md \
   docs/tasks/archive/0005-feature-palette-set-ui.md
```

Szerkesztés:

```markdown
# Status: completed
**Completed:** 2026-05-10
**Duration:** 18 days
```

## Integrációs szabályok

### Kapcsolat a Codebase_Status.md-vel

- A `Codebase_Status.md`-ben definiált mérföldkövek (milestones 1-16) a top-level tervek.
- Minden milestone részfeladatokra bontható (0001, 0002, ...).
- Milestone-szintű teszt/build ellenőrzést követően a feladatok `completed` állapotba kerülnek.

### Szlogen

Codebase_Status.md a "KI/MIRŐL" tervei.
A tasks/ könyvtár a "HOGYAN/MIKOR" megvalósítási naplója.
Ha kérdésed van egy feladattal kapcsolatban:

1. Tekintsd meg a "Megjegyzések" szakaszt
2. Nézd meg az előző hasonló feladatokat az `archive/` könyvtárban
3. Frissítsd a `Codebase_Status.md`-t, ha a scope vagy célok változnak

---

**Verzió:** 1.0
**Utolsó frissítés:** 2026-04-22
**Fenntartó:** GitHub Copilot / C# Expert Agent
