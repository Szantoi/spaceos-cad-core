# Task ID: 0011
# Title: Fázis 1.5 — Local Storage Foundation (SQLite + Migrations + WAL + Integrity Check)
# Category: feature
# Milestone: 11 (Arch Vision v2 — Fázis 1.5 — KRITIKUS ÚJ FÁZIS)
# Status: new
# Source: CabinetBilder_Architecture_Vision_v2.md §9 (teljes szakasz), DB-01, DB-03, DB-04, DB-07, DB-10

## Szándék (Intent)

Megalapozni a lokális SQLite perzisztencia-réteget a `CabinetBilder.SpaceOsBridge` projektben.
Ez az Arch Vision v2 **legfontosabb új fázisa** — a v1-ben csak említett lokális tárolást DDL-szintig specifikálja,
és a következő összes fázis blokkolója (Auth, BOM push, Guid-tracking mind erre épül).

## Elfogadási kritérium (Acceptance Criteria)

### ILocalStore port (Core.Sync)
- [ ] `CabinetBilder.Core.Sync.ILocalStore` interfész létrehozva (§7.3 alapján)
- [ ] `OutboxEntry` rekord definiálva (Operation, PayloadJson, EncryptedPayloadDpapi, Status, RetryCount, stb.)
- [ ] `SeenGuidInfo` rekord definiálva
- [ ] `OutboxStatus` enum létrehozva (`Pending`, `InProgress`, `Succeeded`, `Failed`)
- [ ] `OutboxOperation` enum létrehozva

### SQLite schema — DDL (§9.2–9.5)
- [ ] `OutboxQueue` tábla DDL (§9.2) — Id, Operation, PayloadJson, EncryptedPayloadDpapi, Status, RetryCount, CreatedAt, LastAttemptAt, LastErrorMessage, CompletedAt; CHECK constraintekkel (1MB cap, XOR PayloadJson/Encrypted)
- [ ] `TemplateCache` tábla DDL (§9.3) — ETag, ExpiresAt, TenantId, Version
- [ ] `MaterialCache` tábla DDL (§9.4) — PriceEncryptedDpapi mező (DB-09)
- [ ] `SeenSmartObjectGuids` tábla DDL (§9.5)
- [ ] Minden index létrehozva (§9.7): IX_OutboxQueue_Status_CreatedAt, IX_OutboxQueue_CompletedAt, UX_TemplateCache_Name_TenantId, UX_MaterialCache_MaterialCode_TenantId, IX_TemplateCache_ExpiresAt, IX_MaterialCache_ExpiresAt

### SchemaMigrator (DB-03, §9.8)
- [ ] `ISchemaMigration` interfész létrehozva
- [ ] `SchemaMigrator` osztály (PRAGMA user_version alapú, forward-only)
- [ ] `M001_InitialSchema` migration — Outbox + TemplateCache + MaterialCache táblák + indexek
- [ ] `M002_AddSeenGuidsTable` migration
- [ ] `M003_AddEncryptedMaterialPrice` migration
- [ ] Ha a DB verziója újabb mint a plugin elvár → `Result.Error` (downgrade tiltva)
- [ ] Minden migration atomi tranzakcióban fut

### SqliteLocalStore (§8.1)
- [ ] `SqliteLocalStore : ILocalStore` implementáció létrehozva
- [ ] Connection string: WAL mode, busy_timeout=5000, foreign_keys=ON (§9.9)
- [ ] `PRAGMA journal_mode = WAL` beállítva minden connection nyitáskor
- [ ] `CheckHealthAsync()` implementálva (`PRAGMA integrity_check`) (§9.10)
- [ ] Ha corruption → cache táblák auto-rebuild; OutboxQueue → user-ack dialog

### OutboxLeader — Named Mutex (DB-04, §9.9)
- [ ] `OutboxLeader` osztály `Global\CabinetBilder.OutboxLeader` named mutex-szel
- [ ] `TryBecomeLeader()` — WaitOne(TimeSpan.Zero) — non-blocking
- [ ] `IDisposable` implementáció (Mutex release)

### Fájl-layout (§6.3)
- [ ] `%APPDATA%\CabinetBilder\client.db` elérési út konfigurálva
- [ ] WAL fájlok (`client.db-wal`, `client.db-shm`) automatikusan kezelt

### Tesztek
- [ ] `SqliteLocalStoreTests` — in-memory SQLite-tal: Enqueue, Claim, MarkSuccess, MarkFailed
- [ ] `SchemaMigratorTests` — v0→v3 teljes migration futtatás; downgrade-kísérlet → Error
- [ ] `OutboxLeaderTests` — két példány: csak az egyik lesz leader

## Tanúsítás (Evidence)

- Fájl: `CabinetBilder.Core/Sync/ILocalStore.cs`
- Fájl: `CabinetBilder.SpaceOsBridge/LocalStore/SqliteLocalStore.cs`
- Fájl: `CabinetBilder.SpaceOsBridge/LocalStore/SchemaMigrator.cs`
- Fájl: `CabinetBilder.SpaceOsBridge/LocalStore/OutboxLeader.cs`
- Fájl: `CabinetBilder.SpaceOsBridge/LocalStore/Migrations/M001_InitialSchema.cs`
- Fájl: `CabinetBilder.SpaceOsBridge/LocalStore/Migrations/M002_AddSeenGuidsTable.cs`
- Fájl: `CabinetBilder.SpaceOsBridge/LocalStore/Migrations/M003_AddEncryptedMaterialPrice.cs`
- Fájl: `CabinetBilder.Tests/LocalStore/SqliteLocalStoreTests.cs`
- Fájl: `CabinetBilder.Tests/LocalStore/SchemaMigratorTests.cs`
- Fájl: `CabinetBilder.Tests/LocalStore/OutboxLeaderTests.cs`
- Build: `dotnet build` → 0 error
- Teszt: `dotnet test` → All green
- Kézi: Plugin startup → `client.db` létrehozva `%APPDATA%\CabinetBilder\` alatt

## Megjegyzések (Notes)

- **Blokkolók:** 0010 (projekt-szerkezet) be kell zárni előtte.
- **KRITIKUS:** Ez a fázis blokkolja a 0012 (Auth), 0013 (BOM push) és 0014 (Guid collision) feladatokat.
- NuGet: `Microsoft.Data.Sqlite` 9.x — NE az EF Core Sqlite adaptern, hanem a közvetlen ADO.NET csomagon!
- Az EF Core a lokális cache-hez NEM javasolt — a SqliteLocalStore raw ADO.NET parancsokat használ.
- DB-09 mező-szintű DPAPI titkosítás: `DpapiHelper` segédosztályt hozunk létre a Bridge rétegben.
- Kapcsolódó ADR: CAD-ADR-016 (single client.db), CAD-ADR-018 (forward-only migration)

---

**Started:**
**Completed:**
**Duration:**
**Owner:**
