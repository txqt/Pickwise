using System.Text.Json;

namespace Pickwise.Services;

public sealed class ChampionPreferenceStore(string? path = null)
{
    private readonly string _path = path ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Pickwise",
        "champion-preferences.json");

    public ChampionPreferences Load()
    {
        try
        {
            return File.Exists(_path)
                ? JsonSerializer.Deserialize<ChampionPreferences>(File.ReadAllText(_path)) ?? new()
                : new();
        }
        catch
        {
            return new();
        }
    }

    public void Save(ChampionPreferences preferences)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(preferences));
    }
}

public sealed class ChampionPreferences
{
    public List<int> Favorites { get; set; } = [];
    public List<int> QuickBans { get; set; } = [];
}
