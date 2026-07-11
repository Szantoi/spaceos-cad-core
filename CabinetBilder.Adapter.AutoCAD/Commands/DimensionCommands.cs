using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using CabinetBilder.Core.Common;
using CabinetBilder.Adapter.AutoCAD.Application.UseCases;
using CabinetBilder.Core.FrontMatter;
using CabinetBilder.Core.SmartObjects;
using CabinetBilder.Adapter.AutoCAD.Infrastructure.ObjectMetadata;
using CabinetBilder.Adapter.AutoCAD.Infrastructure.TypeStore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Globalization;
using System.Text;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CabinetBilder.Adapter.AutoCAD.Commands;

/// <summary>
/// Provides AutoCAD command methods for synchronizing dimensions and front matter text.
/// </summary>
public class DimensionCommands
{
    private const string LengthKeyword = "Length";
    private const string WidthKeyword = "Width";
    private const string ThicknessKeyword = "Thickness";
    private const string BlockIdKey = "Block_Id";
    private const string TypeKey = "Type";
    private const string NameKey = "Name";
    private const string MaterialKey = "Material";
    private const string QuantityKey = "Quantity";
    private const string LengthCutKey = "Length_cut";
    private const string WidthCutKey = "Width_cut";
    private const string LayerKey = "Layer";
    private const string DefaultTypeValue = "SzabĂˇszat";
    private static string lastSelectedParameterKeyword = LengthKeyword;
    private static readonly IDrawingTypeStore drawingTypeStore = new DrawingTypeStore();
    private static readonly IDrawingObjectMetadataStore drawingObjectMetadataStore = new DrawingObjectMetadataStore();
    private static readonly CreateFrontMatterFromBlockUseCase createFrontMatterFromBlockUseCase = new(
        NullLogger<CreateFrontMatterFromBlockUseCase>.Instance);

    /// <summary>
    /// Synchronizes a selected dimension measurement with a dynamic block distance property.
    /// </summary>
    /// <remarks>
    /// Command name in AutoCAD: <c>SyncDimToBlock</c>.
    /// </remarks>
    [CommandMethod("SyncDimToBlock")]
    public void SyncDimToBlock()
    {
        Document? doc = AcadApplication.DocumentManager.MdiActiveDocument;
        if (doc is null)
        {
            return;
        }

        Database db = doc.Database;
        Editor ed = doc.Editor;

        string? selectedParameterKeyword = PromptParameterKeyword(ed);
        if (selectedParameterKeyword is null)
        {
            return;
        }

        PromptEntityOptions dimSelectionOptions = new("\nVĂˇlassz egy mĂ©retvonalat: ");
        dimSelectionOptions.SetRejectMessage("\nCsak mĂ©retvonal vĂˇlaszthatĂł!");
        dimSelectionOptions.AddAllowedClass(typeof(Dimension), exactMatch: false);
        PromptEntityResult dimSelection = ed.GetEntity(dimSelectionOptions);
        if (dimSelection.Status != PromptStatus.OK)
        {
            return;
        }

        PromptEntityOptions blockSelectionOptions = new("\nVĂˇlassz egy dinamikus blokkot: ");
        blockSelectionOptions.SetRejectMessage("\nCsak blokkreferencia vĂˇlaszthatĂł!");
        blockSelectionOptions.AddAllowedClass(typeof(BlockReference), exactMatch: false);
        PromptEntityResult blockSelection = ed.GetEntity(blockSelectionOptions);
        if (blockSelection.Status != PromptStatus.OK)
        {
            return;
        }

        using Transaction tr = db.TransactionManager.StartTransaction();

        Dimension? dim = tr.GetObject(dimSelection.ObjectId, OpenMode.ForRead) as Dimension;
        BlockReference? blockReference = tr.GetObject(blockSelection.ObjectId, OpenMode.ForWrite) as BlockReference;

        if (dim is null || blockReference is null)
        {
            ed.WriteMessage("\nA kivĂˇlasztott objektumok nem feldolgozhatĂłk.");
            return;
        }

        if (!blockReference.IsDynamicBlock)
        {
            ed.WriteMessage("\nA kivĂˇlasztott blokk nem dinamikus blokk.");
            return;
        }

        int drawingPrecision = GetDrawingLinearPrecision();
        double measurement = dim.Measurement;
        bool propertyUpdated = false;

        foreach (DynamicBlockReferenceProperty property in blockReference.DynamicBlockReferencePropertyCollection)
        {
            if (!MatchesSelectedParameter(property.PropertyName, selectedParameterKeyword))
            {
                continue;
            }

            if (property.ReadOnly)
            {
                ed.WriteMessage($"\nThe '{property.PropertyName}' property is read-only.");
                break;
            }

            property.Value = measurement;
            propertyUpdated = true;
            ed.WriteMessage($"\nUpdated '{property.PropertyName}' to {FormatNumeric(measurement, drawingPrecision)}.");
            break;
        }

        if (!propertyUpdated)
        {
            ed.WriteMessage($"\nNo matching dynamic property was found for '{selectedParameterKeyword}'.");
            return;
        }

        tr.Commit();
    }

