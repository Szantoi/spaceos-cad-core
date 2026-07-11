# SpaceOS — VPS Infrastruktúra Runbook
## Operations reference · deployment · disaster recovery

> **Verzió:** v1.0 — 2026-04-23
> **Cél:** Egyetlen forrás-dokumentum a SpaceOS production VPS-ről. Mit futtat, milyen portokon, milyen konfigokkal, hogyan indítsd újra, hol vannak a logok, mit csinálj, ha valami elromlik.
> **Típus:** Runbook (ops referencia, nem architektúra-vízió)
> **Célközönség:** a VPS-t üzemeltető személy (ma: Gábor; holnap esetleg csapattag)
> **Frissítés:** minden ops-beli változáskor ugyanabban a commit-ben a dokumentum is frissül. **Elavult runbook rosszabb, mint semmilyen runbook.**
> **Kapcsolódó:**
> - `SpaceOS_Product_Manifesto_v1.md` — miért van az egész
> - `SpaceOS_ProductionReadiness_Sprint_v4.md` — Keycloak telepítés részletei
> - `CabinetBilder_Architecture_Vision_v2.md` — CabinetBilder kliens, ami most ezt a szervert használja
> - `Codebase_Status_20260420.md` — aktuális teszt-állapot

---

## 0. Dokumentum használata

Ez **nem architektúra-terv**. Ez egy **operations runbook** — a szerver pillanatnyi állapotának rögzített tükre. Használd úgy:

- **"Mit futtat a szerver?"** → §2, §3
- **"Mi van hova telepítve?"** → §4 (fájlrendszer)
- **"Hogyan indítsam újra X-et?"** → §10 (restart parancsok)
- **"Mi van, ha valami leáll?"** → §12 (disaster recovery)
- **"Mit kell telepíteni az új CabinetBilder-kliens miatt?"** → §7.3 (Keycloak client config)
- **"Hogy deploy-oljak új kódot?"** → §9 (deploy pipeline)

**Szabály:** ha egy műveletet nem találsz ebben a dokumentumban, és muszáj csinálnod, utána **tedd bele**. A dokumentum csak akkor ér valamit, ha folyamatosan pontos.

---

## 1. Szerver alapadatok

| Tulajdonság | Érték |
|---|---|
| **Hostname** | `joinerytech.hu` (primary), `asztalostech.hu` (HU mirror) |
| **Public IP** | `109.122.222.198` |
| **OS** | Ubuntu 24.04 LTS |
| **Erőforrás** | (kitöltendő: CPU mag, RAM, SSD) |
| **Provider** | (kitöltendő: Contabo / Hetzner / egyéb) |
| **SSH port** | 22 (alapértelmezett — v2-ben érdemes ritkább portra migrálni) |
| **Firewall** | UFW — csak 22, 80, 443 bejövő |
| **Időzóna** | UTC (minden logolás UTC-ben) |
| **TLS** | Let's Encrypt (auto-renew cron) |
| **Admin user** | `gabor` (sudo) |
| **Deploy user** | `spaceos-deploy` (forced SSH command, CI-ből) |
| **Backup user** | `spaceos-backup` (read-only, scheduled dumps) |

---

## 2. Port-mátrix (mi fut, melyik porton, ki éri el)

### 2.1 Public (nginx mögötti) — az internetről elérhető

| Port | Szolgáltatás | Cél | Bind |
|---|---|---|---|
| **443** | Nginx HTTPS | Minden API + portál + auth | `0.0.0.0:443` |
| **80** | Nginx HTTP | Redirect 443-ra + Let's Encrypt ACME challenge | `0.0.0.0:80` |
| 22 | SSH | Admin + deploy | `0.0.0.0:22` |

### 2.2 Internal (csak localhost) — nginx proxy-ból elérhető

| Port | Szolgáltatás | Verzió | Cél | Systemd unit |
|---|---|---|---|---|
| **5001** | Kernel API (.NET 8) | ASP.NET Core | `/api/*` endpoints | `spaceos-kernel.service` |
| **5002** | Orchestrator BFF (Node.js 22) | Express 4 | `/bff/*` endpoints | PM2 managed, `pm2 list` |
| **5004** | Modules.Inventory | .NET 8 | Inventory API | `spaceos-inventory.service` |
| **5005** | Modules.Cutting | .NET 8 | Cutting API | `spaceos-cutting.service` |
| **5006** | Modules.Procurement | .NET 8 | Procurement API | `spaceos-procurement.service` |
| **8080** | Keycloak | 24.0.5 (Docker) | IdP + auth | `docker compose` a `/opt/spaceos/keycloak/` alatt |
| **5432** | PostgreSQL | 16 | Fő adatbázis | `postgresql.service` |
| **5433** | PostgreSQL | 16 | Keycloak DB (külön instance vagy DB) | (ellenőrzendő: külön port vagy külön DB az 5432-n) |
| **6379** | Redis | 7.4 | Cache + pessimistic lock | `redis-server.service` |
| **9000** | MinIO (API) | — | Object storage | `docker compose` vagy systemd |
| **9001** | MinIO (Console) | — | Admin UI | Ugyanaz |

### 2.3 Docker-belső (csak container network-ben)

A Keycloak saját DB-konténere (ha van) + MinIO backend nem exponálódik host-ra.

### 2.4 Ellenőrző parancs

```bash
sudo ss -tlnp | grep -v "127.0.0.53"
```

Output összehasonlítandó a fenti táblával. Ha eltérés van → frissítsd a táblát vagy állítsd le a nem várt szolgáltatást.

---

## 3. Szolgáltatások részletezése

### 3.1 Nginx

**Config path:** `/etc/nginx/sites-available/spaceos` + symlink `/etc/nginx/sites-enabled/spaceos`

