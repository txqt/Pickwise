using Avalonia.Media.Imaging;

namespace Pickwise.Services;

public sealed class SummonerSpellIconCache(LocalDiagnosticLog log)
{
    private const string DataDragonVersion = "16.14.1";
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(4) };
    private bool _remoteDisabled;

    public static string CacheDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Pickwise",
        "spell-icons",
        DataDragonVersion);

    public async Task<Bitmap?> LoadAsync(string fileName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var path = Path.Combine(CacheDirectory, fileName);
        if (File.Exists(path))
        {
            return TryLoad(path, fileName);
        }

        if (_remoteDisabled)
        {
            return null;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var url = $"https://ddragon.leagueoflegends.com/cdn/{DataDragonVersion}/img/spell/{fileName}";
            var bytes = await Http.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);
            await File.WriteAllBytesAsync(path, bytes, cancellationToken).ConfigureAwait(false);
            return TryLoad(path, fileName);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or OperationCanceledException)
        {
            _remoteDisabled = exception is HttpRequestException or OperationCanceledException;
            log.Info($"Summoner spell icon unavailable: {fileName}");
            return null;
        }
    }

    private Bitmap? TryLoad(string path, string fileName)
    {
        try
        {
            return new Bitmap(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            log.Info($"Summoner spell icon cache unreadable: {fileName}");
            return null;
        }
    }
}
