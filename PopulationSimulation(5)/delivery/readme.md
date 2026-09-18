# Population Simulation Prototype

## How to Run
1. Navigate to the `executable` folder
2. Run `PopulationSimulation.exe`

## Build from Source (requires .NET 10 SDK)
```bash
cd source
dotnet restore PopulationSimulation.sln
dotnet run --project PopulationSimulation/PopulationSimulation.csproj
```

## Publish for Windows
```bash
cd source
dotnet publish PopulationSimulation/PopulationSimulation.csproj -c Release -r win-x64 --self-contained -o ../executable
```

## Technology
- C# / .NET 10
- Avalonia UI 11.3 (cross-platform desktop framework, WPF-equivalent)
- MVVM Pattern
- System.Text.Json for serialization
- No database (in-memory only, JSON load/save)

## Assumptions
- Coordinates are in meters (0–10000 for a 10km × 10km map)
- Travel speed: 5 km/h = 83.33 m/min
- 1 simulation step = 1 minute
- Citizens start at their home location when a world is loaded
- The arrival step itself counts as travel; spending begins the next step
- Daily schedule repeats every 24 hours (same every day)
- The first event of the day departs from the last event's location (wrap-around)
- Unicode emoji characters used for icons (platform emoji fonts)

## Features
- **Load/Save**: Open/Save buttons, JSON file dialogs
- **Simulation**: ▶/⏸ play/pause, "+ step" single step, ~60 steps/sec
- **People list**: Scrollable, select to edit, highlights citizen on map
- **Citizen editor**: Icon (clickable), first/last name, home (all locations shown), live status
- **Schedule editor**: Sorted by time, editable time/location/activity, autocomplete activities, auto-calculated travel/spend/total, conflict highlighting (red), conflicts prevent saving
- **Locations list**: Scrollable, select to edit, highlights on map  
- **Location editor**: Icon (clickable), name (unique), coordinates (0-10000, clamped)
- **Map**: 1:1 square, icons scaled to avoid overlap >250m, double-buffered rendering
- **Safety**: All editing disabled during simulation, deletion confirmation dialogs, global exception handling, coordinate validation
