using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;

namespace AssignSticker_X;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _timer;
    private readonly Random _random = new();
    private TrayIcon? _trayIcon;
    private NativeMenuItem? _showMenuItem;
    private Window? _widgetWindow;

    private static readonly string[] CnDays = ["周日", "周一", "周二", "周三", "周四", "周五", "周六"];
    private static readonly string[] EnDays = ["SUNDAY", "MONDAY", "TUESDAY", "WEDNESDAY", "THURSDAY", "FRIDAY", "SATURDAY"];

    public MainWindow()
    {
        InitializeComponent();

        LoadHitokoto();
        SetupWindowIcon();
        SetupTrayIcon();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (_, _) => UpdateTime();
        _timer.Start();

        UpdateTime();

        hidebutton.Click += (_, _) => HideMainWindow();

        AssignHomeworkButton.Click += async (_, _) =>
        {
            var dialog = new FAContentDialog
            {
                Title = "布置作业",
                PrimaryButtonText = "取消",
                CloseButtonText = "确定",
                Content = new StackPanel
                    
                {
                    Spacing = 8,
                Children =
                {
                    
                    
                    new StackPanel
                    {
                        Spacing = 4,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "科目",
                                FontSize = 13
                            },
                            new ComboBox
                            {
                                Width = 100,
                                ItemsSource = new[] { "语文", "数学", "英语", "物理", "化学", "生物", "历史", "地理", "政治" }
                            }
                        }
                    },
                    new Separator(),
                    new TextBox
                    {
                        PlaceholderText = "输入作业...",
                        AcceptsReturn = true,
                        MinHeight = 120,
                        TextWrapping = TextWrapping.Wrap
                    }   
                }
                
                }
            };
            await dialog.ShowAsync(this);
        };

        exitbutton.Click += (_, _) => ShutdownApp();

        restartbutton.Click += async (_, _) =>
        {
            var dllPath = System.Reflection.Assembly.GetEntryAssembly()?.Location;
            if (dllPath != null)
            {
                if (OperatingSystem.IsWindows())
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = Environment.ProcessPath,
                        Arguments = $"\"{dllPath}\"",
                        UseShellExecute = true
                    });
                }
                else
                {
                    Process.Start("/bin/bash", $"-c \"nohup '{Environment.ProcessPath}' '{dllPath}' > /dev/null 2>&1 &\"");
                }
            }
            await Task.Delay(500);
            ShutdownApp();
        };
    }

    private void SetupTrayIcon()
    {
        try
        {
            var uri = new Uri("avares://AssignSticker_X/Assets/logo/logo.png");
            using var stream = AssetLoader.Open(uri);
            var icon = new WindowIcon(stream);

            _showMenuItem = new NativeMenuItem("显示主窗口") { IsEnabled = false };
            _showMenuItem.Click += (_, _) => ShowMainWindow();
            var separator = new NativeMenuItemSeparator();
            var restartItem = new NativeMenuItem("重启应用");
            restartItem.Click += async (_, _) =>
            {
                var dllPath = System.Reflection.Assembly.GetEntryAssembly()?.Location;
                if (dllPath != null)
                {
                    if (OperatingSystem.IsWindows())
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = Environment.ProcessPath,
                            Arguments = $"\"{dllPath}\"",
                            UseShellExecute = true
                        });
                    }
                    else
                    {
                        Process.Start("/bin/bash", $"-c \"nohup '{Environment.ProcessPath}' '{dllPath}' > /dev/null 2>&1 &\"");
                    }
                }
                await Task.Delay(500);
                ShutdownApp();
            };
            var exitItem = new NativeMenuItem("退出应用");
            exitItem.Click += (_, _) => ShutdownApp();

            var menu = new NativeMenu();
            menu.Add(_showMenuItem);
            menu.Add(separator);
            menu.Add(restartItem);
            menu.Add(exitItem);

            _trayIcon = new TrayIcon
            {
                Icon = icon,
                ToolTipText = "AssignSticker_X",
                Menu = menu,
                IsVisible = true
            };
        }
        catch
        {
            // Tray icon setup failed silently
        }
    }

    private void LoadHitokoto()
    {
        try
        {
            var uri = new Uri("avares://AssignSticker_X/Assets/Saying/gushi.json");
            using var stream = AssetLoader.Open(uri);
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            using var doc = JsonDocument.Parse(json);
            var sayings = new List<(string Content, string From)>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var content = prop.Value.GetProperty("content").GetString();
                var from = prop.Value.GetProperty("from").GetString();
                if (content != null && from != null)
                    sayings.Add((content, from));
            }

            if (sayings.Count > 0)
            {
                var (content, from) = sayings[_random.Next(sayings.Count)];
                HitokotoContentTextBlock.Text = content;
                HitokotoFromTextBlock.Text = from;
            }
        }
        catch
        {
            HitokotoContentTextBlock.Text = "一言加载失败";
        }
    }

    private void UpdateTime()
    {
        var now = DateTime.Now;
        TimeTextBlock.Text = now.ToString("HH:mm:ss");
        WeekdayTextBlock.Text = $"{CnDays[(int)now.DayOfWeek]}  {EnDays[(int)now.DayOfWeek]}";
        DateTextBlock.Text = $"{now.Year}年{now.Month}月{now.Day}日";
    }

    private void SetupWindowIcon()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var uri = new Uri("avares://AssignSticker_X/icon.ico");
                using var stream = AssetLoader.Open(uri);
                Icon = new WindowIcon(stream);
            }
            else
            {
                var uri = new Uri("avares://AssignSticker_X/Assets/logo/logo.png");
                byte[] pngBytes;
                using (var stream = AssetLoader.Open(uri))
                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    pngBytes = ms.ToArray();
                }

                using (var ms = new MemoryStream(pngBytes))
                    Icon = new WindowIcon(ms);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    SetMacOSDockIcon(pngBytes);
            }
        }
        catch
        {
            // ignore
        }
    }

    [DllImport("/usr/lib/libobjc.dylib")]
    private static extern IntPtr objc_getClass(string name);

    [DllImport("/usr/lib/libobjc.dylib")]
    private static extern IntPtr sel_registerName(string name);

    [DllImport("/usr/lib/libobjc.dylib")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_1arg(IntPtr receiver, IntPtr selector, IntPtr arg);

    private static void SetMacOSDockIcon(byte[] pngBytes)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "asx_icon.png");
        File.WriteAllBytes(tempPath, pngBytes);

        var nsApp = objc_msgSend(objc_getClass("NSApplication"), sel_registerName("sharedApplication"));

        var pathPtr = Marshal.StringToCoTaskMemUTF8(tempPath);
        var nsStr = objc_msgSend_1arg(
            objc_getClass("NSString"), sel_registerName("stringWithUTF8String:"), pathPtr);
        Marshal.FreeCoTaskMem(pathPtr);

        var nsImage = objc_msgSend(objc_getClass("NSImage"), sel_registerName("alloc"));
        nsImage = objc_msgSend_1arg(nsImage, sel_registerName("initWithContentsOfFile:"), nsStr);

        objc_msgSend_1arg(nsApp, sel_registerName("setApplicationIconImage:"), nsImage);
    }

    private void HideMainWindow()
    {
        Hide();
        if (_showMenuItem != null)
            _showMenuItem.IsEnabled = true;
        ShowWidget();
    }

    private void ShowMainWindow()
    {
        HideWidget();
        Show();
        Activate();
        if (_showMenuItem != null)
            _showMenuItem.IsEnabled = false;
    }

    private void ShowWidget()
    {
        if (_widgetWindow == null)
            _widgetWindow = CreateWidgetWindow();
        _widgetWindow.Show();
    }

    private void HideWidget()
    {
        if (_widgetWindow != null)
            _widgetWindow.Hide();
    }

    private Window CreateWidgetWindow()
    {
        var uri = new Uri("avares://AssignSticker_X/Assets/logo/logo.png");
        byte[] pngBytes;
        using (var stream = AssetLoader.Open(uri))
        using (var ms = new MemoryStream())
        {
            stream.CopyTo(ms);
            pngBytes = ms.ToArray();
        }

        var bitmap = new Bitmap(new MemoryStream(pngBytes));

        var image = new Avalonia.Controls.Image
        {
            Source = bitmap,
            Width = 60,
            Height = 60,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        var button = new Button
        {
            Width = 80,
            Height = 80,
            Background = new SolidColorBrush(Color.FromRgb(42, 42, 42)),
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(12),
            Content = image
        };
        button.Click += (_, _) => ShowMainWindow();

        var win = new Window
        {
            Width = 80,
            Height = 80,
            CanResize = false,
            WindowDecorations = Avalonia.Controls.WindowDecorations.None,
            ShowInTaskbar = false,
            Topmost = true,
            Content = button
        };

        var screen = Screens.Primary;
        if (screen != null)
        {
            var b = screen.WorkingArea;
            win.Position = new PixelPoint(b.Right - 80, b.Bottom - 80 - 40);
        }

        win.PositionChanged += (_, _) =>
        {
            var s = Screens.Primary;
            if (s != null)
            {
                var b = s.WorkingArea;
                var pos = win.Position;
                if (pos.X != b.Right - 80)
                    win.Position = new PixelPoint(b.Right - 80, pos.Y);
            }
        };

        return win;
    }

    private static void ShutdownApp()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    private void MenuItemSettings_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var settingsWindow = new windows.settingswindow.settingshell();
        settingsWindow.Show();
    }
}