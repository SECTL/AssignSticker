using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AssignSticker_X.Utils;

namespace AssignSticker_X.windows.settingswindow.view.custom;

public partial class barsettings_interface : UserControl
{
    public barsettings_interface()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        var opacity = ConfigManager.Get<double>("bar_opacity", 1.0);
        OpacitySlider.Value = opacity * 100;

        var radius = ConfigManager.Get<string>("bar_corner_radius", "large");
        CornerRadiusCombo.SelectedIndex = radius switch { "small" => 0, "medium" => 1, _ => 2 };

        LockToggle.IsChecked = ConfigManager.Get("bar_show_lock", true);
        SaveToggle.IsChecked = ConfigManager.Get("bar_show_save", true);
        MenuToggle.IsChecked = ConfigManager.Get("bar_show_menu", true);
        FullscreenToggle.IsChecked = ConfigManager.Get("bar_show_fullscreen", true);
        HideToggle.IsChecked = ConfigManager.Get("bar_show_hide", true);
        RestartToggle.IsChecked = ConfigManager.Get("bar_show_restart", true);
        ExitToggle.IsChecked = ConfigManager.Get("bar_show_exit", true);
    }

    private void OpacitySlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        var opacity = e.NewValue / 100.0;
        ConfigManager.Set("bar_opacity", opacity);
        ConfigManager.Save();
        MainWindow.BarOpacityChanged?.Invoke(opacity);
    }

    private void CornerRadiusCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox combo || combo.SelectedIndex < 0) return;
        var (key, radius) = combo.SelectedIndex switch
        {
            0 => ("small", 8.0),
            1 => ("medium", 20.0),
            _ => ("large", 100.0)
        };
        ConfigManager.Set("bar_corner_radius", key);
        ConfigManager.Save();
        MainWindow.BarCornerRadiusChanged?.Invoke(radius);
    }

    private void Toggle_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch toggle) return;
        var visible = toggle.IsChecked == true;
        var key = toggle.Name switch
        {
            "LockToggle" => "lock",
            "SaveToggle" => "save",
            "MenuToggle" => "menu",
            "FullscreenToggle" => "fullscreen",
            "HideToggle" => "hide",
            "RestartToggle" => "restart",
            "ExitToggle" => "exit",
            _ => null
        };
        if (key == null) return;
        var configKey = $"bar_show_{key}";
        ConfigManager.Set(configKey, visible);
        ConfigManager.Save();
        MainWindow.BarButtonVisibilityChanged?.Invoke(key, visible);
    }
}