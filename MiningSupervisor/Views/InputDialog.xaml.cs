using System.Windows;

namespace MiningSupervisor.Views;

public partial class InputDialog : Window
{
    public double Value { get; private set; }

    public InputDialog(string prompt, double initialValue = 0)
    {
        InitializeComponent();
        PromptText.Text = prompt;
        ValueBox.Text = initialValue.ToString("0.##");
        ValueBox.SelectAll();
        Loaded += (_, _) => ValueBox.Focus();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (!double.TryParse(ValueBox.Text, out var value) || value < 0)
        {
            MessageBox.Show(this, "Please enter a valid non-negative number.", "Mining Supervisor", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Value = value;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
