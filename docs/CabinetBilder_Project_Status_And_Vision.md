# CabinetBilder — Projekt állapot és jövőkép

> **Dátum:** 2026-04-23  
> **Verzió:** 1.0  
> **Forrás:** `Codebase_Status.md` + `CabinetBilder_Architecture_Vision_v2.md`

---

## 1. Mi van jelenleg megvalósítva?

### 1.1 Projekt-struktúra (jelenlegi)

A projekt jelenleg 3 Visual Studio projektből áll, `.NET 10` runtime-on:

```
App.AutoCadScripts          ← AutoCAD plugin (UI, Commands, Handlers, Infra)
App.AutoCadScripts.Core     ← Domain logika (SmartObject, interfészek)
App.AutoCadScripts.Tests    ← MSTest tesztprojekt (~48 teszt)
```

### 1.2 SmartObject alrendszer

A plugin fő funkciója: az AutoCAD rajzokban lévő "okos objektumokhoz" (SmartObject) metaadatokat rendelni és azokat kezelni.

- **XData tárolás:** A SmartObject metaadatok (kulcs-érték párok) az AutoCAD DWG fájlba mentődnek `XData` (Extended Entity Data) formátumban — nem külső adatbázisba.
- **Schema validáció:** A metaadatmező-definíciók JSON sémával ellenőrizhetők (`SmartObjectSchema`, `SmartObjectSchemaFilter`).
- **CRUD műveletek:** `ReadSmartObjectMetadataUseCase`, `WriteSmartObjectMetadataUseCase` — a meglévő use-case réteg teljesen működik.
- **SHA256 verziókövetés:** Minden SmartObject tartalmaz egy content-hash alapú verziót (`ComputeHash`) a szinkronizáció alapjaként.
- **SyncStatus:** Az objektumokon nyomon követhető az állapot: `Outdated`, `Conflict`, `LocalOnly`, `Synced`.

### 1.3 Anyagkatalógus (helyi SQLite)

