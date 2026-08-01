using Avalonia.Media.Imaging;
using System.Security.Cryptography;
using System.Text;
using Pickwise.Models;

namespace Pickwise.Services;

public sealed class GameModeIconCache(LocalDiagnosticLog log)
{
    private const string DataDragonVersion = "16.14.1";
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(4) };
    private bool _remoteDisabled;

    public static string CacheDirectory => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Pickwise",
        "game-mode-icons",
        DataDragonVersion);

    public async Task<Bitmap?> LoadAsync(string fileName, string label, CancellationToken cancellationToken)
    {
        var path = System.IO.Path.Combine(CacheDirectory, fileName);
        if (File.Exists(path))
        {
            return TryLoad(path, label);
        }

        if (_remoteDisabled)
        {
            return null;
        }

        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            var url = $"https://ddragon.leagueoflegends.com/cdn/{DataDragonVersion}/img/map/{fileName}";
            var bytes = await Http.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);
            await File.WriteAllBytesAsync(path, bytes, cancellationToken).ConfigureAwait(false);
            return TryLoad(path, label);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or OperationCanceledException)
        {
            _remoteDisabled = exception is HttpRequestException or OperationCanceledException;
            log.Info($"Game mode icon unavailable: {label}");
            return null;
        }
    }

    public async Task<Bitmap?> LoadLcuAsync(ILcuClient lcu, GameMode mode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(mode.MapAssetPath))
        {
            return null;
        }

        var key = LcuCacheKey(mode.MapId, mode.GameModeCode, mode.MapAssetPath);
        var extension = System.IO.Path.GetExtension(mode.MapAssetPath);
        var path = System.IO.Path.Combine(CacheDirectory, "lcu", $"{key}{extension}");
        if (File.Exists(path))
        {
            return TryLoad(path, mode.Name);
        }

        try
        {
            var bytes = await lcu.GetMapAssetAsync(mode.MapAssetPath, cancellationToken).ConfigureAwait(false);
            if (bytes is null || bytes.Length == 0)
            {
                log.Info($"LCU game mode icon unavailable: {mode.Name}");
                return null;
            }

            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, bytes, cancellationToken).ConfigureAwait(false);
            return TryLoad(path, mode.Name);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or OperationCanceledException)
        {
            log.Info($"LCU game mode icon unavailable: {mode.Name}");
            return null;
        }
    }

    public static string LcuCacheKey(int mapId, string gameMode, string assetPath) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{mapId}:{gameMode}:{assetPath}"))).ToLowerInvariant();

    private Bitmap? TryLoad(string path, string label)
    {
        try
        {
            return new Bitmap(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            log.Info($"Game mode icon cache unreadable: {label}");
            return null;
        }
    }
}
