# SpaceOS — Doorstar Pilot Onboarding Architecture
## Soft Launch Integration Package (Keycloak + Modules.Joinery + B2B Handshake)

> **Verzió:** v4.0 — 2026-04-08
> **Státusz:** IMPLEMENTÁCIÓRA KÉSZ
> **Blokkoló feltétel:** Phase 3C+ DoD + ProdReady Sprint DoD + Modules.Joinery DoD
> **Kumulált review:** `/database-designer` + `/database-schema-designer` → v2 · `/senior-security` → v3 · `/senior-backend` → v4
> **Érintett repók:** `spaceos-kernel` · `spaceos-orchestrator` · `spaceos-design-portal` · `spaceos-modules-joinery` · VPS infra
> **Becsült effort:** ~7 fejlesztői nap (merge + onboarding fázis, 3 prerequisite track-on felül)

---

## 1. Kumulált Finding Összesítő (v1 → v4)

| Review | Finding-ek | Legfontosabb javítás | Effort delta |
|--------|-----------|----------------------|--------------|
| v1 → `/database-designer` + `/database-schema-designer` → v2 | 0 CRITICAL · 2 HIGH · 1 MEDIUM | Keycloak group attr claim mapping consistency · seed SQL idempotencia | +0.5 nap |
| v2 → `/senior-security` → v3 | 2 CRITICAL · 3 HIGH · 2 MEDIUM | Saga compensation race condition · Keycloak group attr tenant_id spoofing · Joinery :5002 loopback-only | +1 nap |
| v3 → `/senior-backend` → v4 | 0 CRITICAL · 2 HIGH · 1 MEDIUM | kernelClient retry/timeout · joineryClient health check · seed data validation | +0.5 nap |
| **Összesen** | **2 CRITICAL · 7 HIGH · 4 MEDIUM** | | **~7 fejlesztői nap** |

### Finding részletek

| ID | Súly | Terület | Probléma | v_ javítás |
|----|------|---------|----------|------------|
| DB-01 | 🟠 HIGH | Keycloak group attr | `tenant_id` group attribute kézzel beírt UUID — typo = silent auth failure, nem hibaüzenet | v2: Seed script validálja UUID formátumot; Kernel `TenantSessionInterceptor` logol ha JWT `tenant_id` nem létezik a Tenants táblában |
| DB-02 | 🟠 HIGH | Seed SQL | `INSERT INTO Tenants` nem idempotens ha `ON CONFLICT DO NOTHING` mellett más mezők változnak | v2: `ON CONFLICT ("Id") DO UPDATE SET "Name" = EXCLUDED."Name", "EnabledModules" = EXCLUDED."EnabledModules"` — upsert |
| DB-03 | 🟡 MEDIUM | EnabledModules claim | Keycloak group attr `enabledModules` string `["door"]` — JSON parse kell Portal-ban, de claim type text | v2: Orchestrator auth response-ban parse-olja és validálja (nem a Portal) |
| SEC-01 | 🔴 CRITICAL | Saga compensation | Ha Kernel FlowEpic létrejön (①) de Joinery DoorOrder fail (②) és az archive hívás is fail → árva FlowEpic ACTIVE marad | v3: Orchestrator retry archive 3× exponential backoff; ha az is fail → CRITICAL log + `OrphanEpicCleanupJob` (PeriodicTimer, 1h) |
| SEC-02 | 🔴 CRITICAL | Keycloak tenant_id | Group attribute mapper: bármely Keycloak admin hozzáadhat user-t bármely group-hoz → cross-tenant escalation | v3: Keycloak admin konzol 127.0.0.1-only (ProdReady SEC-02); Kernel oldalon `TenantSessionInterceptor` RLS enforced — app-szintű védekezés nem szükséges, DB-szintű elegendő |
| SEC-03 | 🟠 HIGH | Joinery port | Modules.Joinery :5002 ha nem loopback-only → közvetlenül elérhető Nginx bypass-szal | v3: systemd `ExecStart` `--urls=http://127.0.0.1:5002` + UFW `5002 DENY` |
| SEC-04 | 🟠 HIGH | Orchestrator mediation | Orchestrator → Kernel és Orchestrator → Joinery közötti hívások nem timeout-oltak → infinite hang | v3: `AbortController` 10s timeout mindkét client-en |
| SEC-05 | 🟠 HIGH | JWT forwarding | Orchestrator forwarding-olja az eredeti JWT-t Joinery felé — ha Joinery audience `kernel-api` → reject | v3: Joinery JWT audience = `kernel-api` (azonos Keycloak client); egyetlen audience soft launch-ra |
| SEC-06 | 🟡 MEDIUM | Seed data integrity | Static C# seed class compile-time OK, de runtime-ban nem ellenőrzött: pl. DoorType enum és DB CHECK mismatch | v3: Startup `IDataSeeder` futtatás után `COUNT(*)` assertion — ha 0 → startup fail |
| SEC-07 | 🟡 MEDIUM | B2B handshake seed | TenantHandshakeAllowlist seed szükséges ha B2B day-1 — de nincs benne a template-ben | v3: Seed script bővítés: guest tenant + allowlist rekord |
| BE-01 | 🟠 HIGH | kernelClient | `kernelClient.post()` nincs retry — transient network error = user-facing 500 | v4: 1× retry `ECONNRESET`/`ETIMEDOUT`-ra; idempotency key header a Kernel felé (`X-Idempotency-Key: uuid`) |
| BE-02 | 🟠 HIGH | Health check | Joinery :5002 nincs Nginx upstream health check — silent dead backend | v4: Nginx `upstream` block + `max_fails=3 fail_timeout=30s`; Joinery `GET /health` endpoint |
| BE-03 | 🟡 MEDIUM | Seed validation | `DoorstarSeedData.cs` nem validálja hogy a PartDimensionRules ComponentType értékek illeszkednek a DB CHECK-hez | v4: xUnit teszt: `DoorstarSeedData.PartDimensionRules.All(r => validComponentTypes.Contains(r.ComponentType))` |

