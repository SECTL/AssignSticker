using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Documents;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using AssignSticker_X.Models;
using AssignSticker_X.Utils;

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
        Logger.Info("主窗口初始化");

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
            Logger.Info("打开布置作业对话框");
            var item = await ShowHomeworkDialog();
            if (item != null)
            {
                _homeworkItems.Add(item);
                RenderHomeworkCards();
                Logger.Info($"添加作业: {item.Subject} - {item.Type}");
            }
        };

        RenderHomeworkCards();

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
        Logger.Info("隐藏主窗口 -> 显示小部件");
        Hide();
        if (_showMenuItem != null)
            _showMenuItem.IsEnabled = true;
        ShowWidget();
    }

    private void ShowMainWindow()
    {
        Logger.Info("恢复主窗口显示");
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
        Logger.Info("用户请求退出应用");
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    private void MenuItemSettings_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Logger.Info("打开设置窗口");
        var settingsWindow = new windows.settingswindow.settingshell();
        settingsWindow.Show();
    }

    private List<HomeworkItem> _homeworkItems = new();
    private StackPanel? _expandedActionRow;
    private static FontFamily BoldFont = new("avares://AssignSticker_X/Assets/fonts/MiSans-Bold.ttf#MiSans");

    private static List<string> LoadSubjects()
    {
        var saved = ConfigManager.Get<string>("subjects");
        if (!string.IsNullOrEmpty(saved))
        {
            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(saved);
                if (parsed != null && parsed.Count > 0) return parsed;
            }
            catch { }
        }
        return new List<string> { "语文", "数学", "英语", "物理", "化学", "生物", "历史", "地理", "政治" };
    }

    private async Task<HomeworkItem?> ShowHomeworkDialog(HomeworkItem? existing = null)
    {
        var subjectCombo = new ComboBox { Width = 100, ItemsSource = LoadSubjects() };
        var typeCombo = new ComboBox { Width = 140, ItemsSource = new[] { "练习册", "预习", "自定义作业" } };

        var dynamicPanel = new StackPanel { Spacing = 8 };
        TextBox? startBox = null, endBox = null, noteBox = null, contentBox = null;

        StackPanel MakeFormatToolbar(TextBox target)
        {
            var toolbar = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 4, Margin = new Thickness(0, 6, 0, 0) };

            void WrapSel(string open, string close)
            {
                var text = target.Text ?? "";
                var s = Math.Min(target.SelectionStart, target.SelectionEnd);
                var e = Math.Max(target.SelectionStart, target.SelectionEnd);
                if (s == e)
                {
                    text = text.Insert(s, $"{open}文本{close}");
                    target.Text = text;
                    target.SelectionStart = s + open.Length;
                    target.SelectionEnd = s + open.Length + 2;
                }
                else
                {
                    var sel = text[s..e];
                    text = text[..s] + $"{open}{sel}{close}" + text[e..];
                    target.Text = text;
                    target.SelectionStart = s;
                    target.SelectionEnd = s + open.Length + sel.Length + close.Length;
                }
                target.Focus();
            }

            var boldBtn = new Button { Content = "B", Width = 28, Height = 26, FontSize = 12, FontWeight = FontWeight.Bold, Padding = new Thickness(0) };
            ToolTip.SetTip(boldBtn, "粗体");
            boldBtn.Click += (_, _) => WrapSel("{b}", "{/b}");

            var italicBtn = new Button { Content = "I", Width = 28, Height = 26, FontSize = 12, FontStyle = FontStyle.Italic, Padding = new Thickness(0) };
            ToolTip.SetTip(italicBtn, "斜体");
            italicBtn.Click += (_, _) => WrapSel("{i}", "{/i}");

            var strikeBtn = new Button { Content = "S", Width = 28, Height = 26, FontSize = 12, Padding = new Thickness(0) };
            ToolTip.SetTip(strikeBtn, "删除线");
            strikeBtn.Click += (_, _) => WrapSel("{s}", "{/s}");

            var colorBtn = new Button { Content = "A", Width = 28, Height = 26, FontSize = 12, Foreground = new SolidColorBrush(Colors.Red), Padding = new Thickness(0) };
            ToolTip.SetTip(colorBtn, "颜色");
            var colorFlyout = new Flyout();
            var colorPanel = new StackPanel { Spacing = 4 };
            var colors = new[] { (name: "红色", val: "#FF0000"), (name: "蓝色", val: "#0078D4"), (name: "绿色", val: "#00A000"), (name: "橙色", val: "#FF8C00"), (name: "紫色", val: "#8B00FF"), (name: "灰色", val: "#808080") };
            foreach (var c in colors)
            {
                var cb = new Button { Content = c.name, Width = 60, Height = 24, FontSize = 11, Background = new SolidColorBrush(Color.Parse(c.val)), Foreground = Brushes.White, Padding = new Thickness(4, 0) };
                var captured = c.val;
                cb.Click += (_, _) =>
                {
                    WrapSel($"{{c:{captured}}}", "{/c}");
                    colorFlyout.Hide();
                };
                colorPanel.Children.Add(cb);
            }
            colorFlyout.Content = colorPanel;
            colorBtn.Flyout = colorFlyout;

            toolbar.Children.Add(boldBtn);
            toolbar.Children.Add(italicBtn);
            toolbar.Children.Add(strikeBtn);
            toolbar.Children.Add(colorBtn);
            return toolbar;
        }

        void UpdateDynamicContent()
        {
            dynamicPanel.Children.Clear();
            startBox = null; endBox = null; noteBox = null; contentBox = null;
            switch (typeCombo.SelectedIndex)
            {
                case 0:
                    startBox = new TextBox { Width = 80 };
                    endBox = new TextBox { Width = 80 };
                    dynamicPanel.Children.Add(new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        Spacing = 12,
                        Children =
                        {
                            new StackPanel { Spacing = 4, Children = { new TextBlock { Text = "开始页数", FontSize = 13 }, startBox } },
                            new StackPanel { Spacing = 4, Children = { new TextBlock { Text = "结束页数", FontSize = 13 }, endBox } }
                        }
                    });

                    noteBox = new TextBox { MinHeight = 80, PlaceholderText = "备注（支持富文本）", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
                    dynamicPanel.Children.Add(MakeFormatToolbar(noteBox));
                    dynamicPanel.Children.Add(noteBox);
                    break;
                case 1:
                    contentBox = new TextBox { PlaceholderText = "作业内容及作业要求", AcceptsReturn = true, MinHeight = 120, TextWrapping = TextWrapping.Wrap };
                    dynamicPanel.Children.Add(contentBox);
                    break;
                default:
                    contentBox = new TextBox { PlaceholderText = "输入作业内容...", AcceptsReturn = true, MinHeight = 120, TextWrapping = TextWrapping.Wrap };
                    dynamicPanel.Children.Add(MakeFormatToolbar(contentBox));
                    dynamicPanel.Children.Add(contentBox);
                    break;
                    dynamicPanel.Children.Add(contentBox);
                    break;
            }
        }

        typeCombo.SelectionChanged += (_, _) => UpdateDynamicContent();

        var tagPanel = new StackPanel { Spacing = 8 };
        var tagRow = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8 };
        tagPanel.Children.Add(tagRow);
        var selectedBg = new SolidColorBrush(Color.Parse("#0D6EFD"));
        var unselectedBg = new SolidColorBrush(Colors.Transparent);
        var unselectedFg = new SolidColorBrush(Color.Parse("#888888"));

        var existingTags = existing?.Tags ?? new List<string>();
        var presetTags = new[] { "家长签字", "放学前交" };
        foreach (var t in presetTags)
        {
            var isSelected = existingTags.Contains(t);
            var btn = new Button { Content = t, Width = 90, Tag = isSelected };
            btn.Background = isSelected ? selectedBg : unselectedBg;
            btn.Foreground = isSelected ? Brushes.White : unselectedFg;
            btn.Click += (_, _) =>
            {
                var sel = !(bool)btn.Tag!;
                btn.Tag = sel;
                btn.Background = sel ? selectedBg : unselectedBg;
                btn.Foreground = sel ? Brushes.White : unselectedFg;
            };
            tagRow.Children.Add(btn);
        }

        // Add existing custom tags (non-preset)
        var customTags = existingTags.Where(t => !presetTags.Contains(t)).ToList();

        var customTagBox = new TextBox { Width = 100, PlaceholderText = "自定义标签" };
        var addTagBtn = new Button { Content = "+", Width = 30 };
        addTagBtn.Click += (_, _) =>
        {
            var text = customTagBox.Text?.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                var btn = new Button { Content = text, Width = 90, Tag = true };
                btn.Background = selectedBg;
                btn.Foreground = Brushes.White;
                var idx = tagRow.Children.Count - 2;
                tagRow.Children.Insert(Math.Max(0, idx), btn);
                customTagBox.Text = "";
            }
        };
        var inputRow = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8 };
        inputRow.Children.Add(customTagBox);
        inputRow.Children.Add(addTagBtn);
        tagPanel.Children.Add(inputRow);

        // Pre-populate existing custom tags
        foreach (var ct in customTags)
        {
            var btn = new Button { Content = ct, Width = 90, Tag = true };
            btn.Background = selectedBg;
            btn.Foreground = Brushes.White;
            var idx = tagRow.Children.Count - 2;
            tagRow.Children.Insert(Math.Max(0, idx), btn);
        }

        // Pre-populate fields
        if (existing != null)
        {
            subjectCombo.SelectedItem = existing.Subject;
            var typeIdx = Array.IndexOf(new[] { "练习册", "预习", "自定义作业" }, existing.Type);
            typeCombo.SelectedIndex = typeIdx >= 0 ? typeIdx : 2;
            // UpdateDynamicContent triggered by SelectedIndex, controls now exist
            if (startBox != null) startBox.Text = existing.StartPage ?? "";
            if (endBox != null) endBox.Text = existing.EndPage ?? "";
            if (noteBox != null) noteBox.Text = existing.Note ?? "";
            if (contentBox != null) contentBox.Text = existing.Content ?? "";
        }
        else
        {
            typeCombo.SelectedIndex = 2;
        }

        var content = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 16, Children =
                {
                    new StackPanel { Spacing = 4, Children = { new TextBlock { Text = "科目", FontSize = 13 }, subjectCombo } },
                    new StackPanel { Spacing = 4, Children = { new TextBlock { Text = "作业类型", FontSize = 13 }, typeCombo } }
                }},
                new Separator(),
                dynamicPanel,
                new Separator(),
                new StackPanel { Spacing = 4, Children = { new TextBlock { Text = "标签", FontSize = 13 }, tagPanel } }
            }
        };

        var dialog = new FAContentDialog
        {
            Title = existing != null ? "编辑作业" : "布置作业",
            PrimaryButtonText = "取消",
            CloseButtonText = "确定",
            Content = content
        };
        var font = (FontFamily)Application.Current!.FindResource("DefaultFont")!;
        dialog.Styles.Add(new Style { Selector = Selectors.OfType<TextBlock>(null), Setters = { new Setter(TextBlock.FontFamilyProperty, font) } });
        dialog.Styles.Add(new Style { Selector = Selectors.OfType<TextBox>(null), Setters = { new Setter(TextBox.FontFamilyProperty, font) } });
        dialog.Styles.Add(new Style { Selector = Selectors.OfType<ComboBox>(null), Setters = { new Setter(ComboBox.FontFamilyProperty, font) } });
        dialog.Styles.Add(new Style { Selector = Selectors.OfType<Button>(null), Setters = { new Setter(Button.FontFamilyProperty, font) } });

        var result = await dialog.ShowAsync(this);
        if (result == FAContentDialogResult.None)
        {
            var item = new HomeworkItem
            {
                Subject = subjectCombo.SelectedItem?.ToString() ?? "未设置",
                Type = typeCombo.SelectedItem?.ToString() ?? "自定义作业",
                StartPage = startBox?.Text,
                EndPage = endBox?.Text,
                Note = noteBox?.Text,
                Content = contentBox?.Text
            };

            foreach (var child in tagRow.Children)
            {
                if (child is Button b && b.Content is string s)
                {
                    var selected = b.Tag is bool bv && bv;
                    if (selected)
                        item.Tags.Add(s);
                }
            }

            return item;
        }
        return null;
    }

    private static void AppendRichText(InlineCollection inlines, string? text)
    {
        if (string.IsNullOrEmpty(text)) return;

        var buf = new StringBuilder();
        void Flush()
        {
            if (buf.Length > 0)
            {
                inlines.Add(new Run { Text = buf.ToString() });
                buf.Clear();
            }
        }

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '{')
            {
                int close = text.IndexOf('}', i);
                if (close < 0) { buf.Append(text[i..]); break; }

                var tag = text[(i + 1)..close];
                string tagName;
                string? colorArg = null;
                int colon = tag.IndexOf(':');
                if (colon >= 0) { tagName = tag[..colon]; colorArg = tag[(colon + 1)..]; }
                else tagName = tag;

                if (tagName == "/" || tagName.StartsWith('/'))
                {
                    buf.Append(text[i..(close + 1)]);
                    i = close;
                    continue;
                }

                var closeTag = $"{{/{tagName}}}";
                int end = text.IndexOf(closeTag, close + 1);
                if (end < 0) { buf.Append(text[i..]); break; }

                var inner = text[(close + 1)..end];
                Flush();

                var run = new Run { Text = inner };
                switch (tagName)
                {
                    case "b":
                        run.SetValue(TextElement.FontFamilyProperty, BoldFont);
                        break;
                    case "i":
                        run.SetValue(TextElement.FontStyleProperty, FontStyle.Italic);
                        break;
                    case "s":
                        run.TextDecorations = TextDecorations.Strikethrough;
                        break;
                    case "c":
                        if (colorArg != null && Color.TryParse(colorArg, out var c))
                            run.SetValue(TextElement.ForegroundProperty, new SolidColorBrush(c));
                        break;
                }
                inlines.Add(run);
                i = end + closeTag.Length - 1;
            }
            else
            {
                buf.Append(text[i]);
            }
        }
        Flush();
    }

    private void RenderHomeworkCards()
    {
        HomeworkContainer.Children.Clear();
        _expandedActionRow = null;

        var grouped = _homeworkItems.GroupBy(h => h.Subject);
        foreach (var group in grouped)
        {
            var section = new StackPanel { Spacing = 2, Margin = new Thickness(0, 0, 0, 12) };

            section.Children.Add(new TextBlock
            {
                Text = group.Key,
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(8, 0, 0, 4)
            });

            foreach (var item in group)
            {
                var container = new StackPanel { Spacing = 2, Margin = new Thickness(5) };

                var itemRow = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Spacing = 8,
                    Margin = new Thickness(8, 0, 0, 0),
                    Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
                };

                itemRow.Children.Add(new TextBlock
                {
                    Text = "•",
                    FontSize = 18,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                });

                var contentText = new TextBlock
                {
                    FontSize = 18,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = Avalonia.Media.TextAlignment.Left,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Inlines = new InlineCollection()
                };
                var inlines = contentText.Inlines!;
                if (item.Type != "自定义作业")
                    inlines.Add(new Run { Text = item.Type });

                if (item.Type == "练习册" && item.StartPage != null)
                    inlines.Add(new Run { Text = $" 第{item.StartPage}页-第{item.EndPage}页" });

                if (!string.IsNullOrEmpty(item.Note))
                {
                    inlines.Add(new Run { Text = " " });
                    AppendRichText(inlines, item.Note);
                }

                if (!string.IsNullOrEmpty(item.Content))
                {
                    inlines.Add(new Run { Text = " " });
                    AppendRichText(inlines, item.Content);
                }

                itemRow.Children.Add(contentText);

                foreach (var tag in item.Tags)
                {
                    itemRow.Children.Add(new Border
                    {
                        Child = new TextBlock { Text = tag, FontSize = 14, Margin = new Thickness(6, 2), Foreground = Brushes.White },
                        Background = new SolidColorBrush(Color.Parse("#0D6EFD")),
                        CornerRadius = new CornerRadius(4),
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    });
                }

                container.Children.Add(itemRow);

                var actionRow = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Spacing = 6,
                    Margin = new Thickness(40, 0, 0, 2),
                    IsVisible = false
                };

                var editBtn = new Button { Content = "编辑", FontSize = 14, Padding = new Thickness(10, 4) };
                var deleteBtn = new Button { Content = "删除", FontSize = 14, Padding = new Thickness(10, 4) };

                var capturedItem = item;
                editBtn.Click += async (_, _) =>
                {
                    var updated = await ShowHomeworkDialog(capturedItem);
                    if (updated != null)
                    {
                        var idx = _homeworkItems.IndexOf(capturedItem);
                        if (idx >= 0)
                        {
                            _homeworkItems[idx] = updated;
                            RenderHomeworkCards();
                            Logger.Info($"编辑作业: {updated.Subject} - {updated.Type}");
                        }
                    }
                };

                deleteBtn.Click += (_, _) =>
                {
                    _homeworkItems.Remove(capturedItem);
                    RenderHomeworkCards();
                    Logger.Info($"删除作业: {capturedItem.Subject} - {capturedItem.Type}");
                };

                actionRow.Children.Add(editBtn);
                actionRow.Children.Add(deleteBtn);
                container.Children.Add(actionRow);

                itemRow.Tapped += (_, _) =>
                {
                    if (_expandedActionRow != null && _expandedActionRow != actionRow)
                        _expandedActionRow.IsVisible = false;
                    actionRow.IsVisible = !actionRow.IsVisible;
                    _expandedActionRow = actionRow.IsVisible ? actionRow : null;
                };

                section.Children.Add(container);
            }

            HomeworkContainer.Children.Add(section);
        }
    }
}