- **SQLite + EF Core:** A anyagkatalógus (Material catalog) egy helyi SQLite adatbázisban él (`%LOCALAPPDATA%\CabinetBilder\`).
- **Teljes CRUD:** Anyagok (panel, élelszalag, furatok) hozzáadása, szerkesztése, törlése.
- **CatalogManagementWindow:** WPF-alapú felhasználói ablak az anyagkatalógus kezeléséhez.
- **SqliteMaterialRepository:** Repository pattern, EF Core `DbContext`-tel.

### 1.4 AutoCAD Palette UI

- **SmartObjectPalette:** Beágyazott AutoCAD paletta (WPF `PaletteSet`) a SmartObject metaadatainak megjelenítéséhez és szerkesztéséhez.
- **MVVM architektúra:** `SmartObjectPaletteViewModel`, `MetadataFieldViewModel` — binding-ready view modellek.
- **Szinkronizációs UI:** Push / Pull gombok és státuszjelzők a palettán (Conflict, Outdated, LocalOnly).
- **Multi-select szerkesztés:** Több objektum egyidejű szerkesztése (közös és egyedi mezők kezelése).

### 1.5 MediatR Command/Query szétválasztás

- **MediatR 12.4.1** integrálva — minden domain-művelet kérésobjektumon keresztül megy.
- **Handlers:** `PushMetadataHandler`, `PullMetadataHandler`, `CheckSyncStatusHandler`, `GetSmartObjectMetadataHandler`, `UpdateSmartObjectMetadataHandler`.
- **Ardalis.Result** pattern — egységes hibakezelés az összes handler kimenetelén.

### 1.6 Elosztott szinkronizáció (szerver integráció)

- **PostgreSQL:** `SmartObjectEntity` (composit kulcs: DrawingId + Handle) a szerver-oldali metaadat-tároláshoz.
- **Redis pessimistic locking:** Atomikus Lua szkriptekkel megvalósított zárolás a konkurens szerkesztés ellen.
- **Push/Pull workflow:** Git-stílusú commit és szinkronizálás — a user manuálisan indítja.
- **Optimistic concurrency:** Verzió-összehasonlítás push előtt, konfliktusnál hibajelezes.

### 1.7 Infrastruktúra

- **MinIO blob storage:** `MinioStorageService` fájl feltöltéséhez/letöltéséhez.
- **Redis service:** `RedisService` (`StackExchange.Redis 2.8.24`).
- **SmartObject Overrule-ok:** AutoCAD Overrule rendszer a natív grip és context-menü megjelenítéséhez SmartObject-eken.
- **DI kontainer:** Centralizált `IServiceProvider` — MediatR, ViewModelek, Infrastructure automatikusan feloldva.

### 1.8 Tesztek

- **~48 MSTest teszt** — lefedi: FrontMatter parsing, SmartObject metaadat read/write, Schema validáció, ViewModel logika.
- Build: `dotnet build` → **0 error** (2026-04-23 állapot szerint).

---

## 2. Mi lesz a projektben a feladatok elvégzése után?

### 2.1 Projekt-struktúra (jövő)

A `0010` feladat elvégzése után az egész projekt átrendeződik a `CabinetBilder.*` névtér-hierarchiába:

```
CabinetBilder.Core                   ← Domain + portok (AutoCAD-független)
│   ├── SmartObjects/                    (jelenlegi SmartObject logika, átnevezve)
│   ├── Skeleton/                        (ÚJ: parametrikus bútor aggregate)
│   └── Sync/                            (ÚJ: ILocalStore, ISpaceOsClient portok)
│
CabinetBilder.SpaceOsBridge          ← Szerver + helyi tárolás implementációk
│   ├── LocalStore/                      (ÚJ: SqliteLocalStore, SchemaMigrator, Migrations)
│   ├── Auth/                            (ÚJ: DeviceCodeAuthenticator)
│   ├── TokenStorage/                    (ÚJ: DpapiTokenStore, tenant manifest)
│   ├── Http/                            (ÚJ: HttpSpaceOsClient)
│   ├── Outbox/                          (ÚJ: OutboxQueue, OutboxProcessor)
│   └── StateMonitor/                    (ConnectionStateMonitor)
│
CabinetBilder.Adapter.AutoCAD        ← AutoCAD-specifikus adapter (csak itt függ az API-tól)
│   ├── Commands/                        (jelenlegi Commands, átnevezve + OnDrawingOpenedHandler)
│   ├── Persistence/                     (ÚJ: AutoCadSkeletonStore — DWG XRecord)
│   └── UI/                              (jelenlegi WPF palette, átnevezve)
│
CabinetBilder.Cli                    ← ÚJ: Parancssori eszköz
│   └── Commands/                        (login, templates pull, diagnose, admin)
│
CabinetBilder.Tests                  ← Tesztek (bővített)
    ├── LocalStore/                      (ÚJ: SqliteLocalStore, SchemaMigrator, OutboxLeader)
    ├── Outbox/                          (ÚJ: OutboxQueue, GuidCollision)
    ├── Skeleton/                        (ÚJ: Skeleton domain tesztek)
    └── Cli/                             (ÚJ: DiagnoseCommand tesztek)
```

### 2.2 Helyi SQLite adatbázis (`client.db`) — `0011`

Az anyagkatalógus mellé egy **teljes körű lokális perzisztencia-réteg** kerül a `%APPDATA%\CabinetBilder\client.db` fájlban:

| Tábla | Tartalom |
|-------|----------|
| `OutboxQueue` | Offline pufferelt szinkronizációs műveletek (BOM, hash submit) |
| `TemplateCache` | Szerver-ről letöltött ProductTemplate-ek, ETag-gel |
| `MaterialCache` | Anyagkatalógus + tenant-specifikus **titkosított árak** |
| `SeenSmartObjectGuids` | Guid-kollízió detekción nyomon követett objektumok |

**Technikai jellemzők:**
- WAL mode — több AutoCAD instance egyszerre olvashat
- `PRAGMA busy_timeout = 5000` — 5 másodperces várakozás íráskonfliktus esetén
- Előre-csak (forward-only) séma migráció — `PRAGMA user_version` alapján
- Induláskor integritásellenőrzés — sérült DB esetén automatikus cache-rebuild

### 2.3 Bejelentkezés és biztonság — `0012`

- **Keycloak Device Code Flow:** A felhasználó a böngészőben hitelesíti magát — az AutoCAD command line-on megjelenik a link/QR-kód.
- **Per-tenant token tárolás:** Minden SpaceOS tenant-hoz külön, **DPAPI-titkosított** `.token.dpapi` fájl a `%APPDATA%\CabinetBilder\tokens\` mappában.
- **Multi-tenant support:** Egy gépen párhuzamosan több SpaceOS bérlő (pl. céges + személyes fiók).
- **Refresh token race protection:** Egyszerre csak egy token-megújítás futhat.
- **Mező-szintű titkosítás:** Az anyagárak és érzékeny outbox payloadok DPAPI-vel titkosítva a lokális DB-ben.

### 2.4 Template és anyagkatalógus szinkronizáció — `0013`

- **ETag-alapú pull:** A plugin csak akkor tölt le újabb adatot a szerverről, ha az valóban változott (HTTP `304 Not Modified` optimalizáció).
- **Offline-türelmes cache:** Ha nincs internet, a lejárt cache még **használható** — a felhasználó munkája nem akad meg.
- **Titkosított árak:** A tenant-specifikus anyagárak lokálisan DPAPI-val titkosítva tárolódnak.

### 2.5 BOM push és Guid-kollízió kezelés — `0014`

- **Offline-pufferelt BOM submit:** A Bill of Materials (vágási lista) feltöltése offline módban is működik — az OutboxQueue puffereli, és visszakapcsoláskor automatikusan elküldi.
- **Csak az egyik AutoCAD instance küld:** A `Global\CabinetBilder.OutboxLeader` named mutex biztosítja, hogy párhuzamos AutoCAD-futtatás esetén csak egy példány végez HTTP submit-et.
- **Guid-kollízió detekció ("Save As" védelem):**
  - Ha a felhasználó egy rajzot más névvel ment el (Save As), és mindkét rajzban ugyanaz a SmartObject Guid szerepel, a plugin **automatikusan új Guid-ot generál** a másolatban.
  - A felhasználó toast-értesítést kap, és a másolat önállóan szinkronizálódik.
- **30 napos retention:** A sikeres outbox bejegyzések 30 nap után automatikusan törlődnek.

### 2.6 Skeleton domain — parametrikus bútor modell — `0015`

- **Skeleton aggregate:** A parametrikus bútortervezés central domain modellje — panelekből, élelszalagokból, furatokból épül fel.
- **Kizárólag DWG-ben él:** A Skeleton állapota az AutoCAD DWG fájl Extension Dictionary-jába (XRecord) mentődik — nem a SQLite-ba. A DWG az **egyetlen igazságforrás**.
- **BOM kiszámítás:** A Skeleton-ból automatikusan generálható a BOM (anyaglista), amelyből vágási lista küldhető a szerverre.
- **User-élmény:** "Mentsd el a rajzot, mint mindig" — nincs extra mentési lépés, nincs két source of truth.

### 2.7 Parancssori eszköz (CLI) — `0016`

Egy önálló `cabinetbilder.exe` parancs lesz elérhető a plugin mellé telepítve:

```bash
cabinetbilder login                              # Keycloak bejelentkezés
cabinetbilder templates pull                     # Template cache frissítés
cabinetbilder materials list --category panel    # Anyagkatalógus listázás
cabinetbilder bom submit --drawing file.dwg      # BOM feltöltés DWG-ből

# ÚJ v2 diagnosztika:
cabinetbilder diagnose                           # Teljes helyi állapot
cabinetbilder diagnose --outbox                  # Pending szinkronizációk
cabinetbilder diagnose --export bundle.json      # Support/LLM export

# Admin:
cabinetbilder admin clear-cache                  # Cache törlése
cabinetbilder admin clear-seen-guids             # Guid-tracker reset
```

A `diagnose --export` egy strukturált JSON-t ad ki, amelyet közvetlenül át lehet adni egy LLM-nek (`"Miért nem szinkronizál a plugin?"`).

---

## 3. Összehasonlítás: Előtte ↔ Utána

| Képesség | Jelenlegi állapot | Feladatok után |
|----------|-------------------|----------------|
| **Projekt-struktúra** | `App.AutoCadScripts.*` (monolith plugin) | `CabinetBilder.*` (Core / Bridge / Adapter / CLI) |
| **Helyi adatbázis** | SQLite anyagkatalógus (EF Core) | SQLite `client.db` — Outbox + Cache + Guid-tracker + anyagkatalógus |
| **Bejelentkezés** | Nincs (közvetlen szerver-hívás) | Keycloak Device Code Flow, per-tenant DPAPI token |
| **Multi-tenant** | Nincs | Igen — több SpaceOS bérlő egyidejűleg |
| **Offline működés** | Részleges (helyi XData) | Teljes — cache + outbox offline-türelmes |
| **Template szinkronizáció** | Nincs cache | ETag-alapú pull, 24h TTL, offline fallback |
| **Anyagárak biztonsága** | Nincs titkosítás | DPAPI mező-szintű titkosítás |
| **BOM feltöltés** | Közvetlen HTTP (online kötelező) | Outbox puffer — offline is elfogad, online küldi el |
| **Guid-ütközés kezelés** | Nincs | Automatikus detekció + regenerálás "Save As" esetén |
| **Skeleton / parametrikus modell** | Nincs | In-memory aggregate + DWG XRecord persistence |
| **Parancssori eszköz** | Nincs | Teljes CLI — login, pull, submit, diagnose |
| **Multi-CAD-instance** | Veszélyes (race condition) | WAL + named mutex leader election |
| **Diagnosztika** | Log fájlok | Gép-olvasható JSON export, LLM-barát |
| **Tesztek** | ~48 teszt | ~48 + ~30 új teszt (SQLite, Outbox, Skeleton, CLI) |

---

## 4. Mi NEM változik / szándékosan kizárva

| Terület | Miért marad változatlan |
|---------|------------------------|
| **Skeleton a SQLite-ban** | Szándékosan TILTOTT (CAD-ADR-019) — a DWG az egyetlen igazságforrás a user munkájára |
| **AutoCAD bundle formátum** | Az `.arx`/`.dll` bundle-csomagolás nem változik |
| **XData mint SmartObject-tárolás** | A meglévő DWG-kompatibilitás megmarad |
| **User munkafolyamat** | "Mentsd a rajzot, ahogy mindig" — nincs plusz kötelező lépés |

---

*Készítette: Antigravity (Arch Vision v2 alapján) — 2026-04-23*
