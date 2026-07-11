# Task: Csoportos Objektum Szerkesztés

## Státusz
- **Típus:** Feature
- **Kezdés:** 2026-04-23
- **Befejezés:** 2026-04-23
- **Státusz:** Kész
- **Prioritás:** Magas

## Elfogadási kritériumok
- [x] Több Smart Object kijelölésekor a paletta megjeleníti a mezőket.
- [x] Az eltérő értékekkel rendelkező mezőkben a `*Változó*` felirat látható.
- [x] A palettán végzett módosítás az összes kijelölt objektumra mentésre kerül.
- [x] Nem-Smart Object kijelölése nem zavarja meg a paletta működését (kihagyásra kerül).
- [x] A teljesítmény nem romlik jelentősen 10+ elem kijelölése esetén sem.

## Teendők
- [x] `DrawingObjectMetadataStore` felkészítése több handle kezelésére.
- [x] `ReadSmartObjectMetadataUseCase` frissítése az összefésülési logikával.
- [x] `WriteSmartObjectMetadataUseCase` frissítése a tömeges mentéshez.
- [x] `SmartObjectPaletteManager` kijelölés-kezelésének átalakítása.
- [x] `SmartObjectPaletteViewModel` frissítése a "Változó" állapot kezelésére.
- [x] Egységtesztek bővítése (79/79 sikeres).

## Bizonyítékok
- Build állapot: SIKERES (Release & Debug)
- Teszteredmények: 79/79 teszt sikeres (mstest), beleértve a determinisztikus ViewModel teszteket.
- Architektúra: A metaadatok mostantól érték-alapú egyenlőséget használnak, az aszinkron betöltés pedig biztonságos `Task`-alapú mintára lett refaktorálva.
