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
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return _champions.Take(20).ToList();
        }

        var normalized = query.Trim().ToLowerInvariant();
        return _champions
            .Where(champion => champion.SearchText.Contains(normalized))
            .Take(30)
            .ToList();
    }
}
