using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AssignSticker_X.Utils;
using AssignSticker_X;
using FluentAvalonia.UI.Controls;

namespace AssignSticker_X.windows.settingswindow.view;

public partial class savessetting_interface : UserControl
{
    public savessetting_interface()
    {
        InitializeComponent();
        LoadSettings();
        AutoSaveExpander.PointerPressed += AutoSaveExpander_PointerPressed;
    }

    private void LoadSettings()
    {
        var autoSave = ConfigManager.Get<bool>("auto_save_enabled", false);
        AutoSaveToggle.IsChecked = autoSave;
        AutoSaveExpander.IsExpanded = autoSave;

        var autoClear = ConfigManager.Get<bool>("auto_clear_expired_enabled", false);
        AutoClearToggle.IsChecked = autoClear;
    }

    private void AutoSaveExpander_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (!(AutoSaveToggle.IsChecked ?? false))
        {
            AutoSaveExpander.IsExpanded = false;
            e.Handled = true;
        }
    }

    private void AutoSaveToggle_IsCheckedChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var isChecked = AutoSaveToggle.IsChecked ?? false;
        AutoSaveExpander.IsExpanded = isChecked;
        ConfigManager.Set("auto_save_enabled", isChecked);
        ConfigManager.Save();
    }

    private void AutoClearToggle_IsCheckedChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var isChecked = AutoClearToggle.IsChecked ?? false;
        ConfigManager.Set("auto_clear_expired_enabled", isChecked);
        ConfigManager.Save();
        MainWindow.AutoClearExpiredChanged?.Invoke(isChecked);
    }

    private async void OpenDataFolderButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top != null)
        {
            var path = AppData.BasePath;
            if (System.IO.Directory.Exists(path))
                await top.Launcher.LaunchUriAsync(new Uri(path));
        }
    }

    private async void UafHelpLink_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var content = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "UAF (Unified Assignment Format) 是一种基于 PDF+csv 的统一作业展示与交换格式，它支持不同的作业看板软件。",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    FontSize = 14
                },
                new FAInfoBar
                {
                    Title = "提示",
                    Message = "UAF格式尚处于测试阶段，可能并不稳定。",
                    Severity = FAInfoBarSeverity.Warning,
                    IsOpen = true,
                    FontSize = 14
                },
                new Separator(),
                new TextBlock
                {
                    Text = "目前已经/计划支持该格式的软件有",
                    FontSize = 16,
                    FontWeight = Avalonia.Media.FontWeight.SemiBold
                },
                new TextBlock
                {
                    Text = "• 本应用（AssignSticker)≥1.8.100.1",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    FontSize = 13
                },
                new TextBlock
                {
                    Text = "• StickyHomework2(计划支持）",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    FontSize = 13
                },
                new TextBlock
                {
                    Text = "• Classworks（计划支持）",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    FontSize = 13
                },
                new Separator(),
                new TextBlock
                {
                    Text = "（以上数据截至 2026年 6 月 20日）",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    FontSize = 13,
                    Foreground = Avalonia.Media.Brushes.Gray
                }
            }
        };

        var dialog = new FAContentDialog
        {
            Title = "UAF(统一作业展示与交换格式）",
            Content = content,
            CloseButtonText = "知道了"
        };

        var top = TopLevel.GetTopLevel(this);
        if (top is Window window)
            await dialog.ShowAsync(window);
    }
}
