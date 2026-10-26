using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using MiningSupervisor.Services;

namespace MiningSupervisor.Views;

public partial class SurveyWindow : Window
{
    [DllImport("user32.dll")]
    private static extern uint GetClipboardSequenceNumber();

    private readonly SurveyTracker _tracker;
    private readonly DispatcherTimer _timer;
    private uint _lastClipboardSequence;

    public SurveyWindow(SurveyTracker tracker)
    {
        InitializeComponent();
        _tracker = tracker;

        // Ignore whatever is on the clipboard right now; only react to new copies.
        _lastClipboardSequence = GetClipboardSequenceNumber();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timer.Tick += (_, _) => PollClipboard();
        Closed += (_, _) => _timer.Stop();
        _timer.Start();

        Refresh();
    }

    private void PollClipboard()
    {
        var sequence = GetClipboardSequenceNumber();
        if (sequence == _lastClipboardSequence)
        {
            return;
        }

        _lastClipboardSequence = sequence;

        string text;
        try
        {
            if (!Clipboard.ContainsText())
            {
                return;
            }

            text = Clipboard.GetText();
        }
        catch
        {
            // Clipboard can be locked by another process; treat as no scan.
            return;
        }

        if (_tracker.TryIngest(text, DateTime.Now))
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        var scan = _tracker.Current;
        HintText.Visibility = scan == null ? Visibility.Visible : Visibility.Collapsed;
        ResultPanel.Visibility = scan == null ? Visibility.Collapsed : Visibility.Visible;
        if (scan == null)
        {
            RowList.ItemsSource = null;
            return;
        }

        TotalText.Text = $"{scan.TotalM3:N0} m³";
        RateText.Text = _tracker.RatePerMinute is { } rate
            ? $"{rate / 60:N1} m³/s | {rate * 60:N0} m³/h"
            : "–";

        if (_tracker.TimeToDepletion is { } left)
        {
            DepletionText.Text = $"{(int)left.TotalHours}h {left.Minutes}m";
        }
        else
        {
            DepletionText.Text = "–";
        }

        RowList.ItemsSource = scan.Rows;
    }

    private void DetailsToggle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var show = RowList.Visibility != Visibility.Visible;
        RowList.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        DetailsToggle.Text = show ? "▾ Details" : "▸ Details";

        // Don't let the click start a window drag.
        e.Handled = true;
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _tracker.Reset();
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
