using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace PopulationSimulation.Models;

public class Location : INotifyPropertyChanged
{
    private string _icon = "📍";
    private string _name = "";
    private double _x;
    private double _y;

    [JsonPropertyName("icon")]
    public string Icon
    {
        get => _icon;
        set { if (_icon != value) { _icon = value; OnPropertyChanged(); } }
    }

    [JsonPropertyName("name")]
    public string Name
    {
        get => _name;
        set { if (_name != value) { _name = value; OnPropertyChanged(); } }
    }

    [JsonPropertyName("x")]
    public double X
    {
        get => _x;
        set { var v = Math.Clamp(value, 0, 10000); if (Math.Abs(_x - v) > 0.001) { _x = v; OnPropertyChanged(); } }
    }

    [JsonPropertyName("y")]
    public double Y
    {
        get => _y;
        set { var v = Math.Clamp(value, 0, 10000); if (Math.Abs(_y - v) > 0.001) { _y = v; OnPropertyChanged(); } }
    }

    public static double DistanceMeters(double ax, double ay, double bx, double by)
    {
        double dx = bx - ax;
        double dy = by - ay;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public override string ToString() => $"{Icon} {Name}";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
