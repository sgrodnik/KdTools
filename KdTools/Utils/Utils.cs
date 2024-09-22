using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using static KdTools.Properties.Settings;

namespace KdTools;

public static class Utils
{
    internal static string DayLogPath;
    private static readonly string Appdata =
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    private static readonly string LogDir = Path.Combine(Appdata, "KdTools", "Logs");
    private static readonly string PathCommon = Path.Combine(LogDir, "KdTools.log");

    internal static void Log(string s, bool newLineAndTime = true)
    {
        var now = DateTime.Now;
        var monthDir = Path.Combine(LogDir, $"{now:yyyy-MM}");
        DayLogPath = Path.Combine(monthDir, $"{now:dd}.log");
        Directory.CreateDirectory(monthDir);
        var prefix = newLineAndTime ? $"\n{now:HH:mm:ss} " : "";
        File.AppendAllText(DayLogPath, $"{prefix}{s}");

        if (Environment.UserName != "sg22") return;
        File.AppendAllText(PathCommon, $"{prefix}{s}");
    }

    internal static void LogStartRibbon()
    {
        var pluginVersion = $"v{Assembly.GetExecutingAssembly().GetName().Version}";
        var pluginVersionAndBuildDate = $"v{Assembly.GetExecutingAssembly().GetName().Version} ({GetAssemblyBuildVersion()})";
        var runCounterAndPid = $"{++Default.CounterRunRibbon}:pid{Process.GetCurrentProcess().Id}";
        Default.Save();
        var clientId = $"{Environment.UserName}";
        var hardware = GetHardwareInfo();
        var revit = App.UicApp.ControlledApplication;
        var info = new List<string>
        {
            runCounterAndPid,
            pluginVersion,
            clientId,
            Environment.MachineName,
            Environment.UserDomainName,
            Environment.UserName,
            pluginVersionAndBuildDate,
            revit.VersionName,
            revit.VersionBuild,
            hardware,
            "OSv" + Environment.OSVersion.Version,
        };
        Log(string.Join(" - ", info));
    }

    private static DateTime buildDate = default;
    internal static string GetAssemblyBuildVersion()
    {
        if (buildDate != default)
            return buildDate.ToString();
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        buildDate = new DateTime(2000, 1, 1).AddDays(version.Build).AddSeconds(version.Revision * 2);
        return buildDate.ToString();
    }

    internal static void LogStartCommand(string title, ExternalCommandData commandData, long counterRun)
    {
        _watch = Stopwatch.StartNew();
        var common = ++Default.CounterRunCommon;
        var ribbon = Default.CounterRunRibbon;
        Default.Save();
        var fileSize = GetProjectSize(commandData);
        var pid = "pid" + Process.GetCurrentProcess().Id;
        var memoryInfo = GetMemoryInfo();
        var pluginVersionAndBuildDate = $"v{Assembly.GetExecutingAssembly().GetName().Version} ({GetAssemblyBuildVersion()})";
        var doc = commandData.Application.ActiveUIDocument.Document;
        var docPath = doc.PathName;
        Log($"{title} Start\t{common}:{counterRun}:{ribbon}:{pid}\t{docPath}\t{fileSize}\t{memoryInfo}\t{pluginVersionAndBuildDate}");
    }

    private static Stopwatch _watch = Stopwatch.StartNew();
    internal static void LogEndCommand(string title)
    {
        var duration = $"{RoundTimeSpan(_watch.Elapsed)}".TrimEnd('0');
        Log($"{title} End, duration: {duration}\n");
    }

    private static TimeSpan RoundTimeSpan(TimeSpan span, int precision = 2, int timespanSize = 7)
    {
        var factor = (int)Math.Pow(10, timespanSize - precision);
        return new TimeSpan((long)Math.Round(1.0 * span.Ticks / factor) * factor);
    }

    private static string GetProjectSize(ExternalCommandData commandData)
    {
        var doc = commandData.Application.ActiveUIDocument.Document;
        var docPath = doc.PathName;
        try
        {
            long fileSize = 0;
            if (docPath.StartsWith("RSN://"))
            {
                var server = string.Empty;
                var subfolder = string.Empty;

                var splits = docPath.Split('/');
                for (var i = 2; i < splits.Length; i++)
                {
                    if (i == 2) server = splits[i];
                    else subfolder = subfolder + splits[i] + (i == splits.Length - 1 ? string.Empty : "|");
                }

                var clientPath = "http://" + server;
                var requestPath = "RevitServerAdminRestService" + "/" + "AdminRestService.svc/" +
                                  subfolder + "/modelinfo";

                fileSize = GetFileInfoFromRevitServer(clientPath, requestPath);
            }

            else if (docPath.StartsWith("BIM 360://"))
            {
                var fileName = doc.WorksharingCentralGUID + ".rvt";
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var collaborationDir = Path.Combine(localAppData,
                    "Autodesk\\Revit\\Autodesk Revit " + doc.Application.VersionNumber, "CollaborationCache");

                var file = Directory.GetFiles(collaborationDir, fileName, SearchOption.AllDirectories)
                    .FirstOrDefault(x => new FileInfo(x).Directory?.Name != "CentralCache");
                if (file == null) return BytesToString(fileSize);

                var fileInfo = new FileInfo(file);
                fileSize = fileInfo.Length;
            }

            else
            {
                var fileInfo = new FileInfo(docPath);
                fileSize = fileInfo.Length;
            }
            return BytesToString(fileSize);
        }
        catch
        {
            return string.IsNullOrWhiteSpace(docPath) ? "(No path)" : $"{docPath.Substring(0, 3)}...";
        }
    }

