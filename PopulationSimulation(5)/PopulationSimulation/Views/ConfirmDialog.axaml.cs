using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PopulationSimulation.Views;

public partial class ConfirmDialog : Window
{
    public bool Result { get; private set; }

    public ConfirmDialog() { InitializeComponent(); }

    public ConfirmDialog(string title, string message) : this()
    {
        Title = title;
        MessageText.Text = message;
    }

    private void Yes_Click(object? s, RoutedEventArgs e) { Result = true; Close(true); }
    private void No_Click(object? s, RoutedEventArgs e) { Result = false; Close(false); }
}
