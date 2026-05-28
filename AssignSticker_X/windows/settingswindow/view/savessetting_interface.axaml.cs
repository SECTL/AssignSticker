using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AssignSticker_X.Utils;
using AssignSticker_X;

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
}