    private static int GetFileInfoFromRevitServer(string clientPath, string requestPath)
    {
        var size = 0;

        // var client = new RestClient(clientPath);
        // var request = new RestRequest(requestPath, Method.GET);
        // request.AddHeader("User-Name", Environment.UserName);
        // request.AddHeader("User-Machine-Name", Environment.UserName + "PC");
        // request.AddHeader("Operation-GUID", Guid.NewGuid().ToString());
        //
        // var response = client.Execute<RsFileInfo>(request);
        // if (response.Data != null)
        // {
        //     size = response.Data.ModelSize;
        // }

        return size;
    }

    private static string BytesToString(long byteCount)
    {
        string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
        if (byteCount == 0)
            return "0" + suf[0];
        var bytes = Math.Abs(byteCount);
        var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
        var num = Math.Round(bytes / Math.Pow(1024, place), 0);
        return Math.Sign(byteCount) * num + suf[place];
    }

    private static string GetHardwareInfo()
    {
        var info = "";
        var processorSearcher = new ManagementObjectSearcher("select * from Win32_Processor");
        foreach (ManagementObject obj in processorSearcher.Get())
        {
            info += $"{obj["Name"]}";
        }
        var memorySearcher = new ManagementObjectSearcher("select * from Win32_OperatingSystem");
        foreach (ManagementObject obj in memorySearcher.Get())
        {
            var totalMemoryGb = Convert.ToInt64(obj["TotalVisibleMemorySize"]) / 1024 / 1024;
            var memoryUsage = Convert.ToInt64(obj["FreePhysicalMemory"]) * 100 / Convert.ToInt64(obj["TotalVisibleMemorySize"]);
            var memoryUsed = Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024;
            info += $" RAM{totalMemoryGb}GB{memoryUsage}%(used{memoryUsed}MB)";
        }
        var windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var systemDrive = DriveInfo.GetDrives().FirstOrDefault(drive => windowsDirectory.StartsWith(drive.RootDirectory.FullName));
        if (systemDrive != null)
        {
            var letter = systemDrive.Name;
            var total = systemDrive.TotalSize / 1024 / 1024 / 1024;
            var free = systemDrive.AvailableFreeSpace / 1024 / 1024 / 1024;
            info += $" Drive {letter} {total}GB {free}GB";
        }
        var videoSearcher = new ManagementObjectSearcher("select * from Win32_VideoController");
        foreach (ManagementObject obj in videoSearcher.Get())
        {
            var memory = Convert.ToInt64(obj["AdapterRAM"]) / 1024 / 1024 / 1024;
            info += $" {obj["Name"]} {memory}GB";
        }
        return info;
    }

    internal static string GetMemoryInfo()
    {
        var info = "";
        var memorySearcher = new ManagementObjectSearcher("select * from Win32_OperatingSystem");
        foreach (ManagementObject obj in memorySearcher.Get())
        {
            var totalMemoryGb = Convert.ToInt64(obj["TotalVisibleMemorySize"]) / 1024 / 1024;
            var memoryUsage = Convert.ToInt64(obj["FreePhysicalMemory"]) * 100 / Convert.ToInt64(obj["TotalVisibleMemorySize"]);
            var memoryUsed = Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024;
            info += $"RAM{totalMemoryGb}GB{memoryUsage}%(used{memoryUsed}MB)";
        }
        return info;
    }

    internal static ImageSource GetImageSource(Bitmap source, int size)
    {
        return Imaging.CreateBitmapSourceFromHBitmap(
            source.GetHbitmap(),
            IntPtr.Zero,
            Int32Rect.Empty,
            BitmapSizeOptions.FromWidthAndHeight(size, size)
        );
    }

    internal static void LogException(Exception e)
    {
        Log($"Возникло исключение {e}");
        Log("\nEnd\n", newLineAndTime: false);
    }

    internal static void ShowException(Exception e)
    {
        var dialog = new TaskDialog("Exception");
        dialog.MainInstruction = e.Message;
        dialog.ExpandedContent = e.ToString();
        dialog.FooterText = $"<a href=\"file:///{DayLogPath} \">Открыть лог-файл</a>";///////
        dialog.Show();
    }
}

class UserException : Exception
{
    public UserException()
    {
    }

    public UserException(string message) : base(message)
    {
    }

    public override string ToString()
    {
        return base.Message;
    }
}

public static class Selection
{
    public static IEnumerable<Element> GetElems(UIApplication uiApp, BuiltInCategory bic = BuiltInCategory.INVALID)
    {
        return uiApp.ActiveUIDocument.Selection.GetElementIds()
            .Select(id => uiApp.ActiveUIDocument.Document.GetElement(id))
            .Where(el => IsOfNeededCategory(el, bic));
    }

    private static bool IsOfNeededCategory(Element el, BuiltInCategory bic)
    {
        if (bic == BuiltInCategory.INVALID)
            return true;
        return el.Category.Id.IntegerValue == (int)bic;
    }
}