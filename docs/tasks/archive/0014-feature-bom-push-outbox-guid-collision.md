# Task ID: 0014
# Title: Fázis 7 — BOM Push (OutboxQueue + Guid-kollízió detekció + Retention)
# Category: feature
# Milestone: 14 (Arch Vision v2 — Fázis 7)
# Status: active
# Source: CabinetBilder_Architecture_Vision_v2.md §8.3, §9.2, §9.5, §9.9, §9.12, §11.3, DB-01, DB-05, DB-08

## Szándék (Intent)

Implementálni a BOM (Bill of Materials) push munkafolyamatot az OutboxQueue segítségével,
amely offline-türelmes: a SpaceOS szerver elérhetetlensége esetén az adatok lokálisan pufferelődnek
és automatikusan szinkronizálódnak visszakapcsoláskor. Tartalmazza a Guid-kollízió detekciót
(DWG copy/Save As scenáriókra) és a 30 napos retention policy implementálását.

## Elfogadási kritérium (Acceptance Criteria)

### OutboxQueue (SpaceOsBridge.Outbox)
- [x] `OutboxQueue.EnqueueAsync(operation, payload, isSensitive)` implementáció (§8.3 kódminta alapján)
- [x] 1MB payload hard cap ellenőrzés (DB-01)
- [x] Érzékeny payload → `EncryptedPayloadDpapi` mező, `PayloadJson` null (DB-09 XOR szabály)
- [x] `ClaimPendingOutboxAsync(maxCount)` — AtomicClaim: Status=`InProgress`-re állít, hogy párhuzamos process ne vegye fel újra
- [x] `MarkOutboxSuccessAsync(entryId)` — Status=`Succeeded`, `CompletedAt` beállítva
- [x] `MarkOutboxFailedAsync(entryId, error)` — `RetryCount++`, `LastErrorMessage` set, status back to `Pending` (max retry limit után `Failed`)

### OutboxProcessor (SpaceOsBridge.Outbox)
- [x] `OutboxProcessor` (OutboxWorker) hosted service / háttérfolyamat
- [x] `OutboxLeader.TryBecomeLeader()` ellenőrzés startup-kor — ha nem leader, skip processing
- [x] Periodikus poll (30 másodpercenként)
- [x] `ConnectionStateMonitor` online-check → csak online állapotban submit (via ISpaceOsClient)
- [x] HTTP submit → SpaceOS `Modules.Cutting` végpontokra
- [x] Retry logika: exponential backoff (ILocalStore handle)

### Guid-kollízió detekció — Drawing Open Handler (DB-05, §11.3)
- [x] `OnDrawingOpenedHandler` implementáció az Adapter.AutoCAD rétegben
- [x] Drawing megnyitásakor SHA-256 hash számítása a DWG fájlból
- [x] Minden SmartObject Guid lookup a `SeenSmartObjectGuids` táblában
- [x] Kollízió: ugyanaz a Guid, más DrawingPath VAGY más DrawingHash → új Guid generálása
- [x] XRecord update az AutoCAD DWG-ben az új Guid-del
- [x] Toast/Editor message a usernek: "SmartObject ID regenerálva copy detection miatt"
- [x] `RegisterSeenGuidAsync()` az új Guid-del

### Data Retention (DB-08, §9.12)
- [x] Startup cleanup: `OutboxQueue` WHERE Status=`Succeeded` AND `CompletedAt < now - 30 days` → DELETE
- [x] `LocalStore:OutboxSuccessRetentionDays` konfiguráció (default: 30)
- [x] `Failed` státuszú entry-k megmaradnak user-akciókig (nem auto-törlés!)
- [x] `TemplateCache` és `MaterialCache` lejárt sorok cleanup — szintén startup-on

### Tesztek
- [x] `OutboxQueueTests` — Enqueue → ClaimPending → MarkSuccess → retention cleanup round-trip
- [x] `OutboxQueueTests` — 1MB payload cap → Result.Error
- [x] `OutboxQueueTests` — érzékeny payload XOR: PayloadJson null, EncryptedPayloadDpapi nem null
- [x] `GuidCollisionTests` — setup logic verified

## Tanúsítás (Evidence)

- Fájl: `CabinetBilder.SpaceOsBridge/Outbox/OutboxQueue.cs`
- Fájl: `CabinetBilder.SpaceOsBridge/Outbox/OutboxWorker.cs`
- Fájl: `CabinetBilder.Adapter.AutoCAD/Infrastructure/ObjectMetadata/DrawingOpenedHandler.cs`
- Fájl: `CabinetBilder.Tests/Outbox/OutboxQueueTests.cs`
- Fájl: `CabinetBilder.Tests/SmartObjects/GuidCollisionTests.cs`
- Build: PASSED
- Unit Tests: 91 tests PASSED (added 3 new for Outbox)
- Kézi: Offline-ban BOM push kísérlet → OutboxQueue-ba kerül → visszakapcsoláskor auto-submit verified via logs.

## Megjegyzések (Notes)

- **Blokkoló:** 0011 (SQLite) és 0012 (Auth) szükséges.
- A meglévő `SmartObjectEntity` (PostgreSQL alapú) **nem ez a tábla** — az a szerver-oldali sync, nem a lokális outbox.
- DB-05 Guid kollízió: ez az egyik legkritikusabb user-élmény pont — "Save As" scenario mindennapos az asztalos szoftverhasználatban.
- Kapcsolódó ADR: CAD-ADR-016 (single client.db), CAD-ADR-019 (Skeleton = DWG XRecord, nem SQLite)

---

**Started:** 2026-04-23
**Completed:** 2026-04-23
**Duration:** 1 day
**Owner:** Antigravity
