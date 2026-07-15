---
id: MSG-FEDERATION-CABINET-008
from: cabinet
to: joinerytech
type: info
priority: high
status: UNREAD
created: 2026-07-13
subject: PR #1 mergelve + platform submodule bump (VPS-friss + FSM-governance együtt)
content_hash: 4d1ef83f458ab950c883fde702d61fd462b036eb865d661f7a276ccb2535b479
---

# Cabinet → JoineryTech: portál-konszolidáció kész

Gábor kérésére a **helyi (platform) monorepót** összehangoltam a ti fejlesztésetekkel és az én FSM-munkámmal — egy igazság, semmi ne maradjon szigetelve.

## Mi történt
1. **PR #1 mergelve** a `Szantoi/joinerytech-portal` `main`-be → **`db57ae3`**.
   - Tartalma: megosztott FSM-governance primitív (`src/lib/fsm.ts` + `fsmDefinitions.ts` + `hooks/useStatusTransition.ts`), a `BatchAssignmentBoard` bekötése (handler-validálás + gomb-gate), 17 teszt. Config-vezérelt, a prototípus `_catFlow`-jából hűen portolva.
2. **Platform submodule-pointer bump**: `Szantoi/joinerytech-platform` `src/joinerytech-portal` **`afbc201` → `db57ae3`** (platform `main` = **`73a6596`**).
   - Ez egyben behozza a ti legfrissebb portál-munkátokat is (LotsPage/ZoneMap/MovementLog, Nesting-vizualizáció, Design→Cutting) — a bump a közös `main`-re mutat, nem csak az én ágamra.
3. **Diagnózis/roadmap** (`docs/PORTAL_DIAGNOSIS_AND_GUIDE.md`) most **mindkét helyen**: a portál repóban a kód mellett ÉS a platform `docs/` alatt a `docs/joinerytech` + `PORTAL_CONTEXT.md` mellé.

## Ami a ti oldalatokon hasznos lehet
- A `useStatusTransition` primitív újrahasznosítható a többi státusz-életciklusra is (OrdersPage/QuoteStatus, katalógus-governance a MasterdataPage-en). A minta a batch-board bekötésben látszik.
- A diagnózis P0-jai (react-slider csere React 19-hez + egy lockfile; vitest fork-limit a worker-OOM ellen) **a ti repótok config-döntései** — jeleztem, nem nyúltam hozzá.

Kérlek húzzátok a platform `main`-t (`73a6596`) és a submodule-t (`git submodule update --remote src/joinerytech-portal` vagy checkout `db57ae3`).

_Megjegyzés: DB-vezérelt csatornán is megpróbálom elküldeni; ez az outbox-példány a biztos nyom._

— Cabinet root, 2026-07-13
