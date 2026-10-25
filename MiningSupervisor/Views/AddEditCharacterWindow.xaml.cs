using System.Windows;

namespace MiningSupervisor.Views;

public partial class AddEditCharacterWindow : Window
{
    public string CharacterName { get; private set; } = "";
    public double CapacityM3 { get; private set; }

    public AddEditCharacterWindow(IEnumerable<string> availableNames, string? name = null, double capacityM3 = 11500)
    {
        InitializeComponent();
        NameBox.ItemsSource = availableNames.ToList();
        NameBox.Text = name ?? "";
        CapacityBox.Text = capacityM3.ToString("0.##");
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = (NameBox.Text ?? "").Trim();
        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show(this, "Please select or enter a character name.", "Character", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!double.TryParse(CapacityBox.Text, out var capacity) || capacity <= 0)
        {
            MessageBox.Show(this, "Please enter a valid cargo capacity.", "Character", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        CharacterName = name;
        CapacityM3 = capacity;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
