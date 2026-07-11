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
