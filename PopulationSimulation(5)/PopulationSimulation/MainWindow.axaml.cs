using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PopulationSimulation.ViewModels;
using PopulationSimulation.Views;

using MapLocation = PopulationSimulation.Models.Location;
using ScheduleEvent = PopulationSimulation.Models.ScheduleEvent;

namespace PopulationSimulation;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly DispatcherTimer _renderTimer;
    private bool _suppressHomeComboEvent;

    public MainWindow()
    {
        InitializeComponent();

        _vm = new MainViewModel();
        DataContext = _vm;

        // Wire ViewModel events → View dialogs
        _vm.OpenFileRequested += OpenFilePicker;
        _vm.SaveFileRequested += SaveFilePicker;
        _vm.ShowMessageRequested += ShowMsg;
        _vm.ConfirmRequested += ShowConfirm;
        _vm.IconPickerRequested += ShowIconPicker;
        _vm.ExitRequested += (_, _) => Close();
        _vm.MapRefreshRequested += (_, _) => MapDisplay.Refresh();

        // When citizen selection changes, sync home combobox
        _vm.CitizenSelectionChanged += (_, _) => SyncHomeCombo();

        // Periodic map refresh during simulation
        _renderTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _renderTimer.Tick += (_, _) => { if (_vm.IsRunning) MapDisplay.Refresh(); };
        _renderTimer.Start();
    }

    // ---- Home ComboBox sync ----
    private void SyncHomeCombo()
    {
        if (_vm.SelectedCitizen == null) return;
        _suppressHomeComboEvent = true;
        var home = _vm.Locations.FirstOrDefault(l => l.Name == _vm.SelectedCitizen.HomeName);
        HomeComboBox.SelectedItem = home;
        _suppressHomeComboEvent = false;
    }

    private void HomeCombo_Changed(object? s, SelectionChangedEventArgs e)
    {
        if (_suppressHomeComboEvent) return;
        if (_vm.SelectedCitizen != null && HomeComboBox.SelectedItem is MapLocation loc)
        {
            _vm.SelectedCitizen.HomeName = loc.Name;
            _vm.RecalculateAndRefreshSchedule();
            MapDisplay.Refresh();
        }
    }

    // ---- Schedule location ComboBox: set initial value when loaded ----
    private void ScheduleLocCombo_Loaded(object? s, RoutedEventArgs e)
    {
        if (s is ComboBox combo && combo.DataContext is ScheduleEvent evt)
        {
            var loc = _vm.Locations.FirstOrDefault(l => l.Name == evt.LocationName);
            if (loc != null && combo.SelectedItem != loc)
                combo.SelectedItem = loc;
        }
    }

    private void ScheduleLoc_Changed(object? s, SelectionChangedEventArgs e)
    {
        if (s is ComboBox combo && combo.SelectedItem is MapLocation loc && combo.DataContext is ScheduleEvent evt)
        {
            if (evt.LocationName != loc.Name)
            {
                evt.LocationName = loc.Name;
                _vm.RecalculateAndRefreshSchedule();
                MapDisplay.Refresh();
            }
        }
    }

    // ---- Schedule field changes ----
    private void Schedule_LostFocus(object? s, RoutedEventArgs e)
    {
        _vm.RecalculateAndRefreshSchedule();
        MapDisplay.Refresh();
    }

    // ---- Location editor ----
    private async void LocName_LostFocus(object? s, RoutedEventArgs e)
    {
        if (_vm.SelectedLocationEditor == null) return;
        if (!_vm.IsLocationNameUnique(_vm.SelectedLocationEditor.Name, _vm.SelectedLocationEditor))
            await ShowMsg("Validation", "Location name must be unique!");
        _vm.RecalculateAndRefreshSchedule();
        MapDisplay.Refresh();
    }

    private void LocCoord_LostFocus(object? s, RoutedEventArgs e)
    {
        // Coordinates are auto-clamped by the model (0-10000)
        _vm.RecalculateAndRefreshSchedule();
        MapDisplay.Refresh();
    }

    // ---- File dialogs ----
    private async Task<string?> OpenFilePicker()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Load World State",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } },
                new FilePickerFileType("All") { Patterns = new[] { "*" } }
            }
        });
        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    private async Task<string?> SaveFilePicker()
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save World State",
            SuggestedFileName = "world.json",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } },
                new FilePickerFileType("All") { Patterns = new[] { "*" } }
            }
        });
        return file?.TryGetLocalPath();
    }

    // ---- Message / Confirm ----
    private async Task ShowMsg(string title, string msg)
    {
        var dlg = new ConfirmDialog(title, msg);
        await dlg.ShowDialog(this);
    }

    private async Task<bool> ShowConfirm(string title, string msg)
    {
        var dlg = new ConfirmDialog(title, msg);
        await dlg.ShowDialog(this);
        return dlg.Result;
    }

    // ---- Icon picker ----
    private async void ShowIconPicker(object? s, IconPickerEventArgs e)
    {
        var dlg = new IconPickerDialog(e.AvailableIcons, e.CurrentIcon);
        var result = await dlg.ShowDialog<bool?>(this);
        if (result == true) e.SelectedIcon = dlg.SelectedIcon;
    }
}
