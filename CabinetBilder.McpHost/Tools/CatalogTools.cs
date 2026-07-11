using System.Linq;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using CabinetBilder.Core.Sync;
using CabinetBilder.McpHost.Catalog;
using CabinetBilder.McpHost.Serialization;

namespace CabinetBilder.McpHost.Tools;

/// <summary>
/// Anyag- és sablon-katalógus MCP toolok. A lokál SQLite cache-ből olvasnak
/// (ILocalStore DI-injektálva); üres anyag-cache esetén interim seed (CatalogSeeder).
/// </summary>
[McpServerToolType]
public static class CatalogTools
{
    [McpServerTool(Name = "list_materials"), Description("Visszaadja az elérhető anyagkatalógust (lokál cache; üres esetén interim seed). Webre kész JSON.")]
    public static async Task<McpToolResponse<object>> ListMaterials(ILocalStore store, CancellationToken ct = default)
    {
        var materials = await CatalogSeeder.EnsureSeededAsync(store, ct);
        var value = materials.Select(m => new
        {
            materialCode = m.MaterialCode,
            displayName = m.DisplayName,
            category = m.Category,
            thickness = m.Thickness,
            price = m.Price
        }).ToArray();

        return new McpToolResponse<object> { IsSuccess = true, Status = "Ok", Value = value };
    }

    [McpServerTool(Name = "list_templates"), Description("Visszaadja a gyorssablon-katalógust a lokál cache-ből (jelenleg üres lehet).")]
    public static async Task<McpToolResponse<object>> ListTemplates(ILocalStore store, CancellationToken ct = default)
    {
        var result = await store.GetCachedTemplatesAsync(ct);
        if (!result.IsSuccess)
        {
            return new McpToolResponse<object>
            {
                IsSuccess = false,
                Status = "Error",
                Errors = result.Errors.ToList(),
                Value = null
            };
        }

        var value = result.Value.Select(t => new
        {
            id = t.Id.ToString(),
            name = t.Name,
            version = t.Version
        }).ToArray();

        return new McpToolResponse<object> { IsSuccess = true, Status = "Ok", Value = value };
    }
}
