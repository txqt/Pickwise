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

sealed class SlowLcuClient : ILcuClient
{
    public Task<LcuSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        Thread.Sleep(1_000);
        return Task.FromResult(new LcuSnapshot(AppPhase.WaitingForLeagueClient, null, null, null, "slow"));
    }

    public Task AcceptReadyCheckAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeclineReadyCheckAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task PickChampionAsync(int championId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task BanChampionAsync(int championId, CancellationToken cancellationToken) => Task.CompletedTask;
}
