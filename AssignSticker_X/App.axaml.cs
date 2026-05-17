using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace AssignSticker_X;

public partial class App : Application
{
    public static bool IsFirstInstance { get; set; } = true;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (!IsFirstInstance)
            {
                desktop.MainWindow = new windows.doubleswindow();
                base.OnFrameworkInitializationCompleted();
                return;
            }

            var splash = CreateSplashWindow();
            splash.Show();

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
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