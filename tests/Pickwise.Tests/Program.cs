using Pickwise.Models;
using Pickwise.Services;
using Pickwise.ViewModels;
using System.Diagnostics;

var session = new ChampionSelectSession(
    LocalPlayerCellId: 2,
    Actions:
    [
        [new ChampionSelectAction(1, 1, "pick", 0, false, true)],
        [new ChampionSelectAction(2, 2, "ban", 0, false, true)],
        [new ChampionSelectAction(3, 2, "pick", 0, false, false)]
    ],
    MyTeam: [],
    TheirTeam: []);

Assert(session.CurrentAction("ban")?.Id == 2, "finds active local ban action");
Assert(session.CurrentAction("pick") is null, "ignores inactive pick action");
AssertChampionCatalogSearchWorks();
AssertGameModeCatalogWorks();
AssertLcuLockfileParserWorks();
AssertConstructorDoesNotBlockOnSlowLcu();
Console.WriteLine("Pickwise self-checks passed.");

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new Exception(message);
    }
}

static void AssertConstructorDoesNotBlockOnSlowLcu()
{
    var stopwatch = Stopwatch.StartNew();
    _ = new MainViewModel(new SlowLcuClient(), new LocalDiagnosticLog());
    stopwatch.Stop();
    Assert(stopwatch.ElapsedMilliseconds < 200, "MainViewModel constructor blocks on LCU polling");
}

static void AssertChampionCatalogSearchWorks()
{
    var catalog = new ChampionCatalog();
    Assert(catalog.All.Count > 100, "catalog loads champion list");
    Assert(catalog.Search("ahri").Any(champion => champion.Name == "Ahri" && champion.ChampionId == 103), "finds Ahri by lowercase name");
    Assert(catalog.Search("AAT").Any(champion => champion.Name == "Aatrox" && champion.ChampionId == 266), "search is case-insensitive");
}

static void AssertGameModeCatalogWorks()
{
    var catalog = new GameModeCatalog();
    Assert(catalog.All.Any(mode => mode.Name == "Normal Draft 5v5" && mode.QueueId == 400), "catalog contains Normal Draft 5v5 queue");
    Assert(catalog.All.Any(mode => mode.Name == "ARAM" && mode.QueueId == 450), "catalog contains ARAM queue");
    Assert(catalog.All.Any(mode => mode.Name == "ARAM Mayhem" && mode.QueueId == 2400), "catalog contains ARAM Mayhem queue");
}

static void AssertLcuLockfileParserWorks()
{
    var connection = LcuConnection.TryParse("LeagueClient:13968:55888:secret:https");
    Assert(connection is not null, "parses valid LCU lockfile");
    Assert(connection!.BaseUri.ToString() == "https://127.0.0.1:55888/", "uses lockfile port");
    Assert(connection.Authorization.Scheme == "Basic", "uses basic auth");
    Assert(LcuConnection.TryParse("bad") is null, "rejects invalid lockfile");
}

sealed class SlowLcuClient : ILcuClient
{
    public Task<LcuSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        Thread.Sleep(1_000);
        return Task.FromResult(new LcuSnapshot(AppPhase.WaitingForLeagueClient, null, null, null, "slow"));
    }

    public Task AcceptReadyCheckAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeclineReadyCheckAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task CreateLobbyAsync(int queueId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task PickChampionAsync(int championId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task BanChampionAsync(int championId, CancellationToken cancellationToken) => Task.CompletedTask;
}
