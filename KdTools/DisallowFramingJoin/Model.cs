using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;

namespace KdTools.DisallowFramingJoin;

public class Model
{
    private readonly UIApplication _uiApp;
    private readonly UIDocument _uiDoc;
    private readonly Document _doc;

    private int _resultCounter;

    public Model(ExternalCommandData commandData)
    {
        _uiApp = commandData.Application;
        _uiDoc = _uiApp.ActiveUIDocument;
        _doc = _uiDoc.Document;
    }

    internal void DoJob()
    {
        var sel = Selection.GetElems(_uiApp, BuiltInCategory.OST_StructuralFraming);
        if (!sel.Any())
            sel = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .WhereElementIsNotElementType()
                .ToElements();

        using var t = new Transaction(_doc);
        t.Start(new Command().Title);

        foreach (FamilyInstance el in sel)
        {
            if (StructuralFramingUtils.IsJoinAllowedAtEnd(el, 0) ||
                StructuralFramingUtils.IsJoinAllowedAtEnd(el, 1))
                _resultCounter++;
            StructuralFramingUtils.DisallowJoinAtEnd(el, 0);
            StructuralFramingUtils.DisallowJoinAtEnd(el, 1);
        }

        t.SetName($"{t.GetName()} ({_resultCounter})");
        t.Commit();
    }
}
