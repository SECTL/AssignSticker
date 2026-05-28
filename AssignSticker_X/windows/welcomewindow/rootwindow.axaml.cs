using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AssignSticker_X.Utils;

namespace AssignSticker_X.windows.welcomewindow;

public partial class rootwindow : Window
{
    public rootwindow()
    {
        InitializeComponent();
        WelcomePage.StartClicked += OnWelcomeStartClicked;
        NavView.SetupCompleted += OnSetupCompleted;
    }

    private void OnWelcomeStartClicked()
    {
        WelcomePage.IsVisible = false;
        NavView.IsVisible = true;
    }

    private void OnSetupCompleted()
    {
        var finishesPath = Path.Combine(AppData.DataPath, "finishes");
        File.WriteAllText(finishesPath, "completed");
        Logger.Info("引导设置已完成，已创建 finishes 标记文件");

        var main = new MainWindow();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = main;
        main.Show();
        Close();
    }
}