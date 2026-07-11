using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CabinetBilder.Adapter.AutoCAD.Infrastructure.Persistence.Entities;

/// <summary>
/// Persistence entity for storing smart object metadata in a central database.
/// Supports versioning and multi-user synchronization.
/// </summary>
[Table("smart_objects")]
public class SmartObjectEntity
{
    /// <summary>
    /// Composite Key Part 1: Unique identifier for the drawing (e.g. Fingerprint GUID or File Path).
    /// </summary>
    [Key, Column("drawing_id", Order = 0)]
    [Required]
    public string DrawingId { get; set; } = string.Empty;

    /// <summary>
    /// Composite Key Part 2: The AutoCAD handle of the object within the drawing.
    /// </summary>
    [Key, Column("handle", Order = 1)]
    [Required]
    public string Handle { get; set; } = string.Empty;

    /// <summary>
    /// JSON representation of the metadata fields.
    /// In PostgreSQL, this should map to a jsonb column.
    /// </summary>
    [Column("metadata_json", TypeName = "jsonb")]
    public string MetadataJson { get; set; } = "{}";

    /// <summary>
    /// Version identifier (hash or timestamp) for optimistic concurrency and git-style sync.
    /// </summary>
    [Column("version")]
    [Required]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the last synchronization.
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// The user or machine that last pushed this version.
    /// </summary>
    [Column("updated_by")]
    public string UpdatedBy { get; set; } = string.Empty;
}

