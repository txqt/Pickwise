using System.Text.Json;
using Pickwise.Models;

namespace Pickwise.Services;

public sealed class ChampionCatalog
{
    private readonly List<Champion> _champions;

    public ChampionCatalog() : this(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "champions.json"))
    {
    }

    public ChampionCatalog(string path)
    {
        using var stream = File.OpenRead(path);
        _champions = JsonSerializer.Deserialize<List<Champion>>(stream) ?? [];
    }

    public IReadOnlyList<Champion> All => _champions;

    public IReadOnlyList<Champion> Search(string query)
        => Filter(query, "All");

    public IReadOnlyList<Champion> Filter(string query, string? role)
    {
        var matches = _champions.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(role) && role != "All")
        {
            matches = matches.Where(champion => champion.Tags.Contains(role));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalized = query.Trim().ToLowerInvariant();
            matches = matches.Where(champion => champion.SearchText.Contains(normalized));
        }

        return matches
            .Take(string.IsNullOrWhiteSpace(query) && (string.IsNullOrWhiteSpace(role) || role == "All") ? 20 : 30)
            .ToList();
    }
}
