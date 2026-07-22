using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using Pickwise.Models;
using Pickwise.Services;

namespace Pickwise.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ILcuClient _lcu;
    private readonly LocalDiagnosticLog _log;
    private readonly ChampionCatalog _championCatalog;
    private readonly GameModeCatalog _gameModeCatalog;
    private readonly ChampionIconCache _championIconCache;
    private readonly IReadOnlyList<ChampionTileViewModel> _allChampionTiles;
    private readonly IReadOnlyDictionary<int, ChampionTileViewModel> _championTilesById;
    private readonly CancellationTokenSource _polling = new();

    [ObservableProperty]
    private AppPhase _phase = AppPhase.WaitingForLeagueClient;

    [ObservableProperty]
    private string _status = "Waiting for League Client";

    [ObservableProperty]
    private string _summoner = "Not connected";

    [ObservableProperty]
    private string _readyCheck = "No ready check";

    [ObservableProperty]
    private string _championSelect = "Not in champion select";

    [ObservableProperty]
    private string _championSearch = "";

    [ObservableProperty]
    private Champion? _selectedChampion;

    [ObservableProperty]
    private ChampionTileViewModel? _selectedChampionTile;

    [ObservableProperty]
    private IReadOnlyList<ChampionTileViewModel> _champions = [];

    public IReadOnlyList<ChampionRoleOptionViewModel> ChampionRoles { get; } =
    [
        Role("All", "M3,3 L11,3 L11,11 L3,11 Z M13,3 L21,3 L21,11 L13,11 Z M3,13 L11,13 L11,21 L3,21 Z M13,13 L21,13 L21,21 L13,21 Z"),
        Role("Fighter", "M12,2 L15,9 L22,12 L15,15 L12,22 L9,15 L2,12 L9,9 Z"),
        Role("Tank", "M12,2 L21,6 L19,17 L12,22 L5,17 L3,6 Z M8,9 L16,9 L16,15 L8,15 Z"),
        Role("Mage", "M5,18 L18,5 L20,7 L7,20 Z M7,5 L19,17 L17,19 L5,7 Z M11,5 L14,5 L14,8 L11,8 Z M16,10 L19,10 L19,13 L16,13 Z"),
        Role("Assassin", "M12,2 L16,10 L22,12 L16,14 L12,22 L8,14 L2,12 L8,10 Z M12,7 L10,12 L12,17 L14,12 Z"),
        Role("Marksman", "M12,2 L15,9 L22,12 L15,15 L12,22 L9,15 L2,12 L9,9 Z M12,7 L12,17 M7,12 L17,12"),
        Role("Support", "M12,3 L15,9 L21,9 L16,13 L18,20 L12,16 L6,20 L8,13 L3,9 L9,9 Z")
    ];

    [ObservableProperty]
    private ChampionRoleOptionViewModel _selectedChampionRole = null!;

    [ObservableProperty]
    private GameMode? _selectedGameMode;

    [ObservableProperty]
    private IReadOnlyList<GameMode> _gameModes = [];

    [ObservableProperty]
    private string _lastCommandResult = "";

    public string LogPath => _log.Path;
    public string ChampionIconCachePath => ChampionIconCache.CacheDirectory;
    public string RiotDisclaimer => "Pickwise is not endorsed by Riot Games and does not reflect the views or opinions of Riot Games or anyone officially involved in producing or managing Riot Games properties. Riot Games and all associated properties are trademarks or registered trademarks of Riot Games, Inc.";
    public bool CanRespondReadyCheck => Phase == AppPhase.ReadyCheck;
    public bool CanChampionCommand => Phase == AppPhase.ChampionSelect && SelectedChampion is not null;
    public bool CanCreateLobby => Phase == AppPhase.Connected && SelectedGameMode is not null;
    public bool CanUseMatchmaking => Phase == AppPhase.Connected;

    public MainViewModel() : this(CreateDefaultLog())
    {
    }

    public MainViewModel(ILcuClient lcu, LocalDiagnosticLog log) : this(lcu, log, new ChampionCatalog(), new GameModeCatalog(), new ChampionIconCache(log))
    {
    }

    public MainViewModel(ILcuClient lcu, LocalDiagnosticLog log, ChampionCatalog championCatalog, GameModeCatalog gameModeCatalog, ChampionIconCache championIconCache)
    {
        _lcu = lcu;
        _log = log;
        _championCatalog = championCatalog;
        _gameModeCatalog = gameModeCatalog;
        _championIconCache = championIconCache;
        _allChampionTiles = _championCatalog.All.Select(champion => new ChampionTileViewModel(champion)).ToList();
        _championTilesById = _allChampionTiles.ToDictionary(tile => tile.Champion.ChampionId);
        Champions = _allChampionTiles;
        SelectedChampionRole = ChampionRoles[0];
        SelectedChampionRole.IsSelected = true;
        GameModes = _gameModeCatalog.All;
        SelectedGameMode = GameModes.FirstOrDefault();
        _ = Task.Run(() => LoadChampionIconsAsync(_polling.Token));
        _ = Task.Run(() => PollAsync(_polling.Token));
    }

    private MainViewModel(LocalDiagnosticLog log) : this(new KuncLcuClient(log), log)
    {
    }

    private static LocalDiagnosticLog CreateDefaultLog() => new();

    private static ChampionRoleOptionViewModel Role(string name, string path) =>
        new(name, path);

    partial void OnPhaseChanged(AppPhase value)
    {
        OnPropertyChanged(nameof(CanRespondReadyCheck));
        OnPropertyChanged(nameof(CanChampionCommand));
        OnPropertyChanged(nameof(CanCreateLobby));
        OnPropertyChanged(nameof(CanUseMatchmaking));
    }

    partial void OnChampionSearchChanged(string value)
    {
        RefreshChampions();
    }

    partial void OnSelectedChampionRoleChanged(ChampionRoleOptionViewModel value)
    {
        foreach (var role in ChampionRoles)
        {
            role.IsSelected = role == value;
        }

        RefreshChampions();
    }

    private void RefreshChampions()
    {
        Champions = _championCatalog.Filter(ChampionSearch, SelectedChampionRole.Name)
            .Select(champion => _championTilesById[champion.ChampionId])
            .ToList();
        if (SelectedChampionTile is not null && !Champions.Contains(SelectedChampionTile))
        {
            SelectedChampionTile = null;
        }
    }

    partial void OnSelectedChampionTileChanged(ChampionTileViewModel? value)
    {
        SelectedChampion = value?.Champion;
    }

    partial void OnSelectedChampionChanged(Champion? value)
    {
        OnPropertyChanged(nameof(CanChampionCommand));
        PickCommand.NotifyCanExecuteChanged();
        BanCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedGameModeChanged(GameMode? value)
    {
        OnPropertyChanged(nameof(CanCreateLobby));
        CreateLobbyCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanRespondReadyCheck))]
    private Task AcceptAsync() => RunCommandAsync(token => _lcu.AcceptReadyCheckAsync(token), "Accepted ready check");

    [RelayCommand(CanExecute = nameof(CanRespondReadyCheck))]
    private Task DeclineAsync() => RunCommandAsync(token => _lcu.DeclineReadyCheckAsync(token), "Declined ready check");

    [RelayCommand(CanExecute = nameof(CanCreateLobby))]
    private Task CreateLobbyAsync()
    {
        if (SelectedGameMode is null)
        {
            LastCommandResult = "Select a mode first";
            return Task.CompletedTask;
        }

        return RunCommandAsync(
            token => _lcu.CreateLobbyAsync(SelectedGameMode.QueueId, token),
            $"Created {SelectedGameMode.Name} lobby");
    }

    [RelayCommand(CanExecute = nameof(CanUseMatchmaking))]
    private Task StartMatchmakingAsync() => RunCommandAsync(_lcu.StartMatchmakingAsync, "Started matchmaking");

    [RelayCommand(CanExecute = nameof(CanUseMatchmaking))]
    private Task CancelMatchmakingAsync() => RunCommandAsync(_lcu.CancelMatchmakingAsync, "Cancelled matchmaking");

    [RelayCommand]
    private void SelectChampionRole(ChampionRoleOptionViewModel role)
    {
        SelectedChampionRole = role;
    }

    [RelayCommand(CanExecute = nameof(CanChampionCommand))]
    private Task PickAsync() => RunChampionCommandAsync(_lcu.PickChampionAsync, "Pick submitted");

    [RelayCommand(CanExecute = nameof(CanChampionCommand))]
    private Task BanAsync() => RunChampionCommandAsync(_lcu.BanChampionAsync, "Ban submitted");

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(2));
                var snapshot = await _lcu.GetSnapshotAsync(timeout.Token);
                Apply(snapshot);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                Apply(new(AppPhase.WaitingForLeagueClient, null, null, null, "Waiting for League Client"));
            }
            catch (Exception exception)
            {
                _log.Error("Polling failed", exception);
                Apply(new(AppPhase.Error, null, null, null, "Polling failed; see diagnostic log"));
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task LoadChampionIconsAsync(CancellationToken cancellationToken)
    {
        try
        {
            foreach (var tile in _allChampionTiles)
            {
                var icon = await _championIconCache.LoadAsync(tile.Champion, cancellationToken).ConfigureAwait(false);
                if (icon is not null)
                {
                    Dispatcher.UIThread.Post(() => tile.Icon = icon);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _log.Error("Champion icon loading failed", exception);
        }
    }

    private void Apply(LcuSnapshot snapshot)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => Apply(snapshot));
            return;
        }

        Phase = snapshot.Phase;
        Status = snapshot.Message;
        Summoner = snapshot.Summoner?.Name ?? "Not connected";
        ReadyCheck = snapshot.ReadyCheck is null
            ? "No ready check"
            : $"{snapshot.ReadyCheck.State} / {snapshot.ReadyCheck.PlayerResponse}";
        ChampionSelect = snapshot.ChampionSelect is null
            ? "Not in champion select"
            : $"Allies: {snapshot.ChampionSelect.MyTeam.Count}, enemies: {snapshot.ChampionSelect.TheirTeam.Count}";
        AcceptCommand.NotifyCanExecuteChanged();
        DeclineCommand.NotifyCanExecuteChanged();
        CreateLobbyCommand.NotifyCanExecuteChanged();
        StartMatchmakingCommand.NotifyCanExecuteChanged();
        CancelMatchmakingCommand.NotifyCanExecuteChanged();
        PickCommand.NotifyCanExecuteChanged();
        BanCommand.NotifyCanExecuteChanged();
    }

    private async Task RunChampionCommandAsync(Func<int, CancellationToken, Task> command, string success)
    {
        if (SelectedChampion is null)
        {
            LastCommandResult = "Select a champion first";
            return;
        }

        await RunCommandAsync(token => command(SelectedChampion.ChampionId, token), success);
    }

    private async Task RunCommandAsync(Func<CancellationToken, Task> command, string success)
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await command(timeout.Token);
            LastCommandResult = success;
        }
        catch (Exception exception)
        {
            LastCommandResult = exception.Message;
        }
    }
}
