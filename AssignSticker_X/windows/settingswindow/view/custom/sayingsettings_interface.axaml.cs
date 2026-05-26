using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using AssignSticker_X.Utils;
using Avalonia.Media;

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

        var savedMode = ConfigManager.Get("hitokoto_source_mode", "local");
        SourceModeCombo.SelectedIndex = savedMode == "online" ? 1 : 0;
        ApiSourceItem.IsVisible = savedMode == "online";
    }

    private async void ApiSourceCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox combo || combo.SelectedIndex != 1) return;

        var top = TopLevel.GetTopLevel(this);
        if (top is not Window window) return;

        var dialog = new FAContentDialog
        {
            Title = "免责声明",
            Content = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = "第三方 API 不由SECTL Studio提供，也无法掌控其内容，", Margin = new Thickness(0, 8, 0, 0) },
                    new TextBlock { Text = "并且可能会含有令人不适的内容，从而引发不必要的麻烦。", TextWrapping = TextWrapping.Wrap },
                    new TextBlock { Text = "您确定要继续吗？", TextWrapping = TextWrapping.Wrap }
                }
            },
            
            CloseButtonText = "确定",
            PrimaryButtonText = "取消"
        };

        var result = await dialog.ShowAsync(window);
        if (result == FAContentDialogResult.Primary)
            combo.SelectedIndex = 0;
    }

    private async void SubmitSaying_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top != null)
            await top.Launcher.LaunchUriAsync(new Uri("https://lcn79k2pnlbi.feishu.cn/share/base/form/shrcnlgskTroWGSP57En9MDzkss"));
    }

    private void SourceModeCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox combo) return;
        var isOnline = combo.SelectedIndex == 1;
        if (ApiSourceItem != null)
            ApiSourceItem.IsVisible = isOnline;
        ConfigManager.Set("hitokoto_source_mode", isOnline ? "online" : "local");
        ConfigManager.Save();
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
