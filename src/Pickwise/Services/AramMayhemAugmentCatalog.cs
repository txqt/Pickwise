using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Avalonia.Media.Imaging;

namespace Pickwise.Services;

public static class AramMayhemAugmentCatalog
{
    private const string CatalogUrl = "https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/cherry-augments.json";
    private const string DetailsUrl = "https://raw.communitydragon.org/latest/cdragon/arena/en_us.json";
    private const string AramMayhemAugmentUrl = "https://arammayhem.com/augments/";
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly object Gate = new();
    private static IReadOnlyDictionary<int, AramMayhemAugmentData> _items = new Dictionary<int, AramMayhemAugmentData>();
    private static Task? _loading;
    private static readonly HashSet<int> ExternalDescriptionAttempts = [];

    public static bool IsLoaded => _items.Count > 0;

    public static string Name(int id) =>
        _items.TryGetValue(id, out var item) ? item.Name : $"Augment {id}";

    public static string? IconPath(int id) =>
        _items.TryGetValue(id, out var item) ? item.IconPath : null;

    public static string Rarity(int id) =>
        _items.TryGetValue(id, out var item) ? item.Rarity : "";

    public static string Description(int id) =>
        _items.TryGetValue(id, out var item) ? item.Description : "";

    public static async Task<bool> EnsureDescriptionAsync(int id, LocalDiagnosticLog log, CancellationToken cancellationToken)
    {
        AramMayhemAugmentData? item;
        lock (Gate)
        {
            if (!_items.TryGetValue(id, out item) || !string.IsNullOrWhiteSpace(item.Description) || !ExternalDescriptionAttempts.Add(id))
            {
                return false;
            }
        }

        try
        {
            var html = await Http.GetStringAsync($"{AramMayhemAugmentUrl}{Slug(item.Name)}/", cancellationToken).ConfigureAwait(false);
            var description = DescriptionFromAramMayhemPage(html);
            if (string.IsNullOrWhiteSpace(description))
            {
                return false;
            }

            lock (Gate)
            {
                if (!_items.TryGetValue(id, out item) || !string.IsNullOrWhiteSpace(item.Description))
                {
                    return false;
                }

                _items = _items.ToDictionary(pair => pair.Key, pair => pair.Key == id
                    ? pair.Value with { Description = description }
                    : pair.Value);
                return true;
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or OperationCanceledException)
        {
            log.Info($"ARAM Mayhem augment description unavailable: {id}");
            return false;
        }
    }

    public static Task EnsureLoadedAsync(LocalDiagnosticLog log, CancellationToken cancellationToken)
    {
        lock (Gate)
        {
            if (_items.Count > 0)
            {
                return Task.CompletedTask;
            }

            return _loading ??= LoadAsync(log, cancellationToken);
        }
    }

    private static async Task LoadAsync(LocalDiagnosticLog log, CancellationToken cancellationToken)
    {
        try
        {
            var json = await Http.GetStringAsync(CatalogUrl, cancellationToken).ConfigureAwait(false);
            var rows = JsonSerializer.Deserialize<List<AugmentRow>>(json) ?? [];
            var descriptions = await LoadDescriptionsAsync(log, cancellationToken).ConfigureAwait(false);
            var items = rows
                .Where(row => row.Id > 0 && !string.IsNullOrWhiteSpace(row.Name))
                .GroupBy(row => row.Id)
                .ToDictionary(
                    group => group.Key,
                    group =>
                    {
                        var row = group.First();
                        var description = DescriptionFor(row, descriptions);
                        description ??= KnownDescription(row.Name);
                        return new AramMayhemAugmentData(row.Name.Trim(), row.IconPath, CleanRarity(row.Rarity), description ?? "");
                    });

            lock (Gate)
            {
                _items = items;
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or OperationCanceledException)
        {
            log.Info("ARAM Mayhem augment catalog unavailable");
        }
    }

    private static async Task<IReadOnlyDictionary<string, string>> LoadDescriptionsAsync(LocalDiagnosticLog log, CancellationToken cancellationToken)
    {
        try
        {
            var json = await Http.GetStringAsync(DetailsUrl, cancellationToken).ConfigureAwait(false);
            using var data = JsonDocument.Parse(json);
            if (!data.RootElement.TryGetProperty("augments", out var augments) || augments.ValueKind != JsonValueKind.Array)
            {
                return new Dictionary<string, string>();
            }

            var descriptions = new Dictionary<string, string>();
            foreach (var row in augments.EnumerateArray())
            {
                var name = StringProperty(row, "name");
                var text = StringProperty(row, "tooltip");
                if (string.IsNullOrWhiteSpace(text))
                {
                    text = StringProperty(row, "desc");
                }

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                var description = CleanMarkup(text, DataValues(row));
                descriptions.TryAdd(Key(name), description);

                var apiName = StringProperty(row, "apiName");
                if (!string.IsNullOrWhiteSpace(apiName))
                {
                    descriptions.TryAdd(Key(apiName), description);
                }

                var iconSmall = StringProperty(row, "iconSmall");
                if (!string.IsNullOrWhiteSpace(iconSmall))
                {
                    descriptions.TryAdd(IconKey(iconSmall), description);
                }
            }

            return descriptions;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or OperationCanceledException)
        {
            log.Info("ARAM Mayhem augment descriptions unavailable");
            return new Dictionary<string, string>();
        }
    }

    private static string StringProperty(JsonElement row, string name) =>
        row.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? ""
            : "";

    private static string? DescriptionFor(AugmentRow row, IReadOnlyDictionary<string, string> descriptions)
    {
        if (descriptions.TryGetValue(Key(row.Name), out var description))
        {
            return description;
        }

        var apiName = row.NameId.StartsWith("ARAM_", StringComparison.OrdinalIgnoreCase)
            ? row.NameId["ARAM_".Length..]
            : row.NameId;
        if (descriptions.TryGetValue(Key(apiName), out description))
        {
            return description;
        }

        return !string.IsNullOrWhiteSpace(row.IconPath) && descriptions.TryGetValue(IconKey(row.IconPath), out description)
            ? description
            : null;
    }

    private static IReadOnlyDictionary<string, double[]> DataValues(JsonElement row)
    {
        if (!row.TryGetProperty("dataValues", out var dataValues) || dataValues.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, double[]>();
        }

        var values = new Dictionary<string, double[]>();
        foreach (var property in dataValues.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetDouble(out var number))
            {
                values[property.Name] = [number];
                continue;
            }

            if (property.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            values[property.Name] = property.Value.EnumerateArray()
                .Where(value => value.ValueKind == JsonValueKind.Number)
                .Select(value => value.GetDouble())
                .ToArray();
        }

        return values;
    }

    private sealed record AugmentRow(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("augmentNameId")] string NameId,
        [property: JsonPropertyName("nameTRA")] string Name,
        [property: JsonPropertyName("augmentSmallIconPath")] string? IconPath,
        [property: JsonPropertyName("rarity")] string? Rarity);

    private static string CleanRarity(string? rarity) =>
        string.IsNullOrWhiteSpace(rarity)
            ? ""
            : rarity.Trim().Replace("k", "", StringComparison.OrdinalIgnoreCase);

    private static string CleanMarkup(string value, IReadOnlyDictionary<string, double[]> dataValues)
    {
        var text = System.Net.WebUtility.HtmlDecode(value)
            .Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<br/>", "\n", StringComparison.OrdinalIgnoreCase);
        text = Regex.Replace(text, @"@(?<key>\w+)(?:\*(?<factor>\d+(?:\.\d+)?))?@", match =>
        {
            var key = match.Groups["key"].Value;
            var factor = match.Groups["factor"].Success
                ? double.Parse(match.Groups["factor"].Value, System.Globalization.CultureInfo.InvariantCulture)
                : 1;
            return dataValues.TryGetValue(key, out var values) && values.Length > 0
                ? (values[0] * factor).ToString("0.##")
                : "";
        });
        text = text.Replace("{{ Item_Keyword_OnHit }}", "On-hit effects", StringComparison.OrdinalIgnoreCase);
        text = Regex.Replace(text, @"\{\{.*?\}\}", "");
        text = Regex.Replace(text, @"\[(?:/?(?:b|i|stat|keyword|stat:[^\]]+|keyword:[^\]]+))\]", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"%i:[^%\s]+%", "");
        text = Regex.Replace(text, "<.*?>", "");
        return text
            .Replace("\n\n\n", "\n\n")
            .Replace("  ", " ")
            .Trim();
    }

    private static string Key(string value) =>
        Regex.Replace(value, "[^a-z0-9]", "", RegexOptions.IgnoreCase).ToLowerInvariant();

    private static string? KnownDescription(string name) =>
        name.Trim() switch
        {
            "Scoped Weapons" => "Gain Attack Range.",
            "Scopier Weapons" => "Gain Attack Range.",
            "Scopiest Weapons" => "Gain Attack Range.",
            _ => null
        };

    private static string IconKey(string value)
    {
        var fileName = Path.GetFileNameWithoutExtension(value);
        fileName = Regex.Replace(fileName, @"_(small|large)(\..*)?$", "", RegexOptions.IgnoreCase);
        return Key(fileName);
    }

    private static string Slug(string value) =>
        Regex.Replace(value.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');

    private static string DescriptionFromAramMayhemPage(string html)
    {
        var match = Regex.Match(html, @"props=""(?<props>[^""]*&quot;description&quot;[^""]*)""");
        if (!match.Success)
        {
            return "";
        }

        var props = System.Net.WebUtility.HtmlDecode(match.Groups["props"].Value);
        using var data = JsonDocument.Parse(props);
        if (!data.RootElement.TryGetProperty("description", out var description)
            || description.ValueKind != JsonValueKind.Array
            || description.GetArrayLength() < 2
            || description[1].ValueKind != JsonValueKind.String)
        {
            return "";
        }

        var text = System.Net.WebUtility.HtmlDecode(description[1].GetString() ?? "");
        text = text.Replace("[br/]", "\n", StringComparison.OrdinalIgnoreCase);
        text = Regex.Replace(text, @"\[(?:/?(?:b|i|stat|keyword|stat:[^\]]+|keyword:[^\]]+))\]", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "<.*?>", "");
        text = Regex.Replace(text, @"\n{3,}", "\n\n");
        text = Regex.Replace(text, @"[ \t]{2,}", " ");
        return text.Trim();
    }
}

public sealed record AramMayhemAugmentData(string Name, string? IconPath, string Rarity, string Description);

public sealed class AramMayhemAugmentIconCache(LocalDiagnosticLog log)
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(4) };