**Szerepe:**
- TLS termináláskor (`joinerytech.hu`, `asztalostech.hu`, `eszkozok.joinerytech.hu` subdomain)
- Reverse proxy a belső szolgáltatásokhoz
- Rate limiting (per-kliens IP)
- Static file serving (Portal build)
- ACME challenge Let's Encrypt-nek

**Location-map (főbb):**

| Route | Target | Megjegyzés |
|---|---|---|
| `/` | static: `/opt/spaceos/portal-dist/` | Portal (multi-brand Turborepo build) |
| `/auth/` | `http://127.0.0.1:8080/auth/` | Keycloak proxy |
| `/auth/admin/` | `http://127.0.0.1:8080/auth/admin/` + `allow 127.0.0.1; deny all;` | Admin console NINCS publikus |
| `/api/` | `http://127.0.0.1:5001/` | Kernel API |
| `/bff/` | `http://127.0.0.1:5002/` | Orchestrator |
| `/modules/inventory/` | `http://127.0.0.1:5004/` | Inventory module |
| `/modules/cutting/` | `http://127.0.0.1:5005/` | Cutting module |
| `/modules/procurement/` | `http://127.0.0.1:5006/` | Procurement module |
| `/.well-known/acme-challenge/` | static: `/var/www/acme/` | Let's Encrypt |

**Rate limiting (jelenlegi):**
- `/auth/*` → 60 req/min per IP
- `/api/*` → 1000 req/min per IP (CabinetBilder-pluginok sok polling miatt)
- `/bff/*` → 300 req/min per IP

**Reload:** `sudo nginx -t && sudo systemctl reload nginx`

### 3.2 Keycloak

**Verzió:** 24.0.5 (Docker image: `quay.io/keycloak/keycloak:24.0`)
**Path:** `/opt/spaceos/keycloak/`
**Konfig:** `/opt/spaceos/keycloak/docker-compose.yml`
**Env fájl:** `/opt/spaceos/keycloak/.env` (nem verzionált, secret-eket tartalmaz)
**Bind:** `127.0.0.1:8080` (csak localhost — kifelé nginx-en keresztül megy)
**Hostname:** `joinerytech.hu` (issuer-ben így jelenik meg a JWT-ben)

**Realm:** `spaceos` (single realm a multi-tenant modellhez)

**Clientek a `spaceos` realmben:**

| Client ID | Típus | Célja |
|---|---|---|
| `kernel-api` | bearer-only | Kernel JWT validációhoz (nem login-client) |
| `portal-app` | public + PKCE | Browser-based portál (joinerytech.hu) |
| `orchestrator-bff` | confidential | Orchestrator BFF session-ökhöz |
| `test-runner` | confidential | E2E tesztek (Direct Access Grant engedélyezve!) |
| **`cabinetbilder-plugin`** | **public + Device Code Flow** | **Desktop plugin — CabinetBilder** ⟵ ÚJ |

**`cabinetbilder-plugin` konkrét beállítás — lásd §7.3.**

**Service management:**
```bash
cd /opt/spaceos/keycloak
docker compose ps                    # státusz
docker compose logs -f keycloak      # logok
docker compose restart keycloak      # restart
docker compose pull && docker compose up -d   # verzió-upgrade
```

### 3.3 PostgreSQL

**Verzió:** 16
**Port:** 5432
**Data dir:** `/var/lib/postgresql/16/main/`
**Config:** `/etc/postgresql/16/main/postgresql.conf` + `pg_hba.conf`

**Adatbázisok:**

| DB | Owner | Tartalom |
|---|---|---|
| `spaceos` | `spaceos` | Kernel + Modules (single DB, schema-separated) |
| `spaceos_keycloak` | `spaceos_keycloak_user` | Keycloak user adat |
| `spaceos_audit_sink` | `spaceos_audit_sink_user` | HashSink audit chain (bypass-RLS dedikált role) |

**Schemas a `spaceos` DB-ben:**
- `public` — Kernel core
- `spaceos_joinery` — Modules.Joinery
- `spaceos_cutting` — Modules.Cutting
- `spaceos_inventory` — Modules.Inventory
- `spaceos_procurement` — Modules.Procurement
- `spaceos_modules` — Modules.Abstractions (ProductTemplate + Graph Engine)

**RLS állapot:** minden tenant-scoped tábla `FORCE ROW LEVEL SECURITY` módban. `spaceos_audit_sink_user` role explicit `BYPASSRLS` (audit chain írásához).

**Nyitott minor gap** (a `Codebase_Status_20260411`-ből):
- `spaceos_schema_owner` DB role nem létezik — táblák `spaceos` user tulajdonában vannak
- Nem blokkoló, de jövőbeli migrációnál figyelni kell

**Connection string formátum** (env fájlokban):
```
ConnectionStrings__SpaceOsDb=Host=127.0.0.1;Port=5432;Database=spaceos;Username=spaceos_app;Password=<secret>
```

**Backup:** napi pg_dump cron (lásd §11).

### 3.4 Redis

**Verzió:** 7.4
**Port:** 6379
**Bind:** `127.0.0.1:6379` (localhost-only)
**Persistence:** AOF (append-only file) enabled
**Systemd unit:** `redis-server.service`

**Használata:**
- Pessimistic locking (SmartObject Push/Pull sync — Lua script atomikus lock)
- Cache (OpenID config, JWKS keys)
- Session store (FreeTier anonymous sessions, később)

**Connection string** (Kernel env):
```
Redis__ConnectionString=127.0.0.1:6379
Redis__InstanceName=spaceos:
```

**⚠️ Fontos:** ha a `Redis__ConnectionString` **hiányzik** a `kernel.env`-ből, a Kernel **in-memory fallback**-re esik vissza. Ez egy Kernel-instance-re jól működik, de 2+ instance-nél (horizontal scaling) a pessimistic lockok különböző folyamatokban születnek → race condition. **Single-VPS setupban most nem probléma**, de skálázáskor ellenőrizni kell.

