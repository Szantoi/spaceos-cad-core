# Doorstar — Műhely munkafolyamat-követés digitális megoldás (üzleti igény)

> Forrás: Gábor, 2026-07-08. VALÓS ügyfél-igény a Doorstar Kft.-től (fizető ügyfél, Gábor eredetileg az ő rendszerüket fejlesztette).
> Státusz: **probléma-megértés / tervezési fázis** — még NEM implementáció.

## A probléma (ügyfél szavaival)

A munkafolyamatok **nyomon követése szétesik**, folyton **elcsúsznak a határidők**, és nincs valós idejű láthatóság. Digitális megoldást kérnek.

## A jelenlegi (fizikai) folyamat = papír-kanban

1. Kitöltik a táblázatot, elmentik a `<Projekt> - Műhely - Munkamenet.pdf`-et.
2. **Kinyomtatják és kitűzik a falra.**
3. Ha változás van, a **műhelyvezető KÉZZEL módosítja** a papíron.
4. Állapotjelölés szövegkiemelővel:
   - **SÁRGA** = a munkafolyamatot **elkezdték**
   - **ZÖLD** = **elkészült**
5. Ha a **csomagolás is megvan** → **kiszállítható**.
6. **Befotózzák és Viberen elküldik** a tulajnak / salesnek (ez a "riport").

## A fő fájdalompontok

- A papír nem valós idejű: a tulaj/sales csak a Viber-fotóból tud a státuszról, késve.
- Kézi módosítás → nincs történet, nincs mérhető csúszás-ok.
- **Gábor csinált egy Folyamatok Excelt Gantt-charttal** (lásd lent), de **a műhely NEM tudja használni az Excelt.**
- **KRITIKUS CONSTRAINT: a műhelyvezető CSAK a telefonját tudja kezelni.** Nem Excel, nem desktop. → a megoldás **mobil-first, radikálisan egyszerű** kell legyen.

## Meglévő eszköz: `... - 02 - Folyamatok.xlsm` (Gábor műve)

Gazdag adatmodell, munkalapjai: Beállítások, Projekt adatok, AlapAdatok, Szabászat, Folyamatok Alap, Folyamatok, Folyamatok tervezése, **Gantt diagram**, **Részlegek napi terhelése**, Gyártáselőkészítés Kísérőlevél. Mezők: Sorszám, Prioritás, Cím, Telefonszám, Start Date, Deadline, DSMR, Ajtó mennyisége… Forrásláncot olvas (Gyártásmegrendelő → Kalkulátor → Egység_idő), SharePointon (doorstarkft.sharepoint.com).
→ **Jó adatmodell van, csak a felhasználói felület nem való a műhelynek.** A digitális megoldás erre az adatmodellre építhet.

## Két sarkalatos pontosítás (Gábor, 2026-07-08)

1. **Nem az Excel volt rossz** — Gábor tudta használni, a modell működik. A gond a **célközönség: szakmunkás szint.** A megoldás UX-ét a szakmunkásra kell szabni (nem "egyszerűbb Excel", hanem alapvetően más, koppintós, telefon-natív felület).
2. **SpaceOS-integrált megoldás kell — NEM különálló app.** A műhely-követő a **SpaceOS rendszer része** legyen (a VPS-en futó Datahaven/JoineryTech platform egy modulja), nem egy izolált Doorstar-alkalmazás. → ez megválaszolja a hosting-kérdést: SpaceOS-platform, annak stackjén (React/TS, Datahaven Kanban/UI patterns, identity.spaceos.io auth), a JoineryTech modul-architektúrába illesztve (egy "Shop Floor / Gyártáskövetés" modul a Cutting/CRM/HR mellé).

**Következmény:** ez valójában egy **SpaceOS/JoineryTech modul-igény**, amit a federáción keresztül a VPS-csapattal közösen kell pozicionálni. A Cabinet hozza a valós Doorstar domain-tudást + a szakmunkás-UX követelményt; a VPS a platformot.

## Megoldás-irány (javaslat, egyeztetendő)

**Egy mobil-first, böngészős web-app** (telefon, app-telepítés nélkül) **a SpaceOS platform moduljaként**, ami a papír-kanban digitális mása:
- A projekt **munkamenet-lépései** listában/kanbanban, nagy érintőfelületekkel.
- **Egy koppintás = állapotváltás**: „elkezdtem" (sárga) → „kész" (zöld) → „csomagolva/kiszállítható". Pontosan a szövegkiemelő-logika.
- **Élő státusz a tulaj/sales felé** — a Viber-fotó helyett bárhonnan látható valós idejű nézet (ki min dolgozik, mi csúszik).
- A **lépések és határidők a meglévő Folyamatok/Munkamenet adatból** jönnek (ne legyen dupla adatbevitel), a Gantt/ütemezés a háttérben marad.
- **Auth**: a meglévő SharePoint-réteghez illeszkedve (a rendszer már azon hitelesít).

Illeszkedés a fleethez: teljesíti Gábor **"egyszerű webes megjelenítés"** alapelvét; kapcsolódik a VPS Datahaven **Kanban + React/TypeScript** világához (a governance-csomagban van UI design, Zustand state, drag-drop pattern) — az ottani UI-mintákra építhet.

## Nyitott kérdések a tervezéshez (Gáborral)

1. **Adatforrás**: a lépéseket a Folyamatok Excelből (SharePoint) automatikusan húzzuk, vagy induljon kézi felvitellel az MVP? (Cél: ne legyen dupla munka.)
2. **Szerepek/eszközök**: műhelyvezető (telefon, státuszt vált) + tulaj/sales (néző, élő). Kell-e a műhelyi dolgozóknak külön hozzáférés?
3. **Hosting**: VPS-en (datahaven mellett) hostolt web-app URL, telefon böngészőből? SharePoint-auth integrációval?
4. **MVP-scope**: egy projekt lépései telefonon + állapotváltás + tulaj élő nézet — ennyi az első kör? Vagy rögtön a teljes ütemterv több projekttel?

## Tervezői szándékok (gyűjtés — Gábor alapelve szerint)

- A megoldás NE váltsa le a bevált Excel-adatlánc/kalkuláció rendszert — csak a **műhely-követés UI-t** digitalizálja (a lánc vége).
- A cél a **valós idejű láthatóság + csúszás-mérés**, nem a papír 1:1 másolása.
- A UX-nek a **legkevésbé technikai felhasználóra** kell szabva lennie (műhelyvezető, csak telefon).