---

## 2. Architekturális döntések (ADR)

### ADR-010: Orchestrator-Mediated FlowEpic Pattern

**Kontextus:** Modules.Joinery (L2) szükségszerűen kapcsolódik a Kernel FlowEpic-jéhez (L1), de az Island Architecture tiltja a közvetlen kommunikációt.

**Döntés:** Az Orchestrator (L3) közvetít:
1. BFF fogadja a DoorOrder create kérést
2. Orchestrator létrehozza a FlowEpic-et a Kernel-ben
3. Orchestrator továbbítja a FlowEpicId-t a Modules.Joinery-nek
4. Modules.Joinery soha nem tud a Kernel létezéséről

**Következmény:** Saga kompenzáció szükséges ha ② sikerül de ③ nem.

### ADR-011: One Realm, Group-per-Tenant (Keycloak)

**Kontextus:** Multi-tenant auth szükséges, de egyetlen reference customer (Doorstar) van soft launch-ra.

**Döntés:** Egyetlen `spaceos` Keycloak realm. Minden tenant = egy Keycloak Group. Group attributes: `tenant_id`, `tenant_type`, `enabled_modules`. Claim mapper injektálja a JWT-be.

**Következmény:** Keycloak admin jogosultság = cross-tenant escalation lehetőség → admin konzol kizárólag 127.0.0.1. Kernel RLS a végső védelmi vonal.

### ADR-012: Modules.Joinery Separate Process

**Kontextus:** Modules.Joinery külön repo, külön DB schema.

**Döntés:** Önálló process (`:5002`), saját systemd service. L1/L2 layer boundary = process boundary.

**Következmény:** +1 systemd service, +1 Nginx upstream, de Kernel crash nem viszi magával a Joinery-t.

### ADR-013: Static Seed Data (Soft Launch)

**Kontextus:** Doorstar termékadatok ritkán változnak.

**Döntés:** Compile-time C# static class (`DoorstarSeedData.cs`). `IDataSeeder` startup-on `ON CONFLICT DO NOTHING`.

**Következmény:** Adatváltozás = recompile. Második tenant → CSV runtime load-ra váltás.

---

## 3. Prerequisite track-ök

