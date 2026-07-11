# INFRA Terminál Kontextusa

> **Source:** Infra memory sync 2026-06-17

---

## Szerepkör

Az INFRA terminál **SpaceOS VPS operátor és fejlesztő**. Feladata:
- Production deploy (Kernel, Orchestrator, modulok)
- VPS konfigurációs üzemeltetés (systemd, PostgreSQL, nginx, Keycloak)
- Incident response (debug, rollback, failover)
- Documentation (gotchas, runbook-ok)

---

## Kritikus VPS infrastruktúra tények

### Fájlok és repók

| Leírás | Helye | Megjegyzés |
|---|---|---|
| Kernel forráskód | `/opt/spaceos/SpaceOS.Kerner/` | **Elgépelt dir név!** Nem `Kernel`, hanem `Kerner`. |
| Kernel deploy target | `/opt/spaceos/spaceos-kernel/publish/` | Systemd binárisok futnak innen. |
| Kernel DB | `spaceos` | NEM `spaceos_kernel`. Connection string: `Host=localhost;Port=5433` |
| Orchestrator | `/opt/spaceos/spaceos-orchestrator/` | PM2-vel futtatva. |
| Orchestrator .env | `/opt/spaceos/spaceos-orchestrator/.env` | PM2 olvassa, NEM `/etc/spaceos/orch.env`. |

### Felhasználó-szeparáció

| Role | User | Célok |
|---|---|---|
| Git pull + build | `gabor` (uid 1000) | Repository owner. |
| Service futtatás | `spaceos` (systemd) | App runtime. |
| PM2 & Node | `root` | Orchestrator PM2 daemon. |
| PostgreSQL | `postgres` | DB admin. |

**Kritikus:** `git pull` csak `gabor` user-rel megy (dubious ownership error egyébként). `systemctl` és `psql` a megfelelő felhasználóval.

### Portok & Loopback

| Service | Port | Type |
|---|---|---|
| Kernel | 5000 | loopback-only, reverse proxy behind nginx |
| Joinery | 5002 | loopback-only |
| Abstractions | 5003 | loopback-only |
| Orchestrator | 3000 | loopback-only |
| Keycloak | 8080 | loopback-only |
| PostgreSQL natív | 5433 | TCP, Unix socket `/var/run/postgresql` |
| PostgreSQL Docker | 5432 | Docker network (pgAdmin) |
| Nginx | 80/443 | Public-facing reverse proxy |

**Kritikus:** App connection string NEM `127.0.0.1`, hanem `localhost` — különben a Docker PG-t találja meg!

### Deployment csapdák (top 10)

1. **EF Core tool verzió mismatch** → manuális SQL kell
2. **Stale model snapshot** → `dotnet ef migrations` nem frissít
3. **Git user + service user confusion** → dubious ownership, readonly errors
4. **PM2 not in PATH** → `sudo env PATH=$PATH:/root/.npm-global/bin pm2 restart ...`
5. **EF migration suppressTransaction partial-apply** → DB van, history nincs
6. **dotnet publish incremental DLL bug** → `rm -rf publish/` majd fresh build
7. **Nginx AllowedHosts 127.0.0.1 restriction** → HTTP 400 ha proxy-n van
8. **Port 5432 vs 5433 confusium** → 5432 = Docker, 5433 = natív
9. **Kernel repo path typo** → SpaceOS.Kerner, nem Kernel
10. **Orchestrator .env vs /etc/spaceos/** → .env az orchestrator mappában van

---

## AuditChain:GenesisHash — KRITIKUS érték

⚠️ **Ha ez elvész, az összes audit event chain TÖRÖTT lesz.**

```
2642d19566d737defbb9bf29e4fe98fa20a1d2dd0700ea2ecb05ced877758fac
```

**Deployment-invariáns helyzete:** `kernel.env` → `AuditChain__GenesisHash=2642d195...`

**Addig:** `appsettings.Production.json` (INFRA-096 előtt) — de `dotnet publish` felülírja!

**VPS újraépítés:** Kötelezően visszaállítani mielőtt kernel indul!

---

## Kommunikációs preferenciák

- **Magyarul ír** — konkrét parancsok, listák előnye az esszénél
- **Bash tool használat:** Standard deploy parancsok közvetlenül futhatók (git, curl, systemctl)
- **BLOCKED mikor:** Más terminál territory-ja (pl. kernel .Designer.cs regenerálás), vagy 2 próba után nem derül ki a root cause
- **Rollback OK önállóan** — ha a deploy kontext-jében van

---

## Doorstar Soft Launch prioritás

Minden infra döntésnél: **"Ez tartja a 2026 Q2 élesítést?"**

- Stabilitás > új feature
- Rollback-elhetőség kötelező
- Backup minden deploy előtt

---

## Források

- `/opt/spaceos/infra/CLAUDE.md` — Infra terminál teljes dokumentációja
- `docs/knowledge/deployment/KNOWN_GOTCHAS.md` — Top 10 csapda részletezve
- `docs/knowledge/deployment/DEPLOYMENT_RUNBOOK.md` — Deploy step-by-step (ha létezik)
