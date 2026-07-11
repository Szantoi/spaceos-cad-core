---
goal: Doorstar projekt-export — a dokumentum-négyes betáplálása az Excel/Power Query láncba
version: 1.0
date_created: 2026-07-10
owner: Cabinet root
status: 'Done'
tags: [feature, mcphost, export, doorstar, excel]
---

# Bevezetés

A doorstar_dokumentacios_sema.md kulcs-következtetése: a CabinetBilder értéke a lánc ELEJÉN van
(Gyartasmegrendelő → 01 Kalkulátor → 02 Folyamatok → 03 Kiíró), és a lánc Excel + Power Query +
SharePoint hármason áll → a célformátum STRUKTURÁLT TÁBLA. A feature: `skeleton_export_project`
tool, ami a projekt-mappába írja a dokumentum-négyes adatait Power Query-fogyasztható formában.

## 1. Requirements & Constraints
- REQ-001: Fájlnév-konvenció: `<ProjektPrefix> - CabinetBilder - <Dokumentum>.<ext>` (a plugin-átnevezés kereszthivatkozás-elvéhez illeszkedve).
- REQ-002: CSV-k magyar Excel-konvencióval: pontosvessző elválasztó, tizedesvessző, UTF-8 BOM, CRLF, magyar fejlécek.
- REQ-003: A CSV-k oszlopfejlécei a VALÓS Doorstar PQ-sémát követik (docs/knowledge/doorstar_power_query_semak.md):
  Szabaszat.csv = `DSMR;Sorszám;Hosszúság;Szélesség;Darab;Név;Megjegyzés;Tipus;Alkatrész Megnevezése;Anyag;Vastagság;Felület tipus;Szín;Minta`;
  Mennyisegek.csv = `DSMR;Sorszám;Alkatrész Megnevezése;Anyag;Vastagság;Szélesség;Hosszúság;Darab;Szín`;
  + Kalkulacio.csv (11 lépés), Muszaki-Leiras.md, export.json (teljes gépi payload).
  Alkatrész-név angol→magyar: Side Left→Bal oldal, Side Right→Jobb oldal, Bottom→Fenék, Top→Fedél, Back→Hátlap.
- REQ-004: A fájl-TARTALOM építése tiszta függvény (tesztelhető); az IO külön lépés.
- REQ-005: A tool a megadott outputDir-be ír (létrehozza, ha kell), visszaadja az írt fájlok abszolút útvonalát.
- CON-001: NEM írunk .xlsm-et (makró/plugin-logika a Doorstar oldalon él — nyitott kérdés a sémadokban); a Power Query a CSV-inket húzza be.

## 2. Implementation Steps
- TASK-001 ✅: `Catalog/MaterialAttributes.cs` (color/attribútum-olvasó a "Szín" oszlophoz) + `Export/ProjectExporter.cs` — BuildFiles (pure) + WriteAll (UTF-8 BOM). A VALÓS PQ-sémákkal (Szabászat 14 oszlop, Anyag Szükséglet 9 oszlop), HU-konvenció (; tizedesvessző CRLF), alkatrész angol→magyar.
- TASK-002 ✅: `Tools/ExportTools.cs` — `skeleton_export_project` (outputDir, dsmr, allowanceMm, laborHours, hourlyRate).
- TASK-003 ✅: 7 unit teszt (ProjectExporterTests: valós fejlécek, tizedesvessző 2,5 nem 2.5, 11 kalkuláció-sor, PartHu) → össz. **53 teszt PASS**; smoke: **20 tool**, export 5 fájl a lemezre, a kiírt Szabaszat.csv fejléce SZÓ SZERINT egyezik a PQ-sémával → **PASS**.

**Alap:** a Power Query M-kód kinyerve a valós projekt-.xlsm-ekből (`docs/knowledge/doorstar_power_query_semak.md`) — a séma nem kitalált, hanem a Doorstar-lánc tényleges céloszlopai.

## 3. Risks & Assumptions
- ASSUMPTION-001: a Power Query oldal (Doorstar karbantartója) a CSV-ket be tudja kötni — a pontos PQ-séma nyitott kérdés, az oszlopfejlécek magyarul, egyértelműen nevezettek.
- ASSUMPTION-002: hu-HU számformátum (tizedesvessző) a HU Excel dupla-katt kompatibilitásért.