| Track | Tervdok | Effort | Státusz |
|-------|---------|--------|---------|
| Track 1 — Phase 3C+ | `SpaceOS_Phase3Cplus_Architecture_v3.md` | 16 nap | IN_PROGRESS |
| Track 2 — ProdReady Sprint | `SpaceOS_ProductionReadiness_Sprint_v4.md` | 8 nap | IN_PROGRESS |
| Track 3 — Modules.Joinery | `SpaceOS_Modules_Joinery_v4.md` | 16 nap | READY |

**Ez a dokumentum a 3 track MERGE POINT-ját specifikálja.**

---

## 4. VPS Deploy Topológia (végállapot)

```
Browser  https://joinerytech.hu
  │
  ▼
Nginx :443 (TLS 1.2/1.3 + HSTS)
  ├── /                        → Portal static (Phase 3C+)
  ├── /auth/*                  → Keycloak :8080 (ProdReady)
  ├── /auth/admin/*            → 403 (csak 127.0.0.1)
  ├── /bff/api/*               → Orchestrator :3000 → Kernel :5001
  ├── /bff/joinery/*           → Orchestrator :3000 → Joinery :5002    ← ÚJ
  ├── /bff/handshakes/*        → Orchestrator :3000 → Kernel :5001
  └── /bff/nodes/*             → Orchestrator :3000 → Kernel :5001

Keycloak :8080     (Docker, PostgreSQL: spaceos_keycloak)
Orchestrator :3000 (PM2)
Kernel :5001       (systemd, loopback-only)
Joinery :5002      (systemd, loopback-only)                             ← ÚJ
PostgreSQL :5433
  ├── spaceos              (Kernel)
  ├── spaceos_keycloak     (Keycloak)
  ├── spaceos_joinery      (Modules.Joinery)                            ← ÚJ
  └── spaceos_audit_sink   (Audit)
```

---

## 5. Keycloak — Doorstar konfiguráció

### 5.1 Doorstar Group

```
Realm: spaceos (ProdReady Sprint által létrehozott)
Group: doorstar
  ├── Attributes:
  │   ├── tenant_id = "{DOORSTAR_TENANT_UUID}"
  │   ├── tenant_type = "Manufacturer"
  │   └── enabled_modules = '["door"]'
  ├── Members:
  │   ├── doorstar-admin (realm role: Admin)
  │   ├── doorstar-op1 (realm role: User)
  │   └── doorstar-op2 (realm role: User)
```

### 5.2 Protocol Mapper (Group Attribute → JWT Claim)

| Mapper name | Mapper type | Token claim name | Claim JSON type |
|---|---|---|---|
| `tenant_id` | Group Attribute | `tenant_id` | String |
| `tenant_type` | Group Attribute | `tenant_type` | String |
| `enabled_modules` | Group Attribute | `enabled_modules` | JSON |

### 5.3 JWT token struktúra

```json
{
  "sub": "keycloak-user-uuid",
  "iss": "https://joinerytech.hu/auth/realms/spaceos",
  "aud": "kernel-api",
  "tenant_id": "DOORSTAR_TENANT_UUID",
  "tenant_type": "Manufacturer",
  "enabled_modules": ["door"],
  "allowed_hosts": [],
  "realm_access": { "roles": ["Admin"] },
  "exp": 1720000000
}
```

**SEC-05 döntés:** Joinery API audience = `kernel-api` (azonos Keycloak client). Soft launch, 1 VPS, belső hálózat.

---

## 6. Orchestrator Mediation Pattern (ADR-010)

### 6.1 Sequence

```
Portal          Orchestrator :3000     Kernel :5001          Joinery :5002
  │                  │                    │                       │
  ├─POST /bff/joinery/orders──→│          │                       │
  │                  │                    │                       │
  │                  ├─POST /api/epics───→│                       │
  │                  │  {tenantId,        │                       │
  │                  │   tradeType:"door",│                       │
  │                  │   title}           │                       │
  │                  │←─201 {flowEpicId}──┤                       │
  │                  │                    │                       │
  │                  ├─POST /api/joinery/orders────────────────→  │
  │                  │  {flowEpicId,      │                       │
  │                  │   projectId,       │                       │
  │                  │   items[...]}      │                       │
  │                  │←─201 {orderId}─────────────────────────── ┤
  │                  │                    │                       │
  │←─201 {orderId, flowEpicId}──┤         │                       │
```

### 6.2 Saga kompenzáció

