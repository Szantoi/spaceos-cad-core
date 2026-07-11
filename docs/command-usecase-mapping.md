# Command → Use-Case → Infrastructure Mapping

> **Karbantarthatósági referencia** — Ez a dokumentum az összes regisztrált AutoCAD parancsot és azok teljes végrehajtási útvonalát mutatja be a komponenseken keresztül. Minden fejlesztés után frissítendő.

---

## Áttekintés

Az architektúra három határos rétegre épül:

```
AutoCAD UI
    ↓
Command Layer     (App.AutoCadScripts/Commands/)         — AutoCAD API, user prompts
    ↓
Application Layer (App.AutoCadScripts/Application/)      — Use-cases, Result<T>, logika
    ↓
Core Layer        (App.AutoCadScripts.Core/)              — Domain modell, port interfészek
    ↑
Infrastructure    (App.AutoCadScripts/Infrastructure/)    — XRecord, TypeStore, MetadataStore
```

---

## Parancsok részletes leképezése

### 1. `SyncDimToBlock`

**Célja:** Egy kijelölt méret (dimension) értékét szinkronizálja egy dinamikus blokk távolság-tulajdonságával.

| Réteg | Komponens | Felelősség |
|---|---|---|
| Command | `DimensionCommands.SyncDimToBlock()` | User prompt, entitás kijelölés, tranzakció |
| Application | — | *(use-case nem kiemelve, direkt command logika)* |
| Core | — | *(nincs Core függőség)* |
| Infrastructure | — | *(nincs infrastructure függőség)* |

**Bemenetek:**
- Dimension entitás (felhasználó által kijelölve)
- BlockReference (felhasználó által kijelölve)
- Keyword: `Length` | `Width` | `Thickness`

**Kimenetek:**
- A blokk kiválasztott dinamikus property értéke frissül a dimension mért értékével

**Megjegyzés:** Ez a parancs még nem refaktorált use-case-be. Refaktoring candidate: Task #0009.

---

### 2. `SyncBlockToFrontMatter`

**Célja:** Egy dinamikus blokk tulajdonságaiból front matter szöveges dobozt generál a rajzon.

| Réteg | Komponens | Felelősség |
|---|---|---|
| Command | `DimensionCommands.SyncBlockToFrontMatter()` | User prompt, entitás kijelölés, MText elhelyezés, tranzakció |
| Application | `CreateFrontMatterFromBlockUseCase` | Front matter szöveg összeállítása, `Result<T>` visszaadása |
| Core | `FrontMatterValueBuilder` | Dinamikus property → kanonikus kulcs mapping |
| Core | `FrontMatterTextService` | Template buildés, szám-formázás |
| Core | `FrontMatterKeys` | Kanonikus kulcsnevek (`Length_cut`, `Width_cut`, …) |
| Infrastructure | `DrawingObjectMetadataStore.MarkAsSmartObject()` | SchemaID jelölő írása az MText XRecord-jába |
| Infrastructure | `DrawingTypeStore.AddType()` | Rajz-szintű type lista frissítése |

**Bemenetek:**
- `BlockReference` (dinamikus blokk, felhasználó által kijelölve)
- Type selector (prompt: meglévő típus indexe vagy új típus neve)
- Insertion point (MText elhelyezési pontja)

**Kimenetek:**
- `MText` entitás a rajzban (front matter tartalom)
- SchemaID marker az MText Extension Dictionary-jében
- Rajz-szintű type lista frissítve

**Hibakezelés:**
- `Result.IsFailure` → `ed.WriteMessage(error)`, tranzakció nem commit-álódik
- Üres Type → visszatér hiba nélkül

---

### 3. `FrontMatterToTable`

**Célja:** Az aktív rajzban lévő front matter szövegdobozokat összegyűjti és AutoCAD táblává alakítja.

| Réteg | Komponens | Felelősség |
|---|---|---|
| Command | `DimensionCommands.FrontMatterToTable()` | User prompt, entity scan, tábla elhelyezés, tranzakció |
| Application | — | *(use-case nem kiemelve, direkt command logika)* |
| Core | `FrontMatterTextService` | Front matter szöveg → mező-dictionary parse |
| Core | `FrontMatterTypeInputParser` | Type input értelmezése (index vs. szöveges) |
| Core | `SmartObjectSchemaFilter` | Schema-aware szűrés: csak "okos" MText entitások |
| Infrastructure | `DrawingObjectMetadataStore.TryGetSchemaId()` | SchemaID olvasása az objektum XRecord-jából |
| Infrastructure | `DrawingTypeStore` | Rajz-szintű type lista olvasása a Type picker prompthoz |

**Bemenetek:**
- Type filter (prompt: meglévő típus indexe, kötelező)
- Layer filter (prompt: opcionális string)
- Table insertion point

