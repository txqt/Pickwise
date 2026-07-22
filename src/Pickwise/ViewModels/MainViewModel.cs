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
    private string _championId = "";

    [ObservableProperty]
    private string _lastCommandResult = "";

    public string LogPath => _log.Path;
    public bool CanRespondReadyCheck => Phase == AppPhase.ReadyCheck;
    public bool CanChampionCommand => Phase == AppPhase.ChampionSelect && int.TryParse(ChampionId, out var id) && id > 0;

    public MainViewModel() : this(CreateDefaultLog())
    {
    }

    public MainViewModel(ILcuClient lcu, LocalDiagnosticLog log)
    {
        _lcu = lcu;
        _log = log;
        _ = Task.Run(() => PollAsync(_polling.Token));
    }

    private MainViewModel(LocalDiagnosticLog log) : this(new KuncLcuClient(log), log)
    {
    }

    private static LocalDiagnosticLog CreateDefaultLog() => new();

    partial void OnPhaseChanged(AppPhase value)
    {
        OnPropertyChanged(nameof(CanRespondReadyCheck));
        OnPropertyChanged(nameof(CanChampionCommand));
    }

    partial void OnChampionIdChanged(string value)
    {
        OnPropertyChanged(nameof(CanChampionCommand));
        PickCommand.NotifyCanExecuteChanged();
        BanCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanRespondReadyCheck))]
    private Task AcceptAsync() => RunCommandAsync(token => _lcu.AcceptReadyCheckAsync(token), "Accepted ready check");

    [RelayCommand(CanExecute = nameof(CanRespondReadyCheck))]
    private Task DeclineAsync() => RunCommandAsync(token => _lcu.DeclineReadyCheckAsync(token), "Declined ready check");

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
        PickCommand.NotifyCanExecuteChanged();
        BanCommand.NotifyCanExecuteChanged();
    }

    private async Task RunChampionCommandAsync(Func<int, CancellationToken, Task> command, string success)
    {
        if (!int.TryParse(ChampionId, out var championId) || championId <= 0)
        {
            LastCommandResult = "Enter a champion id first";
            return;
        }

        await RunCommandAsync(token => command(championId, token), success);
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
