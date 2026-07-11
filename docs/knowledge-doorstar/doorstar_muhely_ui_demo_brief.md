# Doorstar Műhely-követő — demó UI design brief (Claude Design-hoz)

> Cél: a Doorstar műhely munkafolyamat-követő **demó megjelenésének** megtervezése Claude Designnel.
> Kontextus: `doorstar_workflow_tracking_igeny.md` (üzleti igény) + `doorstar-gyartas-workflow` (folyamat).

## Kit szolgál (persona-vezérelt)

1. **Műhelyvezető** — szakmunkás, **CSAK telefont kezel**. Ő váltja a státuszokat. → A fő UI az ő telefonjára készül.
2. **Tulaj / sales** — élő áttekintést akar (a Viber-fotó kiváltása), telefonon vagy gépen.

## Szakmunkás-UX alapelvek (nem alkudható)

- **Mobil-first, hüvelykujj-barát**: nagy érintőfelületek, egy kézzel kezelhető.
- **Szín = állapot** (a papír szövegkiemelő-logika): SZÜRKE=hátravan · **SÁRGA**=folyamatban · **ZÖLD**=kész.
- **Egy koppintás = egy állapotváltás.** Nincs űrlap, nincs menürengeteg.
- **Minimális szöveg, sok ikon.** Magyar nyelv.
- Azonnal érthető "min dolgozom / mi van kész / mi csúszik".

## Demó-képernyők (MVP scope)

