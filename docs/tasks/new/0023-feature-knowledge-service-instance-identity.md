# Task ID: 0023
# Title: Knowledge-service példány-identitás (sziget-alap)
# Category: feature
# Milestone: 13
# Status: new

## Szándék (Intent)

Minden knowledge-service példány azonosítsa magát: a `/health` és a `get_service_status` adja vissza az `instance`, `project`, `environment` mezőket. Enélkül a lokál dev és a VPS prod példány megkülönböztethetetlen (lásd 2026-07-06 session: a port-forward miatt órákig a rossz példányt konfiguráltuk).

## Elfogadási kritérium (Acceptance Criteria)

- [ ] `.env`: `INSTANCE_NAME`, `PROJECT_NAME`, `ENVIRONMENT` (dev/prod) változók
- [ ] `/health` válaszban: `instance`, `project`, `environment` mezők
- [ ] `get_service_status` MCP tool válaszában ugyanezek
- [ ] Lokális dev példány külön porton fut (javaslat: 3457), `.env`-ben dokumentálva
- [ ] `islands.json` regiszter a workspace gyökérben: sziget → url + instance név lista
- [ ] VPS példány frissítése egyeztetve a VPS-csapattal (ne törjön ERP-oldali integrációt)

## Tanúsítás (Evidence)

- Kód: `knowledge-service-0.0.01/src/bootstrap/` + `src/interfaces/http/routes/health.routes.ts`
- Teszt: vitest unit a health route-ra (mezők jelen vannak)
- Kézi teszt: lokál `curl localhost:3457/health` vs VPS-forward `curl localhost:3456/health` → különböző instance nevek

## Megjegyzések (Notes)

- Ez a sziget-architektúra (0025) előfeltétele.
- Kapcsolódó terv: `docs/Terv_MCP_Sziget_Roadmap_2026H2.md` (1. fázis).

---

**Started:**
**Completed:**
**Duration:**
**Owner:**
