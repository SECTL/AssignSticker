using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using AssignSticker_X.Utils;

namespace AssignSticker_X.windows.settingswindow.view;

public partial class generalsettings_interface : UserControl
{
    private const int DefaultFontScale = 100;

    public generalsettings_interface()
    {
        InitializeComponent();
        LoadThemeSetting();
        LoadFontScaleSetting();
    }

    private void LoadThemeSetting()
    {
        var savedTheme = ConfigManager.Get<string>("theme", "default");
        ThemeComboBox.SelectedIndex = savedTheme switch
        {
            "light" => 0,
            "dark" => 1,
            _ => 2
        };
    }

    private void ThemeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ThemeComboBox.SelectedIndex < 0) return;

        var theme = ThemeComboBox.SelectedIndex switch
        {
            0 => "light",
            1 => "dark",
            _ => "default"
        };

        ConfigManager.Set("theme", theme);
        ConfigManager.Save();

        App.Current.RequestedThemeVariant = theme switch
        {
            "light" => ThemeVariant.Light,
            "dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }

    private void LoadFontScaleSetting()
    {
        var scale = ConfigManager.Get<int>("homework_font_scale", DefaultFontScale);
        HomeworkFontScaleSlider.Value = scale;
    }

    private void HomeworkFontScaleSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        var val = (int)e.NewValue;
        ConfigManager.Set("homework_font_scale", val);
        ConfigManager.Save();
        MainWindow.HomeworkFontScaleChanged?.Invoke(val);
    }

    private void ResetFontScaleBtn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        HomeworkFontScaleSlider.Value = DefaultFontScale;
    }
}