    /// <summary>
    /// Fills a text box front matter template with values from a selected dynamic block.
    /// </summary>
    /// <remarks>
    /// Command name in AutoCAD: <c>SyncBlockToFrontMatter</c>.
    /// </remarks>
    [CommandMethod("SyncBlockToFrontMatter")]
    public void SyncBlockToFrontMatter()
    {
        Document? doc = AcadApplication.DocumentManager.MdiActiveDocument;
        if (doc is null)
        {
            return;
        }

        Database db = doc.Database;
        Editor ed = doc.Editor;

        PromptEntityOptions blockSelectionOptions = new("\nSelect a dynamic block: ");
        blockSelectionOptions.SetRejectMessage("\nOnly block references are allowed!");
        blockSelectionOptions.AddAllowedClass(typeof(BlockReference), exactMatch: false);
        PromptEntityResult blockSelection = ed.GetEntity(blockSelectionOptions);
        if (blockSelection.Status != PromptStatus.OK)
        {
            return;
        }

        using Transaction tr = db.TransactionManager.StartTransaction();

        BlockReference? blockReference = tr.GetObject(blockSelection.ObjectId, OpenMode.ForRead) as BlockReference;
        if (blockReference is null)
        {
            ed.WriteMessage("\nThe selected block reference could not be read.");
            return;
        }

        if (!blockReference.IsDynamicBlock)
        {
            ed.WriteMessage("\nThe selected block is not dynamic.");
            return;
        }

        BlockTableRecord? space = tr.GetObject(blockReference.OwnerId, OpenMode.ForWrite) as BlockTableRecord;
        if (space is null)
        {
            ed.WriteMessage("\nThe target space could not be read.");
            return;
        }

        string? selectedType = PromptFrontMatterType(ed, db);
        if (selectedType is null)
        {
            return;
        }

        int drawingPrecision = GetDrawingLinearPrecision();
        Point3d defaultInsertionPoint = GetFrontMatterInsertionPoint(blockReference);

        PromptPointOptions insertionPointOptions = new("\nSpecify front matter insertion point or Enter for default: ");
        insertionPointOptions.AllowNone = true;
        insertionPointOptions.BasePoint = defaultInsertionPoint;
        insertionPointOptions.UseBasePoint = true;

        PromptPointResult insertionPointResult = ed.GetPoint(insertionPointOptions);
        if (insertionPointResult.Status == PromptStatus.Cancel)
        {
            return;
        }

        Point3d insertionPoint = insertionPointResult.Status == PromptStatus.None
            ? defaultInsertionPoint
            : insertionPointResult.Value;

        Dictionary<string, object> dynamicProperties = new(StringComparer.OrdinalIgnoreCase);
        foreach (DynamicBlockReferenceProperty property in blockReference.DynamicBlockReferencePropertyCollection)
        {
            dynamicProperties[property.PropertyName] = property.Value;
        }

        ObjectId blockTableRecordId = blockReference.IsDynamicBlock
            ? blockReference.DynamicBlockTableRecord
            : blockReference.BlockTableRecord;
        
        BlockTableRecord? blockTableRecord = tr.GetObject(blockTableRecordId, OpenMode.ForRead) as BlockTableRecord;
        string blockName = blockTableRecord?.Name ?? string.Empty;

        CreateFrontMatterFromBlockRequest request = new(
            blockReference.Handle.ToString(),
            blockName,
            dynamicProperties,
            drawingPrecision,
            selectedType);

        Result<CreateFrontMatterFromBlockResult> useCaseResult = createFrontMatterFromBlockUseCase.Execute(request);

        if (useCaseResult.IsFailure)
        {
            ed.WriteMessage($"\nError: {useCaseResult.ErrorMessage}");
            return;
        }

        MText frontMatterBox = new()
        {
            Location = insertionPoint,
            Width = 180.0, // Default width
            Attachment = AttachmentPoint.TopLeft,
            Contents = useCaseResult.Value.FrontMatterText.Replace("\n", "\\P", StringComparison.Ordinal)
        };

        space.AppendEntity(frontMatterBox);
        tr.AddNewlyCreatedDBObject(frontMatterBox, true);
        drawingObjectMetadataStore.MarkAsSmartObject(frontMatterBox, tr);

        tr.Commit();
        drawingTypeStore.AddType(db, selectedType);

        ed.WriteMessage("\nFront matter text box created from the selected block.");
    }

