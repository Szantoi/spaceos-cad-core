# Doorstar Egység_idő — munkanaplóval mért folyamatmodell (legacy) + modernizációs terv

> Forrás: `10 - Adatok/Egység_idő.xlsx`, `Feladat_Egység_idő` munkalap (a valós, munkanaplós
> felméréssel készült egységidő-adat). Kinyerés: 2026-07-10, Claude Code.
> **Gábor: a fájl kezdetleges, legacy okokból maradt — MODERNIZÁLNI kell. A mért ÉRTÉK az arany, a séma nem.**

## A legacy séma (amit NEM másolunk le, csak az értékét vesszük át)

30 oszlop, feladat-soronként. Kulcsmezők:
- **Feladat Rövidnév** — hierarchikus kód: `<Részleg><alegység>-<Feladatjel>.<sorszám>`, pl. `GyV-L.09`
  (Gy=Gyártás, V=Gyártásvezetés/műhely vagy I=Iroda; L=Ajtólap, T=Tok, B=Borítás, BF=Bútorfront, F=Falpanel…).
- **Egység idő** — Excel nap-tört! `4.1667e-2` = 1/24 nap = **1 óra**; `3.148e-3` ≈ 4,5 perc. → tiszta óra = érték × 24.
- **Humánerő** — egyszerre dolgozó emberek száma (a man-hour = egységidő × humánerő × darab).
- **Feladat Tipus** — Folyamat / Összetett folyamat / ToDu(ToDo) / mérföldkő.
- **Részleg 01/02, Felelős, Támogató** — szakma + felelős (Asztalos, CNC, Összeszerelő, Fóliázó…).
- **Szülő feladat + Feladat Függőség Tipusa (FS/SS) + Késleltetés + Extra nap** — ez a DAG (vö. woodwork_domain §11).
- **Keresési Adat Tábla Neve + Keresési Oszlop/Feltétel (1..3)** — a művelet ALKATRÉSZHEZ kötése:
  pl. `Szabászat | [Darab] | [Alkatrész Megnevezése]="Tokmag" | [Felület Tipus]="Fóliás"` →
  a művelet a Szabászat-tábla „Tokmag" + „Fóliás" soraira fut, a mennyiség a `[Darab]`-ból.

### Valós mért egységidők (minta, tiszta órára váltva — REFERENCIA)
| Művelet | Alkatrész | nap-tört | óra | Szakma | Humánerő |
|---|---|---|---|---|---|
| 22-es Szabás | Tokmag | 1.5625e-3 | 0,0375 | Asztalos | 2 |
| CNC-marás | Borítás | 3.97e-3 | 0,0953 | CNC | 1 |
| Csiszolás | Borítás | 3.148e-3 | 0,0756 | Összeszerelő | 1 |
| Csiszolás | Ajtólap | 9.375e-3 | 0,225 | Összeszerelő | 1 |
| Fóliázás | Tok (Fóliás) | 1.9676e-3 | 0,0472 | Fóliázó | 2 |
| CNC Pánt-zár | Ajtólap | 1.1667e-2 | 0,28 | CNC | 1 |

### Felület-feltételes műveletek (kulcs a felület-attribútumhoz!)
- Fóliás: Ragasztó csiszolás / Fóliázás / Kivágás — csak `[Felület Tipus]="Fóliás"`.
- Festett: „Festőnél" + „Festőtöl visszahozva" (4 nap késleltetés!) — csak `[Felület Tipus]="Festett"`.
- → a mi surface-attribútumunk (festett/fóliás/laminált) VEZÉRLI, mely műveletek futnak.

## Modernizációs terv (a CabinetBilder tiszta modellje)

**Elv:** a legacy 30-oszlopos, kódfüggő, Excel-nap-törtes séma helyett tiszta domain-modell:
```
Operation { Id, Name, Role, UnitTimeHours (tiszta óra), Headcount,
            Match { Category?, PartName?, Surface? }, DependsOn?, DependencyType(FS/SS)?, LagDays? }
```
- **Egységidő tiszta órában** (nem nap-tört) — a mért értékből ×24-gyel átváltva, a katalógusban rögzítve.
- **Alkatrész-illesztés strukturáltan** (kategória/rész/felület predikátum) a legacy „Keresési Oszlop/Feltétel" helyett.
- **Munkaidő-becslés**: mancsöra = Σ (UnitTimeHours × Headcount × illeszkedő darab). Ez táplálja az árkalkuláció 2. lépését (Bérköltség) — a kézi `laborHours` helyett.
- **Ütemezés (DAG) külön réteg**: az FS/SS + késleltetés a „02 Folyamatok" kapacitás/átfutás-tervezésé; a KÖLTSÉGHEZ csak az összes mancsóra kell → a DAG-ot most elhalasztjuk, a munkaidő-aggregációt építjük.

**Első modern szelet (megvalósítva 2026-07-10):** `Production/Operation.cs` + `OperationCatalog`
(a korpusz-műveletek mért órákkal: szabás, élzárás, CNC-furat, csiszolás, összeállítás) +
`LaborEstimator` (BOM → mancsóra) + a cost_calculation auto-labor opciója.

## Nyitott (a teljes modernizációhoz)
- A teljes Egység_idő átvétele (Ajtólap/Tok/Borítás/Falpanel taxonómia) — a Skeleton front/fiók/tok bővítésével együtt.
- Az ütemezési DAG (FS/SS + késleltetés) modernizált változata → átfutási idő / kapacitás (02 Folyamatok kiváltása).
- A „Gyártási Naplók DATA.xlsx" (a nyers munkanapló-mérés) — az egységidők forrás-adatai, folyamatos finomításhoz.
- Szakma-alapú órabérek (a jelenlegi kalkuláció egyetlen órabérrel számol; a modell szakmánként eltérhet).
