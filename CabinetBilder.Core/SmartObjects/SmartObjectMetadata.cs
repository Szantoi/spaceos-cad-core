using System.Collections.ObjectModel;

namespace CabinetBilder.Core.SmartObjects;

/// <summary>
/// Immutable value object representing the metadata fields of a smart AutoCAD object.
/// Keys are matched case-insensitively.
/// </summary>
public sealed class SmartObjectMetadata
{
    private readonly IReadOnlyDictionary<string, string> _fields;
    public string Version { get; init; } = string.Empty;

    /// <summary>An empty metadata instance with no fields set.</summary>
    public static SmartObjectMetadata Empty { get; } =
        new SmartObjectMetadata(new Dictionary<string, string>());

    /// <summary>All stored key-value pairs.</summary>
    public IReadOnlyDictionary<string, string> Fields => _fields;

    private SmartObjectMetadata(Dictionary<string, string> fields, string version = "")
    {
        _fields = new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(fields, StringComparer.OrdinalIgnoreCase));
        Version = version;
    }

    /// <summary>
    /// Creates a new <see cref="SmartObjectMetadata"/> from the supplied dictionary and version.
    /// </summary>
    public static SmartObjectMetadata From(IReadOnlyDictionary<string, string> fields, string version = "")
    {
        ArgumentNullException.ThrowIfNull(fields);
        return new SmartObjectMetadata(new Dictionary<string, string>(fields, StringComparer.OrdinalIgnoreCase), version);
    }

    /// <summary>
    /// Tries to get the value for the specified <paramref name="key"/>.
    /// </summary>
    public bool TryGetValue(string key, out string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            value = string.Empty;
            return false;
        }

        return _fields.TryGetValue(key, out value!);
    }

    /// <summary>
    /// Returns a new <see cref="SmartObjectMetadata"/> with the specified key set to the given value.
    /// Does NOT update the version; versioning is usually handled by the persistence layer upon "commit".
    /// </summary>
    public SmartObjectMetadata With(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        var copy = new Dictionary<string, string>(_fields, StringComparer.OrdinalIgnoreCase)
        {
            [key] = value
        };
        return new SmartObjectMetadata(copy, Version);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not SmartObjectMetadata other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (_fields.Count != other._fields.Count) return false;

        foreach (var kvp in _fields)
        {
            if (!other._fields.TryGetValue(kvp.Key, out string? otherValue) || kvp.Value != otherValue)
                return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var kvp in _fields.OrderBy(x => x.Key))
        {
            hash.Add(kvp.Key.ToLowerInvariant());
            hash.Add(kvp.Value);
        }
        return hash.ToHashCode();
    }

    /// <summary>
    /// Computes a stable hash of the metadata content for versioning.
    /// </summary>
    public string ComputeHash()
    {
        var sortedFields = _fields.OrderBy(x => x.Key)
            .Select(x => $"{x.Key.ToLowerInvariant()}:{x.Value}");
        var content = string.Join("|", sortedFields);
        
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}

