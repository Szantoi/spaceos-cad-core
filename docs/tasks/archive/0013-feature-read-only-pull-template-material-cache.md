# Task ID: 0013
# Title: Fázis 4+5 — Read-Only Pull: Template & Material Cache (ETag + DPAPI ár-titkosítás)
# Category: feature
# Milestone: 13 (Arch Vision v2 — Fázis 4 + Fázis 5)
# Status: archive
# Source: CabinetBilder_Architecture_Vision_v2.md §9.3, §9.4, §9.11, DB-02, DB-09

## Szándék (Intent)

A SpaceOS szerverről letölteni a ProductTemplate-eket és a Material-katalógust a lokális SQLite cache-be,
ETag-alapú cache coherence protokollal (304 Not Modified optimalizáció) és a tenant-specifikus anyagárak
DPAPI-encrypted mezőkben való tárolásával.

## Elfogadási kritérium (Acceptance Criteria)

### ISpaceOsClient port (Core.Sync)
- [ ] `ISpaceOsClient` interfész definiálva: `PullTemplatesAsync(ifNoneMatch)`, `PullMaterialsAsync(ifNoneMatch)` metódusokkal
- [ ] `ProductTemplateDto` és `MaterialDto` transzfer-objektumok definiálva

### HttpSpaceOsClient (SpaceOsBridge.Http)
- [ ] `HttpSpaceOsClient : ISpaceOsClient` implementáció
- [ ] ETag-alapú conditional GET: `If-None-Match` header küldése
- [ ] `304 Not Modified` kezelés → `ExpiresAt` kitolása, tartalom változatlan
- [ ] `200 OK` + új ETag → teljes cache-csere (`UpsertTemplateCacheAsync`)
- [ ] Bearer token automatikus csatolása (`Authorization: Bearer {accessToken}`)
- [ ] Token refresh ha 401 → retry

### TemplateCache flow (§9.3)
- [ ] `GetCachedTemplatesAsync()` — ha `ExpiresAt > now`, SQLite cache-ből visszaad
- [ ] Ha lejárt (vagy user refresh-t kér) → HTTP pull → cache update
- [ ] Ha lejárt **és offline** → figyelmeztetés toast, de cache **használható** (Manifesto T3)
- [ ] `GetTemplateEtagAsync()` — aktuális ETag lekérése a következő conditional GET-hez

### MaterialCache flow (§9.4, DB-09)
- [ ] `GetCachedMaterialsAsync()` — ugyanaz a TTL + ETag logika
- [ ] `UpsertMaterialCacheAsync()` — `PriceEncryptedDpapi` mező DPAPI-encrypted írás
- [ ] `GetCachedMaterialsAsync()` — `PriceEncryptedDpapi` mező DPAPI decrypt olvasáskor
- [ ] A `MaterialCache.BodyJson` (plain metadata) és `PriceEncryptedDpapi` (sensitive ár) szétválik

### Data Retention (§9.12)
- [ ] Startup cleanup: `TemplateCache` lejárt sorai törlése (ha offline, de rendelkezik friss cache-sel)
- [ ] `LocalStore:TemplateCacheTtlHours` konfiguráció `appsettings.json`-ban (default: 24)

### Tesztek
- [ ] `TemplateCacheTests` — in-memory SQLite: cache hit (ExpiresAt jövőben), cache miss (lejárt), ETag round-trip
- [ ] `MaterialCacheTests` — DPAPI encrypt/decrypt a price mezőn; offline fallback
- [ ] `HttpSpaceOsClientTests` (mock HttpMessageHandler) — 304 kezelés, 200 csere, 401 retry

## Tanúsítás (Evidence)

- Fájl: `CabinetBilder.Core/Sync/ISpaceOsClient.cs`
- Fájl: `CabinetBilder.SpaceOsBridge/Http/HttpSpaceOsClient.cs`
- Fájl: `CabinetBilder.Tests/LocalStore/TemplateCacheTests.cs`
- Fájl: `CabinetBilder.Tests/LocalStore/MaterialCacheTests.cs`
- Build + Teszt: `dotnet test` → All green
- Kézi: `cabinetbilder templates pull` → cache feltöltve, repeated pull → 304 HTTP kód a log-ban

## Megjegyzések (Notes)

- **Blokkoló:** 0012 (Auth) szükséges a Bearer token miatt.
- A meglévő `GetCatalogMaterialsUseCase` / SQLite EF Core katalógus **átmenetileg párhuzamos** marad a `MaterialCache`-sel — a refactor feladata az 0010 (projekt átnevezés) keretében.
- Manifesto T3 (adat-tulajdon): stale cache esetén **nem blokkolunk** — a user saját adata.
- DB-02 finding: ETag-alapú cache coherence kötelező, TTL-alapú lejárat + szerver push jövőbeli (WebSocket).
- Kapcsolódó ADR: CAD-ADR-020 (DPAPI mező-szintű titkosítás)

---

**Started:** 2026-04-21
**Completed:** 2026-04-23
**Duration:** 2 days
**Owner:** Antigravity
