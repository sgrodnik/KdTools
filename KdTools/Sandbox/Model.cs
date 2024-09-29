using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

namespace KdTools.Sandbox;

public class Model
{
    private readonly UIApplication _uiApp;
    private readonly UIDocument _uiDoc;
    private readonly Document _doc;

    public Model(ExternalCommandData commandData)
    {
        _uiApp = commandData.Application;
        _uiDoc = _uiApp.ActiveUIDocument;
        _doc = _uiDoc.Document;
    }

    internal void DoJob()
    {
        // var rooms = GetRooms();

        // using var t = new Transaction(_doc);
        // t.Start(new Command().Title);

        // foreach (var room in rooms)
        // {
        //     CreateCeiling(room as SpatialElement);
        //     _resultCounter++;
        // }

        // t.SetName($"{t.GetName()} ({_resultCounter})");
        // t.Commit();
    }
}
