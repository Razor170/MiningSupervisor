using System.Windows;
using System.Windows.Media;
using MiningSupervisor.Models;

namespace MiningSupervisor.Views;

public partial class SettingsWindow : Window
{
    private readonly AppState _state;
    private readonly Action _addCharacter;
    private readonly Action _setAllCargo;

    public bool StayOnTop => StayOnTopBox.IsChecked == true;

    public SettingsWindow(AppState state, bool stayOnTop, Action addCharacter, Action setAllCargo)
    {
        InitializeComponent();
        _state = state;
        _addCharacter = addCharacter;
        _setAllCargo = setAllCargo;
        StayOnTopBox.IsChecked = stayOnTop;

        WarningThresholdBox.Text = state.WarningThresholdPercent.ToString("0.#");
        WarningColorBox.Text = state.WarningColorHex;
        WarningAudioBox.IsChecked = state.WarningAudioEnabled;

        CriticalThresholdBox.Text = state.CriticalThresholdPercent.ToString("0.#");
        CriticalColorBox.Text = state.CriticalColorHex;
        CriticalAudioBox.IsChecked = state.CriticalAudioEnabled;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!double.TryParse(WarningThresholdBox.Text, out var warningThreshold) || warningThreshold is < 0 or > 100)
        {
            ShowError("Please enter a valid warning threshold between 0 and 100.");
            return;
        }

        if (!double.TryParse(CriticalThresholdBox.Text, out var criticalThreshold) || criticalThreshold is < 0 or > 100)
        {
            ShowError("Please enter a valid critical threshold between 0 and 100.");
            return;
        }

        if (criticalThreshold < warningThreshold)
        {
            ShowError("The critical threshold must be greater than or equal to the warning threshold.");
            return;
        }

        if (!TryParseColor(WarningColorBox.Text, out var warningColorHex))
        {
            ShowError("Please enter a valid warning color, e.g. #FFCC00.");
            return;
        }

        if (!TryParseColor(CriticalColorBox.Text, out var criticalColorHex))
        {
            ShowError("Please enter a valid critical color, e.g. #FF3333.");
            return;
        }

        _state.WarningThresholdPercent = warningThreshold;
        _state.WarningColorHex = warningColorHex;
        _state.WarningAudioEnabled = WarningAudioBox.IsChecked == true;

        _state.CriticalThresholdPercent = criticalThreshold;
        _state.CriticalColorHex = criticalColorHex;
        _state.CriticalAudioEnabled = CriticalAudioBox.IsChecked == true;

        DialogResult = true;
    }

    private void AddCharacter_Click(object sender, RoutedEventArgs e) => _addCharacter();

    private void SetAllCargo_Click(object sender, RoutedEventArgs e) => _setAllCargo();

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void ShowError(string message)
    {
        MessageBox.Show(this, message, "Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private static bool TryParseColor(string text, out string normalizedHex)
    {
        text = text.Trim();
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(text)!;
            normalizedHex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            return true;
        }
        catch
        {
            normalizedHex = "";
            return false;
        }
    }
}
