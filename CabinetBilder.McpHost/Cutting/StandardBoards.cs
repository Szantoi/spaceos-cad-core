namespace CabinetBilder.McpHost.Cutting;

/// <summary>
/// Standard táblaméret + rostirány egy anyagkategóriához. A valódi nesting/optimalizálás
/// a VPS lapszabász-modul dolga (EPIC-CUTTING-Q3); ez csak procurement-becsléshez kell.
/// </summary>
public sealed record BoardSpec(double LengthMm, double WidthMm, string Grain);

public static class StandardBoards
{
    /// <summary>Kihasználtsági faktor a tábla-becsléshez (nesting-veszteség nélkül ~0.8).</summary>
    public const double UsableFactor = 0.8;

    private static readonly BoardSpec DefaultBoard = new(2800, 2070, "nincs");

    private static readonly BoardSpec Panel = new(2800, 2070, "hossz");   // bútorlap, front (erezett)
    private static readonly BoardSpec Sheet = new(2800, 2070, "nincs");   // hdf hátlap (nem erezett)

    /// <summary>Kategória alapján a standard tábla + rostirány.</summary>
    public static BoardSpec ForCategory(string? category)
    {
        return category?.Trim().ToLowerInvariant() switch
        {
            "bútorlap" or "butorlap" => Panel,
            "front" => Panel,
            "hátlap" or "hatlap" => Sheet,
            _ => DefaultBoard
        };
    }
}
