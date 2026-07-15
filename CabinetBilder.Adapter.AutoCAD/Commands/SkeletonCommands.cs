using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using CabinetBilder.Core.Skeletons;
using CabinetBilder.Core.Ports;
using CabinetBilder.Core.Common;
using Microsoft.Extensions.DependencyInjection;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CabinetBilder.Adapter.AutoCAD.Commands;

public class SkeletonCommands
{
    [CommandMethod("CB_SKELETON_CREATE")]
    public async void CreateSkeleton()
    {
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        var ed = doc.Editor;

        var prompt = ed.GetSelection();
        if (prompt.Status != PromptStatus.OK) return;

        var serviceProvider = AutoCadPlugin.ServiceProvider;
        var store = serviceProvider.GetRequiredService<ICadSkeletonStore>();

        foreach (var id in prompt.Value.GetObjectIds())
        {
            var skeleton = new Skeleton(SkeletonId.New());
            var result = await store.WriteSkeletonAsync(id.Handle.ToString(), skeleton);
            
            if (result.IsSuccess)
            {
                // Trigger initial geometry sync
                Infrastructure.Geometry.SkeletonSyncService.Sync(id, skeleton);
                
                ed.WriteMessage($"\nSkeleton created for object {id.Handle}");
            }
            else
            {
                ed.WriteMessage($"\nFailed to create skeleton: {result.ErrorMessage}");
            }
        }
    }

    /// <summary>
    /// Edits a single skeleton parameter from the command line. This is the
    /// supported replacement for the removed OPM property palette (AutoCAD 2027
    /// dropped AcPropServices.dll): ApplyParameter re-runs Rebuild() (including
    /// drilling and grooving), the skeleton is persisted back to the XRecord,
    /// and the geometry (panels + drillings + grooves) is redrawn.
    /// </summary>
    [CommandMethod("CB_SKELETON_PARAM")]
    public async void EditSkeletonParameter()
    {
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        var ed = doc.Editor;

        var prompt = ed.GetEntity("\nSelect skeleton object: ");
        if (prompt.Status != PromptStatus.OK) return;

        var store = AutoCadPlugin.ServiceProvider.GetRequiredService<ICadSkeletonStore>();
        var handle = prompt.ObjectId.Handle.ToString();

        var skeleton = await store.ReadSkeletonAsync(handle);
        if (skeleton == null)
        {
            ed.WriteMessage("\nNo skeleton found on this object.");
            return;
        }

        ed.WriteMessage("\nParameters:");
        foreach (var p in skeleton.Parameters)
        {
            ed.WriteMessage($"\n - {p.Key} = {p.Value} ({p.Type})");
        }

        var keyPrompt = ed.GetString(new PromptStringOptions("\nParameter name: ") { AllowSpaces = false });
        if (keyPrompt.Status != PromptStatus.OK) return;

        var parameter = skeleton.Parameters.FirstOrDefault(
            p => p.Key.Equals(keyPrompt.StringResult, StringComparison.OrdinalIgnoreCase));
        if (parameter == null)
        {
            ed.WriteMessage($"\nUnknown parameter '{keyPrompt.StringResult}'.");
            return;
        }

        object? newValue = PromptForValue(ed, parameter);
        if (newValue == null) return;

        var applyResult = skeleton.ApplyParameter(parameter.Key, newValue);
        if (!applyResult.IsSuccess)
        {
            ed.WriteMessage($"\nFailed to apply parameter: {applyResult.ErrorMessage}");
            return;
        }

        var writeResult = await store.WriteSkeletonAsync(handle, skeleton);
        if (!writeResult.IsSuccess)
        {
            ed.WriteMessage($"\nParameter applied but persisting failed: {writeResult.ErrorMessage}");
            return;
        }

        Infrastructure.Geometry.SkeletonSyncService.Sync(prompt.ObjectId, skeleton);
        ed.WriteMessage($"\n{parameter.Key} = {newValue}; skeleton saved and geometry redrawn.");
    }

    /// <summary>Prompts for a new parameter value matching the parameter's type; null on cancel/parse error.</summary>
    private static object? PromptForValue(Editor ed, SkeletonParameter parameter)
    {
        switch (parameter.Type)
        {
            case ParameterType.Double:
            {
                var res = ed.GetDouble(new PromptDoubleOptions($"\nNew value for {parameter.Key}: "));
                return res.Status == PromptStatus.OK ? res.Value : null;
            }
            case ParameterType.Boolean:
            {
                var res = ed.GetString(new PromptStringOptions($"\nNew value for {parameter.Key} (true/false): ") { AllowSpaces = false });
                if (res.Status != PromptStatus.OK) return null;
                if (bool.TryParse(res.StringResult, out bool b)) return b;
                ed.WriteMessage($"\n'{res.StringResult}' is not a valid boolean.");
                return null;
            }
            default:
            {
                var res = ed.GetString(new PromptStringOptions($"\nNew value for {parameter.Key}: ") { AllowSpaces = true });
                return res.Status == PromptStatus.OK ? res.StringResult : null;
            }
        }
    }

    [CommandMethod("CB_SKELETON_INFO")]
    public async void SkeletonInfo()
    {
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        var ed = doc.Editor;

        var prompt = ed.GetEntity("\nSelect object to see skeleton info: ");
        if (prompt.Status != PromptStatus.OK) return;

        var serviceProvider = AutoCadPlugin.ServiceProvider;
        var store = serviceProvider.GetRequiredService<ICadSkeletonStore>();

        var skeleton = await store.ReadSkeletonAsync(prompt.ObjectId.Handle.ToString());
        if (skeleton == null)
        {
            ed.WriteMessage("\nNo skeleton found on this object.");
            return;
        }

        ed.WriteMessage($"\nSkeleton ID: {skeleton.Id}");
        ed.WriteMessage($"\nName: {skeleton.Name}");
        ed.WriteMessage("\nParameters:");
        foreach (var p in skeleton.Parameters)
        {
            ed.WriteMessage($"\n - {p.Key}: {p.Value} ({p.Type})");
        }
    }
}
