using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FluentAvalonia.UI.Controls;
using AssignSticker_X.windows.settingswindow.view;
using AssignSticker_X.windows.settingswindow.view.cloudservice;
using AssignSticker_X.windows.settingswindow.view.management;
using AssignSticker_X.windows.settingswindow.view.custom;
using Avalonia.Input.Platform;
using AssignSticker_X.Utils;
using Avalonia.Media;
using FluentAvalonia.UI.Windowing;

namespace AssignSticker_X.windows.settingswindow;

public partial class settingshell : FAAppWindow
{
    public settingshell()
    {
        InitializeComponent();
        Logger.Info("设置窗口初始化");

        TitleBar.Height = 48;
        TitleBar.ExtendsContentIntoTitleBar = true;
        // TitleBar.TitleBarHitTestType = FATitleBarHitTestType.Complex;
        TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(23, 0, 0, 0);
        TitleBar.ButtonPressedBackgroundColor = Color.FromArgb(52, 0, 0, 0);
        TitleBar.ButtonInactiveForegroundColor = Colors.Gray;

        PageContent.Content = new generalsettings_interface();
    }

    private void NavView_ItemInvoked(object sender, FANavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer is FANavigationViewItem item && item.Tag is string tag)
        {
            Logger.Info($"导航到页面: {tag}");
            switch (tag)
            {
                case "general":
                    PageContent.Content = new generalsettings_interface();
                    break;
                case "bar":
                    PageContent.Content = new barsettings_interface();
                    break;
                case "about":
                    PageContent.Content = new about_interface();
                    break;
                case "asxweb":
                    PageContent.Content = new asxweb();
                    break;
                case "classworkskv":
                    PageContent.Content = new classworkskv();
                    break;
                case "subjectmanag":
                    PageContent.Content = new subjectmanag_interface();
                    break;
                case "saying":
                    PageContent.Content = new sayingsettings_interface();
                    break;
            }
        }
    }

    private async void FeedbackMenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
            await clipboard.SetTextAsync($"AssignSticker_X Feedback - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        var warnUri = new Uri("avares://AssignSticker_X/Assets/imgs/warn.png");
        var warnImage = new Image
        {
            Source = new Bitmap(AssetLoader.Open(warnUri)),
            Width = 280,
            Height = 280,
            Margin = new Thickness(0, 12, 0, 0)
        };

        var content = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = "已将日志粘贴到剪贴板，请点击反馈按钮", TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                warnImage
            }
        };

        var dialog = new FAContentDialog
        {
            Title = "反馈",
            Content = content,
            PrimaryButtonText = "反馈",
            CloseButtonText = "取消"
        };

        var result = await dialog.ShowAsync(this);
        if (result == FAContentDialogResult.Primary)
        {
            var top = TopLevel.GetTopLevel(this);
            if (top != null)
                await top.Launcher.LaunchUriAsync(new Uri("https://github.com/SECTL/AssignSticker/issues"));
        }
    }
}