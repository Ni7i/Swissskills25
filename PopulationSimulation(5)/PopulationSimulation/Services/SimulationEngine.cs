using PopulationSimulation.Models;
using System.Collections.ObjectModel;

namespace PopulationSimulation.Services;

public class SimulationEngine
{
    // 5 km/h = 5000m per 60min = 83.333 m/min
    public const double SpeedMetersPerMinute = 5000.0 / 60.0;

    private readonly ObservableCollection<Location> _locations;
    private readonly ObservableCollection<Citizen> _citizens;

    public DateTime SimulationDateTime { get; set; }

    public SimulationEngine(ObservableCollection<Location> locations, ObservableCollection<Citizen> citizens)
    {
        _locations = locations;
        _citizens = citizens;
    }

    /// <summary>
    /// Recalculate travel/spend/total times and conflict flags for a citizen's schedule.
    /// </summary>
    public static void RecalculateSchedule(Citizen citizen, IList<Location> locations)
    {
        if (citizen.Schedule.Count == 0) return;

        var sorted = citizen.Schedule.OrderBy(e => e.StartMinutes).ToList();

        for (int i = 0; i < sorted.Count; i++)
        {
            var evt = sorted[i];
            var nextEvt = sorted[(i + 1) % sorted.Count];

            // Departure: previous event's target, or for first event: last event's target (wrap-around day)
            string depName;
            if (i == 0)
                depName = sorted[^1].LocationName;
            else
                depName = sorted[i - 1].LocationName;

            var depLoc = locations.FirstOrDefault(l => l.Name == depName)
                         ?? locations.FirstOrDefault(l => l.Name == citizen.HomeName);
            var targetLoc = locations.FirstOrDefault(l => l.Name == evt.LocationName);

            int travelMin = 0;
            if (depLoc != null && targetLoc != null && depLoc.Name != targetLoc.Name)
            {
                double dist = Location.DistanceMeters(depLoc.X, depLoc.Y, targetLoc.X, targetLoc.Y);
                travelMin = (int)Math.Ceiling(dist / SpeedMetersPerMinute);
            }
            evt.TravelMinutes = travelMin;

            // Total time until next event
            int thisStart = evt.StartMinutes;
            int nextStart = nextEvt.StartMinutes;
            int totalMin;
            if (i == sorted.Count - 1)
                totalMin = (1440 - thisStart) + nextStart;
            else
                totalMin = nextStart - thisStart;
            if (totalMin <= 0) totalMin += 1440;

            evt.SpendMinutes = Math.Max(0, totalMin - travelMin);
            evt.IsConflict = travelMin > totalMin;
        }
    }

    public static bool HasScheduleConflict(Citizen citizen)
        => citizen.Schedule.Any(e => e.IsConflict);

    /// <summary>Advance one step = 1 minute.</summary>
    public void Step()
    {
        SimulationDateTime = SimulationDateTime.AddMinutes(1);
        int currentMinute = SimulationDateTime.Hour * 60 + SimulationDateTime.Minute;
        foreach (var citizen in _citizens)
            UpdateCitizen(citizen, currentMinute);
    }

    /// <summary>Place all citizens at home.</summary>
    public void InitializeCitizens()
    {
        foreach (var citizen in _citizens)
        {
            var home = _locations.FirstOrDefault(l => l.Name == citizen.HomeName);
            if (home != null)
            {
                citizen.CurrentX = home.X;
                citizen.CurrentY = home.Y;
            }
            citizen.IsTraveling = false;
            RecalculateSchedule(citizen, _locations);
            UpdateCitizen(citizen, 0);
        }
    }

    private void UpdateCitizen(Citizen citizen, int currentMinute)
    {
        if (citizen.Schedule.Count == 0)
        {
            citizen.Status = "No schedule";
            return;
        }

        var sorted = citizen.Schedule.OrderBy(e => e.StartMinutes).ToList();

        // Find which event is currently active
        int activeIdx = -1;
        for (int i = sorted.Count - 1; i >= 0; i--)
        {
            if (currentMinute >= sorted[i].StartMinutes)
            {
                activeIdx = i;
                break;
            }
        }
        // Before first event of the day => last event from "yesterday"
        if (activeIdx == -1) activeIdx = sorted.Count - 1;

        var activeEvt = sorted[activeIdx];

        // Departure location (where citizen was before this event)
        string depName;
        if (activeIdx == 0)
            depName = sorted[^1].LocationName;
        else
            depName = sorted[activeIdx - 1].LocationName;

        var depLoc = _locations.FirstOrDefault(l => l.Name == depName)
                     ?? _locations.FirstOrDefault(l => l.Name == citizen.HomeName);
        var targetLoc = _locations.FirstOrDefault(l => l.Name == activeEvt.LocationName);

        if (depLoc == null || targetLoc == null)
        {
            citizen.Status = "Unknown location";
            return;
        }

        int minSinceStart = currentMinute - activeEvt.StartMinutes;
        if (minSinceStart < 0) minSinceStart += 1440;

        double distance = Location.DistanceMeters(depLoc.X, depLoc.Y, targetLoc.X, targetLoc.Y);
        int travelMin = distance < 0.01 ? 0 : (int)Math.Ceiling(distance / SpeedMetersPerMinute);

        if (travelMin == 0 || depLoc.Name == targetLoc.Name)
        {
            // Same location or zero distance
            citizen.CurrentX = targetLoc.X;
            citizen.CurrentY = targetLoc.Y;
            citizen.IsTraveling = false;
            citizen.Status = $"{activeEvt.Activity} at {targetLoc}";
        }
        else if (minSinceStart <= travelMin)
        {
            // Still traveling. Arrival step itself = travel.
            double traveled = minSinceStart * SpeedMetersPerMinute;
            traveled = Math.Min(traveled, distance);
            double ratio = traveled / distance;
            citizen.CurrentX = depLoc.X + (targetLoc.X - depLoc.X) * ratio;
            citizen.CurrentY = depLoc.Y + (targetLoc.Y - depLoc.Y) * ratio;
            citizen.IsTraveling = true;
            citizen.Status = $"Traveling to {targetLoc}";
        }
        else
        {
            // Arrived, spending time
            citizen.CurrentX = targetLoc.X;
            citizen.CurrentY = targetLoc.Y;
            citizen.IsTraveling = false;
            citizen.Status = $"{activeEvt.Activity} at {targetLoc}";
        }
    }
}