**Ellenőrzés:**
```bash
grep -i redis /etc/spaceos/kernel.env     # kell legyen output
redis-cli ping                             # válasz: PONG
redis-cli INFO memory | grep used_memory_human
```

### 3.5 MinIO

**Fut:** Docker vagy systemd (tisztázandó — egyelőre docker-compose feltételezés)
**API port:** 9000 (localhost-only)
**Console port:** 9001 (localhost-only — admin UI nginx-en keresztül védve)
**Data volume:** `/opt/spaceos/minio/data/` (nagy lemez, WORM-compliance mode)
**Módok:**
- WORM Object Lock (Joinery Phase 2 ProofHash-hez)
- Compliance mode (a legszigorúbb — objektum törölhetetlen a retention alatt)

**Buckets (jelenleg):**

| Bucket | Tartalom | Retention |
|---|---|---|
| `spaceos-audit-sink` | Audit chain hash-anchoring | 7 év (WORM) |
| `spaceos-joinery-proofs` | Joinery ProofHash artifactok | 7 év (WORM) |
| `spaceos-portal-uploads` | Portal feltöltések | 30 nap |

**ÚJ bucket-ek amire szükség lehet** (CabinetBilder miatt):

| Bucket | Tartalom | Retention | Státusz |
|---|---|---|---|
| `cabinetbilder-drawings` | DWG-snapshot feltöltések (ha a plugin ezt csinálja) | 30 nap default, ProofHash-elt verziók 7 év | **Létrehozandó, ha tényleg feltöltünk DWG-t** |
| `cabinetbilder-bom-pdfs` | Generált PDF gyártásilapok | 90 nap | **Létrehozandó** |

**Ellenőrzés:**
```bash
docker exec -it minio mc alias set local http://localhost:9000 <ADMIN> <SECRET>
docker exec -it minio mc ls local
```

### 3.6 SpaceOS Kernel + Modules (systemd .NET szolgáltatások)

**Telepítési path:** `/opt/spaceos/<service>/`
**Env fájlok:** `/etc/spaceos/<service>.env` (szeparáltan, root-only olvasható)
**Logok:** `/var/log/spaceos/<service>.log` (+ journalctl)

**Systemd unit-ok:**

| Service | Unit file | Binary |
|---|---|---|
| Kernel | `/etc/systemd/system/spaceos-kernel.service` | `/opt/spaceos/kernel/SpaceOS.Kernel.Api.dll` |
| Inventory | `/etc/systemd/system/spaceos-inventory.service` | `/opt/spaceos/inventory/SpaceOS.Modules.Inventory.Api.dll` |
| Cutting | `/etc/systemd/system/spaceos-cutting.service` | `/opt/spaceos/cutting/SpaceOS.Modules.Cutting.Api.dll` |
| Procurement | `/etc/systemd/system/spaceos-procurement.service` | `/opt/spaceos/procurement/SpaceOS.Modules.Procurement.Api.dll` |

**Közös beállítás a unit fájlokban:**
```ini
[Service]
User=spaceos
WorkingDirectory=/opt/spaceos/<service>/
ExecStart=/usr/bin/dotnet <SERVICE>.dll --urls "http://127.0.0.1:<PORT>"
EnvironmentFile=/etc/spaceos/<service>.env
Restart=on-failure
RestartSec=10
StandardOutput=append:/var/log/spaceos/<service>.log
StandardError=append:/var/log/spaceos/<service>.log

[Install]
WantedBy=multi-user.target
```

**Env fájl minta (`/etc/spaceos/kernel.env`):**
```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__SpaceOsDb=Host=127.0.0.1;Port=5432;Database=spaceos;Username=spaceos_app;Password=<REDACTED>
ConnectionStrings__AuditSink=Host=127.0.0.1;Port=5432;Database=spaceos_audit_sink;Username=spaceos_audit_sink_user;Password=<REDACTED>
Redis__ConnectionString=127.0.0.1:6379
Redis__InstanceName=spaceos:
Minio__Endpoint=127.0.0.1:9000
Minio__AccessKey=<REDACTED>
Minio__SecretKey=<REDACTED>
Auth__Authority=https://joinerytech.hu/auth/realms/spaceos
Auth__Audience=kernel-api
AuditChain__GenesisHash=<REDACTED, deploy-invariáns>
```

### 3.7 Orchestrator (Node.js 22)

**Path:** `/opt/spaceos/orchestrator/`
**Process manager:** PM2
**Port:** 5002 (localhost-only)
**Config:** `/opt/spaceos/orchestrator/ecosystem.config.js`
**Env:** `/opt/spaceos/orchestrator/.env`

**Kezelés:**
```bash
pm2 list                                   # státusz
pm2 logs orchestrator                      # élő logok
pm2 restart orchestrator                   # restart
pm2 save                                   # persistence restart után
```

### 3.8 Portal static files

**Path:** `/opt/spaceos/portal-dist/`
**Build eredménye:** `pnpm turbo build --filter=joinerytech`
**Deploy:** CI másolja be a `portal-dist/` mappába, nginx ezt serválja

---

## 4. Fájlrendszer-layout

