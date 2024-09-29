using System;
using System.Drawing;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using KdTools.BASE;
using static KdTools.Utils;

namespace KdTools.Sandbox;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
class Command : IButtonCommand, IExternalCommand
{
    public string ButtonText => "Sandbox";
    public string Title => "Sandbox";
    public string Tooltip => "";
    public Bitmap Logo { get; } = Properties.Resources.LogoTemplate;

    private static void Execute(ExternalCommandData commandData)
    {
        new Model(commandData).DoJob();
    }
    
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            LogStartCommand(Title, commandData, 0);
            Execute(commandData);
            LogEndCommand(Title);
        }
        catch (Exception e)
        {
            LogException(e);
            ShowException(e);
        }
        return Result.Succeeded;
    }
}
