using System.Collections.Generic;

namespace CabinetBilder.McpHost.Production;

/// <summary>Függőség típusa (a legacy Egység_idő "Feladat Függőség Tipusa" modern változata).</summary>
public enum DependencyType
{
    FinishStart,  // FS: a függő művelet a szülő BEFEJEZÉSE után indul
    StartStart    // SS: a függő művelet a szülő INDÍTÁSA után indul
}

/// <summary>Egy művelet függősége egy másiktól (FS/SS + késleltetés órában).</summary>
public sealed record OperationDependency(string OnOperationId, DependencyType Type, double LagHours = 0);

/// <summary>
/// Egy gyártási művelet MODERN definíciója — a legacy Egység_idő.xlsx tiszta változata.
/// Az egységidő TISZTA ÓRÁBAN (a mért nap-tört ×24-ből), az alkatrész-illesztés strukturáltan.
/// A legacy 30-oszlopos, kódfüggő séma helyett ez a domain-modell.
/// </summary>
public sealed record Operation(
    string Id,
    string Name,
    string Role,
    double UnitTimeHours,
    int Headcount,
    string? MatchCategory = null,   // null = minden anyagkategória
    string? MatchSurface = null,    // null = minden felület
    bool PerCabinet = false,        // true = egyszer/szekrény (nem alkatrészenként)
    IReadOnlyList<OperationDependency>? DependsOn = null); // ütemezési függőségek (FS/SS)

/// <summary>
/// A korpusz-gyártás modern műveleti katalógusa. Az egységidők a valós, munkanaplóval mért
/// Doorstar Egység_idő adatból származnak, tiszta órára váltva
/// (docs/knowledge/doorstar_egysegido_folyamatmodell.md). Pure static.
/// </summary>
public static class OperationCatalog
{
    /// <summary>Korpusz-panelek anyagkategóriája (a Skeleton carcass = Bútorlap).</summary>
    public const string CarcassCategory = "Bútorlap";
    public const string BackCategory = "Hátlap";

    public static IReadOnlyList<Operation> CarcassOperations { get; } = new List<Operation>
    {
        // Korpusz-lánc: Szabás → CNC-furat → Élzárás → Csiszolás → Összeállítás.
        // Hátlap-szabás párhuzamos ág, az Összeállításnál csatlakozik (mint a woodwork_domain §11 DAG).

        // Mért referencia: 22-es Szabás (Tokmag) 0,0375 h; itt korpuszlapra alkalmazva
        new("SZABAS", "Szabás (nesting)", "Asztalos", 0.0375, 2, MatchCategory: CarcassCategory),
        // Mért: CNC-marás (Borítás) 0,0953 h — a szabás után
        new("CNC_FURAT", "CNC furat/marás", "CNC", 0.0953, 1, MatchCategory: CarcassCategory,
            DependsOn: new[] { new OperationDependency("SZABAS", DependencyType.FinishStart) }),
        // Élzárás — a korpuszpanelek látszó éleire (a mért Borítás-műveletek nagyságrendje)
        new("ELZARAS", "Élzárás", "Asztalos", 0.0500, 1, MatchCategory: CarcassCategory,
            DependsOn: new[] { new OperationDependency("CNC_FURAT", DependencyType.FinishStart) }),
        // Mért: Csiszolás (Borítás) 0,0756 h
        new("CSISZOLAS", "Csiszolás", "Összeszerelő", 0.0756, 1, MatchCategory: CarcassCategory,
            DependsOn: new[] { new OperationDependency("ELZARAS", DependencyType.FinishStart) }),
        // Hátlap: egyszerű szabás (párhuzamos ág)
        new("HATLAP_SZABAS", "Hátlap szabás", "Asztalos", 0.0375, 1, MatchCategory: BackCategory),
        // Összeállítás — egyszer a szekrényre; a korpusz csiszolása UTÁN és a hátlap kész
        new("OSSZEALLITAS", "Bútor összeállítása", "Összeszerelő", 0.5, 1, PerCabinet: true,
            DependsOn: new[]
            {
                new OperationDependency("CSISZOLAS", DependencyType.FinishStart),
                new OperationDependency("HATLAP_SZABAS", DependencyType.FinishStart)
            }),
    };
}