    /// <summary>
    /// Reads a front matter text box and creates an AutoCAD table from its fields.
    /// </summary>
    /// <remarks>
    /// Command name in AutoCAD: <c>FrontMatterToTable</c>.
    /// </remarks>
    [CommandMethod("FrontMatterToTable")]
    public void FrontMatterToTable()
    {
        Document? doc = AcadApplication.DocumentManager.MdiActiveDocument;
        if (doc is null)
        {
            return;
        }

        Database db = doc.Database;
        Editor ed = doc.Editor;

        string? requiredType = PromptFrontMatterType(ed, db, allowCreateNewType: false, promptLabel: "Select Type filter");
        if (requiredType is null)
        {
            return;
        }

        PromptStringOptions layerFilterOptions = new("\nLayer filter (optional, Enter = any): ");
        layerFilterOptions.AllowSpaces = true;
        PromptResult layerFilterResult = ed.GetString(layerFilterOptions);
        if (layerFilterResult.Status != PromptStatus.OK)
        {
            return;
        }

        string requiredLayer = layerFilterResult.StringResult.Trim();

        PromptPointOptions pointOptions = new("\nSpecify table insertion point: ");
        PromptPointResult pointResult = ed.GetPoint(pointOptions);
        if (pointResult.Status != PromptStatus.OK)
        {
            return;
        }

        using Transaction tr = db.TransactionManager.StartTransaction();

        List<Dictionary<string, string>> rows = new();
        BlockTableRecord? currentSpace = tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead) as BlockTableRecord;
        if (currentSpace is null)
        {
            ed.WriteMessage("\nThe current space could not be opened.");
            return;
        }

        foreach (ObjectId objectId in currentSpace)
        {
            Entity? textEntity = tr.GetObject(objectId, OpenMode.ForRead) as Entity;
            if (textEntity is null || textEntity is not MText && textEntity is not DBText)
            {
                continue;
            }

            bool hasSchemaMarker = drawingObjectMetadataStore.TryGetSchemaId(textEntity, tr, out string? schemaId);
            if (!SmartObjectSchemaFilter.ShouldInclude(hasSchemaMarker, schemaId))
            {
                continue;
            }

            string sourceText = ReadTextContent(textEntity);
            Dictionary<string, string> entries = ParseFrontMatterEntries(sourceText);
            if (entries.Count == 0)
            {
                continue;
            }

            if (!entries.TryGetValue(FrontMatterKeys.Type, out string? rowType) || !string.Equals(rowType, requiredType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(requiredLayer) && !string.Equals(textEntity.Layer, requiredLayer, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            entries[FrontMatterKeys.Layer] = textEntity.Layer;
            rows.Add(entries);
        }

        if (rows.Count == 0)
        {
            ed.WriteMessage("\nNo matching front matter text boxes were found for the selected Type/Layer filters.");
            return;
        }

        List<string> columns = BuildColumns(rows);

        BlockTableRecord? space = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
        if (space is null)
        {
            ed.WriteMessage("\nThe current space could not be opened.");
            return;
        }

        Table table = new();
        table.TableStyle = db.Tablestyle;
        table.Position = pointResult.Value;
        table.SetSize(rows.Count + 2, columns.Count);

        string title = string.IsNullOrWhiteSpace(requiredLayer)
            ? requiredType
            : requiredType + " | Layer: " + requiredLayer;

        table.Cells[0, 0].TextString = title;
        if (columns.Count > 1)
        {
            table.MergeCells(CellRange.Create(table, 0, 0, 0, columns.Count - 1));
        }

        for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
        {
            table.Cells[1, columnIndex].TextString = columns[columnIndex];
        }

        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            IReadOnlyDictionary<string, string> row = rows[rowIndex];

            for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                string columnName = columns[columnIndex];
                row.TryGetValue(columnName, out string? cellValue);
                table.Cells[rowIndex + 2, columnIndex].TextString = cellValue ?? string.Empty;
            }
        }

        space.AppendEntity(table);
        tr.AddNewlyCreatedDBObject(table, true);
        tr.Commit();

        ed.WriteMessage("\nFront matter table created from the selected text box.");
    }

