using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AssignSticker_X.windows.settingswindow.view.cloudservice;

public partial class asxweb : UserControl
{
    public asxweb()
    {
        InitializeComponent();
    }

    private void OpenWeb_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var launcher = TopLevel.GetTopLevel(this)?.Launcher;
        launcher?.LaunchUriAsync(new System.Uri("https://assignsticker.sectl.top/"));
    }
}
