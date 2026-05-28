using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AssignSticker_X.windows.welcomewindow.views;

namespace AssignSticker_X.windows.welcomewindow;

public partial class navigationview : UserControl
{
    private readonly List<UserControl> _pages;
    private int _currentPage;

    public navigationview()
    {
        InitializeComponent();
        _pages = new List<UserControl>
        {
            new appearance(),
            new subject(),
            new successfully()
        };
        _currentPage = -1;
        NavView.SelectedItem = NavView.MenuItems[0];
        ShowPage(0);
    }

    public event Action? SetupCompleted;

    private void ShowPage(int index)
    {
        if (index == _currentPage) return;
        _currentPage = index;
        PageContent.Content = _pages[index];
        PrevButton.IsVisible = index == 1;
        NextButton.IsVisible = index < 2;
        FinishButton.IsVisible = index == 2;
    }

    private void PrevButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_currentPage > 0)
        {
            var idx = _currentPage - 1;
            ShowPage(idx);
            NavView.SelectedItem = NavView.MenuItems[idx];
        }
    }

    private void NextButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_currentPage < 2)
        {
            var idx = _currentPage + 1;
            ShowPage(idx);
            NavView.SelectedItem = NavView.MenuItems[idx];
        }
    }

    private void FinishButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SetupCompleted?.Invoke();
    }
}
