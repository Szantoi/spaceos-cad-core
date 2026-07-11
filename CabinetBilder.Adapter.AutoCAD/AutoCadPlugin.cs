using CabinetBilder.Core.FrontMatter;
using CabinetBilder.Adapter.AutoCAD.Infrastructure.ObjectMetadata;
using CabinetBilder.Adapter.AutoCAD.Infrastructure;
using CabinetBilder.Adapter.AutoCAD.Infrastructure.Overrules;
using CabinetBilder.Adapter.AutoCAD.UI.SmartObjectPalette;
using CabinetBilder.Adapter.AutoCAD.Infrastructure.OPM;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using CabinetBilder.Core.Ports;
using CabinetBilder.Core.Common;
using Microsoft.Extensions.DependencyInjection;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using System.Linq;

// Define an alias to avoid conflict with the local Application namespace

[assembly: ExtensionApplication(typeof(CabinetBilder.Adapter.AutoCAD.AutoCadPlugin))]

namespace CabinetBilder.Adapter.AutoCAD
{
    public class AutoCadPlugin : IExtensionApplication
    {
        private static IServiceProvider? _serviceProvider;
        public static IServiceProvider ServiceProvider => _serviceProvider ?? throw new InvalidOperationException("Plugin not initialized.");

        public void Initialize()
        {
            // 1. Build Service Provider
            _serviceProvider = DependencyInjection.BuildServiceProvider();

            // 2. Initialize Smart Object PaletteSet
            _ = SmartObjectPaletteManager.Instance;

            // 3. Register overrules: grip guard + change observer
            SmartObjectGripOverrule.Register();
            SmartObjectChangeObserver.Register();

            // 4. Wire change observer -> palette refresh
            SmartObjectChangeObserver.SmartObjectModified += SmartObjectPaletteManager.Instance.NotifyObjectModified;

            // 4.1 Register OPM properties
            SkeletonPropertyManager.Register();

            // 5. Subscribe to documents being created/opened
            AcadApp.DocumentManager.DocumentCreated += OnDocumentCreated;

            // 6. Run collision detection for existing documents (startup)
            var collisionHandler = ServiceProvider.GetRequiredService<IDrawingOpenedHandler>();
            foreach (Document doc in AcadApp.DocumentManager)
            {
                SubscribeToDocumentEvents(doc);
                collisionHandler.HandleDocument(doc);
            }
        }

        public void Terminate()
        {
            // Unsubscribe change observer before unregistering
            SmartObjectChangeObserver.SmartObjectModified -= SmartObjectPaletteManager.Instance.NotifyObjectModified;

            SmartObjectChangeObserver.Unregister();
            SmartObjectGripOverrule.Unregister();

            SkeletonPropertyManager.Unregister();

            SmartObjectPaletteManager.Instance.Dispose();

            AcadApp.DocumentManager.DocumentCreated -= OnDocumentCreated;

            foreach (Document doc in AcadApp.DocumentManager)
            {
                UnsubscribeFromDocumentEvents(doc);
            }
        }

        private void OnDocumentCreated(object sender, DocumentCollectionEventArgs e)
        {
            if (e.Document != null)
            {
                SubscribeToDocumentEvents(e.Document);
                
                var collisionHandler = ServiceProvider.GetRequiredService<IDrawingOpenedHandler>();
                collisionHandler.HandleDocument(e.Document);
            }
        }

        private void SubscribeToDocumentEvents(Document doc)
        {
            AcadApp.DocumentManager.DocumentLockModeChanged += OnDocumentLockModeChanged;
        }

        private void UnsubscribeFromDocumentEvents(Document doc)
        {
            AcadApp.DocumentManager.DocumentLockModeChanged -= OnDocumentLockModeChanged;
        }


        private void OnDocumentLockModeChanged(object sender, DocumentLockModeChangedEventArgs e)
        {
            if (e.GlobalCommandName.ToUpper() == "REFEDIT")
            {
                var doc = e.Document;
                if (doc == null) return;

                var editor = doc.Editor;
                
                // Get pre-selected objects (implied selection)
                var selRes = editor.SelectImplied();
                if (selRes.Status == PromptStatus.OK && selRes.Value != null)
                {
                    var metadataStore = new DrawingObjectMetadataStore();
                    using (var transaction = doc.TransactionManager.StartTransaction()) // Lightweight transaction just for reading
                    {
                        var blockIds = selRes.Value.GetObjectIds();
                        foreach (var id in blockIds)
                        {
                            if (id.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(BlockReference))))
                            {
                                // Check if this BlockReference has our smart marker
                                using (var blkRef = (BlockReference)transaction.GetObject(id, OpenMode.ForRead))
                                {
                                    if (metadataStore.TryGetSchemaId(blkRef, transaction, out _))
                                    {
                                        editor.WriteMessage("\n[CabinetBilder] HIBA: Dinamikus CabinetBilder blokkok nem szerkeszthetĹ‘k a REFEDIT paranccsal, mert elveszĂ­tik a dinamikus tulajdonsĂˇgaikat. KĂ©rjĂĽk, hasznĂˇld a BEDIT parancsot vagy az okos palettĂˇt!\n");
                                        e.Veto();
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

