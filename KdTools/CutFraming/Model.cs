using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace KdTools.CutFraming;

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
        var sel = Selection.GetElems(_uiApp, BuiltInCategory.OST_StructuralFraming).ToList();

        using var t = new Transaction(_doc);
        t.Start(new Command().Title);

        if (sel.Count == 1)
        {
            var solidToBeCut = sel[0];
            var ids = _uiDoc.Selection.PickObjects(ObjectType.Element, "Выберите элементы, которые будут вырезаны из целевого");
            var cuttingSolids = ids.Select(id => _doc.GetElement(id)).ToList();
            foreach (var cuttingSolid in cuttingSolids)
                Cut(solidToBeCut, cuttingSolid);
        }
        else
        {
            var solidsToBeCut = sel;
            var id = _uiDoc.Selection.PickObject(ObjectType.Element, "Выберите элемент, который будет вырезан из целевых");
            var cuttingSolid = _doc.GetElement(id);
            foreach (var solidToBeCut in solidsToBeCut)
                Cut(solidToBeCut, cuttingSolid);
        }

        t.SetName($"{t.GetName()} ({_resultCounter})");
        t.Commit();
    }

    private void Cut(Element solidToBeCut, Element cuttingSolid)
    {
        if (SolidSolidCutUtils.CanElementCutElement(solidToBeCut, cuttingSolid, out CutFailureReason _))
        {
            SolidSolidCutUtils.AddCutBetweenSolids(_doc, solidToBeCut, cuttingSolid);
            _resultCounter++;
        }
    }
}