```
/opt/spaceos/
├── keycloak/
│   ├── docker-compose.yml
│   ├── realm-export.json          (verzionált, no secrets)
│   └── .env                        (secret — nem verzionált)
├── kernel/                         (Kernel deploy artifactjai)
├── orchestrator/                   (Node.js dist + node_modules)
├── inventory/                      (Modules.Inventory)
├── cutting/                        (Modules.Cutting)
├── procurement/                    (Modules.Procurement)
├── portal-dist/                    (Portal static build)
└── minio/
    ├── config/
    └── data/                       (object storage backend, NAGY)

/etc/spaceos/
├── kernel.env                      (chmod 600, owner: spaceos)
├── inventory.env
├── cutting.env
├── procurement.env
└── orchestrator.env                (symlinked from /opt/spaceos/orchestrator/.env)

/etc/nginx/sites-available/spaceos
/etc/letsencrypt/live/joinerytech.hu/
/etc/systemd/system/spaceos-*.service

/var/log/spaceos/                   (application logok)
├── kernel.log
├── inventory.log
├── cutting.log
└── procurement.log

/var/log/nginx/
├── access.log
└── error.log

/var/lib/postgresql/16/main/        (DB data dir)
/var/lib/redis/                      (Redis AOF + RDB)

/home/gabor/                         (admin user)
/home/spaceos-deploy/                (CI deploy user, forced command)
├── .ssh/authorized_keys              (ed25519, GitHub Actions public key)
└── .ssh/authorized_keys tartalom:
    command="/opt/spaceos/bin/deploy.sh",no-port-forwarding,no-X11-forwarding ssh-ed25519 AAAA... github-actions
```

---

## 5. Környezeti változók / secrets térkép

**Hol vannak secret-ek, és ki olvashatja őket:**

| Fájl | Owner | Mode | Tartalom |
|---|---|---|---|
| `/etc/spaceos/kernel.env` | spaceos | 600 | DB connection string, Keycloak audience, Redis, MinIO credentials, Audit Genesis Hash |
| `/etc/spaceos/inventory.env` | spaceos | 600 | DB connection string (saját schema) |
| `/etc/spaceos/cutting.env` | spaceos | 600 | Ugyanaz |
| `/etc/spaceos/procurement.env` | spaceos | 600 | Ugyanaz |
| `/opt/spaceos/orchestrator/.env` | spaceos | 600 | Keycloak client secret (orchestrator-bff), session secret |
| `/opt/spaceos/keycloak/.env` | root | 600 | Keycloak admin password, DB password, realm import key |
| `/root/.pgpass` | root | 600 | PostgreSQL superuser jelszó (csak manuális ops-hoz) |
| GitHub Actions Secrets | — | — | `VPS_HOST`, `VPS_DEPLOY_USER`, `VPS_DEPLOY_KEY` (ed25519 private key) |

**Secret rotation policy:**

| Secret | Rotation gyakoriság | Ki rotálja |
|---|---|---|
| Keycloak admin password | Évente + ha kompromittálódott | Manuális (Gábor) |
| DB user jelszavak | Évente | Manuális (Gábor) |
| Keycloak client secret (orchestrator-bff) | Évente | Admin console-ból, aztán `.env` frissítés |
| ed25519 deploy kulcs | Évente, vagy ha team-tag kilép | GitHub Actions Secrets frissítés |
| Let's Encrypt TLS cert | 90 napos — auto-renew (certbot cron) | Automatikus |
| Audit Genesis Hash | **SOHA** — deploy-invariáns | — |

---

## 6. Hálózati beállítások

### 6.1 DNS

| Rekord | Típus | Cél |
|---|---|---|
| `joinerytech.hu` | A | 109.122.222.198 |
| `www.joinerytech.hu` | CNAME | `joinerytech.hu` |
| `*.joinerytech.hu` | A (wildcard) | 109.122.222.198 |
| `asztalostech.hu` | A | 109.122.222.198 |
| `www.asztalostech.hu` | CNAME | `asztalostech.hu` |

A wildcard fontos az `eszkozok.joinerytech.hu` (FreeTier) és jövőbeli subdomain-ek (pl. `docs.*`, `api.*`) miatt.

### 6.2 UFW tűzfal-szabályok

```bash
sudo ufw status numbered
```

Elvárt állapot:
```
1. 22/tcp    ALLOW IN    Anywhere
2. 80/tcp    ALLOW IN    Anywhere
3. 443/tcp   ALLOW IN    Anywhere
4. Deny (default) incoming
5. Allow (default) outgoing
```

### 6.3 TLS tanúsítványok

**Let's Encrypt (certbot):**
- Path: `/etc/letsencrypt/live/joinerytech.hu/`
- Renewal: automata cron (`/etc/cron.d/certbot`)
- Wildcard támogatás: DNS challenge kell (HTTP-01 nem elég wildcard-hoz) — csak ha wildcard cert kell

**Ellenőrzés:**
```bash
sudo certbot certificates
sudo certbot renew --dry-run         # szimuláció
```

---

## 7. CabinetBilder integráció — új feladat

### 7.1 Kontextus

A CabinetBilder desktop plugin most már közvetlenül a SpaceOS Kernel-t és a Keycloak-ot hívja. A v1 release kapcsán **egy ténylegesen új ops-feladat** van: egy Keycloak client-et kell létrehozni.

**Ami már kész és nem kell semmit csinálni:**
- Redis fut (pessimistic lockingot támogat)
- MinIO fut (ha fájl kell, csak új bucket)
- Kernel API elérhető `/api/*` útvonalon
- JWT validation él (JWKS auto-discovery)

### 7.2 Ami hátravan — **egy Keycloak client konfiguráció**

### 7.3 Keycloak `cabinetbilder-plugin` client — lépésről lépésre

**1. SSH tunnel megnyitás (admin console elérés):**
```bash
ssh -L 8080:127.0.0.1:8080 gabor@109.122.222.198
```

**2. Böngészőben:** `http://localhost:8080/admin`
- Login: `<KC_ADMIN_USER>` / `<KC_ADMIN_PASSWORD>` (`/opt/spaceos/keycloak/.env`-ből)
- Realm switcher → `spaceos`

**3. Clients → Create client:**

