using System.Text.Json.Serialization;

namespace Pickwise.Models;

public sealed record Champion(
    [property: JsonPropertyName("championId")] int ChampionId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("searchText")] string SearchText)
{
    public string Label => $"{Name} ({ChampionId})";
}
