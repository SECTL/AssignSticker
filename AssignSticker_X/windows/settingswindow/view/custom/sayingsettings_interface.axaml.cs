using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AssignSticker_X.Utils;

namespace AssignSticker_X.windows.settingswindow.view.custom;

public partial class sayingsettings_interface : UserControl
{
    public sayingsettings_interface()
    {
        InitializeComponent();
        var enabled = ConfigManager.Get("hitokoto_enabled", true);
        HitokotoToggle.IsChecked = enabled;
        SourceExpander.IsEnabled = enabled;

        var savedSource = ConfigManager.Get("hitokoto_source", "poem");
        SourceCombo.SelectedIndex = savedSource == "wenan" ? 1 : 0;
    }

    private async void SubmitSaying_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top != null)
            await top.Launcher.LaunchUriAsync(new Uri("https://lcn79k2pnlbi.feishu.cn/share/base/form/shrcnlgskTroWGSP57En9MDzkss"));
    }

    private void SourceCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox combo) return;
        var source = combo.SelectedIndex == 1 ? "wenan" : "poem";
        ConfigManager.Set("hitokoto_source", source);
        ConfigManager.Save();
        MainWindow.HitokotoSourceChanged?.Invoke();
    }

    private void HitokotoToggle_IsCheckedChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var enabled = HitokotoToggle.IsChecked == true;
        ConfigManager.Set("hitokoto_enabled", enabled);
        ConfigManager.Save();
        MainWindow.HitokotoEnabledChanged?.Invoke(enabled);
        SourceExpander.IsEnabled = enabled;
    }
}
