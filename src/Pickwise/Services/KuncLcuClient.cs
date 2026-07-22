using System.Net.Http.Json;
using System.Diagnostics;
using Kunc.RiotGames.Lol.LeagueClientUpdate;
using Pickwise.Models;

namespace Pickwise.Services;

public sealed class KuncLcuClient : ILcuClient, IDisposable
{
    private readonly Lazy<ILolLeagueClientUpdate> _lcu = new(() => LolLeagueClientUpdate.Create());
    private readonly LocalDiagnosticLog _log;

    public KuncLcuClient(LocalDiagnosticLog log)
    {
        _log = log;
    }

    public async Task<LcuSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!Process.GetProcessesByName("LeagueClientUx").Any())
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
        await _lcu.Value.PostAsJsonAsync("lol-matchmaking/v1/ready-check/accept", new { }, cancellationToken);
        _log.Info("Ready Check accepted by Player Command");
    }

    public async Task DeclineReadyCheckAsync(CancellationToken cancellationToken)
    {
        await _lcu.Value.PostAsJsonAsync("lol-matchmaking/v1/ready-check/decline", new { }, cancellationToken);
        _log.Info("Ready Check declined by Player Command");
    }

    public Task PickChampionAsync(int championId, CancellationToken cancellationToken) =>
        PatchCurrentChampionActionAsync("pick", championId, cancellationToken);

    public Task BanChampionAsync(int championId, CancellationToken cancellationToken) =>
        PatchCurrentChampionActionAsync("ban", championId, cancellationToken);

    public void Dispose()
    {
        if (_lcu.IsValueCreated)
        {
            _lcu.Value.Dispose();
        }
    }

    private async Task<T?> GetOrNull<T>(string endpoint, CancellationToken cancellationToken)
    {
        try
        {
            return await _lcu.Value.GetFromJsonAsync<T>(endpoint, cancellationToken);
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
        var session = await _lcu.Value.GetFromJsonAsync<ChampionSelectSession>("lol-champ-select/v1/session", cancellationToken);
        var action = session?.CurrentAction(type) ?? throw new InvalidOperationException($"No active {type} action.");
        var request = new HttpRequestMessage(HttpMethod.Patch, $"lol-champ-select/v1/session/actions/{action.Id}")
        {
            Content = JsonContent.Create(new { championId, completed = true })
        };

        using var response = await _lcu.Value.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        _log.Info($"Champion {type} submitted by Player Command");
    }
}
