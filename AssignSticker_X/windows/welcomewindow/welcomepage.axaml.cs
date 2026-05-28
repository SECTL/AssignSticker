using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;

namespace AssignSticker_X.windows.welcomewindow;

public partial class welcomepage : UserControl
{
    private static readonly string[] Greetings = { "你好", "Hello", "Hallo", "Bonjour", "こんにちは", "안녕하세요", "مرحبًا", "שלום" };
    private int _currentIndex;
    private DispatcherTimer _cycleTimer = null!;
    private TextBlock _active = null!;
    private TextBlock _inactive = null!;

    public welcomepage()
    {
        InitializeComponent();
        GreetingTextA.Text = Greetings[0];
        _active = GreetingTextA;
        _inactive = GreetingTextB;
        _active.Opacity = 1;
        _inactive.Opacity = 0;
        StartButton.Click += StartButton_Click;
        _cycleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
        _cycleTimer.Tick += async (_, _) => await SlideToNext();
        _cycleTimer.Start();
    }

    private async System.Threading.Tasks.Task SlideToNext()
    {
        _cycleTimer.Stop();
        var nextIdx = (_currentIndex + 1) % Greetings.Length;
        _inactive.Text = Greetings[nextIdx];
        _inactive.Opacity = 0;
        _inactive.RenderTransform = new TranslateTransform(30, 0);

        var duration = 400;
        var interval = 16;
        var steps = duration / interval;
        for (int i = 1; i <= steps; i++)
        {
            var t = (double)i / steps;
            _active.Opacity = 1 - t;
            _active.RenderTransform = new TranslateTransform(-30 * t, 0);
            _inactive.Opacity = t;
            _inactive.RenderTransform = new TranslateTransform(30 * (1 - t), 0);
            await System.Threading.Tasks.Task.Delay(interval);
        }

        _active.Opacity = 0;
        (_active, _inactive) = (_inactive, _active);
        _currentIndex = nextIdx;
        _cycleTimer.Start();
    }

    public event Action? StartClicked;

    private void StartButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _cycleTimer.Stop();
        StartClicked?.Invoke();
    }
}