| Lépés | Ha sikertelen | Kompenzáció |
|---|---|---|
| ① Kernel FlowEpic | 4xx/5xx | Nincs — semmi nem jött létre |
| ② Joinery DoorOrder | 4xx/5xx | Kernel Epic archive: 3× retry exponential backoff (SEC-01) |
| Archive retry fail | 3× fail | CRITICAL log → `OrphanEpicCleanupJob` (1h periodic) |

### 6.3 Orchestrator route

```typescript
// spaceos-orchestrator/src/routes/joineryOrders.ts

import { Router } from 'express';
import { authMiddleware } from '../middleware/jwtVerify';
import { createKernelClient, createJoineryClient } from '../clients';

const router = Router();

router.post('/orders', authMiddleware, async (req, res) => {
  const { projectId, projectName, clientName, items, ...rest } = req.body;
  const tenantId = req.jwt.tenant_id;
  const idempotencyKey = crypto.randomUUID();

  try {
    // ① Kernel: FlowEpic
    const kernelClient = createKernelClient({ timeout: 10_000 });
    const epicRes = await kernelClient.post('/api/epics', {
      tenantId,
      title: `Door Order: ${projectId}`,
      tradeType: 'door',
    }, {
      headers: {
        'Authorization': req.headers.authorization,
        'X-Idempotency-Key': idempotencyKey,
      },
    });

    if (!epicRes.ok) return res.status(epicRes.status).json(epicRes.body);

    const flowEpicId = epicRes.body.id;

    // ② Joinery: DoorOrder
    const joineryClient = createJoineryClient({ timeout: 10_000 });
    const orderRes = await joineryClient.post('/api/joinery/orders', {
      ...rest, projectId, projectName, clientName, items, flowEpicId,
    }, {
      headers: { 'Authorization': req.headers.authorization },
    });

    if (!orderRes.ok) {
      await archiveEpicWithRetry(kernelClient, flowEpicId, req);
      return res.status(orderRes.status).json(orderRes.body);
    }

    return res.status(201).json({ orderId: orderRes.body.id, flowEpicId });
  } catch (err) {
    console.error('[joinery-orders] Unhandled error:', err);
    return res.status(502).json({ error: 'Upstream service unavailable' });
  }
});

async function archiveEpicWithRetry(client, epicId, req) {
  const delays = [100, 500, 2000];
  for (const delay of delays) {
    try {
      const res = await client.post(`/api/epics/${epicId}/archive`, {}, {
        headers: { 'Authorization': req.headers.authorization },
      });
      if (res.ok) return;
    } catch { /* continue */ }
    await new Promise(r => setTimeout(r, delay));
  }
  console.error(`[CRITICAL] Orphan FlowEpic ${epicId} — archive failed after 3 retries`);
}

export default router;
```

### 6.4 Joinery proxy (pass-through — nem-mediated endpoint-ok)

```typescript
// spaceos-orchestrator/src/routes/joineryProxy.ts
import { Router } from 'express';
import { createProxyMiddleware } from 'http-proxy-middleware';

const router = Router();
router.use('/', createProxyMiddleware({
  target: process.env.JOINERY_API_URL || 'http://127.0.0.1:5002',
  changeOrigin: true,
  pathRewrite: { '^/bff/joinery': '/api/joinery' },
  timeout: 10_000,
}));

export default router;
```

### 6.5 app.ts bővítés

```typescript
// diff
import joineryOrdersRouter from './routes/joineryOrders';
import joineryProxyRouter from './routes/joineryProxy';

app.use('/bff/joinery/orders', joineryOrdersRouter);    // mediated
app.use('/bff/joinery', authMiddleware, joineryProxyRouter); // pass-through
```

---

## 7. Kernel Tenant Seed

```sql
-- 01_doorstar_tenant_seed.sql

INSERT INTO "Tenants" ("Id", "Name", "TenantType", "SourceBrand", "EnabledModules", "IsArchived")
VALUES (
    '{DOORSTAR_TENANT_UUID}',
    'Doorstar Kft.',
    'Manufacturer',
    'joinerytech',
    ARRAY['door'],
    false
) ON CONFLICT ("Id") DO UPDATE SET
    "Name" = EXCLUDED."Name",
    "EnabledModules" = EXCLUDED."EnabledModules";

INSERT INTO "Facilities" ("Id", "TenantId", "Name", "IsArchived")
VALUES (
    '{FACILITY_UUID}',
    '{DOORSTAR_TENANT_UUID}',
    'Doorstar Üzem',
    false
) ON CONFLICT ("Id") DO NOTHING;
```

