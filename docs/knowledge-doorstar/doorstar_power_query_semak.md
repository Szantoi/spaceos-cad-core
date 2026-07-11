# Doorstar Power Query lánc — valós sémák (Excel M-kódból kinyerve)

> Forrás: a valós `26144 - Aptermanné dr. Csoma Barbara` projekt .xlsm fájljai
> (Gyartasmegrendelő / 01 Kalkulátor / 03 Kiíró). Kinyerés: a DataMashup (customXml/item3.xml,
> UTF-16, MS-QDEFF: [4B verzió][4B hossz][ZIP → Formulas/Section1.m]). 2026-07-10, Claude Code.
> **Ez a valós céloszlop-séma, amit a CabinetBilder projekt-exportjának követnie kell.**

## A lánc adatforrása (SharePoint, autentikációval)

- **Gyartasmegrendelő.xlsm** egyetlen PQ-t tartalmaz (`SEGÉD`): a SharePointról húzza a törzsadatot:
  `Excel.Workbook(Web.Contents("https://doorstarkft.sharepoint.com/sites/Gyartas-Dokumentumok/.../10 - Adatok/Data-05.0.01.xlsm"))`, `GYM változok` munkalap, PromoteHeaders.
  → **A SharePoint adja az auth-réteget** (ez a valós identity, vö. VPS identity.spaceos.io kérdés).
- A **01 Kalkulátor** a `Gyartasmegrendelse_Data`-ból (DATA) dolgozik, és kategóriánként szűri az oszlopokat
  (Ajtó/Vasalat/Fix Oldal/Mozgó Oldal/Üveg/Megmunkálás/Anyag/…), a `Text.Contains` oszlopnév-mintával.
- Több tábla a `Beállítás_..._WEB_Conect` névtartomány cellájából olvassa a forrás-URL-t (Web.Contents) —
  a fájlok **kereszthivatkozásai a fájlnév-prefixre** épülnek (ezért nevezi át a plugin az Exceleket).

## 🎯 Szabászat tábla — a szabásjegyzék VALÓS oszlopsémája

A `Kalkulátor!Szabászat` query az `Excel.CurrentWorkbook()` `Szabászat_*` névtartományait fűzi össze,
szűri `Darab <> 0`-ra, és erre a sorrendre rendezi (a mi cutting_plan → CSV exportunk célsémája):

```
DSMR | Sorszám | Hosszúság | Szélesség | Darab | Név | Megjegyzés | Tipus |
Alkatrész Megnevezése | Anyag | Vastagság | Felület tipus | Szín | Minta
```
- Kerekítés: Hosszúság/Szélesség/Vastagság → 2 tizedes (`Number.Round(_, 2)`).
- A **Kiíró!Etiket** (etikett/címke a PDF-hez) sorrendje: DSMR, Sorszám, Tipus, Alkatrész Megnevezése,
  Anyag, Vastagság, Szélesség, Hosszúság, Felület tipus, Megjegyzés, Szín, Minta.

## Anyag Szükséglet tábla — az anyagszükséglet VALÓS oszlopsémája

A `Kiíró!"Anyag Szükséglet"` (a "Műhely - Menyíségek" PDF forrása) a Szabászatból származik:
```
DSMR | Sorszám | Alkatrész Megnevezése | Anyag | Vastagság | Szélesség | Hosszúság | Darab | Szín
```

## Következmény a CabinetBilder projekt-exportra

1. A mi `Szabaszat.csv`-nk oszlopfejlécei EGYEZZENEK a fenti Szabászat-sémával (magyar nevek, ; elválasztó,
   tizedesvessző) → a Doorstar PQ-lánc `Table.Combine`/`Excel.CurrentWorkbook` mintája be tudja húzni.
2. Kulcsmezők, amiket a modellből ki kell tölteni: **Hosszúság, Szélesség, Darab, Tipus, Alkatrész Megnevezése,
   Anyag, Vastagság, Felület tipus, Szín** (a Név/Megjegyzés/Minta opcionális). A DSMR/Sorszám a projektből.
3. A **Felület tipus** oszlop pontosan a mi surface-attribútumunk (festett/fóliás/laminált) — már megvan.
4. Az auth a SharePoint — a lokál oldalon nem kell külön identity; a CSV a lánc ELEJÉRE táplál be.

## Nyitott
- A Data-05.0.01.xlsm `GYM változok` teljes oszloplistája (a SharePoint-forrás sémája) — külön kinyerhető.
- Az `Anyag`/`Szín`/`Felület tipus` megengedett értékkészlete (a Doorstar törzsadatból) — leképzés a mi katalógusunkra.