    public static string CacheDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Pickwise",
        "aram-mayhem-augment-icons",
        "latest");

    public async Task<Bitmap?> LoadAsync(int augmentId, CancellationToken cancellationToken)
    {
        await AramMayhemAugmentCatalog.EnsureLoadedAsync(log, cancellationToken).ConfigureAwait(false);
        var iconPath = AramMayhemAugmentCatalog.IconPath(augmentId);
        if (string.IsNullOrWhiteSpace(iconPath))
        {
            return null;
        }

        var path = Path.Combine(CacheDirectory, $"{augmentId}.png");
        if (File.Exists(path))
        {
            return TryLoad(path, augmentId);
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var bytes = await Http.GetByteArrayAsync(ToRawUrl(iconPath), cancellationToken).ConfigureAwait(false);
            await File.WriteAllBytesAsync(path, bytes, cancellationToken).ConfigureAwait(false);
            return TryLoad(path, augmentId);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or OperationCanceledException)
        {
            log.Info($"ARAM Mayhem augment icon unavailable: {augmentId}");
            return null;
        }
    }

    private static string ToRawUrl(string iconPath)
    {
        var relative = iconPath
            .Replace("/lol-game-data/assets/", "", StringComparison.OrdinalIgnoreCase)
            .TrimStart('/')
            .ToLowerInvariant();
        return $"https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/{relative}";
    }

    private Bitmap? TryLoad(string path, int augmentId)
    {
        try
        {
            return new Bitmap(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            log.Info($"ARAM Mayhem augment icon cache unreadable: {augmentId}");
            return null;
        }
    }
}
