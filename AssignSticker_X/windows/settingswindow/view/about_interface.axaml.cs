using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AssignSticker_X.windows;

namespace AssignSticker_X.windows.settingswindow.view;

public partial class about_interface : UserControl
{
    public about_interface()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        var grid = this.FindControl<Grid>("InfoGrid");
        if (grid == null) return;

        var count = grid.Children.Count;
        if (count == 0) return;

        var colDefs = new ColumnDefinitions();
        for (var i = 0; i < count; i++)
        {
            colDefs.Add(new ColumnDefinition(GridLength.Star));
            Grid.SetColumn(grid.Children[i], i);
        }
        grid.ColumnDefinitions = colDefs;
    }

    private async void OnOpenOrgWebsite(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top != null)
            await top.Launcher.LaunchUriAsync(new Uri("https://sectl.cn"));
    }

    private async void OnOpenSecRandom(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top != null)
            await top.Launcher.LaunchUriAsync(new Uri("https://secrandom.sectl.cn/"));
    }

    private async void OnOpenLuminalium(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top != null)
            await top.Launcher.LaunchUriAsync(new Uri("https://luminalium.sectl.cn/"));
    }

    private async void OnOpenQQGroup(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top != null)
            await top.Launcher.LaunchUriAsync(new Uri("https://qm.qq.com/q/U2mZmPgwyk"));
    }

    private async void OnOpenGitHub(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top != null)
            await top.Launcher.LaunchUriAsync(new Uri("https://github.com/SECTL/AssignSticker"));
    }

    private async void OnOpenWebsite(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top != null)
            await top.Launcher.LaunchUriAsync(new Uri("https://assignsticker.sectl.top/"));
    }
    private async void OnOpenstcn(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top != null)
            await top.Launcher.LaunchUriAsync(new Uri("https://forum.smart-teach.cn/t/assignsticker"));
    }


    private void OnOpenAuthorsWindow(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top is Window owner)
        {
            var window = new authers_window();
            window.ShowDialog(owner);
        }
    }
}