---

## 8. Infrastructure

### 8.1 systemd — spaceos-joinery.service

```ini
[Unit]
Description=SpaceOS Modules.Joinery API
After=postgresql.service spaceos-kernel.service

[Service]
Type=simple
User=spaceos
WorkingDirectory=/opt/spaceos/modules-joinery
ExecStart=/usr/bin/dotnet SpaceOS.Modules.Joinery.Api.dll --urls=http://127.0.0.1:5002
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ConnectionStrings__JoineryDb=Host=localhost;Port=5433;Database=spaceos_joinery;Username=spaceos_app;Password=${JOINERY_DB_PASSWORD}
Environment=JWT__AUTHORITY=https://joinerytech.hu/auth/realms/spaceos
Environment=JWT__AUDIENCE=kernel-api
NoNewPrivileges=true
ProtectSystem=strict
PrivateTmp=true
CapabilityBoundingSet=
SystemCallFilter=@system-service
ReadWritePaths=/opt/spaceos/modules-joinery/logs
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
```

### 8.2 Nginx upstream

```nginx
upstream joinery_backend {
    server 127.0.0.1:5002 max_fails=3 fail_timeout=30s;
}
```

### 8.3 UFW + sudo

```bash
sudo ufw deny 5002
# /etc/sudoers.d/deploy-spaceos bővítés:
# deploy-spaceos ALL=(root) NOPASSWD: /usr/bin/systemctl restart spaceos-joinery
```

---

## 9. Seed Data Pipeline

### 9.1 Forrás

`Doorstar_Seed_Data_Template.xlsx` — 6 sheet, Doorstar tölti valós adatokkal.

### 9.2 Pipeline

```
Doorstar_Seed_Data_Template.xlsx (kitöltve)
    ↓ Gábor review + validáció
CSV (5 fájl)
    ↓ Code generator script
DoorstarSeedData.cs (compile-time)
    ↓ IDataSeeder.SeedAsync() startup
PostgreSQL spaceos_joinery
```

### 9.3 Startup assertion (SEC-06)

```csharp
// Joinery Api Program.cs
app.Lifetime.ApplicationStarted.Register(async () =>
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
    await seeder.SeedAsync(CancellationToken.None);

    var db = scope.ServiceProvider.GetRequiredService<JoineryDbContext>();
    var count = await db.DoorTypeRules.CountAsync();
    if (count == 0)
        throw new InvalidOperationException("FATAL: DoorTypeRules seed empty");
});
```

### 9.4 Seed validation tesztek (BE-03)

```csharp
[Fact]
public void AllPartDimensionRules_HaveValidComponentType()
{
    var valid = new[] { "Frame", "Insert", "Clad", "FrameCore", "Blende", "Coating" };
    Assert.All(DoorstarSeedData.PartDimensionRules,
        r => Assert.Contains(r.ComponentType, valid));
}

[Fact]
public void AllDoorTypeRules_CoverEveryDoorTypeEnum()
{
    var enumValues = Enum.GetNames<DoorType>();
    var seedTypes = DoorstarSeedData.DoorTypeRules.Select(r => r.DoorType).ToArray();
    Assert.Equal(enumValues.OrderBy(x => x), seedTypes.OrderBy(x => x));
}

[Fact]
public void GlobalConstants_ContainRequiredKeys()
{
    var required = new[] { "CuttingOversize", "CladdingOverhang", "MatyiWidth" };
    var keys = DoorstarSeedData.GlobalConstants.Select(c => c.Key).ToArray();
    Assert.All(required, k => Assert.Contains(k, keys));
}
```

---

## 10. Door System Prompt

```typescript
export const doorSystemPrompt = `
You are a production assistant for Doorstar Kft., a professional door manufacturer.

