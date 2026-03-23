using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PopulationSimulation.Views;

public partial class IconPickerDialog : Window
{
    public string? SelectedIcon { get; private set; }

    public IconPickerDialog() { InitializeComponent(); }

    public IconPickerDialog(string[] icons, string currentIcon) : this()
    {
        foreach (var icon in icons)
        {
            var btn = new Button { Content = icon, FontSize = 24, Width = 48, Height = 48, Margin = new Avalonia.Thickness(3) };
            btn.Click += (_, _) => { SelectedIcon = icon; Close(true); };
            IconPanel.Children.Add(btn);
        }
    }

    private void Cancel_Click(object? s, RoutedEventArgs e) => Close(false);
}
