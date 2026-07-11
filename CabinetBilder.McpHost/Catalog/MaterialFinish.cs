using System.Text.Json;

namespace CabinetBilder.McpHost.Catalog;

/// <summary>
/// A felület-attribútum (festett/fóliás/laminált/hdf) származtatása az anyag
/// BodyJson `finish` mezőjéből. A Doorstar 'Menyíségek' dokumentum ezek szerint
/// bontja az anyagszükségletet. A felület az anyag tulajdonsága (nem a Skeleton-domainé).
/// </summary>
public static class MaterialFinish
{
    public const string Unknown = "ismeretlen";

    /// <summary>A nyers `finish` kulcs (pl. "festett") normalizált magyar címkéje.</summary>
    public static string Label(string? finishKey)
    {
        return finishKey?.Trim().ToLowerInvariant() switch
        {
            "festett" => "festett",
            "folias" or "fóliás" => "fóliás",
            "laminalt" or "laminált" => "laminált",
            "hdf" => "hdf hátlap",
            null or "" => Unknown,
            _ => finishKey!.Trim().ToLowerInvariant()
        };
    }

    /// <summary>A felület-címke kinyerése az anyag BodyJson-jából (finish kulcs).</summary>
    public static string FromBodyJson(string? bodyJson)
    {
        if (string.IsNullOrWhiteSpace(bodyJson)) return Unknown;
        try
        {
            using var doc = JsonDocument.Parse(bodyJson);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("finish", out var finish) &&
                finish.ValueKind == JsonValueKind.String)
            {
                return Label(finish.GetString());
            }
        }
        catch (JsonException)
        {
            // rossz JSON → ismeretlen
        }
        return Unknown;
    }
}