Context: ajtógyártás, units: mm, default materials: MDF 18mm + HDF 3mm
Capabilities: door order creation, cutting list review, production scheduling
DoorTypes: Butorfront, Disztok, FAF_T, FAF_TN, FAF_TN_KetSzarny, Falcos, Falsikban,
  FEF_T, FEF_T_KetSzarny, FEF_TN, FEF_TN_KetSzarny, Falpanel, Sikban, Tokba, Pivot,
  PivotDisztokkal, TUS_Tokba, TUT_Sikba, TPL_Sikba, TPS_Tokba, KetSzarny_Sikba, KetSzarny_Tokba
OpeningDirections: Left, Right, Double, PivotLeft, PivotRight
SurfaceTypes: Painted, Foiled

Rules:
- NEVER calculate dimensions yourself — call CalculateDoorOrder API
- NEVER guess materials — ask the user or look up from DoorTypeRules
- Prices: HUF (Ft), dimensions: mm
- Respond in Hungarian unless user writes in English
- Technical terms stay in English
`;
```

---

## 11. E2E Acceptance — Go/No-Go

| # | Teszt | Blokkoló? |
|---|---|---|
| 1 | Doorstar admin login → JWT claims correct | 🔴 GO |
| 2 | Portal `/door/` visible, `/cabinet/` not | 🔴 GO |
| 3 | `POST /bff/joinery/orders` → FlowEpic + DoorOrder created | 🔴 GO |
| 4 | `POST /bff/joinery/orders/{id}/items` → DoorItem added | 🔴 GO |
| 5 | `POST /bff/joinery/orders/{id}/calculate` → CuttingList ≈ Excel (±1mm) | 🔴 GO |
| 6 | Kanban: Draft → Submitted → InProduction → Completed | 🔴 GO |
| 7 | Audit trail: all order ops logged | 🔴 GO |
| 8 | B2B: external Handshake to Doorstar | 🟡 NICE |
| 9 | `/auth/admin/` → 403 externally | 🔴 GO |
| 10 | Operator non-Admin → tenant CRUD blocked | 🔴 GO |
| 11 | Saga: Joinery fail → Epic archived | 🔴 GO |
| 12 | `:5002` not reachable externally | 🔴 GO |
| 13 | Seed: ≥1 DoorType full CuttingList pathway | 🔴 GO |

**Go: #1-7, #9-13 all 🟢. #8 can be 🟡.**

---

## 12. Definition of Done

### Migration gates
- [ ] Kernel Migration 0025 (EnabledModules) VPS-en
- [ ] Kernel Migration 0026 (TenantHandshakeAllowlist) VPS-en
- [ ] Joinery Migration 0001 (teljes schema) VPS-en
- [ ] `spaceos_joinery` schema owner = `spaceos_schema_owner`

### Infrastructure gates
- [ ] `spaceos-joinery.service` enabled + active
- [ ] UFW: `5002 DENY`
- [ ] Nginx: `/bff/joinery/*` proxy működik
- [ ] Nginx: Joinery upstream health check (BE-02)
- [ ] deploy-spaceos sudo: `systemctl restart spaceos-joinery`

### Keycloak gates
- [ ] `doorstar` group + attributes beállítva
- [ ] Protocol mapper: 3 claim aktív
- [ ] Admin + operator user(ek) regisztrálva
- [ ] `/auth/admin/` → 403 externally

### Seed data gates
- [ ] `DoorstarSeedData.cs` compile-time OK
- [ ] Seed validation xUnit tesztek zöldek (BE-03)
- [ ] `IDataSeeder.SeedAsync()` startup OK (SEC-06)
- [ ] `01_doorstar_tenant_seed.sql` lefuttatva

### Integration gates
- [ ] Orchestrator mediation route működik (ADR-010)
- [ ] Saga compensation: archive 3× retry (SEC-01)
- [ ] Client timeout: 10s (SEC-04)
- [ ] Idempotency key header (BE-01)

### E2E smoke test
- [ ] Acceptance criteria #1-7, #9-13 mind 🟢

### Összesített
- [ ] Meglévő 1452 teszt zöld
- [ ] Új tesztek: ≥ 8 db (3 seed + 3 mediation + 2 smoke)
- [ ] 0 build warning
- [ ] `dotnet list package --vulnerable` → 0 high/critical
- [ ] `ConfigureAwait(false)` minden Joinery production async call-ban

---

## 13. Security adósság

