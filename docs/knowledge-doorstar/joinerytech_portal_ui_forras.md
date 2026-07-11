# JoineryTech Portál — UI design-forrás és a Doorstar illeszkedés

> Gábor jelölte ki (2026-07-08): a Doorstar műhely-demo a **SpaceOS megvalósításhoz + JoineryTech portálhoz** illeszkedjen.
> UI-terv helye: `C:\Users\szant\Downloads\joinerytech` (teljes prototípus: JSX forrás + `build/` lefordított JS + `apakovasz/` ügyfél-példa).

## A JoineryTech háromréteg-architektúra (forrás: apakovasz/CORE_MAP.md)

```
BRAND    (ügyfelenként): szín · logó · hangnem · persona   — pl. Doorstar brand
DOMAIN   (ágazatonként): termék/művelet/állomás szótár      — asztalos: szabászat/élzárás/CNC
CORE     (domén-vak, közös): FSM-motor · kapacitás-ütemező · BOM/MRP · raktár · rendelés-pipeline · feladat-aggregátor · store
```

Bizonyított: az **Apakovász pékség** ugyanazon a magon fut, csak konfiguráció + adapter + brand (nincs új üzleti logika). → A JoineryTech portál egy **domén-független vállalatirányítási platform**, nem asztalos-specifikus.

## A Doorstar műhely-modul illeszkedése (KULCS felismerés)

A műhely-követő **NEM új rendszer** — a JoineryTech portál egy nézete:
- **BRAND** = Doorstar (szín/logó)
- **DOMAIN** = asztalos-adapter (szabászat→élzárás→CNC→…→csomagolás állomások)
- **CORE** = a feladat-aggregátor + FSM (állapot: elkezdve/kész) + kapacitás-ütemező vizuális megjelenítése

A prototípus MÁR tartalmazza a releváns oldalakat (`build/`-ben lefordítva):
- **`page-prodterminal`** — gyártás-terminál (valószínűleg a szakmunkás/műhely-terminál nézet!)
- **`page-workshop`**, **`page-production`**, **`page-workflow`**, **`page-prodsched`** (gyártás-ütemezés), **`page-floorplan`**
- **`mobile-nav`** — mobil navigáció (a szakmunkás-telefon use-case!)

→ A demót ezekre kell építeni, nem tiszta lapra. A "tiszta lap" korábbi döntés FELÜLÍRVA: JoineryTech portál-illeszkedés a cél.

## Design-forrás TISZTÁZVA (VPS válasz, MSG-CABINET-BRIDGE-017)

- ✅ **HIVATALOS forrás:** `/opt/spaceos/datahaven-web/client/` — VPS éles **React 19.2.6** + TypeScript + Vite, **"Industrial" design system** (inline styles, **dark-first**, LED-ek, jog-wheel). Komponensek: `Industrial/TerminalRack.tsx`, `Industrial/JogWheel.tsx`, `Graph/EpicGraph.tsx`, `Dashboard/TerminalCard.tsx`. Színek pl.: WORKING=zöld radial-gradient, IDLE=kék, OFFLINE=szürke.
- ❌ **NEM hivatalos (legacy):** `Downloads/joinerytech` (és `/opt/spaceos/docs/joinerytech/`) — 2021–2026 proof-of-concept, monolitikus (488KB app-store.jsx), JSX-build, standalone. **Csak UI-koncepció referencia** (a `page-prodterminal.jsx`/`page-workshop.jsx` értékes mint minta), NEM éles kód. → a korábbi feltárásom design-NYELVE (Tailwind stone/teal, StatusPill, MobileBottomNav) a LEGACY-é; az éles az Industrial dark.
- `JoineryTech.Flow.Web` (React 19) — nem ezt jelölte meg a VPS hivatalosként; a datahaven-web az.

**SpaceOS Vision Master 4-réteg** (a CORE/DOMAIN/BRAND hivatalos megfeleltetése):
- Layer 4 BRANDS (React portálok: JoineryTech, DesignPortal…) = **BRAND**
- Layer 3 ORCHESTRATOR (BFF, Node.js)
- Layer 2 DRIVERS (`Modules.Joinery`, C# .NET) = **DOMAIN adapter**
- Layer 1 KERNEL (Auth, FSM, Escrow, C#) = **CORE**
→ **Doorstar = asztalos domain-adapter (Layer 2) + Doorstar brand (Layer 4)** — a VPS ✅ megerősítette, helyesen illeszkedik.

**Root DÖNTÉS: APPROVE** — a Cabinet sync-elheti az Industrial komponenseket a Claude Designba. A műhely/gyártás oldalak (prodterminal, workshop, mobile-nav) az éles FE-ben MÉG NINCSENEK → a Doorstar műhely-terminál **ÚJ fejlesztés** (a legacy page-prodterminal csak inspiráció), React 19 + Industrial designban. Formális design-token/Storybook NINCS — a Cabinet extractálja a tokeneket a TSX inline-style-okból.

⚠️ **NYITOTT DESIGN-TENZIÓ (Gábor döntése kell):** a hivatalos "Industrial" esztétika **dark, LED, ipari-monitoring** (a 17-terminál rack figyeléséhez tervezve) — ez vs. a **szakmunkás-telefonos, egyszerű, világos kanban** persona-igény. A VPS az Industrial-t ajánlja + a prodterminal sárga/zöld/piros logikát; de a szakmunkás-UX világosabb/egyszerűbb irányt kívánhat.

## Egyéb hasznos anyagok a prototípusban

- `arazas-egyedi-kutatas.md` — árazási kutatás (kapcsolódik a Kalkulátor/11-lépéses árkalkulációhoz)
- `page-crm`, `page-controlling`, `page-contracts`, `page-configurator`, `item-builder`, `design-item-wizard`, `catalog-manager` — a teljes ERP-portál oldalai
- `CLAUDE.md`, `BUILD_NOTES.md`, `ENTITY_LINKS.md`, `EHS_PLAN.md` — a prototípus saját doksijai
