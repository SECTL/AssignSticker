using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using AssignSticker_X.Utils;

namespace AssignSticker_X.windows.settingswindow.view;

public partial class generalsettings_interface : UserControl
{
    public generalsettings_interface()
    {
        InitializeComponent();
        LoadThemeSetting();
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
}