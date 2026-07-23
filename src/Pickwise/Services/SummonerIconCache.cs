using Avalonia.Media.Imaging;

namespace Pickwise.Services;

public sealed class SummonerIconCache(LocalDiagnosticLog log)
{
    private const string DataDragonVersion = "16.14.1";
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(4) };
    private readonly Dictionary<int, Bitmap?> _loaded = [];
    private readonly Dictionary<int, Task<Bitmap?>> _loading = [];
    private readonly Func<int, CancellationToken, Task<Bitmap?>>? _loader;
    private bool _remoteDisabled;

    public SummonerIconCache(LocalDiagnosticLog log, Func<int, CancellationToken, Task<Bitmap?>> loader) : this(log)
    {
        _loader = loader;
    }

    public static string CacheDirectory => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Pickwise",
        "summoner-icons",
        DataDragonVersion);

    public async Task<Bitmap?> LoadAsync(int? iconId, CancellationToken cancellationToken)
    {
        if (iconId is null or < 0)
        {
            return null;
        }

        Task<Bitmap?> load;
        lock (_loaded)
        {
            if (_loaded.TryGetValue(iconId.Value, out var icon))
            {
                return icon;
            }

            if (!_loading.TryGetValue(iconId.Value, out load!))
            {
                load = LoadUncachedAsync(iconId.Value, cancellationToken);
                _loading[iconId.Value] = load;
            }
        }

        try
        {
            var loaded = await load.ConfigureAwait(false);
            lock (_loaded)
            {
                _loaded[iconId.Value] = loaded;
                _loading.Remove(iconId.Value);
            }

            return loaded;
        }
        catch
        {
            lock (_loaded)
            {
                _loading.Remove(iconId.Value);
            }

            throw;
        }
    }

    private async Task<Bitmap?> LoadUncachedAsync(int iconId, CancellationToken cancellationToken)
    {
        if (_loader is not null)
        {
            return await _loader(iconId, cancellationToken).ConfigureAwait(false);
        }

        var fileName = $"{iconId}.png";
        var path = System.IO.Path.Combine(CacheDirectory, fileName);
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
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            var url = $"https://ddragon.leagueoflegends.com/cdn/{DataDragonVersion}/img/profileicon/{fileName}";
            var bytes = await Http.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);
            await File.WriteAllBytesAsync(path, bytes, cancellationToken).ConfigureAwait(false);
            return TryLoad(path, fileName);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or OperationCanceledException)
        {
            _remoteDisabled = exception is HttpRequestException or OperationCanceledException;
            log.Info($"Summoner icon unavailable: {fileName}");
            return null;
        }
    }

    private Bitmap? TryLoad(string path, string label)
    {
        try
        {
            return new Bitmap(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            log.Info($"Summoner icon cache unreadable: {label}");
            return null;
        }
    }
}