| Fül | Mező | Érték |
|---|---|---|
| General | Client type | OpenID Connect |
| General | Client ID | `cabinetbilder-plugin` |
| General | Name | `CabinetBilder Desktop Plugin` |
| General | Description | `Desktop plugin client using Device Authorization Grant (RFC 8628)` |
| Capability | Client authentication | **OFF** (public) |
| Capability | Authorization | OFF |
| Login settings | Standard flow | OFF |
| Login settings | Implicit flow | OFF |
| Login settings | Direct access grants | **OFF** ← KRITIKUS |
| Login settings | Device authorization grant | **ON** ← EZ A LÉNYEG |
| Login settings | OAuth 2.0 Mutual TLS | OFF |
| Login settings | OIDC CIBA grant | OFF |
| Login settings | Service accounts roles | OFF |
| Login settings | Valid redirect URIs | (üres) |
| Login settings | Valid post logout redirect URIs | (üres) |
| Login settings | Web origins | (üres) |

**4. Advanced tab:**

| Mező | Érték |
|---|---|
| Access Token Lifespan | `15 Minutes` |
| Client Session Idle | `30 Days` |
| Client Session Max | `30 Days` |
| OAuth 2.0 Device Authorization Grant Enabled | On |
| OAuth 2.0 Device Polling Interval | `5` (seconds) |

**5. Client scopes tab → `cabinetbilder-plugin-dedicated`:**

Ugyanazok a mapper-ek kellenek, amik a Portal client-en vannak, legalábbis a `tenantId` és `tenantType` claim-ek. Másolási útmutató:

- Portal-client-ből másold át a `script-mapper` beállítást (vagy az ekvivalens `user-attribute-mapper`-eket)
- A `tenantId` mappolódjon ID-tokenbe és access-tokenbe is
- A `realm_access.roles` legyen látható az access-tokenben (CabinetBilder oldalán authorization policy-khez kell)

**6. Verifikáció curl-lel (saját gépről):**

```bash
# Device authorization request
curl -s -X POST "https://joinerytech.hu/auth/realms/spaceos/protocol/openid-connect/auth/device" \
  -d "client_id=cabinetbilder-plugin" \
  -d "scope=openid profile email"
```

**Helyes válasz (JSON):**
```json
{
  "device_code": "ABC123...",
  "user_code": "XYZW-9876",
  "verification_uri": "https://joinerytech.hu/auth/realms/spaceos/device",
  "verification_uri_complete": "https://joinerytech.hu/auth/realms/spaceos/device?user_code=XYZW-9876",
  "expires_in": 600,
  "interval": 5
}
```

Ha `404` vagy `unauthorized_client` → a client konfig hibás.
Ha `unsupported_grant_type` → Device authorization grant nincs bekapcsolva a client-en.

### 7.4 Opcionális — CabinetBilder-specifikus nginx rate-limit

Ha a plugin telepítési volumene növekszik (Free Tier terjedés), érdemes lehet külön rate-limit zone-ba tenni a CabinetBilder-endpointokat:

```nginx
# /etc/nginx/sites-available/spaceos (kiegészítés)
limit_req_zone $binary_remote_addr zone=cabinetbilder:10m rate=1000r/m;

location ~ ^/api/smart-objects/ {
    limit_req zone=cabinetbilder burst=100 nodelay;
    proxy_pass http://127.0.0.1:5001;
    # ... egyéb proxy_set_header-ök
}
```

**Most még nem kell** — v1 Free Tier soft launch alatt a default `/api/*` rate-limit elég.

### 7.5 Jövőbeli MinIO bucket-ek (ha a plugin DWG-t feltölt)

**Ha eljön a pont**, hogy a plugin DWG-snapshot-okat vagy PDF-gyártásilapokat tölt fel:

```bash
# SSH a VPS-re
docker exec -it minio mc alias set local http://localhost:9000 <ADMIN> <SECRET>

# Új bucket-ek
docker exec -it minio mc mb local/cabinetbilder-drawings
docker exec -it minio mc mb local/cabinetbilder-bom-pdfs

# Retention policy (30 nap default)
docker exec -it minio mc retention set --default compliance 30d local/cabinetbilder-drawings
docker exec -it minio mc retention set --default compliance 90d local/cabinetbilder-bom-pdfs
```

**Most még nem kell** — csak akkor, ha a Kernel új endpointja már fogadja a fájl-uploadot.

---

## 8. Egészség-ellenőrzés (health checks)

### 8.1 Gyors check script

```bash
#!/bin/bash
# /opt/spaceos/bin/healthcheck.sh

echo "=== SpaceOS VPS Health Check ==="
echo

echo "--- Nginx ---"
systemctl is-active nginx && echo "✅ nginx running"
curl -s -o /dev/null -w "%{http_code}\n" https://joinerytech.hu/

echo "--- Kernel API ---"
curl -s http://127.0.0.1:5001/healthz

echo "--- Orchestrator ---"
curl -s http://127.0.0.1:5002/health
pm2 list | grep orchestrator

echo "--- Keycloak ---"
curl -s http://127.0.0.1:8080/health/ready

echo "--- PostgreSQL ---"
sudo -u postgres psql -c "SELECT 1" -d spaceos > /dev/null && echo "✅ postgres (spaceos)"
sudo -u postgres psql -c "SELECT 1" -d spaceos_keycloak > /dev/null && echo "✅ postgres (spaceos_keycloak)"

echo "--- Redis ---"
redis-cli ping

echo "--- MinIO ---"
curl -s http://127.0.0.1:9000/minio/health/live

echo "--- Modules ---"
for svc in inventory cutting procurement; do
    port_map=("inventory:5004" "cutting:5005" "procurement:5006")
    # (loopolható, most manuálisan)
done
curl -s http://127.0.0.1:5004/healthz && echo "✅ inventory"
curl -s http://127.0.0.1:5005/healthz && echo "✅ cutting"
curl -s http://127.0.0.1:5006/healthz && echo "✅ procurement"

echo "--- Disk ---"
df -h / | tail -1
du -sh /opt/spaceos/minio/data/ 2>/dev/null

echo "--- Memory ---"
free -h

echo "=== End Health Check ==="
```

