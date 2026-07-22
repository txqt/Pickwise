using System.Net.Http.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using Pickwise.Models;

namespace Pickwise.Services;

public sealed class KuncLcuClient : ILcuClient, IDisposable
{
    private readonly LocalDiagnosticLog _log;
    private readonly HttpClient _http;

    public KuncLcuClient(LocalDiagnosticLog log)
    {
        _log = log;
        _http = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });
    }

    public async Task<LcuSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (TryGetConnection() is null)
            {
                return new(AppPhase.WaitingForLeagueClient, null, null, null, "Waiting for League Client");
            }

            var summoner = await GetOrNull<CurrentSummoner>("lol-summoner/v1/current-summoner", cancellationToken);
            if (summoner is null)
            {
                return new(AppPhase.WaitingForLeagueClient, null, null, null, "Waiting for League Client");
            }

            var ready = await GetOrNull<ReadyCheckState>("lol-matchmaking/v1/ready-check", cancellationToken);
            if (ready?.NeedsResponse == true)
            {
                return new(AppPhase.ReadyCheck, summoner, ready, null, "Match found");
            }

            var championSelect = await GetOrNull<ChampionSelectSession>("lol-champ-select/v1/session", cancellationToken);
            if (championSelect is not null)
            {
                return new(AppPhase.ChampionSelect, summoner, ready, championSelect, "Champion select");
            }

            return new(AppPhase.Connected, summoner, ready, null, "Connected");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _log.Error("LCU snapshot failed", exception);
            return new(AppPhase.Error, null, null, null, "Cannot connect to League Client");
        }
    }

    public async Task AcceptReadyCheckAsync(CancellationToken cancellationToken)
    {
        using var response = await SendJsonAsync(HttpMethod.Post, "lol-matchmaking/v1/ready-check/accept", new { }, cancellationToken);
        response.EnsureSuccessStatusCode();
        _log.Info("Ready Check accepted by Player Command");
    }

    public async Task DeclineReadyCheckAsync(CancellationToken cancellationToken)
    {
        using var response = await SendJsonAsync(HttpMethod.Post, "lol-matchmaking/v1/ready-check/decline", new { }, cancellationToken);
        response.EnsureSuccessStatusCode();
        _log.Info("Ready Check declined by Player Command");
    }

    public async Task CreateLobbyAsync(int queueId, CancellationToken cancellationToken)
    {
        using var response = await SendJsonAsync(HttpMethod.Post, "lol-lobby/v2/lobby", new { queueId }, cancellationToken);
        response.EnsureSuccessStatusCode();
        _log.Info($"Lobby created by Player Command: queueId={queueId}");
    }

    public Task PickChampionAsync(int championId, CancellationToken cancellationToken) =>
        PatchCurrentChampionActionAsync("pick", championId, cancellationToken);

    public Task BanChampionAsync(int championId, CancellationToken cancellationToken) =>
        PatchCurrentChampionActionAsync("ban", championId, cancellationToken);

    public void Dispose() => _http.Dispose();

    private async Task<T?> GetOrNull<T>(string endpoint, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await SendAsync(HttpMethod.Get, endpoint, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return default;
            }

            return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        }
        catch (HttpRequestException)
        {
            return default;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return default;
        }
    }

    private async Task PatchCurrentChampionActionAsync(string type, int championId, CancellationToken cancellationToken)
    {
        var session = await GetOrNull<ChampionSelectSession>("lol-champ-select/v1/session", cancellationToken);
        var action = session?.CurrentAction(type) ?? throw new InvalidOperationException($"No active {type} action.");

        using var response = await SendJsonAsync(HttpMethod.Patch, $"lol-champ-select/v1/session/actions/{action.Id}", new { championId, completed = true }, cancellationToken);
        response.EnsureSuccessStatusCode();
        _log.Info($"Champion {type} submitted by Player Command");
    }

    private Task<HttpResponseMessage> SendJsonAsync<T>(HttpMethod method, string endpoint, T body, CancellationToken cancellationToken)
    {
        var request = CreateRequest(method, endpoint);
        request.Content = JsonContent.Create(body);
        return _http.SendAsync(request, cancellationToken);
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string endpoint, CancellationToken cancellationToken) =>
        _http.SendAsync(CreateRequest(method, endpoint), cancellationToken);

    private HttpRequestMessage CreateRequest(HttpMethod method, string endpoint)
    {
        var connection = TryGetConnection() ?? throw new InvalidOperationException("League Client lockfile is not available.");
        var request = new HttpRequestMessage(method, new Uri(connection.BaseUri, endpoint));
        request.Headers.Authorization = connection.Authorization;
        return request;
    }

    private static LcuConnection? TryGetConnection()
    {
        var process = Process.GetProcessesByName("LeagueClientUx")
            .FirstOrDefault(process => !string.IsNullOrWhiteSpace(process.MainModule?.FileName));
        var directory = process is null ? null : Path.GetDirectoryName(process.MainModule?.FileName);
        var lockfilePath = directory is null ? null : Path.Combine(directory, "lockfile");

        return lockfilePath is not null && File.Exists(lockfilePath)
            ? LcuConnection.TryParse(ReadShared(lockfilePath))
            : null;
    }

    private static string ReadShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

public sealed record LcuConnection(Uri BaseUri, AuthenticationHeaderValue Authorization)
{
    public static LcuConnection? TryParse(string lockfile)
    {
        var parts = lockfile.Trim().Split(':');
        if (parts.Length != 5 || !int.TryParse(parts[2], out var port) || string.IsNullOrWhiteSpace(parts[3]))
        {
            return null;
        }

        var token = Convert.ToBase64String(Encoding.ASCII.GetBytes($"riot:{parts[3]}"));
        return new(new Uri($"https://127.0.0.1:{port}/"), new AuthenticationHeaderValue("Basic", token));
    }
}
