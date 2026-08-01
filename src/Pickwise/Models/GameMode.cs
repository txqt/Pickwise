using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pickwise.Models;

public sealed record GameMode(
    string Name,
    int QueueId,
    string Category = "PvP",
    string ModeGroup = "kSummonersRift",
    string GameModeCode = "CLASSIC",
    string Type = "NORMAL",
    int MapId = 11,
    string PickMode = "",
    bool IsCustom = false,
    bool ShowPositionSelector = false,
    bool ShowQuickPlaySlotSelection = false,
    int MaxLobbySize = 5,
    LcuGameTypeConfig? GameTypeConfig = null,
    string ShortName = "",
    string Description = "",
    bool IsRanked = false,
    bool HidePlayerPosition = false,
    string AssetMutator = "",
    int ChampionsRequiredToPlay = 0,
    bool AreFreeChampionsAllowed = true,
    string? MapAssetPath = null)
{
    public bool IsLoLClassic => QueueId == 4310;
    public bool IsJadeMode => string.Equals(GameModeCode, "JADE", StringComparison.OrdinalIgnoreCase)
        || ModeGroup == "kJade"
        || MapId == 453;

    public string Label => IsLoLClassic
        ? "LoL Classic"
        : !string.IsNullOrWhiteSpace(Description)
            ? Description
            : !string.IsNullOrWhiteSpace(Name)
                ? Name
                : !string.IsNullOrWhiteSpace(ShortName)
                    ? ShortName
                    : $"Queue {QueueId}";
    public string DisplayGroup =>
        IsBot ? "Co-op vs AI" :
        IsLoLClassic ? "LoL Classic" :
        IsCustom ? "Custom" :
        ModeGroup == "kSummonersRift" ? "Summoner's Rift" :
        ModeGroup == "kJade" ? "LoL Classic" :
        ModeGroup == "kARAM" ? "ARAM" :
        ModeGroup == "kTeamfightTactics" ? "TFT" :
        "Other";

    public bool IsTft => string.Equals(GameModeCode, "TFT", StringComparison.OrdinalIgnoreCase)
        || Type.Contains("TFT", StringComparison.OrdinalIgnoreCase);
    public bool IsBot => string.Equals(Category, "VersusAi", StringComparison.OrdinalIgnoreCase)
        || Type.Contains("BOT", StringComparison.OrdinalIgnoreCase);
}

public sealed record GameModeMapMetadata(
    [property: JsonPropertyName("mapId")] int MapId,
    [property: JsonPropertyName("gameMode")] string? GameMode,
    [property: JsonPropertyName("assets")] GameModeMapAssetCollection? Assets)
{
    public string? DefaultIconPath => Assets?.DefaultIconPath;
}

public sealed record GameModeMapAsset(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("path")] string? Path);

[JsonConverter(typeof(GameModeMapAssetCollectionConverter))]
public sealed class GameModeMapAssetCollection(IReadOnlyList<GameModeMapAsset> items)
{
    public IReadOnlyList<GameModeMapAsset> Items { get; } = items;

    public string? DefaultIconPath => Items
        .FirstOrDefault(asset => string.Equals(asset.Name, "game-select-icon-default", StringComparison.OrdinalIgnoreCase))
        ?.Path;
}

public sealed class GameModeMapAssetCollectionConverter : JsonConverter<GameModeMapAssetCollection>
{
    public override GameModeMapAssetCollection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var assets = new List<GameModeMapAsset>();
        if (document.RootElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in document.RootElement.EnumerateObject())
            {
                assets.Add(new(property.Name, AssetPath(property.Value)));
            }
        }
        else if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in document.RootElement.EnumerateArray())
            {
                var name = item.ValueKind == JsonValueKind.Object && item.TryGetProperty("name", out var nameElement)
                    ? nameElement.GetString()
                    : null;
                assets.Add(new(name, AssetPath(item)));
            }
        }

        return new(assets.Where(asset => !string.IsNullOrWhiteSpace(asset.Name) || !string.IsNullOrWhiteSpace(asset.Path)).ToList());
    }

    public override void Write(Utf8JsonWriter writer, GameModeMapAssetCollection value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value.Items, options);

    private static string? AssetPath(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            return element.GetString();
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var propertyName in new[] { "path", "assetPath", "value" })
            {
                if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String)
                {
                    return property.GetString();
                }
            }
        }

        return null;
    }
}

public sealed record LcuGameTypeConfig(
    long Id,
    string Name,
    int MaxAllowableBans,
    bool AllowTrades,
    bool ExclusivePick,
    bool DuplicatePick,
    bool TeamChampionPool,
    bool CrossTeamChampionPool,
    bool AdvancedLearningQuests,
    bool BattleBoost,
    bool DeathMatch,
    bool DoNotRemove,
    bool LearningQuests,
    bool OnboardCoopBeginner,
    bool Reroll,
    int MainPickTimerDuration,
    int PostPickTimerDuration,
    int BanTimerDuration,
    string PickMode,
    string BanMode,
    string? GameModeOverride,
    int? NumPlayersPerTeamOverride);