**Futtatás:** `bash /opt/spaceos/bin/healthcheck.sh`

### 8.2 Monitoring (javasolt, jelenleg nincs)

**Nincs** aktív Prometheus / Grafana setup. Ez P2-prioritás — a Manifesto 9. tézis (network density mérése) mérőszám-gyűjtést igényel.

**Minimális ajánlás:** uptime monitoring külső szolgáltatással (UptimeRobot, BetterStack) — ingyenes szintet megcélozva:
- `https://joinerytech.hu/` minden 5 perc — 200 OK elvárás
- `https://joinerytech.hu/auth/realms/spaceos/.well-known/openid-configuration` minden 15 perc — 200 OK
- `https://joinerytech.hu/api/healthz` minden 5 perc — 200 OK

Ha 2 egymást követő hiba → email + SMS riasztás.

---

## 9. Deploy folyamat (CI/CD)

### 9.1 Polyrepo GitHub-ok

| Repo | Ágak | Deploy target |
|---|---|---|
| `spaceos-kernel` | `develop` → staging, `main` → production | Kernel + Modules |
| `spaceos-orchestrator` | Ugyanaz | Orchestrator |
| `spaceos-design-portal` | Ugyanaz | Portal static |
| `spaceos-doorstar-portal` | Ugyanaz | Doorstar portal subdomain |
| `cabinetbilder` | — | Desktop plugin — **NEM szerver-deploy, hanem user-gépre telepítés** |

### 9.2 GitHub Actions deploy-folyamat

A CI build után SSH-n keresztül a `spaceos-deploy` user-rel futtatja a `/opt/spaceos/bin/deploy.sh` scriptet (forced command — nincs shell access).

**`deploy.sh` általános lépések (Kernel példa):**
```bash
#!/bin/bash
set -e

# 1. Megáll a régi szolgáltatás
sudo systemctl stop spaceos-kernel

# 2. Backup a meglévő deploy
cp -r /opt/spaceos/kernel/ /opt/spaceos/kernel.backup.$(date +%Y%m%d-%H%M%S)/

# 3. Új build kicsomagolás
tar -xzf /tmp/kernel-deploy.tar.gz -C /opt/spaceos/kernel/ --overwrite

# 4. Migráció futtatása
sudo -u spaceos /usr/bin/dotnet /opt/spaceos/kernel/SpaceOS.Kernel.Api.dll migrate

# 5. Szolgáltatás indítás
sudo systemctl start spaceos-kernel

# 6. Health check
sleep 5
curl -f http://127.0.0.1:5001/healthz || (echo "Health check failed!"; exit 1)

# 7. Régi backup cleanup (csak 3 utolsó megmarad)
ls -t /opt/spaceos/kernel.backup.*/ | tail -n +4 | xargs rm -rf
```

**Rollback parancs (manuális):**
```bash
sudo systemctl stop spaceos-kernel
rm -rf /opt/spaceos/kernel
mv /opt/spaceos/kernel.backup.<TIMESTAMP> /opt/spaceos/kernel
sudo systemctl start spaceos-kernel
```

### 9.3 Deployment gates (mielőtt production-re megy)

Minden production deploy előtt **kötelezően** zöld:
- [ ] `develop` branch CI zöld
- [ ] `Codebase_Status_YYYYMMDD.md` frissítve
- [ ] Ha új DB migráció van → staging-en végigfutott + rollback teszt
- [ ] Ha új secret / env változó kell → `/etc/spaceos/*.env` frissítve **még a deploy előtt**
- [ ] Manual health check után a `healthcheck.sh` minden ✅-t ad vissza

---

## 10. Restart / reload parancsok (gyors-kártya)

```bash
# Nginx (csak config változáskor)
sudo nginx -t && sudo systemctl reload nginx

# Kernel
sudo systemctl restart spaceos-kernel
sudo journalctl -u spaceos-kernel -f        # logok live

# Modules
sudo systemctl restart spaceos-inventory
sudo systemctl restart spaceos-cutting
sudo systemctl restart spaceos-procurement

# Orchestrator (PM2)
pm2 restart orchestrator
pm2 logs orchestrator

# Keycloak
cd /opt/spaceos/keycloak
docker compose restart keycloak
docker compose logs -f keycloak

# PostgreSQL (ritka)
sudo systemctl restart postgresql

# Redis
sudo systemctl restart redis-server

# MinIO
docker restart minio                          # vagy docker-compose restart

# Minden SpaceOS service újraindítás (lépcsős)
sudo systemctl restart spaceos-kernel
sleep 5
sudo systemctl restart spaceos-inventory spaceos-cutting spaceos-procurement
sleep 3
pm2 restart orchestrator
```

---

## 11. Backup stratégia

### 11.1 Ki mit backup-ol

| Adat | Gyakoriság | Hova | Retention |
|---|---|---|---|
| PostgreSQL `spaceos` DB | Napi 03:00 UTC | `/var/backups/spaceos/pg/` + off-site (jövőbeli) | 30 nap lokál, 1 év off-site |
| PostgreSQL `spaceos_keycloak` DB | Napi 03:00 UTC | Ugyanoda | Ugyanúgy |
| PostgreSQL `spaceos_audit_sink` DB | Napi 03:00 UTC | Ugyanoda | **7 év** (legal + WORM-tükör) |
| MinIO `spaceos-audit-sink` bucket | Már WORM-compliance, nem kell külön backup | — | Object Lock 7 év |
| MinIO `spaceos-joinery-proofs` | Ugyanaz | — | 7 év |
| MinIO `spaceos-portal-uploads` | Heti snapshot | Off-site | 30 nap |
| `/etc/spaceos/*.env` | Manuális, titkosítva | Gábor személyes gépe + offline | — |
| Keycloak realm export | Heti automatizált | `/opt/spaceos/keycloak/realm-export.json` (verzionált) | Git-ben |
| Nginx konfig | Git-be verzionált | GitHub | — |