| ID | Tétel | Ez a fázis | Marad |
|----|-------|------------|-------|
| SEC-01 | Saga orphan Epic | ✅ retry + cleanup job | — |
| SEC-02 | Keycloak tenant_id spoof | — | ProdReady (admin 127.0.0.1 + RLS) |
| SEC-03 | Joinery loopback | ✅ systemd + UFW | — |
| SEC-04 | Timeout | ✅ 10s AbortController | — |
| SEC-05 | JWT audience | ✅ shared kernel-api | — |
| SEC-06 | Seed integrity | ✅ startup assertion | — |
| P0-2 | Hash chain trust | — | ProdReady |
| P2-3 | GDPR pseudo | — | Future |
| Escrow GA | S3 Object Lock | — | Future |

---

## 14. Roadmap

| Sorrend | Téma | Trigger |
|---------|------|---------|
| 1 | VPS deploy: 3 track merge | 3 track DoD kész |
| 2 | Doorstar UAT | Deploy + seed kész |
| 3 | Feedback iteration (1-2 hét) | UAT eredmények |
| 4 | **Soft Launch GO** | UAT pass |
| 5 | Escrow GA Architecture | Post-launch |
| 6 | Második tenant onboarding | Doorstar stabil |

---

## 15. Claude Code implementációs csomag

### Végrehajtási sorrend (5 nap)

| Nap | Feladat | Repo | Függőség |
|-----|---------|------|----------|
| 1 | Orchestrator: `joineryOrders.ts` + `joineryProxy.ts` + `clients/` + saga retry | orchestrator | Phase 3C+ BFF kész |
| 1 | Orchestrator: door system prompt | orchestrator | — |
| 2 | Kernel: tenant seed SQL + UUID-ok | kernel / VPS | ProdReady Keycloak kész |
| 2 | Keycloak: group + attr + mappers + users | VPS | ProdReady kész |
| 3 | Joinery: `DoorstarSeedData.cs` + seed tesztek | modules-joinery | Excel template kitöltve |
| 3 | Joinery: `IDataSeeder` startup assertion | modules-joinery | — |
| 4 | VPS: systemd + Nginx + UFW + sudo | VPS | Joinery build kész |
| 4 | Orchestrator: `JOINERY_API_URL` env + PM2 restart | VPS | Nap 4 infra |
| 5 | E2E smoke + Go/No-Go | E2E | Minden kész |

### Agent utasítás

> "Implementáld a `SpaceOS_Doorstar_Onboarding_v4.md` alapján a merge + onboarding fázist:
>
> **Orchestrator:** joineryOrders mediation (ADR-010), joineryProxy, client factory 10s timeout (SEC-04), archiveEpicWithRetry 3× (SEC-01), door system prompt
>
> **VPS:** spaceos-joinery.service loopback (SEC-03), Nginx upstream health check (BE-02), UFW 5002 DENY, sudo bővítés
>
> **Joinery seed:** DoorstarSeedData.cs statikus (ADR-013), startup assertion (SEC-06), validation tesztek (BE-03)
>
> **Keycloak:** doorstar group + attributes, protocol mappers, users
>
> DoD: #12 · Blokkolók: Migration 0025-0026 + 0001, Keycloak realm
> Gate: `dotnet test && dotnet build`"

### Kockázatok

| Kockázat | P | Hatás | Mitigáció |
|----------|---|-------|-----------|
| Doorstar Excel késik | Közepes | Track 3 blokkolva | Generikus demo seed fallback |
| CuttingList ≠ Excel output | Magas | UAT fail | ±1mm tolerancia; iteratív offset javítás |
| Keycloak claim mapping hiba | Alacsony | Login fail | JWT decode teszt admin UI-ból |
| Saga race condition | Alacsony | Árva Epic | OrphanEpicCleanupJob 1h periodic |
| Joinery crash prod-ban | Közepes | 502 | systemd Restart=always + Nginx max_fails |

---

*SpaceOS — Doorstar Pilot Onboarding Architecture v4.0 · `/database-designer` + `/database-schema-designer` + `/senior-security` + `/senior-backend` reviewed · 2026-04-08*
*Státusz: IMPLEMENTÁCIÓRA KÉSZ — 13 finding beépítve, minden döntés lezárva*
