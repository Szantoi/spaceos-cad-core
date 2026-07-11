# Doorstar Kft. gyártás-dokumentációs séma (valós minta)

> Forrás: `C:\Users\szant\Doorstar Kft\Gyártás-Dokumentumok - Dokumentumok` (Gábor adta, 2026-07-08)
> Ez a VALÓS faipari gyártás-előkészítő dokumentáció-struktúra, amit a CabinetBilder kimenetének követnie kell.
> A `Sablon Mappa\ProjektSzám - ProjektNév` a kanonikus projekt-sablon.

## Projekt-mappa sablon (`<ProjektSzám> - <ProjektNév>`)

```
<ProjektSzám> - <ProjektNév>/
├── Archiv/
├── CAD/
│   └── <Projekt> - Gyartasmegrendelő.dwg      # AutoCAD gyártási rajz
├── CNC/                                         # CNC megmunkálási fájlok
├── Dokumentumok/
│   ├── <Projekt> - 01 - Borítólap.pdf
│   ├── <Projekt> - 02 - Gyártóilap.pdf
│   ├── <Projekt> - Asztalos - Szabászati Tételek.pdf   # → SZABÁSTERV
│   ├── <Projekt> - Műhely - Menyíségek.pdf             # → ANYAGSZÜKSÉGLET (BOM)
│   ├── <Projekt> - Műhely - Menyíségek - Festett.pdf   #   (felület-variáns)
│   ├── <Projekt> - Műhely - Menyíségek - Fóliás.pdf    #   (felület-variáns)
│   ├── <Projekt> - Műhely - Munkamenet.pdf             # → MŰSZAKI LEÍRÁS
│   ├── <Projekt> - Beszerelés - Átadásátvételi.pdf
│   └── Táblázatok/
│       ├── <Projekt> - 01 - Kalkulátor.xlsm            # → ÁRKALKULÁCIÓ (11-lépéses)
│       ├── <Projekt> - 02 - Folyamatok.xlsm
│       └── <Projekt> - 03 - Kiíró.xlsm
├── Felmérés/
├── Jellegrajz/
└── <ProjektSzám> - <ProjektNév> - Gyartasmegrendelő.xlsm   # fő makrós gyártásmegrendelő
```

## Illesztés a CLAUDE.md projekt-checklisthez

| CLAUDE.md checklist | Doorstar dokumentum |
|---------------------|---------------------|
| **Műszaki leírás** | `Dokumentumok/... - Műhely - Munkamenet.pdf` |
| **Anyagszükséglet** (BOM) | `Dokumentumok/... - Műhely - Menyíségek.pdf` (+ Festett/Fóliás felület-variánsok) |
| **Szabásterv** | `Dokumentumok/... - Asztalos - Szabászati Tételek.pdf` |
| **Összetett Árkalkuláció** | `Dokumentumok/Táblázatok/... - 01 - Kalkulátor.xlsm` |

## Következmények a CabinetBilder / MCP-host fejlesztésre

1. **Kimeneti célséma**: a CabinetBilder `ComputeBom()` outputja végső soron a "Műhely - Mennyiségek" és "Asztalos - Szabászati Tételek" dokumentumokat táplálja. A parametrikus Skeleton → BOM lánc ezekben ölt testet.
2. **Felület-variánsok**: az anyagszükségletet felületkezelés szerint bontják (Festett / Fóliás) — a BOM-modellnek támogatnia kell a felület-attribútumot.
3. **Excel-központú**: a kalkuláció és kiírás makrós Excel (.xlsm). A generálás célformátuma tehát nem PDF-first, hanem strukturált adat → Excel/PDF renderelés.
4. **CAD-forrás**: a Gyártásmegrendelő .dwg az AutoCAD-oldal — a SmartObject metadata onnan jön (ez az Adapter dolga, AutoCAD-függő).
5. **Projekt-azonosító konvenció**: `<ProjektSzám> - <ProjektNév>`, éves/havi archívum-struktúrában (2022–2026, azon belül 01_Január…12_December).

> **Eredet:** A Doorstar rendszert **Gábor fejlesztette**, a karbantartását azóta más vette át. A rendszer él és tovább fejlődik — a CabinetBilder ennek a szellemi utódja/következő generációja.

## Koordinációs réteg — `03 - Határidők` (a projekt-dokumentáció FÖLÖTT)

A **`03 - Határidők\Ütemterv.xlsx`** egy **cross-projekt, megosztott ütemterv**, amit a **sales tölt** — így a gyártás-előkészítő nagyjából tudja, **mikorra kell teljesítenie a gyártásnak**. Ez információ-megosztási/koordinációs réteg, NEM egy adott projekt dokumentuma.

- Munkalapok: `ADAT`, `Tervezett_beépítések`, `Ütemterv` + `ExternalData_1/2` (Power Query külső kapcsolat → SharePoint, egyezik a fő folyamat auth-rétegével).
- Oszlopok (a sales↔előkészítés kézfogás mezői): Iroda, MEGR. SZÁMA, MEGRENDELŐ NEVE, Prioritás, Vállalt szállítási határidő, Tervezett beépítés, Gyártásra Kiadva, Gyártás Tervezett Vége, Gyártás elkészült, Mikor tudja fogadni az ügyfél, Beütemezve, + termékjellemzők (Falpanel nm, Blende, ajtó darabszám, Kétszárnyú, Festett/Furnér/Falcos/Síkba/Tokba, TUS/TPS, Dísztok, PIVOT).
- Mellette: `Munkák2026.xlsm`, `Gyártás folyamatok ütemezése - <hónap>` almappák.
- A korábban vizsgált `01 - Megrendelés/Kiírási segédlet, Sorrend.txt` ennek szöveges kivonata (beépítési sorrend).

