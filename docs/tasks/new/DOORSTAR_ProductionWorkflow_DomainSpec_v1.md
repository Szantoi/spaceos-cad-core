---
goal: Doorstar Production Workflow Tracking — domain requirements a hybrid mobil-first műhely-státusz modulhoz
version: 1.0
date_created: 2026-07-08
owner: Cabinet root (domain spec) — VPS root review alatt
status: 'Planned'
tags: [feature, domain, doorstar, production-workflow, epic-doorstar-softlaunch]
---

# Introduction

![Status: Planned](https://img.shields.io/badge/status-Planned-blue)

Domain requirements spec a **Doorstar Kft. papír-kanban műhely-követésének** digitalizálásához, a VPS által javasolt **Production Workflow Tracking** modulhoz (Layer 2 DRIVER, `EPIC-DOORSTAR-SOFTLAUNCH`, target 2026-09-30). Ez a Cabinet-oldali domain-spec a hibrid workflow (Cabinet domain+UX, VPS platform+implementáció) 1. lépése — a VPS root review-ja (MSG-CABINET-BRIDGE-019) alapján készült, az ott javasolt sablont követve.

## 1. Requirements & Constraints

- **REQ-001**: A megoldás a papír-kanban (Munkamenet.pdf a falon, szövegkiemelő) digitális megfelelője legyen — nem új folyamat, hanem a meglévő gyakorlat digitalizálása.
- **REQ-002**: Célközönség = **műhelyvezető, szakmunkás szint, KIZÁRÓLAG telefon**. UI: koppintós, nagy érintőfelület, minimális szöveg.
- **REQ-003**: **Élő státusz** a tulaj/sales felé — a Viber-fotó kiváltása valós idejű nézettel.
- **REQ-004**: Állapot-modell = FSM, a szín-logika megtartásával: hátravan (szürke) → folyamatban (**sárga**) → kész (**zöld**); csomagolva → kiszállítható.
- **REQ-005 (Gábor, 2026-07-08)**: **HIBRID esztétika** — a hivatalos Industrial design-DNS (színek, platform-konzisztencia) + a szakmunkás-telefonra világosított/egyszerűsített felület. Nem a teljes dark/LED Industrial, nem is tiszta lap.
- **CON-001**: A modul **NEM a Track C (CNC kiosk) bővítése** — az géppark-centrikus (CuttingJob start/complete), a Doorstar-igény a teljes munkamenetet (5-7 művelet) fedi le, ember-centrikus.
- **CON-002**: Layer 2 DRIVER modulként illeszkedik a SpaceOS 4-rétegbe (Kernel FSM/Auth = Layer1, ez a modul = Layer2, React frontend = Layer4/Brand).
- **CON-003**: Offline-first PWA **Phase 2** — az MVP feltételezi, hogy a műhelyben van hálózat (a VPS gap-elemzése szerint ez a legnagyobb hiányzó darab, ~2 nap külön munka).
- **GUD-001**: A workflow-lépések listája ágazat/ügyfél-specifikus (Doorstar konkrét munkamenete) — más gyártó más lépéssort kap ugyanazon a modellen (reusable modul, nem Doorstar-specifikus kód).

## 2. Persona és use case

**Elsődleges persona: Műhelyvezető.** Szakmunkás, csak telefont kezel, nem Excel/desktop-felhasználó. Feladata: a folyamatban lévő projekt munkamenet-lépéseinek állapotát frissíteni, ahogy a valóságban haladnak.

**Másodlagos persona: Tulaj / Sales.** Élő rálátást akar minden aktív projekt státuszára — jelenleg ezt Viber-fotóból kapja meg, késve.

**Fő user story:** *„Műhelyvezetőként szeretném egy koppintással jelezni, hogy elkezdtem/befejeztem egy munkafázist, hogy a tulaj és a sales azonnal lássa a projekt állását — anélkül, hogy külön le kéne fotóznom és el kéne küldenem."*

## 3. Workflow steps (Doorstar-domain, munkamenet-modell)

> **PONTOSÍTVA egy valós projekttel** (2026-07-08): `2026/07_Július/26144 - Aptermanné dr. Csoma Barbara/Dokumentumok/26144 - Műhely - Munkamenet.pdf`. A korábbi 6-lépéses feltételezés **hibás volt** — a valóság ennél lényegesen finomabb szemcséjű.

### A valós Munkamenet-tábla szerkezete

A `Munkamenet.pdf` egy **17 fázisos gyártási útvonal-mátrix**, ahol **több párhuzamos alkatrész-ág** (pl. ajtólap-borítás — fix és mozgó oldali —, tok-borítás, üvegezés, HDF keret-betét) halad végig eltérő műveletsorokon, majd **konvergálnak** összeszerelésnél, felületkezelésnél és csomagolásnál. Egy konkrét projektnél (26144) az azonosított műveletek:

**Ág 1 — Ajtólap-borítás** (fix + mozgó oldali): Borítás → Préselés → CNC kontúrmarás → Csiszolás → Fújás → Ragasztó → Fóliázás → Élcézés/Kivágás
**Ág 2 — Tok-borítás**: Borítás → Szabás (22-es marás) → Gérvágás → Tok csiszolás → Gér összerakás → Fújás → Ragasztó → Fóliázás → Kivágás
**Ág 3 — Üvegezés**: Üveges, 5-ös HDF Keret Betét, Kiválogatás+fúrás
**Konvergencia**: CNC Pánt-zár → Tok összerakás → **Csomagolás/Paknizás** → **Kész termék** → Raktár → Beépítés

### Konklúzió a mobil UI-hoz: SZINTVÁLASZTÁS szükséges (nyitott döntés — lásd Risks)

A valós tábla **gép/művelet-szintű** (17 mikro-fázis, CNC-programozáshoz/műhelyi géprend-hez való), ami **túl granuláris egy telefonos koppintós UI-hoz** (a REQ-002 "radikálisan egyszerű" elvével ütközik). Javasolt **STAGE-csoportosítás** a mobil megjelenítéshez (a 17 fázis 5-6 magasabb szintű szakaszba tömörítve):

| # | Mobil STAGE (összevont) | Lefedi a Munkamenet-fázisok közül | Trigger |
|---|--------------------------|-------------------------------------|---------|
| 1 | **Szabászat/Előgyártás** | Szabás, 22-es marás, HDF keret, üvegezés-előkészítés | Auto: `CuttingJob.CuttingCompleted` (ADR-038) |
| 2 | **Megmunkálás** (CNC/csiszolás) | CNC kontúrmarás, Gérvágás, Csiszolás (több variáns) | Manuális |
| 3 | **Felületkezelés** | Fújás, Ragasztó, Fóliázás | Manuális |
| 4 | **Összeszerelés** | Él-lécezés/Kivágás, CNC Pánt-zár, Tok összerakás, Gér összerakás | Manuális, opcionális fotó |
| 5 | **Csomagolás** | Paknizás, Csomagolás | „ZÖLD jelölés" → Kész |
| 6 | **Kiszállítható** | Kész termék → Raktár | Auto: Csomagolás=Kész → push a tulajnak |

Minden STAGE állapota: `Queued` (szürke) → `InProgress` (sárga) → `Done` (zöld). A backend FSM (VPS-javaslat, MSG-CABINET-BRIDGE-019) `Queued→Cutting→Preparation→Assembly→Packaging→ShippingReady` — ez **a fenti 6 STAGE-nak felel meg**, tehát a FSM-modell helyes marad, csak a "Preparation"/"Assembly" fázisok belül több valós műveletet takarnak, mint eredetileg feltételeztük.

**✅ DÖNTÉS (Gábor, 2026-07-08): 6 STAGE, MVP-scope.** A mobil app a fenti 6 összevont STAGE-et követi — a részletes mikro-fázis, alkatrész-szintű (kísérőlevél-alapú) és idő/kapacitás-tervezés **változatlanul a meglévő Excel-rendszerben marad** (Folyamatok.xlsm: Ajtólap/Borítás/Tokmag/Bútorfront/Blende/Falpanel Kísérőlevél munkalapok + Tervezettidő/Munkaidő/Humánerő). A két rendszer egymás mellett él: Excel = részletes tervezés-végrehajtás a műhelyben lévő "rendszergazdáknak"; mobil app = gyors, magas szintű STATUS a műhelyvezetőnek + élő láthatóság a tulajnak/sales-nek. **Nincs duplikált adatbevitel-igény** — a mobil app a 6 STAGE-et frissíti, nem helyettesíti az Excel részletes nyilvántartását.

*(Kiegészítő infó a Folyamatok.xlsm valós szerkezetéről, ami megalapozta a döntést: a munkafüzet külön "Kísérőlevél" munkalapot tartalmaz alkatrész-kategóriánként — Ajtólap, Borítás, Tokmag, Tokmag TU, Bútorfront, Blende, Falpanel —, plusz Tervezettidő/Munkaidő/Humánerő/Feladat_Egység_idő lapokat a kapacitástervezéshez. Ez megerősíti, hogy a részletes szint már jól kezelt eszközzel rendelkezik — a mobil app-nak nem ezt kell kiváltania, hanem a "mi van most, hol tart" gyors kérdésre válaszolnia.)*

**VPS-review megerősítés (MSG-CABINET-BRIDGE-020): 2-szintű FSM — elfogadva, illeszkedik a 6 STAGE döntéshez.**
```
ProductionJob.Status (aggregate, magas szint): Queued | InProgress | Completed | ShippingReady | OnHold
WorkflowStep.Status (a fenti 6 STAGE mindegyikén, mobil UI-nak): Queued(szürke) | InProgress(sárga) | Done(zöld)
```
Invariáns: `ProductionJob.Status = InProgress ⟺ van olyan STAGE, ami InProgress/Done ÉS van olyan, ami még Queued`. A műhelyvezető a WorkflowStep-eket (6 STAGE) váltja; a ProductionJob-ot a rendszer vezeti le belőlük.

## 4. UI-vázlat (hibrid, mobil-first)

**1. Műhely-nézet (fő képernyő, műhelyvezető telefonja):** a fenti táblázat lépései függőleges kártyaként, mindegyik nagy állapot-jelzővel (szürke/sárga/zöld), egy koppintás a következő állapotra. Fejléc: projekt neve (`DSMR 26xxx`) + határidő + haladás (pl. „4/6 kész").

**2. Projekt-lista:** aktív DSMR-projektek, haladás-sávval és csúszás-jelzéssel (piros, ha a `Vállalt szállítási határidő` közeleg és a haladás elmarad).

**3. Élő áttekintő (tulaj/sales):** minden aktív projekt állapota egy pillantásra, csúszók kiemelve — a Viber-fotó digitális, valós idejű megfelelője.

*(Vizuális referencia és Industrial-token-illesztés: `docs/knowledge/doorstar_muhely_ui_demo_brief.md` — a Claude Design tool-lal készülő UI-terv ezt a spec-et és a VPS Industrial-komponenseit egyesíti.)*

## 5. Integrációs pontok (VPS eseményekhez)

- **Bejövő**: `CuttingJob.CuttingCompleted` (Cutting modul, ADR-038) → auto-step; `OrderItem.OrderConfirmed` (CRM/Joinery) → ProductionJob létrehozása.
- **Kimenő**: `ProductionJob.ShippingReady` → Sales/tulaj notifikáció (Telegram/email — a Viber kiváltása); `ProductionJob.WorkflowStepCompleted` → analytics/timeline.

## 6. Acceptance criteria

- **AC-001**: A műhelyvezető egy projekt egy lépését **3 másodpercen belül**, egy koppintással tudja sárgára/zöldre váltani.
- **AC-002**: A tulaj/sales az élő nézetben **Viber nélkül** látja az aktuális projekt-státuszokat.
- **AC-003**: Csúszó projekt (határidőhöz képest elmaradó haladás) vizuálisan azonnal (pl. piros keret/jelzés) kiemelve.
- **AC-004**: A `CuttingJob.CuttingCompleted` esemény automatikusan lezárja a Szabászat lépést, manuális beavatkozás nélkül.

## 7. Alternatives

- **ALT-001**: A Track C (CNC kiosk UI) kibővítése a teljes munkamenetre — elvetve: más célközönség (gép-operátor desktop vs. műhelyvezető telefon) és más granularitás (egy gép egy job-ja vs. 5-7 lépéses teljes folyamat).
- **ALT-002**: Tiszta lap (Industrial-tól független) UI — elvetve (Gábor döntése): a SpaceOS-integráció és platform-konzisztencia előbbre való.

## 8. Dependencies

- **DEP-001**: `EPIC-CUTTING-Q3` — DONE, a `CuttingJob.CuttingCompleted` esemény készen áll.
- **DEP-002**: `EPIC-PORTAL-V2` — DONE, React 19 + Industrial design system komponensek elérhetők (FILE-TRANSFER-rel megkapva: TerminalRack, JogWheel, TerminalCard, TerminalGrid, IndustrialKanbanPage).
- **DEP-003**: VPS Kernel Auth/RBAC (Layer 1) a műhelyvezető/tulaj szerepkör-elkülönítéshez.

## 9. Files

- **FILE-001**: `docs/knowledge/doorstar_workflow_tracking_igeny.md` — az eredeti üzleti igény
- **FILE-002**: `docs/knowledge/doorstar_muhely_ui_demo_brief.md` — UI-terv a Claude Design tool-hoz
- **FILE-003**: `docs/knowledge/joinerytech_portal_ui_forras.md` — design-forrás tisztázás (VPS Industrial vs. legacy prototípus)
- **FILE-004**: ez a dokumentum

## 10. Risks & Assumptions

- **RISK-001**: Offline-mód hiánya (a műhelyben lehet gyenge/nincs wifi) — Phase 2-re halasztva, de MVP-kockázat, ha éles használatban azonnal szükség lenne rá.
- ~~ASSUMPTION-001: 6 lépéses lista feltételezett~~ **LEZÁRVA (2026-07-08): pontosítva a DSMR 26144 valós Munkamenet.pdf-jével.** A valóság 17 mikro-fázisos, párhuzamos alkatrész-ágakkal — lásd 3. szakasz frissítve. **ÚJ NYITOTT KÉRDÉS (RISK-002)**: a mobil UI granularitása (6 STAGE vs. 17 mikro-fázis) még döntendő Gáborral/VPS-szel.
- **RISK-002 (új)**: ha a műhelyvezetőnek ténylegesen a mikro-fázis szinten kell jelentenie (nem csak 6 STAGE), a `WorkflowStep` domain-modellt és a mobil UI-t is át kell tervezni — ez jelentősen megnöveli a scope-ot (17 tap-pont projektenként a 6 helyett). Tisztázandó a Week 1 domain-spec review során.
- **ASSUMPTION-002**: A műhelyben minden szakmunkásnak/műhelyvezetőnek van saját vagy megosztott okostelefonja internet-hozzáféréssel.

## 11. Related Specifications / Further Reading

- VPS válasz és architektúra-javaslat: MSG-CABINET-BRIDGE-019 (2026-07-08)
- `EPIC-DOORSTAR-SOFTLAUNCH` (EPICS.yaml, VPS, target 2026-09-30)
- `SpaceOS_Cutting_Q3_Track_C_ShopFloor_Integration_v1.md` (a NEM-lefedett, gép-centrikus rokon modul)
- Goal-módszertan: `docs/knowledge/federation/GOAL_DEFINING_METHOD_VPS.md`