    private static string? PromptParameterKeyword(Editor editor)
    {
        PromptKeywordOptions keywordOptions = new($"\nSelect target parameter [Length/Width/Thickness] <{lastSelectedParameterKeyword}>: ");
        keywordOptions.AllowNone = true;
        keywordOptions.Keywords.Add(LengthKeyword);
        keywordOptions.Keywords.Add(WidthKeyword);
        keywordOptions.Keywords.Add(ThicknessKeyword);
        keywordOptions.Keywords.Default = lastSelectedParameterKeyword;

        PromptResult keywordResult = editor.GetKeywords(keywordOptions);
        if (keywordResult.Status == PromptStatus.Cancel)
        {
            return null;
        }

        string selectedKeyword = keywordResult.Status == PromptStatus.None || string.IsNullOrWhiteSpace(keywordResult.StringResult)
            ? lastSelectedParameterKeyword
            : keywordResult.StringResult;

        lastSelectedParameterKeyword = selectedKeyword;
        return selectedKeyword;
    }

    private static bool MatchesSelectedParameter(string propertyName, string selectedKeyword)
    {
        if (string.Equals(selectedKeyword, LengthKeyword, StringComparison.OrdinalIgnoreCase))
        {
            return EqualsAny(propertyName, "Length", "HosszĂşsĂˇg", "Tavolsag", "TĂˇvolsĂˇg");
        }

        if (string.Equals(selectedKeyword, WidthKeyword, StringComparison.OrdinalIgnoreCase))
        {
            return EqualsAny(propertyName, "Width", "SzĂ©lessĂ©g", "Szelesseg");
        }

        if (string.Equals(selectedKeyword, ThicknessKeyword, StringComparison.OrdinalIgnoreCase))
        {
            return EqualsAny(propertyName, "Thickness", "VastagsĂˇg", "Vastagsag");
        }

        return false;
    }

