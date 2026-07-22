using Avalonia.Media.Imaging;
using Pickwise.Models;

namespace Pickwise.Services;

public sealed class ChampionIconCache(LocalDiagnosticLog log)
{
    private const string DataDragonVersion = "16.14.1";
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(4) };
    private bool _remoteDisabled;

    public async Task<Bitmap?> LoadAsync(Champion champion, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(champion.ImageFileName))
        {
            return null;
        }

        var path = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Pickwise",
            "champion-icons",
            DataDragonVersion,
            champion.ImageFileName);

        if (File.Exists(path))
        {
            return TryLoad(path, champion);
        }

        if (_remoteDisabled)
        {
            return null;
        }

        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            var url = $"https://ddragon.leagueoflegends.com/cdn/{DataDragonVersion}/img/champion/{champion.ImageFileName}";
            var bytes = await Http.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);
            await File.WriteAllBytesAsync(path, bytes, cancellationToken).ConfigureAwait(false);
            return TryLoad(path, champion);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or OperationCanceledException)
        {
            _remoteDisabled = exception is HttpRequestException or OperationCanceledException;
            log.Info($"Champion icon unavailable: {champion.Name}");
            return null;
        }
    }

    private Bitmap? TryLoad(string path, Champion champion)
    {
        try
        {
            return new Bitmap(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            log.Info($"Champion icon cache unreadable: {champion.Name}");
            return null;
        }
    }
}
