using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using AssignSticker_X.windows.settingswindow.view;
using AssignSticker_X.windows.settingswindow.view.cloudservice;

namespace AssignSticker_X.windows.settingswindow;

public partial class settingshell : Window
{
    public settingshell()
    {
        InitializeComponent();

        if (OperatingSystem.IsMacOS())
        {
            Title = "设置";
            RightPanel.IsVisible = false;
        }
        else
        {
            Title = "";
            WindowDecorations = Avalonia.Controls.WindowDecorations.None;
        }

        PageContent.Content = new generalsettings_interface();
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void NavView_ItemInvoked(object sender, FANavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer is FANavigationViewItem item && item.Tag is string tag)
        {
            switch (tag)
            {
                case "general":
                    PageContent.Content = new generalsettings_interface();
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
            }
        }
    }

    private void MinimizeButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void CloseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}