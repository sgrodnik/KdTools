using System;
using Autodesk.Revit.UI;
using DzRevitTools.BASE;
using static DzRevitTools.Utils;

namespace DzRevitTools;

public class App : IExternalApplication
{
    private void InitRibbon()
    {
        LogStartRibbon();

        var tabName = "DzRevitTools";
        tabName = Autodesk.Windows.ComponentManager.Ribbon.FindTab(tabName) is null
            ? tabName
            : tabName + "2";
        UicApp.CreateRibbonTab(tabName);

        var panelName = "Info";
        var panel = UicApp.CreateRibbonPanel(tabName, panelName);
        CreateButton(panel, new Template.Command());

        Log($"{tabName} ribbon loaded\n");
    }

    internal static UIControlledApplication UicApp;
    public Result OnStartup(UIControlledApplication application)
    {
        UicApp = application;
        try
        {
            InitRibbon();
        }
        catch (Exception e)
        {
            LogException(e);
            ShowException(e);
            return Result.Failed;
        }
        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        return Result.Succeeded;
    }

    private void CreateButton(object host, IButtonCommand command, string availabilityClassName = null)
    {
        var buttonData = CreatePushButtonData(command);
        if (availabilityClassName is not null)
            buttonData.AvailabilityClassName = availabilityClassName;

        PushButton pushButton = null;
        if (host is RibbonPanel panel)
            pushButton = panel.AddItem(buttonData) as PushButton;
        else if (host is SplitButton splitButton)
            pushButton = splitButton.AddPushButton(buttonData);
        TuneButton(command, pushButton);
    }

    private static PushButtonData CreatePushButtonData(IButtonCommand command)
    {
        return new PushButtonData(command.GetType().FullName,
            command.ButtonText,
            command.GetType().Assembly.Location,
            command.GetType().FullName
        );
    }

    private void TuneButton(IButtonCommand command, RibbonButton pushButton)
    {
        pushButton!.LargeImage = GetImageSource(command.Logo, 32);
        pushButton.Image = GetImageSource(command.Logo, 16);
        pushButton.ToolTip = command.Tooltip;
        if (command is not IButtonWithNativeImage image) return;
        var item = Autodesk.Windows.ComponentManager.Ribbon.FindItem(
            image.NativeItemId, false);
        if (item is null) return;
        pushButton.Image = item.Image;
        pushButton.LargeImage = item.LargeImage;
    }
}
