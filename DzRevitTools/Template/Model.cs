using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

namespace DzRevitTools.Template;

public class Model
{
    private readonly UIApplication _uiApp;
    private readonly UIDocument _uiDoc;
    private readonly Document _doc;

    private int _resultCounter;
    private const double Threshold = 0.1;

    public Model(ExternalCommandData commandData)
    {
        _uiApp = commandData.Application;
        _uiDoc = _uiApp.ActiveUIDocument;
        _doc = _uiDoc.Document;
    }

    public static bool BoolFlagOne { get; set; }
    public static bool IsCeilingSelected { get; set; }
    public static string SomeStrVar { get; set; }
    public int AnIntegerCount { get; set; }
    public ElementId CeilingTypeId { get; set; }

    internal void DoJob()
    {
        var rooms = GetRooms();

        using var t = new Transaction(_doc);
        t.Start(new Command().Title);

        foreach (var room in rooms)
        {
            CreateCeiling(room as SpatialElement);
            _resultCounter++;
        }

        t.SetName($"{t.GetName()} ({_resultCounter})");
        t.Commit();
    }

    private IEnumerable<Element> GetRooms()
    {
        return new FilteredElementCollector(_doc)
            .WhereElementIsNotElementType()
            .OfCategory(BuiltInCategory.OST_Rooms)
            .Where(el => el is Room { Area: > Threshold });
    }

    private void CreateCeiling(SpatialElement room)
    {

    }
}
