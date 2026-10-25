using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using MiningSupervisor.Models;

namespace MiningSupervisor.Views;

public partial class StatisticsWindow : Window
{
    private readonly SessionStats _stats;
    private readonly DispatcherTimer _timer;

    public StatisticsWindow(SessionStats stats)
    {
        InitializeComponent();
        _stats = stats;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Refresh();
        Closed += (_, _) => _timer.Stop();
        _timer.Start();
        Refresh();
    }

    private void Refresh()
    {
        TotalText.Text = $"{_stats.TotalM3:N0} m³";
        ExtraText.Text = $"{_stats.ExtraM3:N0} m³ ({_stats.ExtraPercent:0.#}%)";
        LossText.Text = $"{_stats.LossM3:N0} m³ ({_stats.LossPercent:0.#}%)";

        var length = _stats.Length;
        LengthText.Text = $"{(int)length.TotalHours}h {length.Minutes}m";
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _stats.Reset();
        Refresh();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
