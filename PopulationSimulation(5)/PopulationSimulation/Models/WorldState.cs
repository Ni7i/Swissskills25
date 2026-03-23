using System.Text.Json.Serialization;

namespace PopulationSimulation.Models;

public class WorldState
{
    [JsonPropertyName("locations")]
    public List<Location> Locations { get; set; } = new();

    [JsonPropertyName("citizens")]
    public List<Citizen> Citizens { get; set; } = new();
}
