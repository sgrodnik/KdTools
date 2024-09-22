using System;
using System.Drawing;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using KdTools.BASE;
using static KdTools.Utils;

namespace KdTools.DisallowFramingJoin;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
class Command : IButtonCommand, IExternalCommand
{
    public string ButtonText => "Отмена\nсоединений";
    public string Title => "Отмена соединений";
    public string Tooltip => "";
    public Bitmap Logo { get; } = Properties.Resources.LogoDisallowFramingJoin;

    private static void Execute(ExternalCommandData commandData)
    {
        new Model(commandData).DoJob();
    }
    
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            LogStartCommand(Title, commandData, ++Properties.Settings.Default.CounterRunDisallowFramingJoin);
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
