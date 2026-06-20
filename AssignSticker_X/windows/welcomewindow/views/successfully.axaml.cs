using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace AssignSticker_X.windows.welcomewindow.views;

public partial class successfully : UserControl
{
    public successfully()
    {
        InitializeComponent();
    }

    private void Card_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var launcher = TopLevel.GetTopLevel(this)?.Launcher;
        launcher?.LaunchUriAsync(new System.Uri("https://assignsticker.sectl.cn/"));
    }
}
