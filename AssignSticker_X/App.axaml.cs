using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using AssignSticker_X.Utils;

namespace AssignSticker_X;

public partial class App : Application
{
    public static bool IsFirstInstance { get; set; } = true;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        Logger.Info("App 初始化完成");
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

            Logger.Info("主窗口启动中...");
            var splash = CreateSplashWindow();
            splash.Show();
            Logger.Info("启动画面已显示");

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                Logger.Info("启动画面关闭，主窗口显示");
                var main = new MainWindow();
                desktop.MainWindow = main;
                main.Show();
                splash.Close();
            };
            timer.Start();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static Window CreateSplashWindow()
    {
        var uri = new Uri("avares://AssignSticker_X/Assets/imgs/splash_screen.png");
        using var stream = AssetLoader.Open(uri);
        var bitmap = new Bitmap(stream);

        return new Window
        {
            Width = bitmap.PixelSize.Width,
            Height = bitmap.PixelSize.Height,
            WindowDecorations = WindowDecorations.None,
            CanResize = false,
            ShowInTaskbar = false,
            Topmost = true,
            Content = new Image { Source = bitmap },
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };
    }
}