### 11.2 Backup script (pg_dump példa)

```bash
#!/bin/bash
# /opt/spaceos/bin/backup-db.sh (cron: 0 3 * * *)

set -e
BACKUP_DIR=/var/backups/spaceos/pg
DATE=$(date +%Y%m%d)

mkdir -p $BACKUP_DIR

for DB in spaceos spaceos_keycloak spaceos_audit_sink; do
    sudo -u postgres pg_dump -Fc -Z9 -f $BACKUP_DIR/${DB}_${DATE}.dump $DB
    echo "✅ Backed up $DB"
done

# Off-site upload (TODO: S3/B2/rsync)
# aws s3 cp $BACKUP_DIR/ s3://spaceos-backups/pg/ --recursive --exclude "*" --include "*_${DATE}.dump"

# Cleanup old backups (30 napos local retention)
find $BACKUP_DIR -name "*.dump" -mtime +30 -delete
```

### 11.3 Restore teszt

**Kötelező gyakoriság:** negyedévente.
**Eljárás:** staging VPS-en (ha lesz ilyen) vagy lokális Docker PostgreSQL-ben:

```bash
# Új üres DB
createdb spaceos_restore_test

# Restore
pg_restore -d spaceos_restore_test /var/backups/spaceos/pg/spaceos_20260422.dump

# Verifikáció
psql -d spaceos_restore_test -c "SELECT COUNT(*) FROM public.\"Tenants\";"
```

**⚠️ Nem volt még formális restore-tesztelés.** Első restore-teszt ütemezése: a v2.1 roadmap tervezésnél felvesszük.

---

## 12. Disaster recovery — mi van, ha...

### 12.1 "A szolgáltatás nem válaszol — mit csináljak?"

**Lépés 1 — Diagnózis (2 perc):**
```bash
ssh gabor@109.122.222.198
bash /opt/spaceos/bin/healthcheck.sh
```

**Lépés 2 — melyik szolgáltatás hibás?**

| Tünet | Valószínű ok | Művelet |
|---|---|---|
| `nginx` DOWN | Config hiba | `sudo nginx -t` → javítás → reload |
| Kernel HTTP 500-ak | DB connection lost vagy kód-hiba | `journalctl -u spaceos-kernel -n 100` |
| Keycloak 503 | Container crash | `docker compose -f /opt/spaceos/keycloak/docker-compose.yml logs --tail 100 keycloak` |
| Postgres connection refused | Túl sok connection vagy out of disk | `df -h` + `sudo -u postgres psql -c "SELECT count(*) FROM pg_stat_activity;"` |
| 429 Rate limited az nginx-ben | Legitim forgalom vs támadás | `tail -f /var/log/nginx/access.log | grep '429'` |
| Minden DOWN | Szerver reboot / OOM | `uptime`, `dmesg | tail` |

**Lépés 3 — Escalation:**

Ha 15 perc után sem sikerült helyreállítani és production user érintett:
1. Írd le pontosan, mit próbáltál, és mit mutatnak a logok (screenshot vagy copy-paste)
2. Ha van csapattag → értesítés
3. Ha egyedül: vedd fel a "legutóbbi stabil állapot" helyreállítási lépéseit (lásd §12.2)

### 12.2 "Teljes szerver-veszteség — a VPS-provider elvesztette" (katasztrófa-forgatókönyv)

**Helyreállítási idő célkitűzés (RTO):** 4 óra
**Helyreállítási pont célkitűzés (RPO):** 24 óra (napi backupból)

**Előfeltétel:** off-site backup-olva a legutóbbi DB dump + `/etc/spaceos/*.env` + nginx config + Let's Encrypt cert.

**Lépések:**

1. **Új VPS provisioning** (Ubuntu 24.04, azonos méret) — ~30 perc
2. **DNS átirányítás** joinerytech.hu → új IP (TTL függvényében 5 perc – 24 óra) — jó esetben 5 perc
3. **Szoftver-telepítés** saját Ansible / manuális script szerint:
   - PostgreSQL 16 + Redis + nginx + Docker
   - `spaceos` user, `deploy-spaceos` user
   - `/opt/spaceos/` struktúra
4. **Secrets visszatöltés** off-site-ról (`/etc/spaceos/*.env`)
5. **DB restore:**
   ```bash
   sudo -u postgres createdb spaceos
   sudo -u postgres pg_restore -d spaceos /tmp/spaceos_<LATEST>.dump
   # ugyanez a keycloak és audit-sink DB-re
   ```
6. **Keycloak container indítás** + realm-import
7. **Kernel + Modules deploy** GitHub Actions-ból
8. **TLS cert újra-kérés** Let's Encrypt-től
9. **Health check**

**Ez a forgatókönyv még nem lett tesztelve.** — a következő ops-sprint (tervezett 2026 Q3) prioritása.

### 12.3 "Az Audit Genesis Hash elveszett" — worst case

Ez a legsúlyosabb scenario. Az Audit Genesis Hash **deploy-invariáns** — ha elveszik, az audit-chain integritása megszűnik (minden későbbi hash érvénytelenné válhat a verifikáció szempontjából).

**Védelem:**
- A Genesis Hash a `/etc/spaceos/kernel.env` INFRA-096 javítás után van tárolva
- **3 helyen** kell tárolnia:
  1. A VPS `/etc/spaceos/kernel.env`-ben (production)
  2. Off-site backup (titkosított, Gábor személyes device-án)
  3. **Egy fizikai papíron** (nyomtatva, széfben / bankszéfben) — végső backup

Ha 1-es és 2-es is elveszik, és nincs 3-as → az egész audit-chain újraalapozását kell megtenni, ami **jogilag is problémás** egy live tenant-nál.