**Kimenetek:**
- AutoCAD `Table` entitás a rajzban (front matter mezők sorai)

**Megjegyzés:** A rajz-scan lineáris — nagy rajzoknál teljesítményszempont lehet.

---

## Lifecycle események

### `AutoCadPlugin` (IExtensionApplication)

| Esemény | Kezelő | Funkció |
|---|---|---|
| `DocumentCreated` | `OnDocumentCreated` | Új dokumentumon feliratkozás |
| `DocumentLockModeChanged` | `OnDocumentLockModeChanged` | REFEDIT veto okos blokkokon |

**REFEDIT védelem logikája:**
1. Megkapja a `REFEDIT` parancs lock event-jét
2. Lekéri az implied selection-t (előre kijelölt objektumok)
3. Ha bármelyik `BlockReference`-en van SchemaID marker → `e.Veto()` + hibaüzenet

---

## Use-Case katalógus (Application réteg)

| Use-Case | Visszatérés | Függőség (port) |
|---|---|---|
| `CreateFrontMatterFromBlockUseCase` | `Result<CreateFrontMatterFromBlockResult>` | — (Core service-ek direkt) |
| `ReadSmartObjectMetadataUseCase` | `Result<SmartObjectMetadata>` | `ISmartObjectMetadataService` |
| `WriteSmartObjectMetadataUseCase` | `Result` | `ISmartObjectMetadataService` |

---

## Infrastructure komponensek

| Osztály | Interfész | Perzisztencia |
|---|---|---|
| `DrawingObjectMetadataStore` | `IDrawingObjectMetadataStore` | AutoCAD Extension Dictionary + XRecord |
| `DrawingTypeStore` | `IDrawingTypeStore` | XRecord a rajz `NamedObjectsDictionary`-jében |

### `DrawingObjectMetadataStore` metódus-térkép

| Metódus | Hívó | Mit csinál |
|---|---|---|
| `MarkAsSmartObject()` | `SyncBlockToFrontMatter` | SchemaID jelölő írása |
| `TryGetSchemaId()` | `FrontMatterToTable`, `AutoCadPlugin` | SchemaID olvasása |
| `ReadFields()` | `ReadSmartObjectMetadataUseCase` via port | Domain mezők olvasása |
| `WriteFields()` | `WriteSmartObjectMetadataUseCase` via port | Domain mezők írása |

---

## Core domain komponensek

| Osztály/modul | Réteg | Felelősség |
|---|---|---|
| `Result<T>` | Core/Common | Egységes eredménymodell |
| `SmartObjectSchema` | Core/SmartObjects | Schema konstansok (dict neve, kulcs neve) |
| `SmartObjectMetadataKeys` | Core/SmartObjects | Kanonikus domain mezőkulcsok |
| `SmartObjectMetadata` | Core/SmartObjects | Immutábilis metadata value-object |
| `ISmartObjectMetadataService` | Core/SmartObjects | Port interfész a metadata store felé |
| `SmartObjectSchemaFilter` | Core/SmartObjects | Schema-alapú szűrési policy |
| `FrontMatterKeys` | Core/FrontMatter | Front matter mezőkulcs konstansok |
| `FrontMatterValueBuilder` | Core/FrontMatter | Dinamikus prop → front matter érték mapping |
| `FrontMatterTextService` | Core/FrontMatter | Template build és szöveg parse |
| `FrontMatterTypeInputParser` | Core/FrontMatter | Type picker input értelmezése |

---

## Tesztelési lefedettség

| Komponens | Tesztfájl | Megjegyzés |
|---|---|---|
| `CreateFrontMatterFromBlockUseCase` | `Application/CreateFrontMatterFromBlockUseCaseTests.cs` | 5 teszt |
| `SmartObjectMetadata` | `SmartObjects/SmartObjectMetadataTests.cs` | 7 teszt |
| `ReadSmartObjectMetadataUseCase` | `SmartObjects/ReadSmartObjectMetadataUseCaseTests.cs` | 5 teszt |
| `WriteSmartObjectMetadataUseCase` | `SmartObjects/WriteSmartObjectMetadataUseCaseTests.cs` | 6 teszt |
| Front matter Core logika | `FrontMatter/*Tests.cs` | 25 teszt |
| `AutoCadPlugin` | — | Manuális integrációs teszt szükséges |
| `DrawingObjectMetadataStore` | — | Manuális integrációs teszt szükséges |
| `DrawingTypeStore` | — | Manuális integrációs teszt szükséges |

**Összesen: 48 unit teszt, 0 hiba (2026-04-23)**

---

*Utolsó frissítés: 2026-04-23 — Task #0006*