## A TELJES GYÁRTÁS-ELŐKÉSZÍTÉSI MUNKAFOLYAMAT (Gábor leírása, 2026-07-08)

1. **Sales** beteszi a megrendeléshez kapcsolódó dokumentumokat a **`01 - Megrendelés`** mappába.
   - Elnevezés: `DSMR <YYNNN> <Ügyfél Kft.> GYÁRTÁSMEGRENDELÉS` (pl. `DSMR 26104 Mérnökangyal Kft. ...`). A **DSMR** = DoorStar MegRendelés, `26104` = 2026-os év / 104-es projekt.
2. **Gyártás-előkészítés átemeli** az adott **év/hónap** mappájába (pl. `2026/07_Július`).
3. Ott létrehozza a **Sablon mappastruktúra MÁSOLATÁT**.
4. **Plugin átnevezi az Exceleket** a projekt-prefixre (`<ProjektSzám> - <ProjektNév> - ...`), hogy a fájlok **egymásra tudjanak hivatkozni** (a kereszthivatkozások a fájlnevekre épülnek).
5. Kitölti a **`Gyartasmegrendelő.xlsm`**-et (a lánc bemenete).
6. **Sorrendben végigfrissíti** a Táblázatok Exceleit:
   **Gyártásmegrendelő → 01 Kalkulátor → 02 Folyamatok → 03 Kiíró**
7. **Adatátvitel az Excelek között: Power Query, SharePointon keresztül** — így van **autentikáció** is a rendszerben (a SharePoint adja a jogosultság-réteget). Ez az identity/hozzáférés-kezelés valós megvalósítása a jelenlegi rendszerben.
8. **Törzsadat-Excelek** (`10 - Adatok`): `Data - 05.0.01.xlsm`, `Gyártási Naplók DATA.xlsm`, `Egység_idő.xlsx` — Gábor **szétválogatta a felelősségeket**, ezek a referencia/egységidő adatok, amikre a Kalkulátor/Folyamatok hivatkozik.
9. **A Kiíróból nyomtatják a PDF-eket** (`Dokumentumok/` alá: Munkamenet, Mennyiségek, Szabászati Tételek, …), amik **kikerülnek a gyártáshoz**.

### Adatfolyam-lánc (a rendszer lelke)

```
Sales megrendelés (01 - Megrendelés)
   │  átemelés + sablon-másolat + plugin-átnevezés
   ▼
Gyartasmegrendelő.xlsm  ──►  01 Kalkulátor  ──►  02 Folyamatok  ──►  03 Kiíró  ──►  PDF-ek → GYÁRTÁS
       ▲                          ▲                    ▲
       └──────────── Power Query / SharePoint (auth) ──┘
                          ▲
              Törzsadat: Data-05.0.01, Gyártási Naplók DATA, Egység_idő  (10 - Adatok)
```

### Következmények a CabinetBilder integrációra (KULCS!)

- A CabinetBilder / MCP-host természetes belépési pontja a **`Gyartasmegrendelő.xlsm` kitöltése** (vagy táplálása): a parametrikus Skeleton → BOM output ide/ebbe a láncba csatlakozik, a lánc többi lépését (Kalkulátor→Folyamatok→Kiíró) a meglévő Excel+Power Query rendszer viszi tovább.
- Az **autentikáció már megoldott** a SharePoint-rétegen — érdemes ehhez illeszkedni, nem külön identity-t építeni a lokál oldalon (kapcsolódik a VPS identity.spaceos.io kérdéshez: a valóságban SharePoint az auth).
- A rendszer **Excel + Power Query + SharePoint** hármason áll — a generált adat célformátuma strukturált tábla, amit ezek fogyasztanak.
- **NEM kell újraépíteni a kalkuláció/kiírás láncot** — az működik; a CabinetBilder értéke a lánc ELEJÉN van (a modellből jövő pontos mennyiség/szabászat betáplálása).

## Nyitott kérdések (Gáborral / valós projektből tisztázandó)

- A "Gyartasmegrendelő.xlsm" makróinak / a plugin-átnevezés pontos logikája
- A 11-lépéses árkalkuláció (CLAUDE.md, tankönyv 40-41. o.) hogyan képződik le a Kalkulátor.xlsm-ben
- A Power Query lekérdezések sémája (mely mezők mennek át a láncon)
- `01 - Megrendelés/Kiírási segédlet, Sorrend.txt` — MEGNÉZVE (2026-07-08): NEM folyamat-leírás, hanem élő **beépítési ütemterv** (aktuális projektek DSMR-számmal + határidővel, pl. "DSMR 26130 KOBA 2026.07.10."). Operatív munkalista, nem séma-dokumentum.
- A CNC/Felmérés/Jellegrajz mappák tipikus tartalma (a sablonban üresek)
