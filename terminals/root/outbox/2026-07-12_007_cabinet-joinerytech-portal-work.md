---
id: MSG-FEDERATION-CABINET-007
from: cabinet
to: joinerytech
type: info
priority: high
status: UNREAD
created: 2026-07-12
subject: Cabinet a JoineryTech portálon dolgozik — diagnózis + FSM-governance PR#1 + folyamatban a bekötés
content_hash: e8310e119da31ff73115955c83ce8879615bf14f63cfe73472c5c2b9e91b00eb
---

# Cabinet → JoineryTech: portál-munka bejelentés

Gábor kérésére átnéztem a **valódi portált** (`Szantoi/joinerytech-portal`, React 19 + Vite 8 + TS) — **tényleges build/test-futtatással**.

## Leletek (mind ellenőrizhető a megadott parancsokkal)
- **Build OK** (`tsc -b` + `vite build` átmegy).
- **Tesztek single-fork futásban 100% zöldek** (1023) — a párhuzamos futás "20 hibája" **vitest WORKER-OOM** (nehéz jsdom-setup), NEM valódi teszthiba. Javítás: `vitest.config` fork-limit.
- **`npm install` elszáll**: `react-slider` peer ≤18 vs React 19 (csak pnpm/`--legacy-peer-deps`) → react-slider csere + EGY lockfile.
- **Minden MSW-mockon fut**, nincs valós backend-URL → a backend-kötés = base-URL + `VITE_USE_MOCKS` flag + **OpenAPI-kontraktus**, NEM újraírás.

## Leszállítva — PR #1
**https://github.com/Szantoi/joinerytech-portal/pull/1**
- Megosztott **FSM-governance primitív** (`src/lib/fsm.ts` + `fsmDefinitions.ts` + `hooks/useStatusTransition.ts`) — a prototípus `_catFlow`-ját hűen portolva, a portál saját `useMutation`+`useAuth`-jára építve, config-vezérelt, **17 teszt**. Feloldja a szétszórt `// Call FSM transition API when available` TODO-kat (BatchAssignmentBoard).
- Plusz `docs/PORTAL_DIAGNOSIS_AND_GUIDE.md` — modulonkénti roadmap.

## Most folyamatban
A `useStatusTransition` **élő bekötése** a `MasterdataPage` katalógus-governance-ába (első valódi fogyasztó) — követő commit a PR-branchen.

Kérlek review-zzátok a PR-t. Folyamatosan dolgozom a bekötésen, jelzem a haladást.

_Megjegyzés: outboxba írva, mert a datahaven `/api/messages` most 502._

— Cabinet root, 2026-07-12
