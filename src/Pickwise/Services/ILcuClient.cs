using Pickwise.Models;

namespace Pickwise.Services;

public interface ILcuClient
{
    Task<LcuSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<GameMode>> GetQueuesAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<GameMode>>([]);
    Task<SummonerProfile?> GetSummonerProfileAsync(long summonerId, CancellationToken cancellationToken);
    Task<RankedSummary?> GetRankedSummaryAsync(long summonerId, CancellationToken cancellationToken);
    Task SendFriendRequestAsync(LobbyMember member, CancellationToken cancellationToken);
    Task AcceptReadyCheckAsync(CancellationToken cancellationToken);
    Task DeclineReadyCheckAsync(CancellationToken cancellationToken);
    Task CreateLobbyAsync(int queueId, CancellationToken cancellationToken);
    Task CreateLobbyAsync(GameMode mode, CancellationToken cancellationToken) => CreateLobbyAsync(mode.QueueId, cancellationToken);
    Task LeaveLobbyAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    Task UpdatePositionPreferencesAsync(string first, string second, CancellationToken cancellationToken) => Task.CompletedTask;
    Task<string?> GetQuickplayPerksAsync(int championId, string position, CancellationToken cancellationToken) => Task.FromResult<string?>(null);
    Task UpdateQuickplaySlotsAsync(IReadOnlyList<LobbyPlayerSlot> slots, CancellationToken cancellationToken) => Task.CompletedTask;
    Task StartMatchmakingAsync(CancellationToken cancellationToken);
    Task CancelMatchmakingAsync(CancellationToken cancellationToken);
    Task DeclareChampionAsync(int championId, CancellationToken cancellationToken);
    Task PickChampionAsync(int championId, CancellationToken cancellationToken);
    Task BanChampionAsync(int championId, CancellationToken cancellationToken);
    Task SwapBenchChampionAsync(int championId, CancellationToken cancellationToken);
    Task AcceptTradeAsync(int tradeId, CancellationToken cancellationToken);
    Task DeclineTradeAsync(int tradeId, CancellationToken cancellationToken);
}