    private static bool EqualsAny(string value, params string[] candidates)
    {
        foreach (string candidate in candidates)
        {
            if (string.Equals(value, candidate, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static Point3d GetFrontMatterInsertionPoint(BlockReference blockReference)
    {
        try
        {
            Extents3d extents = blockReference.GeometricExtents;
            double offsetX = Math.Max(extents.MaxPoint.X - extents.MinPoint.X, 100.0) + 20.0;
            double offsetY = 20.0;
            return new Point3d(extents.MaxPoint.X + offsetX, extents.MaxPoint.Y + offsetY, extents.MaxPoint.Z);
        }
        catch
        {
            return new Point3d(blockReference.Position.X + 150.0, blockReference.Position.Y + 20.0, blockReference.Position.Z);
        }
    }

    private static Dictionary<string, string> ParseFrontMatterEntries(string sourceText)
    {
        return FrontMatterTextService.ParseEntries(sourceText);
    }

    private static List<string> BuildColumns(IEnumerable<IReadOnlyDictionary<string, string>> rows)
    {
        return FrontMatterTextService.BuildColumns(rows);
    }

    private static int GetDrawingLinearPrecision()
    {
        try
        {
            object precisionValue = AcadApplication.GetSystemVariable("LUPREC");
            int precision = Convert.ToInt32(precisionValue, CultureInfo.InvariantCulture);
            return Math.Clamp(precision, 0, 8);
        }
        catch
        {
            return 4;
        }
    }

    private static string FormatDynamicValue(object? value, int precision)
    {
        return FrontMatterTextService.FormatDynamicValue(value, precision);
    }

    private static string FormatNumeric(double value, int precision)
    {
        return FrontMatterTextService.FormatNumeric(value, precision);
    }

    private static string? PromptFrontMatterType(Editor editor, Database db)
    {
        return PromptFrontMatterType(editor, db, allowCreateNewType: true, promptLabel: "Select Type");
    }

    private static string? PromptFrontMatterType(Editor editor, Database db, bool allowCreateNewType, string promptLabel)
    {
        bool forceRefresh = false;

        while (true)
        {
            List<string> knownTypes = drawingTypeStore.GetKnownTypes(db, forceRefresh);
            forceRefresh = false;

            if (knownTypes.Count == 0)
            {
                knownTypes.Add(DefaultTypeValue);
            }

            StringBuilder listMessage = new();
            listMessage.AppendLine();
            listMessage.AppendLine("Available Type values:");
            for (int index = 0; index < knownTypes.Count; index++)
            {
                listMessage.AppendLine($"  {index + 1}. {knownTypes[index]}");
            }
            editor.WriteMessage(listMessage.ToString());

            string optionsMessage = allowCreateNewType
                ? $"\n{promptLabel} (index or name), [N]ew, [R]efresh <1>: "
                : $"\n{promptLabel} (index or name), [R]efresh <1>: ";

            PromptStringOptions options = new(optionsMessage);
            options.AllowSpaces = true;
            PromptResult result = editor.GetString(options);
            if (result.Status == PromptStatus.Cancel)
            {
                return null;
            }

            string input = result.Status == PromptStatus.None
                ? string.Empty
                : result.StringResult;

            FrontMatterTypeInputParseResult parseResult = FrontMatterTypeInputParser.Parse(input, knownTypes, allowCreateNewType);

            if (parseResult.Action == FrontMatterTypeInputAction.Refresh)
            {
                forceRefresh = true;
                continue;
            }

            if (parseResult.Action == FrontMatterTypeInputAction.CreateNew)
            {
                PromptStringOptions newTypeOptions = new("\nEnter new Type value: ");
                newTypeOptions.AllowSpaces = true;
                PromptResult newTypeResult = editor.GetString(newTypeOptions);
                if (newTypeResult.Status != PromptStatus.OK)
                {
                    return null;
                }

                string newType = newTypeResult.StringResult.Trim();
                if (string.IsNullOrWhiteSpace(newType))
                {
                    editor.WriteMessage("\nType value cannot be empty.");
                    continue;
                }

                drawingTypeStore.AddType(db, newType);
                return newType;
            }

            if (parseResult.Action == FrontMatterTypeInputAction.SelectExisting)
            {
                return parseResult.SelectedType;
            }

            if (parseResult.Action == FrontMatterTypeInputAction.Invalid)
            {
                editor.WriteMessage($"\n{parseResult.Message}");
                continue;
            }
        }
    }

    private static string ReadTextContent(Entity textEntity)
    {
        if (textEntity is MText mText)
        {
            return NormalizeLineBreaks(mText.Contents.Replace("\\P", "\n", StringComparison.Ordinal));
        }

        if (textEntity is DBText dbText)
        {
            return NormalizeLineBreaks(dbText.TextString);
        }

        return string.Empty;
    }

    private static void WriteTextContent(Entity textEntity, string updatedText)
    {
        if (textEntity is MText mText)
        {
            mText.Contents = updatedText.Replace("\n", "\\P", StringComparison.Ordinal);
            return;
        }

        if (textEntity is DBText dbText)
        {
            dbText.TextString = updatedText;
        }
    }

    private static string NormalizeLineBreaks(string value)
    {
        return FrontMatterTextService.NormalizeLineBreaks(value);
    }
}
