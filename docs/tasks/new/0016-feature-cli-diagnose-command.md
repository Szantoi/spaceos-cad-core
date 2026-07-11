# Task ID: 0016
# Title: Fázis 11 — CLI Projekt + `diagnose` parancs (DB-12)
# Category: feature
# Milestone: 16 (Arch Vision v2 — Fázis 11)
# Status: new
# Source: CabinetBilder_Architecture_Vision_v2.md §15.2, §6.2, DB-12

## Szándék (Intent)

Létrehozni a `CabinetBilder.Cli` projektet és implementálni az összes v1+v2 CLI parancsot,
különös tekintettel az új `diagnose` parancsokra (DB-12), amelyek a lokális állapotot
gép-olvasható JSON formátumban exportálják — support-ticket és LLM diagnosztika céljára.

## Elfogadási kritérium (Acceptance Criteria)

### CabinetBilder.Cli projekt
- [ ] `CabinetBilder.Cli` console project létrehozva (`net10.0`, nincs AutoCAD-függőség)
- [ ] `System.CommandLine` NuGet integrálva (parancs-parsing)
- [ ] DI container: `CabinetBilder.SpaceOsBridge` + `CabinetBilder.Core` hivatkozásokkal

### Alap CLI parancsok (v1 örökség)
- [ ] `cabinetbilder login` — Device Code flow triggerelés
- [ ] `cabinetbilder templates pull` — TemplateCache frissítés
- [ ] `cabinetbilder materials list --category <cat>` — cache listázás
- [ ] `cabinetbilder bom submit --drawing <path>` — OutboxQueue enqueue + immediate flush

### Diagnose parancsok (ÚJ v2, DB-12, §15.2)
- [ ] `cabinetbilder diagnose` — pretty-print lokális állapot (console táblás kimenet)
- [ ] `cabinetbilder diagnose --outbox` — csak outbox lista (érzékeny mezők redaktálva)
- [ ] `cabinetbilder diagnose --outbox-history --days 7` — elmúlt N nap outbox
- [ ] `cabinetbilder diagnose --export support-bundle.json` — gép-olvasható JSON export

### JSON export séma (§15.2 kódminta alapján)
- [ ] `plugin_version` mező
- [ ] `runtime` (.NET verzió)
- [ ] `active_tenant` (id, type, login_age_hours)
- [ ] `connection_state` (Online/Offline/Unauthenticated)
- [ ] `local_store.schema_version`, `local_store.integrity_check`
- [ ] `local_store.template_cache_count`, `local_store.material_cache_count`, `local_store.seen_guids_count`
- [ ] `local_store.outbox.pending`, `outbox.succeeded_last_30d`, `outbox.failed`
- [ ] `token_storage.tenants_configured`, `token_storage.refresh_expires_in_days`
- [ ] `last_sync_at`
- [ ] **Sensitive mezők redaktálva** (`"[REDACTED]"`) — tokenek, árak NEM kerülnek exportba

### Admin parancsok
- [ ] `cabinetbilder admin clear-cache` — TemplateCache + MaterialCache törlése
- [ ] `cabinetbilder admin clear-seen-guids` — SeenSmartObjectGuids törlése (copy-paste reset)

### Tesztek
- [ ] `DiagnoseCommandTests` — mock ILocalStore + mock IConnectionState: helyes JSON struktúra
- [ ] `DiagnoseCommandTests` — sensitive mezők redaktálva a kimeneten
- [ ] CLI E2E teszt: `cabinetbilder diagnose --export out.json` → valid JSON, schema-ellenőrzés

## Tanúsítás (Evidence)

- Fájl: `CabinetBilder.Cli/CabinetBilder.Cli.csproj`
- Fájl: `CabinetBilder.Cli/Commands/DiagnoseCommand.cs`
- Fájl: `CabinetBilder.Cli/Commands/LoginCommand.cs`
- Fájl: `CabinetBilder.Cli/Commands/TemplatesCommand.cs`
- Fájl: `CabinetBilder.Tests/Cli/DiagnoseCommandTests.cs`
- Build: `dotnet build` → 0 error
- Teszt: `dotnet test` → All green
- Kézi: `cabinetbilder diagnose --export test.json` → valid JSON, LLM-nek átadható

## Megjegyzések (Notes)

- **Blokkoló:** 0011 (SQLite), 0012 (Auth) és 0013 (Cache pull) szükséges (CLI mindhárom réteget hívja).
- A CLI parancsok **ugyanazokat** a `ILocalStore`, `ISpaceOsClient` portokat hívják, mint az AutoCAD plugin — tesztelhetőség bizonyítéka!
- DB-12: a `diagnose` parancs fő célja: egy support-kérő user átadja a JSON-t → LLM azonnali diagnózist ad.
- Az érzékeny adatok (tokenek, anyagárak) **soha nem kerülnek** a support-bundle-be.
- Manifesto T7 (API-first): minden use-case CLI-ből is elérhető.

---

**Started:**
**Completed:**
**Duration:**
**Owner:**
