using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using PopulationSimulation.Models;
using PopulationSimulation.Services;
using Avalonia.Threading;

namespace PopulationSimulation.ViewModels;

public class MainViewModel : BaseViewModel
{
    public ObservableCollection<Location> Locations { get; } = new();
    public ObservableCollection<Citizen> Citizens { get; } = new();
    public ObservableCollection<string> ActivitySuggestions { get; } = new();

    private SimulationEngine _engine;
    private readonly DispatcherTimer _timer;
    private bool _isRunning;
    private DateTime _simulationDateTime = DateTime.Today;
    private Citizen? _selectedCitizen;
    private Location? _selectedLocationEditor;

    // ---- Icons (from competition spec) ----
    public static readonly string[] CitizenIcons =
    {
        "👨", "👩", "👴", "👵", "👦", "👧", "🧑", "👮", "👷", "💂",
        "🕵", "🧙", "🧝", "🧛", "🧟", "👨\u200d⚕️", "👩\u200d🍳", "👩\u200d🎓",
        "👩\u200d🏫", "👩\u200d💻", "👨\u200d🔧", "👨\u200d🍳", "👨\u200d🎓", "👨\u200d💼"
    };
    public static readonly string[] LocationIcons =
    {
        "🏠", "🏡", "🏢", "🏣", "🏤", "🏥", "🏦", "🏨", "🏩", "🏪",
        "🏫", "🏬", "🏭", "🏯", "🏰", "⛪", "🕌", "🛕", "⛩", "🕍",
        "🛎", "⛽", "🏗", "🏘", "🏙", "🌳", "🌲", "⛰", "🏔"
    };

