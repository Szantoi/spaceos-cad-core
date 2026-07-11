# Task ID: 0001

# Title: Create Application Service Layer (Handlers)

# Category: feature

# Milestone: 9

# Status: active

## Szándék (Intent)

Migráljuk az első use-case-t alkalmazási szinten, hogy a DDD/CQRS irányelveket követhessük és csökkentsük a command osztály komplexitását.

## Elfogadási kritérium (Acceptance Criteria)

- [x] Application use-case mappa és komponensek létrehozása az `App.AutoCadScripts` projekten belül (`Application/UseCases`)
- [x] `CreateFrontMatterFromBlockUseCase` use-case osztály implementálása request/result modellel
- [x] Injekciók előkészítése interfészekkel: `IDrawingTypeStore`, `IDrawingObjectMetadataStore`
- [x] `ILogger` bekötése use-case szinten
- [x] Unit teszt a handler logikához (`App.AutoCadScripts.Tests/Application/`)
- [x] `SyncBlockToFrontMatter` parancs refaktorálása az új handler meghívásához
- [x] Build: `dotnet build -c Release` → Success (3 projekt)
- [x] Teszt: `dotnet test -c Release` → All green (10+ teszt)

## Tanúsítás (Evidence)

Miként ellenőrizzük, hogy kész?

- Kód:
  - `App.AutoCadScripts/Application/UseCases/CreateFrontMatterFromBlockUseCase.cs`
- Refaktor:
  - `App.AutoCadScripts/Commands/DimensionCommands.cs` - `SyncBlockToFrontMatter()` meghívja az új handler-t
- Teszt:
  - `App.AutoCadScripts.Tests/Application/CreateFrontMatterFromBlockUseCaseTests.cs` (Sikeres unit tesztek AutoCAD DLL betöltés nélkül)
- Build: `dotnet build .\ कैबिनेटBilder.AutoCadScripts.slnx -c Release -nologo -v:minimal` → Success
- Teszt: `dotnet test .\App.AutoCadScripts.Tests\App.AutoCadScripts.Tests.csproj -c Release -nologo -v:minimal` → All passed

## Megjegyzések (Notes)

### Kontextus

- A milestone 9 a stratégiai terv szerint az alkalmazási szint alapjainak lerakása.
- Ez az első lépés a mediator integrációhoz, amely később nagyobb use-case-ket támogat.
- A transaction boundary az AutoCAD command-nél marad (nem mozdul az Application layer-be).

### Függőségek

- Core layer (`App.AutoCadScripts.Core`) már stabil
- Infrastructure layer (`DrawingTypeStore`, `DrawingObjectMetadataStore`) már működik
- MSTest 3.8.3 rendelkezésre áll
- AutoCAD függőségek kivezetésre kerültek a teszt projektből, hogy elkerüljük a TESTERROR-okat.

### Megjegyzések az implementációhoz

1. **Projekt struktura:**

   ```
   App.AutoCadScripts.Application/
   ├── App.AutoCadScripts.Application.csproj (net10.0)
   ├── UseCases/
   │   └── CreateFrontMatterFromBlockUseCase.cs (Main handler)
   │   ├── CreateFrontMatterFromBlockRequest.cs
   │   ├── CreateFrontMatterFromBlockResponse.cs
   └── (további use-case-ek később)
   ```

2. **Use-case szerkezet (szokásos minta):**

   ```csharp
   public record CreateFrontMatterFromBlockRequest(ObjectId BlockId, string TypeName);
   public record CreateFrontMatterFromBlockResponse(ObjectId MTextId, string FrontMatterText, bool SchemaMarkerWritten);

   public class CreateFrontMatterFromBlockUseCase
   {
       ...
   }
   ```

3. **Integrációs pont a command-ben:**

   ```csharp
   private static void SyncBlockToFrontMatter(string typeName)
   {
       var useCase = new CreateFrontMatterFromBlockUseCase(_typeStore, _metadataStore, _logger);
       var response = useCase.Execute(new CreateFrontMatterFromBlockRequest(...), tr);
       editor.WriteMessage($"Front matter synced to {response.MTextId}");
   }
   ```

4. **Teszt stratégia:**
   - Szimulált UseCase futtatása (C# adatszerkezetek, BlockId = string)
   - MSTest, Mock ILogger nélkül (Vagy Custom Abstractions), független működés
   - Verify: FrontMatter értékek és template összeállítás

### Jelenlegi állapot

- A `CreateFrontMatterFromBlockUseCase` sikeresen elszeparálva az AutoCAD memóriaterektől.
- Unit tesztek megírva és stabilak (`dotnet test` hiba nélkül lenyílik).
- A teszt projekt (.csproj) tisztítva az AutoCAD Dll referenciáktól.
- A feladatot Sikeresnek nyilvánítom és archiválható.

### Kockázatok

- None

---

**Started:** 2026-04-22
**Completed:** 2026-04-22
**Duration:** 1 d
**Owner:**
