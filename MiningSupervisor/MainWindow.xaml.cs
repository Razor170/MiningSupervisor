using System.Windows;
using System.Windows.Controls;
using MiningSupervisor.Services;
using MiningSupervisor.ViewModels;
using MiningSupervisor.Views;

namespace MiningSupervisor;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        CharacterList.ItemsSource = _viewModel.Characters;
        WindowPlacementHelper.Attach(this, _viewModel.State, "Main");
        Closing += (_, _) => _viewModel.SaveNow();
        Closed += (_, _) =>
        {
            _statisticsWindow?.Close();
            _surveyWindow?.Close();
        };

        Topmost = _viewModel.StayOnTop;
    }

    private StatisticsWindow? _statisticsWindow;

    private void Statistics_Click(object sender, RoutedEventArgs e)
    {
        if (_statisticsWindow != null)
        {
            _statisticsWindow.Activate();
            return;
        }

        _statisticsWindow = new StatisticsWindow(_viewModel.Stats) { Topmost = Topmost };
        WindowPlacementHelper.Attach(_statisticsWindow, _viewModel.State, "Statistics", _viewModel.SaveNow);
        _statisticsWindow.Closed += (_, _) => _statisticsWindow = null;
        _statisticsWindow.Show();
    }

    private readonly SurveyTracker _surveyTracker = new();
    private SurveyWindow? _surveyWindow;

    private void Survey_Click(object sender, RoutedEventArgs e)
    {
        if (_surveyWindow != null)
        {
            _surveyWindow.Activate();
            return;
        }

        _surveyWindow = new SurveyWindow(_surveyTracker) { Topmost = Topmost };
        WindowPlacementHelper.Attach(_surveyWindow, _viewModel.State, "Survey", _viewModel.SaveNow);
        _surveyWindow.Closed += (_, _) => _surveyWindow = null;
        _surveyWindow.Show();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void ClearAllCargo_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Characters.Count == 0)
        {
            return;
        }

        var result = MessageBox.Show(this,
            "Clear cargo for all characters?",
            "Clear All Cargo", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            _viewModel.ClearAllCargo();
        }
    }

    private void SetAllCargo(Window owner)
    {
        if (_viewModel.Characters.Count == 0)
        {
            return;
        }

        var dialog = new InputDialog("Set current cargo (m³) for all characters:") { Owner = owner };
        if (dialog.ShowDialog() == true)
        {
            _viewModel.SetAllCargo(dialog.Value);
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        SettingsWindow? dialog = null;
        dialog = new SettingsWindow(_viewModel.State, _viewModel.StayOnTop, _viewModel.Characters.ToList(),
            () => AddCharacter(dialog!), () => SetAllCargo(dialog!)) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            foreach (var character in dialog.CharactersToDelete)
            {
                _viewModel.RemoveCharacter(character);
            }

            Topmost = dialog.StayOnTop;
            _viewModel.StayOnTop = dialog.StayOnTop;
            _viewModel.NotifySettingsChanged();
        }
    }

    private void AddCharacter(Window owner)
    {
        var existingNames = new HashSet<string>(_viewModel.Characters.Select(c => c.Name), StringComparer.OrdinalIgnoreCase);
        var availableNames = _viewModel.GetAvailableCharacterNames().Where(n => !existingNames.Contains(n));

        var dialog = new AddEditCharacterWindow(availableNames) { Owner = owner };
        if (dialog.ShowDialog() == true)
        {
            _viewModel.AddCharacter(dialog.CharacterName, dialog.CapacityM3);
        }
    }

    private void EditCharacter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: CharacterCardViewModel vm })
        {
            return;
        }

        var existingNames = new HashSet<string>(_viewModel.Characters.Select(c => c.Name), StringComparer.OrdinalIgnoreCase);
        existingNames.Remove(vm.Name);
        var availableNames = _viewModel.GetAvailableCharacterNames().Where(n => !existingNames.Contains(n));

        var dialog = new AddEditCharacterWindow(availableNames, vm.Name, vm.CapacityM3) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            vm.Name = dialog.CharacterName;
            vm.CapacityM3 = dialog.CapacityM3;
        }
    }

    private void RemoveCharacter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: CharacterCardViewModel vm })
        {
            return;
        }

        var result = MessageBox.Show(this,
            $"Remove {vm.Name}? Their tracked cargo will be lost.",
            "Remove Character", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            _viewModel.RemoveCharacter(vm);
        }
    }
}
