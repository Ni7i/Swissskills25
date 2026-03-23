using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace PopulationSimulation.Models;

public class ScheduleEvent : INotifyPropertyChanged
{
    private string _time = "00:00";
    private string _locationName = "";
    private string _activity = "";
    private int _travelMinutes;
    private int _spendMinutes;
    private bool _isConflict;

    [JsonPropertyName("time")]
    public string Time
    {
        get => _time;
        set { _time = value; OnPropertyChanged(); OnPropertyChanged(nameof(StartMinutes)); }
    }

    [JsonPropertyName("location")]
    public string LocationName
    {
        get => _locationName;
        set { _locationName = value; OnPropertyChanged(); }
    }

    [JsonPropertyName("activity")]
    public string Activity
    {
        get => _activity;
        set { _activity = value; OnPropertyChanged(); }
    }

    [JsonIgnore]
    public int TravelMinutes
    {
        get => _travelMinutes;
        set { _travelMinutes = value; OnPropertyChanged(); OnPropertyChanged(nameof(TravelFormatted)); OnPropertyChanged(nameof(TotalFormatted)); OnPropertyChanged(nameof(TotalMinutes)); }
    }

    [JsonIgnore]
    public int SpendMinutes
    {
        get => _spendMinutes;
        set { _spendMinutes = value; OnPropertyChanged(); OnPropertyChanged(nameof(SpendFormatted)); OnPropertyChanged(nameof(TotalFormatted)); OnPropertyChanged(nameof(TotalMinutes)); }
    }

    [JsonIgnore] public int TotalMinutes => TravelMinutes + SpendMinutes;
    [JsonIgnore] public string TravelFormatted => FormatDuration(TravelMinutes);
    [JsonIgnore] public string SpendFormatted => FormatDuration(SpendMinutes);
    [JsonIgnore] public string TotalFormatted => FormatDuration(TotalMinutes);

    [JsonIgnore]
    public bool IsConflict
    {
        get => _isConflict;
        set { _isConflict = value; OnPropertyChanged(); }
    }

    [JsonIgnore]
    public int StartMinutes
    {
        get
        {
            var parts = Time.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out int h) && int.TryParse(parts[1], out int m))
                return Math.Clamp(h, 0, 23) * 60 + Math.Clamp(m, 0, 59);
            return 0;
        }
    }

    public static string FormatDuration(int totalMinutes)
    {
        if (totalMinutes < 0) totalMinutes = 0;
        return $"{totalMinutes / 60}h{totalMinutes % 60:D2}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
