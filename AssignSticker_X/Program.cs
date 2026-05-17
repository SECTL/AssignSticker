using Avalonia;
using System;
using System.IO;

namespace AssignSticker_X;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var lockPath = Path.Combine(Path.GetTempPath(), "AssignSticker_X.lock");
        try
        {
            using var fs = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            fs.WriteByte(0);
            fs.Flush();
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (IOException)
        {
            App.IsFirstInstance = false;
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .LogToTrace();
}