**Ez a kulcs dokumentálva van-e jelenleg 3 helyen?** ⟵ **ACTION ITEM, verifikálni kell.**

---

## 13. Karbantartási ablakok

**Ajánlott karbantartási ablak:** vasárnap 03:00-04:00 UTC (HU idő 05:00-06:00). Minimális forgalom ilyenkor.

**Bejelentés:** lehetőleg 48 órával korábban — Portal-on banner + Doorstar-nak email.

**Frissítési prioritások:**
| Kategória | Gyakoriság |
|---|---|
| Ubuntu security updates | Havi (`apt upgrade`) |
| Nginx / PostgreSQL / Redis minor | Negyedéves |
| Keycloak minor | Negyedéves |
| Keycloak major (24 → 25 → 26) | Évente, tesztelt staging után |
| PostgreSQL major (16 → 17) | 2+ évente, szigorúan staging-tesztelt |

---

## 14. Biztonsági incidens protokoll

### 14.1 Gyanús jelek

- Nginx access log-ban váratlan geo-mintázat (pl. Kína/Oroszország > 20%-ra)
- `auth.log`-ban sikertelen SSH-kísérletek burst-ben
- Kernel log-ban ismeretlen endpoint-ok 401/403 spike
- Keycloak log-ban ismeretlen user-ek createUser attempt-jei
- Disk I/O hirtelen emelkedés okkal

### 14.2 Incident response lépések

**Lépés 1 — Izolálás:**
- UFW-n átmenetileg blokk gyanús IP-ket
- Szükség esetén `systemctl stop` egyes szolgáltatásokat
- **NE töröld a logokat** — minden incidensnyom kell

**Lépés 2 — Eredmény-kinyerés:**
```bash
# Másold le a logokat egy snapshot mappába
sudo cp -r /var/log/nginx /var/log/spaceos /root/incident-$(date +%Y%m%d-%H%M)/
```

**Lépés 3 — Mitigáció:**
- Jelszó-rotáció (ha kompromittálódott)
- Új TLS cert (ha private key-re gyanú)
- Érintett tenant-ok értesítése (GDPR — 72 órán belül kötelező ha személyes adat érintve)

**Lépés 4 — Post-mortem:**
- Dokumentum: mi történt, mit tettünk, mi a javítás
- Runbook-frissítés ha szükséges

---

## 15. Nyitott / rendezetlen ügyek

| # | Téma | Súly | Megjegyzés |
|---|---|---|---|
| 1 | `spaceos_schema_owner` DB role nem létezik | 🟡 MEDIUM | Táblák `spaceos` user tulajdonában. Jövőbeli migrationnál figyelni kell. |
| 2 | Nincs off-site backup upload | 🟠 HIGH | Szerver-veszteség esetén adat-veszteség — S3/B2 integráció kell |
| 3 | Audit Genesis Hash 3x-tárolás ellenőrzése | 🔴 CRITICAL | Ma is élő action item — verifikálandó! |
| 4 | Nincs restore-teszt protokoll | 🟠 HIGH | Első teszt ütemezendő a v2.1 roadmap-be |
| 5 | Nincs Prometheus/Grafana monitoring | 🟡 MEDIUM | Minimum külső uptime-monitor (UptimeRobot) azonnal javasolt |
| 6 | SSH port 22 alapértelmezetten (sok brute-force) | 🟡 MEDIUM | Migrálás ritkább portra (pl. 2222) + fail2ban konfiguráció |
| 7 | Disaster recovery (§12.2) még nem tesztelt | 🟠 HIGH | Negyedéves drill ajánlott — staging-rendszerrel |
| 8 | Keycloak admin password rotáció policy | 🟡 MEDIUM | Utolsó rotáció dátuma dokumentálatlan |
| 9 | CabinetBilder `cabinetbilder-plugin` client létrehozás | 🟢 LOW | Ezen dokumentum §7.3 lépései szerint — 15 perc |
| 10 | Rate limit tuning CabinetBilder-forgalomhoz | 🟢 LOW | Aktuálódik, amikor > 10 plugin-user elérhetetlen lesz a monitoringban |

---

## 16. Változásnapló (a dokumentumé)

| Dátum | Verzió | Változás | Szerző |
|---|---|---|---|
| 2026-04-23 | v1.0 | Kezdeti verzió — meglévő szétszórt ops-információ összegyűjtése, CabinetBilder Keycloak client hozzáadása | Gábor + Claude |

**Következő revízió:** ha bármelyik §15 nyitott ügy lezárul, vagy egy új szolgáltatás települ a VPS-re.

---

## 17. Hogyan használd ezt a dokumentumot

**Napi rutinhoz:**
- Ha valami furcsa → §12 disaster recovery
- Ha új kódot kell deploy-olni → §9
- Ha új szolgáltatást kell elindítani → §10 restart parancsok

**Tervezéshez:**
- Ha architektúra-kérdés van → `CabinetBilder_Architecture_Vision_v2.md` vagy `SpaceOS_*_Architecture_v4.md`
- Ha üzleti elv kell → `SpaceOS_Product_Manifesto_v1.md`
- Ha **ops-kérdés** van → **ez a dokumentum**

**Ha módosítasz a VPS-en:**
- Frissítsd a megfelelő szakaszt ebben a dokumentumban **ugyanabban a munkamenetben**, amikor a módosítást tetted
- Ha új szolgáltatás: §2 port-mátrix + §3 részletek + §10 restart
- Ha új secret: §5 secrets térkép
- Ha új backup-célpont: §11

**Elavult runbook rosszabb, mint nincs runbook.** Ha gondolkodsz rajta, érdemes-e frissíteni — **igen**, érdemes.

---

*SpaceOS · VPS Infrastructure Runbook v1.0 · 2026-04-23*
*Státusz: ÉLŐ — minden ops-beli változásnál frissítendő*