    // ---- Properties ----
    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (SetProperty(ref _isRunning, value))
            {
                OnPropertyChanged(nameof(IsNotRunning));
                OnPropertyChanged(nameof(PlayPauseText));
                RaiseAllCanExecute();
            }
        }
    }
    public bool IsNotRunning => !IsRunning;
    public string PlayPauseText => IsRunning ? "⏸" : "▶";

    public DateTime SimulationDateTime
    {
        get => _simulationDateTime;
        set { SetProperty(ref _simulationDateTime, value); OnPropertyChanged(nameof(SimDateFormatted)); }
    }
    public string SimDateFormatted => SimulationDateTime.ToString("dd.MM.yyyy HH:mm");

    public Citizen? SelectedCitizen
    {
        get => _selectedCitizen;
        set
        {
            if (SetProperty(ref _selectedCitizen, value))
            {
                OnPropertyChanged(nameof(HasSelectedCitizen));
                OnPropertyChanged(nameof(SelectedScheduleSorted));
                if (value != null)
                    SimulationEngine.RecalculateSchedule(value, Locations);
                MapRefreshRequested?.Invoke(this, EventArgs.Empty);
                CitizenSelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
    public bool HasSelectedCitizen => SelectedCitizen != null;

    /// <summary>Returns the selected citizen's schedule sorted by start time.</summary>
    public IEnumerable<ScheduleEvent>? SelectedScheduleSorted =>
        SelectedCitizen?.Schedule.OrderBy(e => e.StartMinutes);

    public Location? SelectedLocationEditor
    {
        get => _selectedLocationEditor;
        set
        {
            if (SetProperty(ref _selectedLocationEditor, value))
            {
                OnPropertyChanged(nameof(HasSelectedLocationEditor));
                MapRefreshRequested?.Invoke(this, EventArgs.Empty);
            }
        }
    }
    public bool HasSelectedLocationEditor => SelectedLocationEditor != null;

    // ---- Commands ----
    public RelayCommand LoadWorldCommand { get; }
    public RelayCommand SaveWorldCommand { get; }
    public RelayCommand ExitCommand { get; }
    public RelayCommand StepCommand { get; }
    public RelayCommand PlayPauseCommand { get; }
    public RelayCommand AddScheduleEventCommand { get; }
    public RelayCommand RemoveScheduleEventCommand { get; }
    public RelayCommand ChangeCitizenIconCommand { get; }
    public RelayCommand ChangeLocationIconCommand { get; }

    // ---- Events for View layer ----
    public event EventHandler? MapRefreshRequested;
    public event EventHandler? CitizenSelectionChanged;
    public event EventHandler<IconPickerEventArgs>? IconPickerRequested;
    public event Func<Task<string?>>? OpenFileRequested;
    public event Func<Task<string?>>? SaveFileRequested;
    public event Func<string, string, Task>? ShowMessageRequested;
    public event Func<string, string, Task<bool>>? ConfirmRequested;
    public event EventHandler? ExitRequested;

    public MainViewModel()
    {
        _engine = new SimulationEngine(Locations, Citizens);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000.0 / 60.0) };
        _timer.Tick += (_, _) => DoStep();

        LoadWorldCommand = new RelayCommand(async () => await LoadWorld(), () => !IsRunning);
        SaveWorldCommand = new RelayCommand(async () => await SaveWorld(), () => !IsRunning);
        ExitCommand = new RelayCommand(() => ExitRequested?.Invoke(this, EventArgs.Empty));
        StepCommand = new RelayCommand(DoStep, () => !IsRunning);
        PlayPauseCommand = new RelayCommand(TogglePlayPause);
        AddScheduleEventCommand = new RelayCommand(AddScheduleEvent, () => SelectedCitizen != null && !IsRunning);
        RemoveScheduleEventCommand = new RelayCommand(p => _ = RemoveScheduleEvent(p), _ => !IsRunning);
        ChangeCitizenIconCommand = new RelayCommand(ChangeCitizenIcon, () => SelectedCitizen != null && !IsRunning);
        ChangeLocationIconCommand = new RelayCommand(ChangeLocationIcon, () => SelectedLocationEditor != null && !IsRunning);
    }

    private void RaiseAllCanExecute()
    {
        LoadWorldCommand.RaiseCanExecuteChanged();
        SaveWorldCommand.RaiseCanExecuteChanged();
        StepCommand.RaiseCanExecuteChanged();
        AddScheduleEventCommand.RaiseCanExecuteChanged();
        RemoveScheduleEventCommand.RaiseCanExecuteChanged();
        ChangeCitizenIconCommand.RaiseCanExecuteChanged();
        ChangeLocationIconCommand.RaiseCanExecuteChanged();
    }

    // ---- Simulation ----
    private void DoStep()
    {
        try
        {
            _engine.Step();
            SimulationDateTime = _engine.SimulationDateTime;
            // Live-update status of selected citizen
            if (SelectedCitizen != null)
                OnPropertyChanged(nameof(SelectedCitizen));
            MapRefreshRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception) { /* robust: never crash */ }
    }

    private void TogglePlayPause()
    {
        if (IsRunning) { _timer.Stop(); IsRunning = false; }
        else { _timer.Start(); IsRunning = true; }
    }

    // ---- Load / Save ----
    private async Task LoadWorld()
    {
        if (OpenFileRequested == null) return;
        var path = await OpenFileRequested.Invoke();
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            _timer.Stop();
            IsRunning = false;

            string json = await File.ReadAllTextAsync(path);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var world = JsonSerializer.Deserialize<WorldState>(json, opts);
            if (world == null)
            {
                if (ShowMessageRequested != null)
                    await ShowMessageRequested("Error", "Failed to parse world state.");
                return;
            }

            // Clear everything (no traces of old worlds)
            Locations.Clear();
            Citizens.Clear();
            ActivitySuggestions.Clear();
            SelectedCitizen = null;
            SelectedLocationEditor = null;

            foreach (var loc in world.Locations) Locations.Add(loc);
            foreach (var cit in world.Citizens) Citizens.Add(cit);

            RefreshActivitySuggestions();

            // Set date to today 00:00
            SimulationDateTime = DateTime.Today;
            _engine = new SimulationEngine(Locations, Citizens) { SimulationDateTime = SimulationDateTime };
            _engine.InitializeCitizens();
            MapRefreshRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            if (ShowMessageRequested != null)
                await ShowMessageRequested("Error", $"Load error:\n{ex.Message}");
        }
    }

    private async Task SaveWorld()
    {
        // Check for schedule conflicts before saving
        var conflictCitizens = Citizens.Where(c => SimulationEngine.HasScheduleConflict(c)).ToList();
        if (conflictCitizens.Count > 0)
        {
            var names = string.Join(", ", conflictCitizens.Select(c => c.DisplayName));
            if (ShowMessageRequested != null)
                await ShowMessageRequested("Cannot Save",
                    $"Schedule conflicts exist for: {names}\nFix unreachable destinations before saving.");
            return;
        }

        if (SaveFileRequested == null) return;
        var path = await SaveFileRequested.Invoke();
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            var world = new WorldState
            {
                Locations = Locations.ToList(),
                Citizens = Citizens.ToList()
            };
            var opts = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(world, opts));
        }
        catch (Exception ex)
        {
            if (ShowMessageRequested != null)
                await ShowMessageRequested("Error", $"Save error:\n{ex.Message}");
        }
    }

    // ---- Schedule editing ----
    private void AddScheduleEvent()
    {
        if (SelectedCitizen == null || IsRunning) return;
        SelectedCitizen.Schedule.Add(new ScheduleEvent
        {
            Time = "00:00",
            LocationName = Locations.FirstOrDefault()?.Name ?? "",
            Activity = "idle"
        });
        RecalculateAndRefreshSchedule();
    }

    private async Task RemoveScheduleEvent(object? param)
    {
        if (param is not ScheduleEvent evt || SelectedCitizen == null || IsRunning) return;
        bool confirmed = true;
        if (ConfirmRequested != null)
            confirmed = await ConfirmRequested("Confirm Delete", "Are you sure that you want to delete this schedule event?");
        if (confirmed)
        {
            SelectedCitizen.Schedule.Remove(evt);
            RecalculateAndRefreshSchedule();
        }
    }

    public void RecalculateAndRefreshSchedule()
    {
        if (SelectedCitizen == null) return;
        SimulationEngine.RecalculateSchedule(SelectedCitizen, Locations);
        // Re-raise sorted schedule so UI updates the sorted order
        OnPropertyChanged(nameof(SelectedScheduleSorted));
    }

    // ---- Icon pickers ----
    private void ChangeCitizenIcon()
    {
        if (SelectedCitizen == null || IsRunning) return;
        var args = new IconPickerEventArgs(CitizenIcons, SelectedCitizen.Icon);
        IconPickerRequested?.Invoke(this, args);
        if (args.SelectedIcon != null)
        {
            SelectedCitizen.Icon = args.SelectedIcon;
            MapRefreshRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ChangeLocationIcon()
    {
        if (SelectedLocationEditor == null || IsRunning) return;
        var args = new IconPickerEventArgs(LocationIcons, SelectedLocationEditor.Icon);
        IconPickerRequested?.Invoke(this, args);
        if (args.SelectedIcon != null)
        {
            SelectedLocationEditor.Icon = args.SelectedIcon;
            MapRefreshRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    // ---- Validation ----
    public bool IsLocationNameUnique(string name, Location? exclude = null)
        => !Locations.Any(l => l != exclude && l.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public void RefreshActivitySuggestions()
    {
        var acts = Citizens.SelectMany(c => c.Schedule).Select(s => s.Activity)
            .Where(a => !string.IsNullOrWhiteSpace(a)).Distinct().OrderBy(a => a).ToList();
        ActivitySuggestions.Clear();
        foreach (var a in acts) ActivitySuggestions.Add(a);
    }
}

public class IconPickerEventArgs : EventArgs
{
    public string[] AvailableIcons { get; }
    public string CurrentIcon { get; }
    public string? SelectedIcon { get; set; }
    public IconPickerEventArgs(string[] icons, string currentIcon)
    {
        AvailableIcons = icons;
        CurrentIcon = currentIcon;
    }
}
