using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CabinetBilder.Core.Skeletons;

/// <summary>
/// Represents a typed parametric value for a Skeleton.
/// </summary>
[JsonConverter(typeof(SkeletonParameterConverter))]
public record SkeletonParameter
{
    public string Key { get; init; } = string.Empty;
    public object Value { get; init; } = 0.0;
    public string? Description { get; init; }
    public ParameterType Type { get; init; }

    public static SkeletonParameter Double(string key, double value, string? description = null) 
        => new() { Key = key, Value = value, Type = ParameterType.Double, Description = description };

    public static SkeletonParameter String(string key, string value, string? description = null) 
        => new() { Key = key, Value = value, Type = ParameterType.String, Description = description };

    public static SkeletonParameter Bool(string key, bool value, string? description = null) 
        => new() { Key = key, Value = value, Type = ParameterType.Boolean, Description = description };
}

public enum ParameterType
{
    Double,
    String,
    Boolean
}

public class SkeletonParameterConverter : JsonConverter<SkeletonParameter>
{
    public override SkeletonParameter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
        {
            var root = doc.RootElement;
            var key = root.GetProperty("Key").GetString() ?? "";
            var description = root.TryGetProperty("Description", out var d) ? d.GetString() : null;
            var type = (ParameterType)root.GetProperty("Type").GetInt32();
            
            object value = type switch
            {
                ParameterType.Double => root.GetProperty("Value").GetDouble(),
                ParameterType.String => root.GetProperty("Value").GetString() ?? "",
                ParameterType.Boolean => root.GetProperty("Value").GetBoolean(),
                _ => root.GetProperty("Value").GetRawText()
            };

            return new SkeletonParameter
            {
                Key = key,
                Value = value,
                Description = description,
                Type = type
            };
        }
    }

    public override void Write(Utf8JsonWriter writer, SkeletonParameter value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("Key", value.Key);
        writer.WritePropertyName("Value");
        JsonSerializer.Serialize(writer, value.Value, options);
        if (value.Description != null) writer.WriteString("Description", value.Description);
        writer.WriteNumber("Type", (int)value.Type);
        writer.WriteEndObject();
    }
}
