using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using AssignSticker_X.Utils;

namespace AssignSticker_X;

public partial class App : Application
{
    public static bool IsFirstInstance { get; set; } = true;
    public static bool IsWindows { get; } = OperatingSystem.IsWindows();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        ApplySavedTheme();
        Logger.Info("App 初始化完成");
    }

    private static void ApplySavedTheme()
    {
        var savedTheme = ConfigManager.Get<string>("theme", "default");
        Current.RequestedThemeVariant = savedTheme switch
        {
            "light" => ThemeVariant.Light,
            "dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (!IsFirstInstance)
            {
                Logger.Warn("启动多开实例窗口");
                desktop.MainWindow = new windows.doubleswindow();
                base.OnFrameworkInitializationCompleted();
                return;
            }

            var finishesPath = Path.Combine(AppData.DataPath, "finishes");
            if (!File.Exists(finishesPath))
            {
                Logger.Info("初次启动，将显示引导设置窗口");
                var welcome = new windows.welcomewindow.rootwindow();
                desktop.MainWindow = welcome;
                welcome.Show();
            }
            else
            {
                var main = new MainWindow();
                desktop.MainWindow = main;
                main.Show();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}