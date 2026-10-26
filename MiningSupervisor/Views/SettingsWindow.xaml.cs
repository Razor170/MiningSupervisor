using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using MiningSupervisor.Models;
using MiningSupervisor.ViewModels;

namespace MiningSupervisor.Views;

public partial class SettingsWindow : Window
{
    private readonly AppState _state;
    private readonly Action _addCharacter;
    private readonly Action _setAllCargo;

    private static readonly double[] PollIntervals = { 0.5, 1, 2, 3, 4, 5 };

    private readonly ObservableCollection<CharacterSetting> _characterSettings = new();

    public bool StayOnTop => StayOnTopBox.IsChecked == true;

    /// <summary>Characters the user marked for deletion; the caller removes them after a successful save.</summary>
    public List<CharacterCardViewModel> CharactersToDelete { get; } = new();

    public class CharacterSetting
    {
        public CharacterCardViewModel Character { get; init; } = null!;
        public string Name => Character.Name;
        public bool Visible { get; set; }
    }

    public SettingsWindow(AppState state, bool stayOnTop, IEnumerable<CharacterCardViewModel> characters,
        Action addCharacter, Action setAllCargo)
    {
        InitializeComponent();
        _state = state;
        _addCharacter = addCharacter;
        _setAllCargo = setAllCargo;
        StayOnTopBox.IsChecked = stayOnTop;

        foreach (var character in characters)
        {
            _characterSettings.Add(new CharacterSetting { Character = character, Visible = !character.IsHidden });
        }

        CharacterSettingsList.ItemsSource = _characterSettings;

        foreach (var interval in PollIntervals)
        {
            PollIntervalBox.Items.Add($"{interval:0.#}s");
        }

        var index = Array.IndexOf(PollIntervals, state.PollIntervalSeconds);
        PollIntervalBox.SelectedIndex = index >= 0 ? index : Array.IndexOf(PollIntervals, 2.0);
        LogDirectoryBox.Text = state.LogDirectory;

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

        var logDirectory = LogDirectoryBox.Text.Trim();
        if (!System.IO.Directory.Exists(logDirectory))
        {
            ShowError("The game log folder does not exist.");
            return;
        }

        _state.LogDirectory = logDirectory;
        _state.PollIntervalSeconds = PollIntervals[PollIntervalBox.SelectedIndex];

        foreach (var setting in _characterSettings)
        {
            setting.Character.IsHidden = !setting.Visible;
        }

        _state.WarningThresholdPercent = warningThreshold;
        _state.WarningColorHex = warningColorHex;
        _state.WarningAudioEnabled = WarningAudioBox.IsChecked == true;

        _state.CriticalThresholdPercent = criticalThreshold;
        _state.CriticalColorHex = criticalColorHex;
        _state.CriticalAudioEnabled = CriticalAudioBox.IsChecked == true;

        DialogResult = true;
    }

    private void BrowseLogDirectory_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select the EVE game log folder",
            InitialDirectory = System.IO.Directory.Exists(LogDirectoryBox.Text) ? LogDirectoryBox.Text : null
        };

        if (dialog.ShowDialog(this) == true)
        {
            LogDirectoryBox.Text = dialog.FolderName;
        }
    }

    private void DeleteCharacter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: CharacterSetting setting })
        {
            return;
        }

        var result = MessageBox.Show(this,
            $"Delete {setting.Name}? Their tracked cargo will be lost (applied when you press Save).",
            "Delete Character", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            _characterSettings.Remove(setting);
            CharactersToDelete.Add(setting.Character);
        }
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
