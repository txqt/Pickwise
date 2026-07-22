using Pickwise.Models;

namespace Pickwise.Services;

public interface ILcuClient
{
    Task<LcuSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
    Task AcceptReadyCheckAsync(CancellationToken cancellationToken);
    Task DeclineReadyCheckAsync(CancellationToken cancellationToken);
    Task CreateLobbyAsync(int queueId, CancellationToken cancellationToken);
    Task StartMatchmakingAsync(CancellationToken cancellationToken);
    Task CancelMatchmakingAsync(CancellationToken cancellationToken);
    Task PickChampionAsync(int championId, CancellationToken cancellationToken);
    Task BanChampionAsync(int championId, CancellationToken cancellationToken);
}
