using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CabinetBilder.Core.Skeletons;

namespace CabinetBilder.McpHost.Skeletons;

/// <summary>
/// A tervezői szándék egyetlen bejegyzése (REQ-008). A Skeleton domain nem tárol
/// intenteket, ezért a host-processzben, a registry mellett gyűjtjük őket.
/// </summary>
public sealed record DesignIntent(DateTime Timestamp, string Intent, string? ParameterKey);

/// <summary>
/// Egy kezelt skeleton + a hozzá tartozó szándék-napló + egy zár az
/// aggregátum-szintű konzisztens módosításhoz (Rebuild alatt).
/// </summary>
public sealed class SkeletonEntry
{
    public Skeleton Skeleton { get; }
    public List<DesignIntent> Intents { get; } = new();
    public object Lock { get; } = new();

    public SkeletonEntry(Skeleton skeleton) => Skeleton = skeleton;
}

/// <summary>
/// A host-process in-memory nyilvántartása a kezelt Skeleton példányokról
/// (TASK-002). Thread-safe: az MCP-host párhuzamos tool-hívásokat kaphat.
/// </summary>
public sealed class SkeletonRegistry
{
    private readonly ConcurrentDictionary<Guid, SkeletonEntry> _items = new();

    public SkeletonEntry Create(Guid? id, string name, string? intent)
    {
        var skeletonId = id.HasValue ? new SkeletonId(id.Value) : SkeletonId.New();
        var skeleton = new Skeleton(skeletonId) { Name = name };
        var entry = new SkeletonEntry(skeleton);
        if (!string.IsNullOrWhiteSpace(intent))
        {
            entry.Intents.Add(new DesignIntent(DateTime.UtcNow, intent!, null));
        }
        _items[skeletonId.Value] = entry;
        return entry;
    }

    public bool TryGet(Guid id, out SkeletonEntry entry) => _items.TryGetValue(id, out entry!);

    public int Count => _items.Count;

    /// <summary>
    /// Webre kész, sima JSON-struktúra a skeleton állapotáról (REQ-007).
    /// </summary>
    public static object ToDto(SkeletonEntry entry)
    {
        var s = entry.Skeleton;
        return new
        {
            id = s.Id.Value.ToString(),
            name = s.Name,
            createdAt = s.CreatedAt,
            updatedAt = s.UpdatedAt,
            parameters = s.Parameters.Select(p => new
            {
                key = p.Key,
                value = p.Value,
                description = p.Description,
                type = p.Type.ToString()
            }).ToArray(),
            components = s.Components.Select(c => new
            {
                name = c.Name,
                materialId = c.MaterialId,
                width = c.Width,
                height = c.Height,
                thickness = c.Thickness,
                posX = c.PosX, posY = c.PosY, posZ = c.PosZ,
                normalX = c.NormalX, normalY = c.NormalY, normalZ = c.NormalZ,
                dirX = c.DirX, dirY = c.DirY, dirZ = c.DirZ
            }).ToArray(),
            intents = entry.Intents.Select(i => new
            {
                timestamp = i.Timestamp,
                intent = i.Intent,
                parameterKey = i.ParameterKey
            }).ToArray()
        };
    }
}
