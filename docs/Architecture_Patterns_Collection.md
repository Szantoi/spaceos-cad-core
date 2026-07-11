# Architekturális és Tervezési Minták Gyujtemenye

Ez a dokumentum az elozo fejlesztesek soran alkalmazott iparagi mintakat foglalja ossze egy helyen.
A fokusz: tiszta, bovitheto, tesztelheto rendszer epites Domain-Driven Design (DDD) es Clean Architecture elvek menten.

## 1. Architekturális Fomintak

### Clean Architecture

- Szigoru retegezodes: Domain es Core logika fuggetlen az infrastrukturatol.
- A fuggosegek befele mutatnak a domain iranyaba.
- Persistence Ignorance: a domain modell nem tartalmaz adatbazis-specifikus vagy web-specifikus kotodest.

Miert hasznos:

- konnyebb tesztelhetoseg
- cserelheto infrastruktura
- kisebb technologiai lock-in

### CQRS (Command Query Responsibility Segregation)

- Irasi (Command) es olvasasi (Query) felelossegek kulon valasztasa.
- Elkerulheto a kevert felelossegu service osztalyok kialakulasa.

Miert hasznos:

- tisztabb use-case hatarok
- egyszerubb skalahatosag es valtozasmenedzsment

### Mediator Pattern

- Kozponti kerestorzsites (request dispatch) handlerek fele.
- Esemenyek pub-sub publikacioja laza csatolassal.

Miert hasznos:

- command endpoint-ek vekonyak maradnak
- keresztmetszeti logikak (validacio, logging) pipeline-ban kezelhetok

## 2. DDD Mintak (Domain Mag)

### Aggregate Root es Entity

- Entitasok aggregatumokba rendezve.
- Allapotmodositas csak explicit uzleti metodusokon keresztul.

Miert hasznos:

- invariansok vedelme
- konzisztens domain allapot

### Value Object

- Immutable, azonosito nelkuli fogalmak modellezese.
- Validacio konstruktorban vagy factory metodusban.

Miert hasznos:

- pontos domain nyelv
- kevesebb allapotkezelesi hiba

### Domain Events

- Aggregatumon belul esemenygyujtes, commit utan publikacio.
- Laza csatolas kulonbozo domain folyamok kozott.

Miert hasznos:

- transzparens oldaleffektek
- jobb bovitheto design

### Static Factory Pattern

- Publikus konstruktorok helyett Create/From/New gyarak.
- Biztonsagos letrehozas es ervenyes allapot garantalasa.

Miert hasznos:

- explicit letrehozasi intent
- ervenyes domain objektumok

### Repository Pattern (Ports/Interfaces)

- Domain csak interfeszt lat (port), implementacio kulso retegben.

Miert hasznos:

- levlasztott domain
- tesztelheto application flow

### Specification Pattern

- Lekerdezesi szabalyok kulon specifikaciokban.
- Tiszta repository interfeszek, kisebb metodus-burjanzas.

Miert hasznos:

- ujrafelhasznalhato query szabalyok
- jobban karbantarthato olvasasi logika

## 3. Application Retege Mintak

### Result Pattern

- Exception alapu control flow helyett explicit siker/hiba eredmenyek.
- Kivetel csak valodi, kritikus uzleti hiba esetben.

Miert hasznos:

- kiszamithato use-case valasz
- egyszerubb API/command hibakezeles

### Pipeline / Decorator Pattern

- Keresztmetszeti logikak request pipeline-ban (pl. validacio).
- Hibas input nem jut el a handlerig.

Miert hasznos:

- DRY validacio
- konzisztens pre-processing

### Unit of Work

- Tranzakcios hatar tobb repository muvelet felett.

Miert hasznos:

- atomi allapotvaltozas
- konzisztens perzisztencia

## 4. Uzleti Folyamat es Allapot Mintak

### Finite State Machine (FSM)

- Allapotgep alapú workflow kezeles explicit tranziciokkal.
- Csak ervenyes allapotvaltasi utvonalak engedelyezettek.

Miert hasznos:

- auditálhato folyamat
- ervenyes workflow konzisztencia

## 5. Fejlesztesi Minta

### TDD (Test-Driven Development)

- Red-Green-Refactor ciklus.
- Eloszor viselkedesi teszt, utana implementacio.

Miert hasznos:

- regresszio csokkentes
- jobb API design
- biztonsagos refaktor

## 6. Projektre Szabott Alkalmazasi Utmutato (CabinetBilder AutoCAD Scripts)

A fenti mintak kozul jelenleg kulonosen ajanlott a kovetkezo strategia:

1. Command-ek maradjanak vekony AutoCAD belepesi pontok.
2. Az uzleti logika a Core/Application retegekbe keruljon.
3. XRecord + Extension Dictionary metadata maradjon Infrastructure felelosseg.
4. Smart objektum azonositas Schema markerrel tortenjen.
5. Uj use-case-eknel (pl. export, panel szerkesztes, overrule) hasznalhato a light CQRS + mediator megkozelites.

## 7. Dontesi Iranyelv: Mikor erdemes Mediator-t bevezetni?

Igen, akkor eri meg, ha:

- no a use-case-ek szama
- tobb keresztmetszeti pipeline logika kell
- command endpoint-eket konzisztens use-case handlerekre bontjuk

Nem kotelezo, ha:

- kis meretu, egyszeru flow van
- a plusz absztrakcio tobbet artana, mint hasznalna

Javaslat:

- light bevezetes use-case szinten
- tranzakcios hatarok maradjanak explicit modon kezelve

## 8. Fogalomtár (Rovid)

- Aggregate: Tranzakcios konzisztenciahatar domain objektumok csoportjan.
- Value Object: Immutable, azonossag helyett ertek-egyenloseg.
- Specification: Ujrafelhasznalhato lekérdezesi szabalyleiras.
- Pipeline: Request koruli feldolgozasi lanc.
- Persistence Ignorance: Domain model fuggetlensege a perzisztenciatol.

## 9. Kovetkezo Lepesek

1. Use-case inventory: mely command melyik handlerbe keruljon.
2. Mediator pilot egy konkret flow-ra (pl. FrontMatter tabla epites).
3. Pipeline validacio bevezetese (input + schema marker ellenorzes).
4. Tesztkeszlet bovitese command-orchestration szintre.
