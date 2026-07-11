using System.Text.Json;

namespace CabinetBilder.McpHost.Catalog;

/// <summary>
/// Tetszőleges attribútum kiolvasása az anyag BodyJson-jából (pl. "color", "finish").
/// A Doorstar szabászat-tábla "Szín" oszlopa ehhez kell. Pure static.
/// </summary>
public static class MaterialAttributes
{
    public static string? Get(string? bodyJson, string key)
    {
        if (string.IsNullOrWhiteSpace(bodyJson)) return null;
        try
        {
            using var doc = JsonDocument.Parse(bodyJson);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty(key, out var v) &&
                v.ValueKind == JsonValueKind.String)
            {
                return v.GetString();
            }
        }
        catch (JsonException)
        {
        }
        return null;
    }

    /// <summary>A "color" attribútum magyar címkéje (feher→Fehér, sonoma→Sonoma), vagy üres.</summary>
    public static string ColorLabel(string? bodyJson)
    {
        var raw = Get(bodyJson, "color");
        return raw switch
        {
            null or "" => "",
            "feher" => "Fehér",
            "sonoma" => "Sonoma",
            _ => char.ToUpper(raw[0]) + raw.Substring(1)
        };
    }
}
