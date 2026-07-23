using Avalonia.Media.Imaging;

namespace Pickwise.Services;

public sealed class ItemIconCache(LocalDiagnosticLog log)
{
    private const string DataDragonVersion = "16.14.1";
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(4) };
    private readonly Dictionary<int, Bitmap?> _loaded = [];
    private readonly Dictionary<int, Task<Bitmap?>> _loading = [];
    private bool _remoteDisabled;

    public static string CacheDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Pickwise",
        "item-icons",
        DataDragonVersion);

    public async Task<Bitmap?> LoadAsync(int itemId, CancellationToken cancellationToken)
    {
        if (itemId <= 0)
        {
            return null;
        }

        Task<Bitmap?> load;
        lock (_loaded)
        {
            if (_loaded.TryGetValue(itemId, out var icon))
            {
                return icon;
            }

            if (!_loading.TryGetValue(itemId, out load!))
            {
                load = LoadUncachedAsync(itemId, cancellationToken);
                _loading[itemId] = load;
            }
        }

        try
        {
            var loaded = await load.ConfigureAwait(false);
            lock (_loaded)
            {
                _loaded[itemId] = loaded;
                _loading.Remove(itemId);
            }

            return loaded;
        }
        catch
        {
            lock (_loaded)
            {
                _loading.Remove(itemId);
            }

            throw;
        }
    }

    private async Task<Bitmap?> LoadUncachedAsync(int itemId, CancellationToken cancellationToken)
    {
        var fileName = $"{itemId}.png";
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
            var url = $"https://ddragon.leagueoflegends.com/cdn/{DataDragonVersion}/img/item/{fileName}";
            var bytes = await Http.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);
            await File.WriteAllBytesAsync(path, bytes, cancellationToken).ConfigureAwait(false);
            return TryLoad(path, fileName);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or OperationCanceledException)
        {
            _remoteDisabled = exception is HttpRequestException or OperationCanceledException;
            log.Info($"Item icon unavailable: {fileName}");
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
            log.Info($"Item icon cache unreadable: {fileName}");
            return null;
        }
    }
}
