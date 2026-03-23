using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace PopulationSimulation.Models;

public class Citizen : INotifyPropertyChanged
{
    private string _icon = "👤";
    private string _firstName = "";
    private string _lastName = "";
    private string _homeName = "";
    private double _currentX;
    private double _currentY;
    private string _status = "";
    private bool _isTraveling;

    [JsonPropertyName("icon")]
    public string Icon
    {
        get => _icon;
        set { _icon = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
    }

    [JsonPropertyName("firstName")]
    public string FirstName
    {
        get => _firstName;
        set { _firstName = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
    }

    [JsonPropertyName("lastName")]
    public string LastName
    {
        get => _lastName;
        set { _lastName = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
    }

    [JsonPropertyName("home")]
    public string HomeName
    {
        get => _homeName;
        set { _homeName = value; OnPropertyChanged(); }
    }

    [JsonPropertyName("schedule")]
    public ObservableCollection<ScheduleEvent> Schedule { get; set; } = new();

    [JsonIgnore] public string DisplayName => $"{Icon} {FirstName} {LastName}";

    [JsonIgnore]
    public double CurrentX
    {
        get => _currentX;
        set { _currentX = value; OnPropertyChanged(); }
    }

    [JsonIgnore]
    public double CurrentY
    {
        get => _currentY;
        set { _currentY = value; OnPropertyChanged(); }
    }

    [JsonIgnore]
    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    [JsonIgnore]
    public bool IsTraveling
    {
        get => _isTraveling;
        set { _isTraveling = value; OnPropertyChanged(); }
    }

    public override string ToString() => DisplayName;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
