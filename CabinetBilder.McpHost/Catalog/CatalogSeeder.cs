using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CabinetBilder.Core.Sync;

namespace CabinetBilder.McpHost.Catalog;

/// <summary>
/// Interim anyagkatalógus-seed. A VPS katalógus-API (GET /api/inventory/materials)
/// még nem él (Week 5-6), ezért a lokál SQLite cache-t egy Doorstar-realisztikus
/// statikus listával töltjük fel, ha üres. Éles VPS-API esetén ezt a
/// PullMaterialsAsync + UpsertMaterialCacheAsync váltja ki (CON-001: most nincs VPS-hívás).
/// </summary>
public static class CatalogSeeder
{
    public const string TenantId = "doorstar-interim";
    public const string Etag = "interim-v2";

    public static IReadOnlyList<MaterialDto> InterimMaterials { get; } = new List<MaterialDto>
    {
        new("LAM18_W1000", "Fehér laminált bútorlap 18mm", "Bútorlap", 18, "{\"color\":\"feher\",\"finish\":\"laminalt\"}", 5200m),
        new("LAM18_SONOMA", "Sonoma tölgy laminált bútorlap 18mm", "Bútorlap", 18, "{\"color\":\"sonoma\",\"finish\":\"laminalt\"}", 5600m),
        new("HDF3_WHITE", "Fehér HDF hátlap 3mm", "Hátlap", 3, "{\"color\":\"feher\",\"finish\":\"hdf\"}", 1250m),
        new("MDF18_PAINT", "Festett MDF front 18mm", "Front", 18, "{\"finish\":\"festett\"}", 9400m),
        new("MDF18_FOIL", "Fóliás MDF front 18mm", "Front", 18, "{\"finish\":\"folias\"}", 7100m),
        // Élzárók — az ár folyóméterre (fm) értendő, nem m²-re (BodyJson unit=fm)
        new("ABS2_WHITE", "ABS élzáró fehér 2mm (22mm)", "Élzáró", 2, "{\"color\":\"feher\",\"unit\":\"fm\"}", 180m),
        new("ABS2_SONOMA", "ABS élzáró sonoma 2mm (22mm)", "Élzáró", 2, "{\"color\":\"sonoma\",\"unit\":\"fm\"}", 220m),
    };

    /// <summary>
    /// Gondoskodik róla, hogy a lokál anyag-cache tartalmazza az interim katalógust:
    /// üres cache VAGY hiányzó interim anyagkód esetén seedel (a katalógus bővülésekor
    /// a meglévő cache-be is bekerülnek az új tételek), különben a cache-t adja vissza.
    /// </summary>
    public static async Task<IReadOnlyList<MaterialDto>> EnsureSeededAsync(ILocalStore store, CancellationToken ct = default)
    {
        var cached = await store.GetCachedMaterialsAsync(ct);
        if (cached.IsSuccess && cached.Value.Count > 0)
        {
            var codes = cached.Value.Select(m => m.MaterialCode).ToHashSet();
            if (InterimMaterials.All(m => codes.Contains(m.MaterialCode)))
            {
                return cached.Value;
            }
        }

        await store.UpsertMaterialCacheAsync(InterimMaterials, Etag, TenantId, ct);
        var reread = await store.GetCachedMaterialsAsync(ct);
        return reread.IsSuccess ? reread.Value : InterimMaterials;
    }
}
