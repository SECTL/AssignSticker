using Avalonia;
using System;
using System.IO;
using AssignSticker_X.Utils;

namespace AssignSticker_X;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        AppData.Initialize();
        ConfigManager.Load();
        Logger.Info("程序启动中...");

        var lockPath = Path.Combine(Path.GetTempPath(), "AssignSticker_X.lock");
        try
        {
            using var fs = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            fs.WriteByte(0);
            fs.Flush();
            Logger.Info("单实例锁获取成功");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (IOException)
        {
            Logger.Warn("检测到已存在运行实例");
            App.IsFirstInstance = false;
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        Logger.Info("程序已退出");
        ConfigManager.Save();
        Logger.FlushToFile();
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .LogToTrace();
}