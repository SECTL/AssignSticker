using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using AssignSticker_X.Utils;

namespace AssignSticker_X.windows.welcomewindow.views;

public partial class appearance : UserControl
{
    private static readonly IBrush AccentBrush = new SolidColorBrush(Color.Parse("#0078D4"));
    private static readonly IBrush TransparentBrush = new SolidColorBrush(Colors.Transparent);

    public appearance()
    {
        InitializeComponent();
        var savedTheme = ConfigManager.Get<string>("theme", "default");
        switch (savedTheme)
        {
            case "light":
                LightRadio.IsChecked = true;
                break;
            case "dark":
                DarkRadio.IsChecked = true;
                break;
            default:
                SystemRadio.IsChecked = true;
                break;
        }
        UpdateBorders();
        LightRadio.IsCheckedChanged += ThemeRadio_Changed;
        DarkRadio.IsCheckedChanged += ThemeRadio_Changed;
        SystemRadio.IsCheckedChanged += ThemeRadio_Changed;
        SaveCurrentTheme();
    }

    private void ThemeRadio_Changed(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.IsChecked != true) return;
        UpdateBorders();
        SaveCurrentTheme();
    }

    private void SaveCurrentTheme()
    {
        var val = LightRadio.IsChecked == true ? "light"
                 : DarkRadio.IsChecked == true ? "dark"
                 : "default";
        ConfigManager.Set("theme", val);
        ConfigManager.Save();
        Application.Current.RequestedThemeVariant = val switch
        {
            "light" => ThemeVariant.Light,
            "dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }

    private void UpdateBorders()
    {
        LightBorder.BorderBrush = LightRadio.IsChecked == true ? AccentBrush : TransparentBrush;
        DarkBorder.BorderBrush = DarkRadio.IsChecked == true ? AccentBrush : TransparentBrush;
        SystemBorder.BorderBrush = SystemRadio.IsChecked == true ? AccentBrush : TransparentBrush;
    }
}
