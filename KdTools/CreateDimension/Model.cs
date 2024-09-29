using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

namespace KdTools.CreateDimension;

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

    internal void DoJob()
    {
        // var rooms = GetRooms();

        using var t = new Transaction(_doc);
        t.Start(new Command().Title);

        var sel = Selection.GetElems(_uiApp, BuiltInCategory.OST_StructuralFraming).ToList();

        var referenceArray = new ReferenceArray();
        foreach (var el in sel)
        {
            var familyInstance = el as FamilyInstance;
            var curRef = GetFamilyReference(familyInstance);
            referenceArray.Append(curRef);
        }
        Line dimLine = Line.CreateBound(new XYZ(0, 0, 0), new XYZ(0, 1, 0));
        Dimension newDim = _doc.Create.NewDimension(_doc.ActiveView, dimLine, referenceArray);

        t.SetName($"{t.GetName()} ({_resultCounter})");
        t.Commit();
    }

    private Reference GetFamilyReference(FamilyInstance inst)
    {
        // source for this method: https://thebuildingcoder.typepad.com/blog/2016/04/stable-reference-string-magic-voodoo.html
        var dbDoc = inst.Document;
        var geomOptions = new Options();
        geomOptions.ComputeReferences = true;
        geomOptions.DetailLevel = ViewDetailLevel.Undefined;
        geomOptions.IncludeNonVisibleObjects = true;
        var geomElement = inst.get_Geometry(geomOptions);
        //System.Windows.MessageBox.Show($"geomElement {geomElement}");
        //foreach (var item in geomElement)
        //{
        //    Utils.Utils.Log($"item {item}");
        //}
        var geomInstance = geomElement.First(i => i is GeometryInstance) as GeometryInstance ??
            throw new UserException($"No GeometryInstance found for {inst.Id}");
        var solid = geomInstance.GetInstanceGeometry().First(s => (s as Solid).Volume > 0) as Solid;
        foreach (Face face in solid.Faces)
        {
            if (face is not PlanarFace planarFace) continue;
            var isValidDirection = planarFace.FaceNormal.IsAlmostEqualTo(_doc.ActiveView.RightDirection);
            // Utils.Utils.Log($"FaceNormal {planarFace.FaceNormal} {planarFace.Area} {isValidDirection}");
            if (isValidDirection)
                return face.Reference;
        }

        var sampleStableRef = GetSampleStableRef(dbDoc, geomInstance.GetInstanceGeometry());
        // Utils.Utils.Log($"sampleStableRef {sampleStableRef}");
        var customStableRef = Tune(sampleStableRef);
        // Utils.Utils.Log($"customStableRef {customStableRef}");
        return Reference.ParseFromStableRepresentation(dbDoc, customStableRef);

        string Tune(string sampleStableRef)
        {
            var parts = sampleStableRef.Split(':');
            //for (int i = 0; i < 20; i++)
            //{
            //    var customStableRef = string.Join(":", parts[0], parts[1], parts[2], parts[3], $"{i}");
            //    var indexRef = Reference.ParseFromStableRepresentation(dbDoc, customStableRef);
            //    Utils.Utils.Log($"{i} {inst.GetGeometryObjectFromReference(indexRef)}");
            //}

            //var customStableRef = string.Join(":", parts[0], parts[1], parts[2], parts[3], $"{(int)refType}");
            var customStableRef = string.Join(":", parts[0], parts[1], parts[2], parts[3], $"{4}");
            var indexRef = Reference.ParseFromStableRepresentation(dbDoc, customStableRef);
            var geomObj = inst.GetGeometryObjectFromReference(indexRef) ??
                throw new ArgumentNullException("geomObj");
            if (geomObj is Edge)
                customStableRef += ":LINEAR";
            else if (geomObj is Face)
                customStableRef += ":SURFACE";
            return customStableRef;
        }
}


    private string GetSampleStableRef(Document doc, GeometryElement symbolGeometry)
    {
        foreach (var geomObject in symbolGeometry)
        {

            if (geomObject is Solid solid && solid.Faces.Size > 0)
            {
                //Utils.Utils.Log($"Solid {solid.Faces.get_Item(0).Reference.ConvertToStableRepresentation(doc)}");
                //continue;
                return solid.Faces.get_Item(0).Reference.ConvertToStableRepresentation(doc);
            }
            if (geomObject is Curve curve && curve.Reference is Reference curveRef)
            {
                //Utils.Utils.Log($"Curve {curveRef.ConvertToStableRepresentation(doc)}");
                //continue;
                return curveRef.ConvertToStableRepresentation(doc);
            }
            if (geomObject is Point point)
            {
                //Utils.Utils.Log($"Point {point.Reference.ConvertToStableRepresentation(doc)}");
                //continue;
                return point.Reference.ConvertToStableRepresentation(doc);
            }
            // Utils.Utils.Log($"111 {geomObject}");
        }
        return null;
    }
}
