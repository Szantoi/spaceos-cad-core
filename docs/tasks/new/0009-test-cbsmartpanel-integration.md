# Task ID: 0009

# Title: Manual Integration Test — CBSmartPanel in Live AutoCAD

# Category: test

# Milestone: 15

# Status: new

## Szándék (Intent)

A `CBSmartPanel` parancs és a teljes PaletteSet infrastruktúra (SmartObjectMetadataService,
SmartObjectPaletteViewModel, SmartObjectPaletteManager) manuálisan tesztelendő valódi AutoCAD
környezetben, mivel ezek az AutoCAD API unmanaged rétegén alapulnak és unit tesztelhetők nem.

Az integrációs teszt célja:
- A palette megnyitásának és bezárásának ellenőrzése
- Smart object kijelölés → metaadatok megjelenésének validálása
- Mezőszerkesztés → Mentés → Újraolvasás (perzisztencia) ellenőrzése
- Nem-smart object viselkedés ellenőrzése
- REFEDIT veto továbbra is működik

## Előfeltételek

- AutoCAD 2027 telepítve és elérhető
- Az `App.AutoCadScripts.dll` betöltve (NETLOAD)
- Legalább egy DWG nyitva, amelyen van dinamikus blokk (smart object SchemaID-del)

## Elfogadási kritérium (Acceptance Criteria)

- [ ] **TC-01 Paletta megnyitás:** `CBSmartPanel` parancs → PaletteSet ablak megjelenik
- [ ] **TC-02 Paletta bezárás:** `CBSmartPanel` újra → PaletteSet ablak eltűnik
- [ ] **TC-03 Smart object kijelölés:** dinamikus blokk kijelölése → metaadatok (ObjectType, Label, CreatedAt) megjelennek a DataGrid-ben
- [ ] **TC-04 Nem-smart object:** normál entitás kijelölése → "Nincs kijelölt smart object" placeholder megjelenik
- [ ] **TC-05 Mezőszerkesztés:** `Label` mező értékének módosítása → sor kiemelve (IsModified = sötét háttér)
- [ ] **TC-06 Mentés:** "Mentés" gomb → "Sikeresen mentve." státusz → módosítás törlődik vizuálisan
- [ ] **TC-07 Perzisztencia:** DWG mentés → bezárás → újranyitás → smart object kijelölése → a módosított `Label` érték megmarad
- [ ] **TC-08 Frissítés gomb:** más objektumra váltás, majd "Frissítés" → aktuális objektum adatai töltődnek
- [ ] **TC-09 REFEDIT veto:** REFEDIT parancs smart objecton → hibaüzenet jelenik meg, REFEDIT nem indul el
- [ ] **TC-10 Dokumentum váltás:** másik DWG-re váltás → paletta ürül (stale adat nem jelenik meg)

## Teszt lépések részletesen

### TC-03 — Smart object kijelölés

```
1. DWG megnyitása, amelyen van CBSmartPanel-lel jelölt blokk
2. CBSmartPanel parancs kiadása → paletta megjelenik
3. A blokk egyszeri kattintással kijelölése
4. Elvárt: a DataGrid feltöltődik a blokk metaadataival
5. Elvárt: az ObjectHandle label megjelenik a fejlécben
```

### TC-07 — Perzisztencia (legfontosabb!)

```
1. Smart object kijelölése → metaadatok betöltve
2. Label mező: eredeti érték: "Alapszekrény"
3. Módosítás: "Felső szekrény (módosított)"
4. "Mentés" gomb → státusz: "Sikeresen mentve."
5. DWG mentése (QSAVE)
6. DWG bezárása
7. DWG újranyitása
8. CBSmartPanel → paletta megnyílik
9. Ugyanaz a blokk kijelölése
10. Elvárt: Label = "Felső szekrény (módosított)"
```

## Tanúsítás (Evidence)

- Screenshot / screengrab: paletta megnyitva, mezőkkel feltöltve
- Perzisztencia teszt eredménye: Label érték megmarad DWG-bezárás és újranyitás után
- REFEDIT veto log: editor üzenet megjelenik

---

**Started:**
**Completed:**
**Duration:**
**Owner:**
**AutoCAD Version:** AutoCAD 2027
