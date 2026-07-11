using System;
using System.Collections.Generic;
using System.Linq;
using CabinetBilder.Core.Common;
using System.Text.Json.Serialization;

namespace CabinetBilder.Core.Skeletons;

/// <summary>
/// The central aggregate root for parametric cabinet design.
/// Lives in-memory and persists to DWG XRecords.
/// </summary>
public sealed class Skeleton
{
    [JsonInclude]
    [JsonPropertyName("Parameters")]
    private List<SkeletonParameter> _parameters = new();

    [JsonInclude]
    [JsonPropertyName("Components")]
    private List<SkeletonComponent> _components = new();

    public SkeletonId Id { get; private set; }
    public string Name { get; set; } = "New Cabinet";
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    [JsonIgnore]
    public IReadOnlyCollection<SkeletonParameter> Parameters => _parameters.AsReadOnly();

    [JsonIgnore]
    public IReadOnlyCollection<SkeletonComponent> Components => _components.AsReadOnly();

    // Private constructor for serialization
    private Skeleton() { }

    public Skeleton(SkeletonId id)
    {
        Id = id;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
        
        // Default parameters
        _parameters.Add(SkeletonParameter.Double("Width", 600.0, "Total cabinet width"));
        _parameters.Add(SkeletonParameter.Double("Height", 720.0, "Total cabinet height"));
        _parameters.Add(SkeletonParameter.Double("Depth", 560.0, "Total cabinet depth"));
        _parameters.Add(SkeletonParameter.Double("Thickness", 18.0, "Material thickness"));
        _parameters.Add(SkeletonParameter.Double("BackOffset", 5.0, "Back panel offset"));
        _parameters.Add(SkeletonParameter.String("CarcassMaterialId", "LAM18_W1000", "Carcass panel material (sides, top, bottom)"));
        _parameters.Add(SkeletonParameter.String("BackMaterialId", "HDF3_WHITE", "Back panel material"));
        _parameters.Add(SkeletonParameter.String("EdgingId", "ABS2_WHITE", "Edge banding for carcass panels"));

        Rebuild();
    }

    public Result ApplyParameter(string key, object value)
    {
        var parameter = _parameters.FirstOrDefault(p => p.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (parameter == null)
        {
            return Result.Failure($"Parameter '{key}' not found.");
        }

        try
        {
            var newValue = parameter with { Value = value };
            
            int index = _parameters.IndexOf(parameter);
            _parameters[index] = newValue;
            
            Rebuild();
            UpdatedAt = DateTime.UtcNow;
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to apply parameter: {ex.Message}");
        }
    }

    /// <summary>
    /// Re-calculates components based on current parameters.
    /// Standard Cabinet Logic: Sides run through (height), Top/Bottom between sides.
    /// </summary>
    public void Rebuild()
    {
        _components.Clear();

        double w = GetParameterValue<double>("Width");
        double h = GetParameterValue<double>("Height");
        double d = GetParameterValue<double>("Depth");
        double t = GetParameterValue<double>("Thickness");
        double bo = GetParameterValue<double>("BackOffset");
        string carcassMat = GetParameterValue<string>("CarcassMaterialId");
        string backMat = GetParameterValue<string>("BackMaterialId");

        // 1. Left Side
        _components.Add(new SkeletonComponent 
        { 
            Name = "Side Left", MaterialId = carcassMat, Width = d, Height = h, Thickness = t,
            PosX = 0, PosY = 0, PosZ = 0,
            NormalX = 1, NormalY = 0, NormalZ = 0,
            DirX = 0, DirY = 1, DirZ = 0
        });

        // 2. Right Side
        _components.Add(new SkeletonComponent 
        { 
            Name = "Side Right", MaterialId = carcassMat, Width = d, Height = h, Thickness = t,
            PosX = w - t, PosY = 0, PosZ = 0,
            NormalX = 1, NormalY = 0, NormalZ = 0,
            DirX = 0, DirY = 1, DirZ = 0
        });

        // 3. Bottom
        _components.Add(new SkeletonComponent 
        { 
            Name = "Bottom", MaterialId = carcassMat, Width = w - (2 * t), Height = d, Thickness = t,
            PosX = t, PosY = 0, PosZ = 0,
            NormalX = 0, NormalY = 0, NormalZ = 1,
            DirX = 1, DirY = 0, DirZ = 0
        });

        // 4. Top
        _components.Add(new SkeletonComponent 
        { 
            Name = "Top", MaterialId = carcassMat, Width = w - (2 * t), Height = d, Thickness = t,
            PosX = t, PosY = 0, PosZ = h - t,
            NormalX = 0, NormalY = 0, NormalZ = 1,
            DirX = 1, DirY = 0, DirZ = 0
        });

        // 5. Back
        _components.Add(new SkeletonComponent 
        { 
            Name = "Back", MaterialId = backMat, Width = w - (2 * bo), Height = h - (2 * bo), Thickness = 3.0,
            PosX = bo, PosY = d - 3.0, PosZ = bo,
            NormalX = 0, NormalY = 1, NormalZ = 0,
            DirX = 1, DirY = 0, DirZ = 0
        });

        // 6. Apply Drillings
        DrillingService.ApplyDrilling(this);
    }

    private T GetParameterValue<T>(string key)
    {
        var param = _parameters.First(p => p.Key == key);
        return (T)Convert.ChangeType(param.Value, typeof(T));
    }

    public void AddComponent(SkeletonComponent component)
    {
        _components.Add(component);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearComponents()
    {
        _components.Clear();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Computes the BOM lines based on the current components.
    /// </summary>
    public IEnumerable<CabinetBilder.Core.Sync.BomLine> ComputeBom()
    {
        // Élzárás: a korpusz-panelek (CarcassMaterialId anyagú sorok) kapnak élzárót,
        // a hátlap nem. Az élzáró anyagát az EdgingId paraméter adja.
        string carcassMat = GetParameterValue<string>("CarcassMaterialId");
        string edgingId = GetParameterValue<string>("EdgingId");

        return _components.Select(c => new CabinetBilder.Core.Sync.BomLine(
            c.Name,
            c.Height, // Length is usually the larger dimension
            c.Width,
            c.Thickness,
            c.MaterialId,
            1,
            EdgingId: c.MaterialId == carcassMat && !string.IsNullOrEmpty(edgingId) ? edgingId : null
        ));
    }
}
