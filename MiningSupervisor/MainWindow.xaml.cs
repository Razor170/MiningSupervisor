using System.Windows;
using System.Windows.Controls;
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
        Closing += (_, _) => _viewModel.SaveNow();

        Topmost = _viewModel.StayOnTop;
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
        dialog = new SettingsWindow(_viewModel.State, _viewModel.StayOnTop,
            () => AddCharacter(dialog!), () => SetAllCargo(dialog!)) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
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