### 1. Műhely-nézet (műhelyvezető telefonja) — a FŐ képernyő
- Egy kiválasztott projekt (`DSMR 26xxx – Ügyfél`) **munkamenet-lépései** függőleges kártyákként (Szabászat → … → Csomagolás).
- Minden kártya: lépés neve + nagy állapot-jelző (szürke/sárga/zöld) + egy koppintás a következő állapotra.
- Legalul kiemelt művelet: **„Csomagolva → Kiszállítható"** gomb (zöld), ha minden lépés kész.
- Fejléc: projekt neve + határidő + haladás (pl. „4/9 kész").

### 2. Projekt-lista (műhelyvezető)
- Az aktív DSMR-projektek kártyái: ügyfél, haladás-sáv (pl. 4/9), **határidő + csúszás-jelzés** (piros, ha csúszik).
- Koppintás → belép az 1. képernyőre.

### 3. Élő áttekintő (tulaj/sales)
- Az összes aktív projekt státusza **egy pillantásra** — a Viber-fotó digitális, valós idejű megfelelője.
- Csúszó projektek felül/kiemelve. Szűrés határidő szerint.

## DÖNTÉS: HIBRID esztétika (Gábor, 2026-07-08)

A demó a **hivatalos Industrial design-DNS-ét** (VPS datahaven-web React 19: színek, státusz-logika sárga/zöld/piros, platform-tokenek) követi, DE a **szakmunkás-telefonra világosabbra és egyszerűbbre** szabva (nagy érintőfelületek, minimál). Egyesíti a SpaceOS-konzisztenciát a szakmunkás-UX-szel. → Az Industrial tokeneket a VPS TSX komponenseiből extractáljuk (FILE-TRANSFER kérve), a világos/egyszerű nézetet a szakmunkás-persona vezérli. A legacy `page-prodterminal.jsx` (Tailwind stone/teal, alább) UI-koncepció-referencia marad, de a design-NYELV az Industrial-ból jön.

## Industrial design-tokenek (kinyerve a VPS TSX komponenseiből, 2026-07-08)

> Forrás: `terminals/root/inbox/files/industrial-components/` (TerminalRack, JogWheel, TerminalCard, TerminalGrid, IndustrialKanbanPage, index.css) — FILE-TRANSFER a VPS-től (MSG-CABINET-BRIDGE-018).

**Egyetlen "hivatalos" token-forrás: `index.css :root`** (a többi komponens saját, inkonzisztens hex-eket használ inline style-ban):
```
--bg-primary:#16171d  --bg-secondary:#1f2028  --bg-tertiary:#2e303a
--text-primary:#f3f4f6  --text-secondary:#9ca3af  --text-muted:#6b7280
--accent-green:#10b981  --accent-yellow:#f59e0b  --accent-red:#ef4444
--space-xs:4px --space-sm:8px --space-md:16px --space-lg:24px --space-xl:32px
--touch-target-min: 44px   ← közvetlenül újrahasznosítható mobil-a11y token!
```

**A hibrid demóhoz javasolt leképezés:**
- **Kész (zöld)** → `#10b981` (--accent-green) vagy a világosabb `#4ade80` (a JogWheel/TerminalRack "élénk zöld" változata)
- **Folyamatban (sárga)** → `#f59e0b` (--accent-yellow) vagy `#fbbf24` (élénkebb amber)
- **Csúszik/hiba (piros)** → `#ef4444` (--accent-red)
- **Hátravan (szürke)** → `--text-muted` / `--bg-tertiary`
- **Érintőfelület minimum** → `--touch-target-min: 44px` (ez már eleve mobil-optimalizált, egyezik az iOS/Android ajánlással)

**Amit ELVETÜNK a hibridhez** (az Industrial "nehéz" jegyei, szakmunkás-UI-hoz nem valók): a dark-chassis háttér-gradiensek, a neon glow-effekt (`box-shadow`/`text-shadow` 0 0 6px), az Oswald/Share Tech Mono ipari kijelző-fontok. Helyettük: világos alap, rendszer-font (`-apple-system/Segoe UI/Roboto` — ez már az index.css alap body-fontja is), lapos kártyák.

**Átvehető interakciós minták:**
- **TerminalCard elrendezés** (státusz-pötty + cím + állapot-alszöveg + badge) → 1:1 alkalmazható a mi munkamenet-lépés-kártyáinkra
- **TerminalGrid csoportosítás** (szekció-fejléc színes pöttyel) → állomások/lépések csoportosítására
- **IndustrialKanbanPage oszlop-minta** (stage-fejléc élő számlálóval + görgethető, "+N more" limitált lista) → a projekt-lista / élő áttekintő nézethez
- **JogWheel elve** (gesztus ÉS explicit gomb egyaránt, "kiválasztás" és "commit" külön lépés) → a mi "koppintás = állapotváltás" mintánk kiegészíthető egy megerősítő lépéssel kritikus váltásoknál (pl. "Kiszállítható" jelölés)

## Legacy prototípus mint UI-koncepció referencia (feltárva 2026-07-08)

> Forrás: `C:\Users\szant\Downloads\joinerytech` (`ui.jsx`, `page-prodterminal.jsx`) — LEGACY (nem éles), csak koncepció.

**Tech-nyelv:** React 18 + **Tailwind** (CDN, forms+typography), font **Inter** (400–700) + JetBrains Mono. Színvilág: alap **stone** (meleg szürke), akcentus **teal** (gradient teal-400→600), + a státusz-palettát lásd lent.

**A gyártás-terminál MÁR LÉTEZIK** (`page-prodterminal.jsx`) — a Doorstar műhely-terminál ennek Doorstar-adaptere. Kész, újrahasznosítható komponensek:
- `ProdTaskTerminal` — a teljes műhely-terminál nézet (a fő képernyő váza)
- `OperatorPicker` — ki dolgozik (szakmunkás kiválasztása)
- `TaskCard`, `TaskList` — munkamenet-lépések kártyái és listája
- `TaskDetail` — egy lépés részletei
- `ScanModal` — QR/vonalkód szkennelés (projekt/lépés azonosítás telefonon)
- `StatusPill` / `PtStatusPill` — státusz-jelző (a szín=állapot logika)
- `MobileBottomNav` (`NAV_ITEMS`: dashboard/workflow/orders/**production**…) — a szakmunkás-telefon alsó navigációja
- `Card`, `PrimaryBtn`, `GhostBtn`, `Icon`, `Sparkline`, `Wordmark`

**A papír-kanban szín-logika 1:1 megvan a portál `STATUS_TONES`-ában** (nincs új színrendszer, csak rá kell képezni):

| Papír-kanban állapot | Portál `STATUS_TONES` kulcs | Szín |
|----------------------|------------------------------|------|
| Hátravan (nincs kihúzva) | `planned` / `draft` | stone (szürke) |
| **Elkezdve (SÁRGA kiemelő)** | `running` (teal) v. `calc`/`low` (amber) | teal/amber |
| **Kész (ZÖLD kiemelő)** | `done` / `released` / `ok` | emerald (zöld) |
| Csúszik / határidő-kritikus | `critical` | rose (piros) |

→ A demó a `ProdTaskTerminal`-ból indul: a projekt (`DSMR 26xxx`) munkamenet-lépései `TaskCard`-okként, `StatusPill`-lel (planned→running→done), `OperatorPicker`-rel, alul `MobileBottomNav`. Doorstar-brand (szín/logó) a BRAND-rétegben, asztalos-állomások (szabászat→élzárás→CNC→…→csomagolás) a DOMAIN-adapterben.

## Design-rendszer illeszkedés (SpaceOS)

- A megoldás **SpaceOS-integrált modul** → illeszkedjen a **Datahaven design-nyelvhez** (a VPS governance-csomagban: `DATAHAVEN_UI_PATTERNS`, bento grid spec, brand guidelines, React/TS + Zustand).
- **DÖNTÉS FELÜLÍRVA (Gábor, 2026-07-08 később): NEM tiszta lap → JoineryTech portál-illeszkedés.** A demó a `Downloads/joinerytech` portál valódi komponenseire épül (lásd a fenti komponens-táblát), a SpaceOS/JoineryTech megvalósításhoz igazodva. A hiteles design-forrást a VPS-sel egyeztetjük (MSG-ROOT-033), mielőtt a Claude Designba sync-elünk.

## Amit a demónak bizonyítania kell

1. Egy szakmunkás **3 másodperc alatt** átvált egy lépést sárgára/zöldre a telefonján.
2. A tulaj **azonnal látja** élőben, hol tart minden projekt — Viber nélkül.
3. A csúszás **vizuálisan azonnal** feltűnik (piros).

## Következő lépések

1. Gábor: `/design consent` (Claude Design bekapcsolása).
2. Döntés: tiszta lap vs. Datahaven design-tokenek.
3. Az 1. képernyő (Műhely-nézet) megtervezése elsőként — ez a termék szíve.
