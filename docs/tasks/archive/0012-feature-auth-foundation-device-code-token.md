# Task ID: 0012
# Title: Fázis 2 — Auth Foundation (Device Code Flow + Per-Tenant Token Storage)
# Category: feature
# Milestone: 12 (Arch Vision v2 — Fázis 2)
# Status: archive
# Source: CabinetBilder_Architecture_Vision_v2.md §8.2, §9.6, §11, DB-06

## Szándék (Intent)

Implementálni a Keycloak Device Code Flow alapú bejelentkezést, a per-tenant DPAPI-encrypted token tárolást
és a multi-tenant manifest kezelést. Ez lehetővé teszi a felhasználónak, hogy több SpaceOS tenantben legyen
aktív ugyanazon a gépen — minden tenant saját titkosított tokenfájlban.

## Elfogadási kritérium (Acceptance Criteria)

### ISpaceOsAuthenticator port (Core.Sync)
- [ ] `ISpaceOsAuthenticator` interfész definiálva (Core.Sync-ben)
- [ ] `IConnectionState` interfész definiálva (Online/Offline/Unauthenticated állapotok)

### DeviceCodeAuthenticator (SpaceOsBridge.Auth)
- [ ] `DeviceCodeAuthenticator : ISpaceOsAuthenticator` implementáció
- [ ] `IdentityModel.OidcClient` 6.x NuGet csomag integrálva
- [ ] Device Code URL + QR-kód megjelenítése AutoCAD command line-on
- [ ] Token-csere implementálva (auth code → access + refresh token)

### DpapiTokenStore — Per-tenant fájl-layout (DB-06, §9.6)
- [ ] `%APPDATA%\CabinetBilder\tokens\tenants.manifest.json` kezelés (plain JSON)
- [ ] `{tenantId}.token.dpapi` titkosított fájlok (DPAPI)
- [ ] `DpapiTokenStore.ReadTokenAsync(tenantId)` — decrypt + deserialize
- [ ] `DpapiTokenStore.WriteTokenAsync(tenantId, tokenData)` — serialize + encrypt
- [ ] **Refresh single-flight lock:** `SemaphoreSlim(1,1)` a concurrent refresh race elkerüléséhez
- [ ] `LogoutAsync(tenantId)` — fájl atomi törlése + manifest frissítése

### Tenant Manifest kezelés
- [ ] `tenants.manifest.json` séma: schemaVersion, activeTenantId, tenants lista
- [ ] Active tenant váltás implementálva
- [ ] Manifest atomic write (temp file + rename pattern)

### `DpapiHelper` — mező-szintű titkosítás (DB-09)
- [ ] `DpapiHelper.EncryptString(plaintext) → string` (Base64 kódolt DPAPI output)
- [ ] `DpapiHelper.DecryptString(ciphertext) → string`
- [ ] `DpapiHelper.EncryptBytes(plaintext) → byte[]`
- [ ] `DpapiHelper.DecryptBytes(ciphertext) → string`
- [ ] Windows-only (`ProtectedData.Protect/Unprotect`)
- [ ] Linux/Mac fallback: `FileTokenStore` (plain-text, only for dev/CI)

### Tesztek
- [ ] `DpapiTokenStoreTests` — write + read round-trip; parallel refresh single-flight teszt
- [ ] `TenantManifestTests` — multi-tenant manifest parse/write; active tenant váltás
- [ ] `DpapiHelperTests` — encrypt/decrypt round-trip (Windows-only teszt, `[TestCategory("WindowsOnly")]`)

## Tanúsítás (Evidence)

- Fájl: `CabinetBilder.Core/Sync/ISpaceOsAuthenticator.cs`
- Fájl: `CabinetBilder.Core/Sync/IConnectionState.cs`
- Fájl: `CabinetBilder.SpaceOsBridge/Auth/DeviceCodeAuthenticator.cs`
- Fájl: `CabinetBilder.SpaceOsBridge/TokenStorage/DpapiTokenStore.cs`
- Fájl: `CabinetBilder.SpaceOsBridge/TokenStorage/DpapiHelper.cs`
- Fájl: `CabinetBilder.SpaceOsBridge/TokenStorage/TenantManifestManager.cs`
- Build: `dotnet build` → 0 error
- Teszt: `dotnet test` → All green
- Kézi: `cabinetbilder login` → Device Code URL megjelenik → Sikeres bejelentkezés → token.dpapi fájl létrejön

## Megjegyzések (Notes)

- **Blokkoló:** 0011 (Local Storage Foundation) szükséges.
- **SEC-pre-03:** DPAPI-key rotation Windows user-profile migráció esetén — v3 security review tárgyalja.
- **SEC-pre-04:** MCP server local port exposure — szintén v3 tárgyalja.
- A `FileTokenStore` Linux/Mac fallback **nem production-ready** — csak CI/fejlesztési célú.
- Kapcsolódó ADR: CAD-ADR-017 (per-tenant DPAPI token), CAD-ADR-020 (mező-szintű DPAPI)

---

**Started:** 2026-04-20
**Completed:** 2026-04-23
**Duration:** 3 days
**Owner:** Antigravity
