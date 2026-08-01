using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Media.Imaging;
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
    private readonly GameModeIconCache _gameModeIconCache;
    private readonly SummonerIconCache _summonerIconCache;
    private readonly SummonerSpellIconCache _summonerSpellIconCache;
    private readonly ItemIconCache _itemIconCache;
    private readonly AramMayhemAugmentIconCache _aramMayhemAugmentIconCache;
    private readonly ChampionPreferenceStore _preferenceStore;
    private readonly ChampionPreferences _preferences;
    private readonly IReadOnlyList<ChampionTileViewModel> _allChampionTiles;
    private readonly IReadOnlyDictionary<int, ChampionTileViewModel> _championTilesById;
    private readonly CancellationTokenSource _polling = new();
    private IReadOnlyList<GameMode> _allGameModes = [];
    private Dictionary<string, LobbyMemberViewModel> _lobbyMembersByKey = [];
    private ChampionSelectSession? _currentChampionSelectSession;
    private CurrentSummoner? _currentSummoner;
    private LobbyState? _currentLobbyState;
    private string _profileBackScreen = "Home";
    private AppPhase _lastAutoOpenedPhase = AppPhase.WaitingForLeagueClient;
    private IReadOnlySet<int> _pickableChampionIds = new HashSet<int>();
    private IReadOnlySet<int> _bannableChampionIds = new HashSet<int>();
    private IReadOnlySet<int> _disabledChampionIds = new HashSet<int>();
    private IReadOnlySet<int> _accountEligibleChampionIds = new HashSet<int>();
    private ChampionSelectAvailability? _championAvailability;
    private IReadOnlySet<int> _allyHoveredChampionIds = new HashSet<int>();
    private bool _suppressDeclare;
    private bool _hydratingPositions;
    private bool _positionEditDirty;
    private IReadOnlyList<MatchHistoryEntry> _currentSummonerMatchSample = [];
    private string? _currentSummonerMatchSamplePuuid;
    private bool _currentSummonerMatchSampleLoading;
    private bool _aramMayhemAugmentCatalogLoading;
    private bool _aramMayhemAugmentCatalogLoadAttempted;
    private int _currentChampionSelectChampionId;

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
    private string _activeGame = "No active game";

    [ObservableProperty]
    private string _championSearch = "";

    [ObservableProperty]
    private Champion? _selectedChampion;

    [ObservableProperty]
    private ChampionTileViewModel? _selectedChampionTile;

    [ObservableProperty]
    private IReadOnlyList<ChampionTileViewModel> _champions = [];

    [ObservableProperty]
    private IReadOnlyList<ChampionTileViewModel> _aramAvailableChampions = [];

    [ObservableProperty]
    private IReadOnlyList<ChampionTileViewModel> _aramBenchChampions = [];

    [ObservableProperty]
    private IReadOnlyList<ChampionTileViewModel> _quickBanChampions = [];

    [ObservableProperty]
    private IReadOnlyList<ChampionTradeRequest> _tradeRequests = [];

    [ObservableProperty]
    private string _currentLobby = "No lobby";

    [ObservableProperty]
    private IReadOnlyList<LobbyMemberViewModel> _lobbyMembers = [];

    [ObservableProperty]
    private ProfilePanelViewModel? _selectedProfile;

    [ObservableProperty]
    private IReadOnlyList<ProfileMatchViewModel> _profileMatches = [];

    [ObservableProperty]
    private IReadOnlyList<AramMayhemAugmentSampleViewModel> _aramMayhemAugmentStats = [];

    [ObservableProperty]
    private string _aramMayhemAugmentTitle = "Popular ARAM Mayhem Augments";

    [ObservableProperty]
    private IReadOnlyList<AramMayhemChampionSampleViewModel> _aramMayhemChampionSamples = [];

    [ObservableProperty]
    private AramMayhemChampionSampleViewModel? _selectedAramMayhemChampionSample;

    [ObservableProperty]
    private IReadOnlyList<AramMayhemAugmentSampleViewModel> _aramMayhemLobbyAugmentStats = [];

    [ObservableProperty]
    private string _aramMayhemLobbyAugmentTitle = "Popular ARAM Mayhem Augments";

    [ObservableProperty]
    private string _selectedHomeTab = "Current Summoner";

    [ObservableProperty]
    private IReadOnlyList<AramMayhemGlobalChampionViewModel> _aramMayhemGlobalChampionStats = [];

    [ObservableProperty]
    private AramMayhemGlobalChampionViewModel? _selectedAramMayhemGlobalChampion;

    [ObservableProperty]
    private IReadOnlyList<AramMayhemAugmentSampleViewModel> _aramMayhemGlobalAugmentStats = [];

    [ObservableProperty]
    private string _aramMayhemGlobalAugmentTitle = "ARAM Mayhem Augments";

    [ObservableProperty]
    private string _selectedAramMayhemLobbySegment = "Champion Pool";

    [ObservableProperty]
    private IReadOnlyList<ActiveGamePlayerViewModel> _activeGameTeammates = [];

    [ObservableProperty]
    private IReadOnlyList<ActiveGamePlayerViewModel> _activeGameEnemies = [];

    [ObservableProperty]
    private string _activeGamePlayer = "Player unavailable";

    [ObservableProperty]
    private string _activeGameRunes = "Runes unavailable";

    [ObservableProperty]
    private ProfileMatchViewModel? _selectedProfileMatch;

    [ObservableProperty]
    private ProfileMatchViewModel? _profileMatchDetail;

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

    public IReadOnlyList<ChampionRoleOptionViewModel> QuickplayChampionRoles { get; } =
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
    private ChampionRoleOptionViewModel _selectedQuickplayChampionRole = null!;

    [ObservableProperty]
    private GameMode? _selectedGameMode;

    [ObservableProperty]
    private IReadOnlyList<GameMode> _gameModes = [];

    [ObservableProperty]
    private IReadOnlyList<string> _modeGroups = [];

    [ObservableProperty]
    private string _lastCommandResult = "";

    [ObservableProperty]
    private string _screen = "Home";

    [ObservableProperty]
    private string _selectedModeGroup = "Summoner's Rift";

    [ObservableProperty]
    private Bitmap? _summonersRiftIcon;

    [ObservableProperty]
    private Bitmap? _aramIcon;

    [ObservableProperty]
    private Bitmap? _loLClassicIcon;

    [ObservableProperty]
    private bool _isAramChampionSelect;

    [ObservableProperty]
    private bool _isAramMayhemChampionSelect;

    [ObservableProperty]
    private bool _isRandomCardChampionSelect;

    [ObservableProperty]
    private string _randomCardChampionSelectLabel = "Random Pick";

    [ObservableProperty]
    private string _currentChampion = "No champion selected";

    [ObservableProperty]
    private string _championSelectTimeline = "Waiting";

    [ObservableProperty]
    private string _championPoolStatus = "";

    [ObservableProperty]
    private string _banWarning = "";

    [ObservableProperty]
    private string _selectedPrimaryPosition = "TOP";

    [ObservableProperty]
    private string _selectedSecondaryPosition = "JUNGLE";

    [ObservableProperty]
    private string _lobbySetupStatus = "";

    [ObservableProperty]
    private IReadOnlyList<GameModeSetupSectionViewModel> _gameModeSetupSections = [];

    [ObservableProperty]
    private IReadOnlyList<QuickplaySlotViewModel> _quickplaySlots = [];

    [ObservableProperty]
    private QuickplaySlotViewModel? _activeQuickplaySlot;

    [ObservableProperty]
    private string _quickplaySearch = "";

    [ObservableProperty]
    private IReadOnlyList<ChampionTileViewModel> _quickplayChampions = [];

    [ObservableProperty]
    private bool _isSettingsOpen;

    [ObservableProperty]
    private bool _overrideQuickplaySpells;

    [ObservableProperty]
    private SummonerSpellOptionViewModel _selectedQuickplaySpell1 = null!;

    [ObservableProperty]
    private SummonerSpellOptionViewModel _selectedQuickplaySpell2 = null!;

    public IReadOnlyList<string> PositionOptions { get; } = ["TOP", "JUNGLE", "MIDDLE", "BOTTOM", "UTILITY"];
    public IReadOnlyList<ChampionTileViewModel> QuickplayChampionOptions => _allChampionTiles;
    public IReadOnlyList<SummonerSpellOptionViewModel> SummonerSpellOptions { get; } =
    [
        new("Cleanse", 1, "SummonerBoost.png"),
        new("Exhaust", 3, "SummonerExhaust.png"),
        new("Flash", 4, "SummonerFlash.png"),
        new("Ghost", 6, "SummonerHaste.png"),
        new("Heal", 7, "SummonerHeal.png"),
        new("Smite", 11, "SummonerSmite.png"),
        new("Teleport", 12, "SummonerTeleport.png"),
        new("Clarity", 13, "SummonerMana.png"),
        new("Ignite", 14, "SummonerDot.png"),
        new("Barrier", 21, "SummonerBarrier.png")
    ];

    public string LogPath => _log.Path;
    public string ChampionIconCachePath => ChampionIconCache.CacheDirectory;
    public string RiotDisclaimer => "Pickwise is not endorsed by Riot Games and does not reflect the views or opinions of Riot Games or anyone officially involved in producing or managing Riot Games properties. Riot Games and all associated properties are trademarks or registered trademarks of Riot Games, Inc.";
    public bool CanRespondReadyCheck => Phase == AppPhase.ReadyCheck;
    public bool CanChampionCommand => Phase == AppPhase.ChampionSelect
        && !IsRandomCardChampionSelect
        && SelectedChampion is not null
        && IsCurrentChampionPoolReady();
    public bool CanPickChampionCommand => CanChampionCommand
        && string.Equals(_currentChampionSelectSession?.CurrentLocalAction()?.Type, "pick", StringComparison.OrdinalIgnoreCase);
    public bool CanBanChampionCommand => CanChampionCommand
        && string.Equals(_currentChampionSelectSession?.CurrentLocalAction()?.Type, "ban", StringComparison.OrdinalIgnoreCase);
    public bool CanPickAramChampion => Phase == AppPhase.ChampionSelect
        && IsRandomCardChampionSelect
        && IsCurrentChampionPoolReady()
        && SelectedChampion is not null
        && AramAvailableChampions.Any(tile => tile.Champion.ChampionId == SelectedChampion.ChampionId);
    public bool CanSwapBenchChampion => Phase == AppPhase.ChampionSelect
        && IsRandomCardChampionSelect
        && IsCurrentChampionPoolReady()
        && SelectedChampion is not null
        && AramBenchChampions.Any(tile => tile.Champion.ChampionId == SelectedChampion.ChampionId);
    public bool CanCreateLobby => Phase == AppPhase.Connected && SelectedGameMode is not null;
    public bool CanUseMatchmaking => Phase == AppPhase.Connected;
    public bool CanLeaveLobby => Phase == AppPhase.Connected && _currentLobbyState is not null;
    public bool HasCurrentLobby => _currentLobbyState is not null || LobbyMembers.Count > 0;
    public bool HasStatusCta => Phase is AppPhase.ActiveGame or AppPhase.ChampionSelect || HasCurrentLobby;
    public string StatusCtaTitle =>
        Phase == AppPhase.ActiveGame ? "Active Game" :
        Phase == AppPhase.ChampionSelect ? "Champion Select" :
        "Current Lobby";
    public string StatusCtaDescription =>
        Phase == AppPhase.ActiveGame ? ActiveGame :
        Phase == AppPhase.ChampionSelect ? ChampionSelect :
        CurrentLobby;
    public bool HasSelectedChampion => SelectedChampion is not null;
    public bool HasBanWarning => !string.IsNullOrWhiteSpace(BanWarning);
    public bool HasChampionPoolStatus => !string.IsNullOrWhiteSpace(ChampionPoolStatus);
    public bool HasQuickBanChampions => QuickBanChampions.Count > 0;
    public bool HasTradeRequests => TradeRequests.Count > 0;
    public bool HasLobbyMembers => LobbyMembers.Count > 0;
    public bool HasProfileMatches => ProfileMatches.Count > 0;
    public bool HasNoProfileMatches => ProfileMatches.Count == 0;
    public bool HasAramMayhemAugmentStats => AramMayhemAugmentStats.Count > 0;
    public bool HasNoAramMayhemAugmentStats => IsAramMayhemAugmentPanelVisible && AramMayhemAugmentStats.Count == 0;
    public bool IsAramMayhemAugmentPanelVisible => Phase == AppPhase.ChampionSelect && IsAramMayhemChampionSelect && _currentChampionSelectChampionId > 0;
    public string AramMayhemAugmentEmptyText => _currentSummonerMatchSampleLoading ? "Loading ARAM Mayhem augment sample" : "No ARAM Mayhem augment sample";
    public bool IsAramMayhemLobby => _currentLobbyState?.GameConfig?.QueueId == 2400;
    public bool IsAramMayhemLobbyHistoryVisible => IsReadyScreen && IsAramMayhemLobby;
    public bool HasAramMayhemChampionSamples => AramMayhemChampionSamples.Count > 0;
    public bool HasNoAramMayhemChampionSamples => IsAramMayhemLobbyHistoryVisible && AramMayhemChampionSamples.Count == 0;
    public bool HasAramMayhemLobbyAugmentStats => AramMayhemLobbyAugmentStats.Count > 0;
    public bool HasNoAramMayhemLobbyAugmentStats => SelectedAramMayhemChampionSample is not null && AramMayhemLobbyAugmentStats.Count == 0;
    public string AramMayhemLobbyHistoryEmptyText => _currentSummonerMatchSampleLoading ? "Loading ARAM Mayhem history sample" : "No ARAM Mayhem history sample";
    public bool IsCurrentSummonerHomeTab => SelectedHomeTab == "Current Summoner";
    public bool IsAramMayhemHomeTab => SelectedHomeTab == "ARAM Mayhem";
    public bool HasAramMayhemGlobalChampionStats => AramMayhemGlobalChampionStats.Count > 0;
    public bool HasNoAramMayhemGlobalStats => IsAramMayhemHomeTab && AramMayhemGlobalChampionStats.Count == 0;
    public bool HasAramMayhemGlobalAugmentStats => AramMayhemGlobalAugmentStats.Count > 0;
    public bool HasNoAramMayhemGlobalAugmentStats => IsAramMayhemHomeTab && AramMayhemGlobalChampionStats.Count > 0 && AramMayhemGlobalAugmentStats.Count == 0;
    public string AramMayhemGlobalEmptyText => _currentSummonerMatchSampleLoading ? "Loading ARAM Mayhem sample" : "No ARAM Mayhem matches found in recent history";
    public bool IsAramMayhemChampionPoolSegment => SelectedAramMayhemLobbySegment == "Champion Pool";
    public bool IsAramMayhemAugmentsSegment => SelectedAramMayhemLobbySegment == "Augments";
    public bool HasActiveGameTeammates => ActiveGameTeammates.Count > 0;
    public bool HasActiveGameEnemies => ActiveGameEnemies.Count > 0;
    public bool IsProfileMatchDetailOpen => ProfileMatchDetail is not null;
    public bool IsHomeScreen => Screen == "Home";
    public bool IsReadyScreen => Screen == "Ready";
    public bool IsChampionSelectScreen => Screen == "ChampionSelect";
    public bool IsActiveGameScreen => Screen == "ActiveGame";
    public bool IsProfileScreen => Screen == "Profile";
    public bool CanOpenCurrentSummonerProfile => _currentSummoner is not null;
    public bool IsFiveVFiveChampionSelect => !IsRandomCardChampionSelect;
    public bool IsFiveVFiveChampionSelectActionBarVisible => IsChampionSelectScreen && IsFiveVFiveChampionSelect;
    public bool HasSummonersRiftIcon => SummonersRiftIcon is not null;
    public bool HasNoSummonersRiftIcon => SummonersRiftIcon is null;
    public bool HasAramIcon => AramIcon is not null;
    public bool HasNoAramIcon => AramIcon is null;
    public bool HasLoLClassicIcon => LoLClassicIcon is not null;
    public bool HasNoLoLClassicIcon => LoLClassicIcon is null;
    public bool HasLoLClassicMode => _allGameModes.Any(mode => mode.QueueId == 4310);
    public IReadOnlyList<string> VisibleModeGroups => ModeGroups
        .Where(group => group is not "Summoner's Rift" and not "ARAM" and not "LoL Classic")
        .ToList();
    public bool IsPositionSelectorVisible => _currentLobbyState?.GameConfig?.ShowPositionSelector == true;
    public bool IsQuickplaySetupVisible => _currentLobbyState?.GameConfig?.ShowQuickPlaySlotSelection == true;
    public bool HasGameModeSetup => GameModeSetupSections.Count > 0;
    public bool HasQuickplaySlots => QuickplaySlots.Count > 0;
    public bool IsQuickplayEditorOpen => ActiveQuickplaySlot is not null;
    public bool CanSaveSettings => SelectedQuickplaySpell1 is not null
        && SelectedQuickplaySpell2 is not null
        && SelectedQuickplaySpell1.Id != SelectedQuickplaySpell2.Id;
    public bool CanSaveQuickplaySlots => IsQuickplaySetupVisible
        && QuickplaySlots.Count == 2
        && QuickplaySlots.All(slot => slot.SelectedChampionTile is not null && PositionOptions.Contains(slot.SelectedPosition));
    public bool CanSavePositions => IsPositionSelectorVisible
        && PositionOptions.Contains(SelectedPrimaryPosition)
        && PositionOptions.Contains(SelectedSecondaryPosition)
        && SelectedPrimaryPosition != SelectedSecondaryPosition;

    public MainViewModel() : this(CreateDefaultLog())
    {
    }

    public MainViewModel(ILcuClient lcu, LocalDiagnosticLog log) : this(lcu, log, new ChampionCatalog(), new GameModeCatalog(), new ChampionIconCache(log), new GameModeIconCache(log), new SummonerIconCache(log), new SummonerSpellIconCache(log), new ItemIconCache(log), new ChampionPreferenceStore())
    {
    }

    public MainViewModel(ILcuClient lcu, LocalDiagnosticLog log, ChampionPreferenceStore preferenceStore) : this(lcu, log, new ChampionCatalog(), new GameModeCatalog(), new ChampionIconCache(log), new GameModeIconCache(log), new SummonerIconCache(log), new SummonerSpellIconCache(log), new ItemIconCache(log), preferenceStore)
    {
    }

    public MainViewModel(ILcuClient lcu, LocalDiagnosticLog log, ChampionCatalog championCatalog, GameModeCatalog gameModeCatalog, ChampionIconCache championIconCache, GameModeIconCache gameModeIconCache, SummonerIconCache summonerIconCache, SummonerSpellIconCache summonerSpellIconCache, ItemIconCache itemIconCache, ChampionPreferenceStore preferenceStore)
    {
        _lcu = lcu;
        _log = log;
        _championCatalog = championCatalog;
        _gameModeCatalog = gameModeCatalog;
        _championIconCache = championIconCache;
        _gameModeIconCache = gameModeIconCache;
        _summonerIconCache = summonerIconCache;
        _summonerSpellIconCache = summonerSpellIconCache;
        _itemIconCache = itemIconCache;
        _aramMayhemAugmentIconCache = new AramMayhemAugmentIconCache(log);
        _preferenceStore = preferenceStore;
        _preferences = _preferenceStore.Load();
        _allChampionTiles = _championCatalog.All.Select(champion => new ChampionTileViewModel(champion)).ToList();
        _championTilesById = _allChampionTiles.ToDictionary(tile => tile.Champion.ChampionId);
        ApplyPreferencesToTiles();
        Champions = _allChampionTiles;
        SelectedChampionRole = ChampionRoles[0];
        SelectedChampionRole.IsSelected = true;
        SelectedQuickplayChampionRole = QuickplayChampionRoles[0];
        SelectedQuickplayChampionRole.IsSelected = true;
        OverrideQuickplaySpells = _preferences.OverrideQuickplaySpells;
        SelectedQuickplaySpell1 = SpellOptionOrDefault(_preferences.QuickplaySpell1, 14);
        SelectedQuickplaySpell2 = SpellOptionOrDefault(_preferences.QuickplaySpell2, 4);
        _allGameModes = _gameModeCatalog.All;
        RefreshModeGroups();
        RefreshGameModes();
        SelectedGameMode = GameModes.FirstOrDefault();
        _ = Task.Run(() => LoadGameModesAsync(_polling.Token));
        _ = Task.Run(() => LoadGameModeIconsAsync(_polling.Token));
        _ = Task.Run(() => LoadSummonerSpellIconsAsync(_polling.Token));
        _ = Task.Run(() => LoadChampionIconsAsync(_polling.Token));
        _ = Task.Run(() => PollAsync(_polling.Token));
    }

    private MainViewModel(LocalDiagnosticLog log) : this(new KuncLcuClient(log), log)
    {
    }

    private static LocalDiagnosticLog CreateDefaultLog() => new();

    private static ChampionRoleOptionViewModel Role(string name, string path) =>
        new(name, path);

    private SummonerSpellOptionViewModel SpellOptionOrDefault(ulong spellId, ulong fallback) =>
        SummonerSpellOptions.FirstOrDefault(spell => spell.Id == spellId)
        ?? SummonerSpellOptions.First(spell => spell.Id == fallback);

    partial void OnPhaseChanged(AppPhase value)
    {
        if (Screen == "Profile")
        {
            return;
        }

        if (value == AppPhase.ReadyCheck)
        {
            Screen = "Ready";
        }
        else if ((value == AppPhase.ChampionSelect || value == AppPhase.ActiveGame) && _lastAutoOpenedPhase != value)
        {
            Screen = value == AppPhase.ActiveGame ? "ActiveGame" : "ChampionSelect";
            _lastAutoOpenedPhase = value;
        }
        else if (value is not AppPhase.ChampionSelect and not AppPhase.ActiveGame)
        {
            _lastAutoOpenedPhase = value;
        }

        OnPropertyChanged(nameof(CanRespondReadyCheck));
        OnPropertyChanged(nameof(CanChampionCommand));
        OnPropertyChanged(nameof(CanPickChampionCommand));
        OnPropertyChanged(nameof(CanBanChampionCommand));
        OnPropertyChanged(nameof(CanCreateLobby));
        OnPropertyChanged(nameof(CanUseMatchmaking));
        OnPropertyChanged(nameof(CanLeaveLobby));
        RefreshAramMayhemAugmentBindings();
        RefreshAramMayhemLobbyHistoryBindings();
        RefreshStatusCtaBindings();
    }

    partial void OnScreenChanged(string value)
    {
        OnPropertyChanged(nameof(IsHomeScreen));
        OnPropertyChanged(nameof(IsReadyScreen));
        OnPropertyChanged(nameof(IsChampionSelectScreen));
        OnPropertyChanged(nameof(IsFiveVFiveChampionSelectActionBarVisible));
        OnPropertyChanged(nameof(IsActiveGameScreen));
        OnPropertyChanged(nameof(IsProfileScreen));
        RefreshAramMayhemLobbyHistoryBindings();
    }

    private void RefreshStatusCtaBindings()
    {
        OnPropertyChanged(nameof(HasStatusCta));
        OnPropertyChanged(nameof(StatusCtaTitle));
        OnPropertyChanged(nameof(StatusCtaDescription));
    }

    partial void OnSummonersRiftIconChanged(Bitmap? value)
    {
        OnPropertyChanged(nameof(HasSummonersRiftIcon));
        OnPropertyChanged(nameof(HasNoSummonersRiftIcon));
    }

    partial void OnAramIconChanged(Bitmap? value)
    {
        OnPropertyChanged(nameof(HasAramIcon));
        OnPropertyChanged(nameof(HasNoAramIcon));
    }

    partial void OnLoLClassicIconChanged(Bitmap? value)
    {
        OnPropertyChanged(nameof(HasLoLClassicIcon));
        OnPropertyChanged(nameof(HasNoLoLClassicIcon));
    }

    partial void OnSelectedModeGroupChanged(string value)
    {
        RefreshGameModes();
    }

    partial void OnModeGroupsChanged(IReadOnlyList<string> value)
    {
        OnPropertyChanged(nameof(VisibleModeGroups));
        OnPropertyChanged(nameof(HasLoLClassicMode));
    }

    partial void OnSelectedPrimaryPositionChanged(string value)
    {
        if (!_hydratingPositions)
        {
            _positionEditDirty = true;
        }

        OnPropertyChanged(nameof(CanSavePositions));
        OnPropertyChanged(nameof(CanLeaveLobby));
        SavePositionsCommand.NotifyCanExecuteChanged();
        LeaveLobbyCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedSecondaryPositionChanged(string value)
    {
        if (!_hydratingPositions)
        {
            _positionEditDirty = true;
        }

        OnPropertyChanged(nameof(CanSavePositions));
        SavePositionsCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsAramChampionSelectChanged(bool value)
    {
        OnPropertyChanged(nameof(RandomCardChampionSelectLabel));
    }

    partial void OnIsAramMayhemChampionSelectChanged(bool value)
    {
        RefreshAramMayhemAugmentBindings();
    }

    partial void OnIsRandomCardChampionSelectChanged(bool value)
    {
        OnPropertyChanged(nameof(IsFiveVFiveChampionSelect));
        OnPropertyChanged(nameof(IsFiveVFiveChampionSelectActionBarVisible));
        OnPropertyChanged(nameof(CanChampionCommand));
        OnPropertyChanged(nameof(CanPickChampionCommand));
        OnPropertyChanged(nameof(CanBanChampionCommand));
        OnPropertyChanged(nameof(CanPickAramChampion));
        OnPropertyChanged(nameof(CanSwapBenchChampion));
        PickCommand.NotifyCanExecuteChanged();
        BanCommand.NotifyCanExecuteChanged();
        ToggleFavoriteCommand.NotifyCanExecuteChanged();
        ToggleQuickBanCommand.NotifyCanExecuteChanged();
        PickAramChampionCommand.NotifyCanExecuteChanged();
        SwapBenchChampionCommand.NotifyCanExecuteChanged();
    }

    partial void OnChampionSearchChanged(string value)
    {
        RefreshChampions();
    }

    partial void OnQuickplaySearchChanged(string value)
    {
        RefreshQuickplayChampions();
    }

    partial void OnActiveQuickplaySlotChanged(QuickplaySlotViewModel? value)
    {
        foreach (var slot in QuickplaySlots)
        {
            slot.IsActive = slot == value;
        }

        OnPropertyChanged(nameof(IsQuickplayEditorOpen));
        RefreshQuickplayChampions();
    }

    partial void OnSelectedChampionRoleChanged(ChampionRoleOptionViewModel value)
    {
        foreach (var role in ChampionRoles)
        {
            role.IsSelected = role == value;
        }

        RefreshChampions();
    }

    partial void OnSelectedQuickplayChampionRoleChanged(ChampionRoleOptionViewModel value)
    {
        foreach (var role in QuickplayChampionRoles)
        {
            role.IsSelected = role == value;
        }

        RefreshQuickplayChampions();
    }

    partial void OnOverrideQuickplaySpellsChanged(bool value)
    {
        SaveSettingsCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedQuickplaySpell1Changed(SummonerSpellOptionViewModel value)
    {
        SaveSettingsCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedQuickplaySpell2Changed(SummonerSpellOptionViewModel value)
    {
        SaveSettingsCommand.NotifyCanExecuteChanged();
    }

    private void RefreshChampions()
    {
        Champions = FilterForCurrentChampionSelect(_championCatalog.Filter(ChampionSearch, SelectedChampionRole.Name))
            .OrderByDescending(champion => _preferences.Favorites.Contains(champion.ChampionId))
            .ThenBy(champion => champion.Name)
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
        if (value is not null
            && Phase == AppPhase.ChampionSelect
            && !IsRandomCardChampionSelect
            && IsCurrentChampionPoolReady()
            && !_suppressDeclare
            && _currentChampionSelectSession?.OpenAction("pick") is not null)
        {
            _ = DeclareChampionAsync(value.Champion.ChampionId);
        }
    }

    partial void OnSelectedChampionChanged(Champion? value)
    {
        OnPropertyChanged(nameof(CanChampionCommand));
        OnPropertyChanged(nameof(CanPickChampionCommand));
        OnPropertyChanged(nameof(CanBanChampionCommand));
        OnPropertyChanged(nameof(CanPickAramChampion));
        OnPropertyChanged(nameof(CanSwapBenchChampion));
        OnPropertyChanged(nameof(HasSelectedChampion));
        RefreshBanWarning();
        PickCommand.NotifyCanExecuteChanged();
        BanCommand.NotifyCanExecuteChanged();
        ToggleFavoriteCommand.NotifyCanExecuteChanged();
        ToggleQuickBanCommand.NotifyCanExecuteChanged();
        PickAramChampionCommand.NotifyCanExecuteChanged();
        SwapBenchChampionCommand.NotifyCanExecuteChanged();
    }

    partial void OnBanWarningChanged(string value)
    {
        OnPropertyChanged(nameof(HasBanWarning));
    }

    partial void OnQuickBanChampionsChanged(IReadOnlyList<ChampionTileViewModel> value)
    {
        OnPropertyChanged(nameof(HasQuickBanChampions));
    }

    partial void OnGameModeSetupSectionsChanged(IReadOnlyList<GameModeSetupSectionViewModel> value)
    {
        OnPropertyChanged(nameof(HasGameModeSetup));
    }

    partial void OnActiveGameChanged(string value)
    {
        RefreshStatusCtaBindings();
    }

    partial void OnChampionSelectChanged(string value)
    {
        RefreshStatusCtaBindings();
    }

    partial void OnCurrentLobbyChanged(string value)
    {
        RefreshStatusCtaBindings();
    }

    partial void OnQuickplaySlotsChanged(IReadOnlyList<QuickplaySlotViewModel> value)
    {
        OnPropertyChanged(nameof(HasQuickplaySlots));
        OnPropertyChanged(nameof(CanSaveQuickplaySlots));
        SaveQuickplaySlotsCommand.NotifyCanExecuteChanged();
    }

    partial void OnProfileMatchesChanged(IReadOnlyList<ProfileMatchViewModel> value)
    {
        OnPropertyChanged(nameof(HasProfileMatches));
        OnPropertyChanged(nameof(HasNoProfileMatches));
    }

    partial void OnAramMayhemAugmentStatsChanged(IReadOnlyList<AramMayhemAugmentSampleViewModel> value)
    {
        OnPropertyChanged(nameof(HasAramMayhemAugmentStats));
        OnPropertyChanged(nameof(HasNoAramMayhemAugmentStats));
        QueueAramMayhemAugmentCatalogLoad();
        _ = Task.Run(() => LoadAramMayhemLobbyAugmentIconsAsync(value, _polling.Token));
    }

    partial void OnAramMayhemChampionSamplesChanged(IReadOnlyList<AramMayhemChampionSampleViewModel> value)
    {
        OnPropertyChanged(nameof(HasAramMayhemChampionSamples));
        OnPropertyChanged(nameof(HasNoAramMayhemChampionSamples));
    }

    partial void OnSelectedAramMayhemChampionSampleChanged(AramMayhemChampionSampleViewModel? value)
    {
        RefreshAramMayhemLobbyAugmentStats();
    }

    partial void OnAramMayhemLobbyAugmentStatsChanged(IReadOnlyList<AramMayhemAugmentSampleViewModel> value)
    {
        OnPropertyChanged(nameof(HasAramMayhemLobbyAugmentStats));
        OnPropertyChanged(nameof(HasNoAramMayhemLobbyAugmentStats));
        QueueAramMayhemAugmentCatalogLoad();
        _ = Task.Run(() => LoadAramMayhemLobbyAugmentIconsAsync(value, _polling.Token));
    }

    partial void OnSelectedHomeTabChanged(string value)
    {
        OnPropertyChanged(nameof(IsCurrentSummonerHomeTab));
        OnPropertyChanged(nameof(IsAramMayhemHomeTab));
        OnPropertyChanged(nameof(HasNoAramMayhemGlobalStats));
        OnPropertyChanged(nameof(HasNoAramMayhemGlobalAugmentStats));
    }

    partial void OnAramMayhemGlobalChampionStatsChanged(IReadOnlyList<AramMayhemGlobalChampionViewModel> value)
    {
        OnPropertyChanged(nameof(HasAramMayhemGlobalChampionStats));
        OnPropertyChanged(nameof(HasNoAramMayhemGlobalStats));
        OnPropertyChanged(nameof(HasNoAramMayhemGlobalAugmentStats));
    }

    partial void OnSelectedAramMayhemGlobalChampionChanged(AramMayhemGlobalChampionViewModel? value)
    {
        RefreshAramMayhemGlobalAugmentStats();
    }

    partial void OnAramMayhemGlobalAugmentStatsChanged(IReadOnlyList<AramMayhemAugmentSampleViewModel> value)
    {
        OnPropertyChanged(nameof(HasAramMayhemGlobalAugmentStats));
        OnPropertyChanged(nameof(HasNoAramMayhemGlobalAugmentStats));
        QueueAramMayhemAugmentCatalogLoad();
        _ = Task.Run(() => LoadAramMayhemLobbyAugmentIconsAsync(value, _polling.Token));
    }

    partial void OnSelectedAramMayhemLobbySegmentChanged(string value)
    {
        OnPropertyChanged(nameof(IsAramMayhemChampionPoolSegment));
        OnPropertyChanged(nameof(IsAramMayhemAugmentsSegment));
    }

    partial void OnActiveGameTeammatesChanged(IReadOnlyList<ActiveGamePlayerViewModel> value)
    {
        OnPropertyChanged(nameof(HasActiveGameTeammates));
    }

    partial void OnActiveGameEnemiesChanged(IReadOnlyList<ActiveGamePlayerViewModel> value)
    {
        OnPropertyChanged(nameof(HasActiveGameEnemies));
    }

    partial void OnProfileMatchDetailChanged(ProfileMatchViewModel? value)
    {
        OnPropertyChanged(nameof(IsProfileMatchDetailOpen));
    }

    partial void OnTradeRequestsChanged(IReadOnlyList<ChampionTradeRequest> value)
    {
        OnPropertyChanged(nameof(HasTradeRequests));
    }

    partial void OnLobbyMembersChanged(IReadOnlyList<LobbyMemberViewModel> value)
    {
        OnPropertyChanged(nameof(HasLobbyMembers));
        OnPropertyChanged(nameof(HasCurrentLobby));
        RefreshStatusCtaBindings();
    }

    partial void OnSelectedGameModeChanged(GameMode? value)
    {
        OnPropertyChanged(nameof(CanCreateLobby));
        if (Dispatcher.UIThread.CheckAccess())
        {
            CreateLobbyCommand.NotifyCanExecuteChanged();
        }
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
            token => _lcu.CreateLobbyAsync(SelectedGameMode, token),
            $"Created {SelectedGameMode.Name} lobby",
            refreshSnapshot: true);
    }

    [RelayCommand(CanExecute = nameof(CanSavePositions))]
    private Task SavePositionsAsync() =>
        RunCommandAsync(
            async token =>
            {
                await _lcu.UpdatePositionPreferencesAsync(SelectedPrimaryPosition, SelectedSecondaryPosition, token);
                _positionEditDirty = false;
            },
            $"Saved lanes: {SelectedPrimaryPosition}, {SelectedSecondaryPosition}",
            refreshSnapshot: true);

    [RelayCommand(CanExecute = nameof(CanSaveQuickplaySlots))]
    private Task SaveQuickplaySlotsAsync() =>
        RunCommandAsync(
            async token =>
            {
                var slots = new List<LobbyPlayerSlot>(QuickplaySlots.Count);
                foreach (var slot in QuickplaySlots)
                {
                    slots.Add(await ToQuickplaySlotAsync(slot, token));
                }

                await _lcu.UpdateQuickplaySlotsAsync(slots, token);
            },
            "Saved Quickplay slots",
            refreshSnapshot: true);

    [RelayCommand(CanExecute = nameof(CanUseMatchmaking))]
    private Task StartMatchmakingAsync()
    {
        if (_currentLobbyState?.BlocksSwiftplayMatchmaking == true)
        {
            LastCommandResult = SwiftplayRestrictionMessage(_currentLobbyState);
            return Task.CompletedTask;
        }

        return RunCommandAsync(_lcu.StartMatchmakingAsync, "Started matchmaking", refreshSnapshot: true);
    }

    [RelayCommand(CanExecute = nameof(CanUseMatchmaking))]
    private Task CancelMatchmakingAsync() => RunCommandAsync(_lcu.CancelMatchmakingAsync, "Cancelled matchmaking", refreshSnapshot: true);

    [RelayCommand(CanExecute = nameof(CanLeaveLobby))]
    private async Task LeaveLobbyAsync()
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _lcu.LeaveLobbyAsync(timeout.Token);
            LastCommandResult = "Left lobby";
            await RefreshSnapshotAsync();
            Screen = "Home";
        }
        catch (Exception exception)
        {
            LastCommandResult = exception.Message;
        }
    }

    [RelayCommand]
    private void OpenCurrentLobby()
    {
        Screen = "Ready";
    }

    [RelayCommand]
    private void SelectHomeTab(string tab)
    {
        if (tab is "Current Summoner" or "ARAM Mayhem")
        {
            SelectedHomeTab = tab;
        }
    }

    [RelayCommand]
    private void SelectAramMayhemChampionSample(AramMayhemChampionSampleViewModel sample)
    {
        SelectedAramMayhemChampionSample = sample;
        foreach (var championSample in AramMayhemChampionSamples)
        {
            championSample.IsSelected = championSample == sample;
        }

        SelectedAramMayhemLobbySegment = "Augments";
    }

    [RelayCommand]
    private void SelectAramMayhemGlobalChampion(AramMayhemGlobalChampionViewModel sample)
    {
        SelectedAramMayhemGlobalChampion = sample;
        foreach (var championSample in AramMayhemGlobalChampionStats)
        {
            championSample.IsSelected = championSample == sample;
        }
    }

    [RelayCommand]
    private void SelectAramMayhemLobbySegment(string segment)
    {
        if (segment is "Champion Pool" or "Augments")
        {
            SelectedAramMayhemLobbySegment = segment;
        }
    }

    [RelayCommand]
    private void OpenStatus()
    {
        Screen = Phase == AppPhase.ActiveGame ? "ActiveGame" :
            Phase == AppPhase.ChampionSelect ? "ChampionSelect" :
            "Ready";
    }

    [RelayCommand]
    private void SelectModeGroup(string group)
    {
        SelectedModeGroup = group;
        Screen = "Ready";
    }

    [RelayCommand]
    private void Back()
    {
        if (Screen == "Profile")
        {
            Screen = ForcedScreenOr(_profileBackScreen);
            return;
        }

        if (Phase == AppPhase.ChampionSelect)
        {
            Screen = "ChampionSelect";
        }
        else if (Phase == AppPhase.ActiveGame)
        {
            Screen = "ActiveGame";
        }
        else if (Phase == AppPhase.ReadyCheck)
        {
            Screen = "Ready";
        }
        else
        {
            Screen = "Home";
        }
    }

    [RelayCommand(CanExecute = nameof(CanOpenCurrentSummonerProfile))]
    private Task OpenCurrentSummonerProfileAsync() =>
        _currentSummoner is null
            ? Task.CompletedTask
            : OpenProfileAsync(SummonerProfile.FromCurrent(_currentSummoner));

    [RelayCommand]
    private Task CheckProfileAsync(LobbyMemberViewModel member) =>
        OpenProfileAsync(SummonerProfile.FromLobbyMember(member.Member));

    [RelayCommand(CanExecute = nameof(CanAddFriend))]
    private Task AddFriendAsync(LobbyMemberViewModel member) =>
        RunCommandAsync(token => _lcu.SendFriendRequestAsync(member.Member, token), $"Sent friend request to {member.Name}");

    [RelayCommand]
    private void ToggleProfileMatch(ProfileMatchViewModel match)
    {
        var shouldExpand = !match.IsExpanded;
        foreach (var profileMatch in ProfileMatches)
        {
            profileMatch.IsExpanded = false;
        }

        match.IsExpanded = shouldExpand;
        SelectedProfileMatch = shouldExpand ? match : null;
    }

    [RelayCommand]
    private void OpenProfileMatchDetail(ProfileMatchViewModel match)
    {
        ProfileMatchDetail = match;
    }

    [RelayCommand]
    private void CloseProfileMatchDetail()
    {
        ProfileMatchDetail = null;
    }

    [RelayCommand]
    private void OpenSettings()
    {
        IsSettingsOpen = true;
    }

    [RelayCommand]
    private void CloseSettings()
    {
        IsSettingsOpen = false;
    }

    [RelayCommand(CanExecute = nameof(CanSaveSettings))]
    private void SaveSettings()
    {
        _preferences.OverrideQuickplaySpells = OverrideQuickplaySpells;
        _preferences.QuickplaySpell1 = SelectedQuickplaySpell1.Id;
        _preferences.QuickplaySpell2 = SelectedQuickplaySpell2.Id;
        _preferenceStore.Save(_preferences);
        LastCommandResult = "Saved settings";
        IsSettingsOpen = false;
    }

    [RelayCommand]
    private void OpenQuickplaySlot(QuickplaySlotViewModel slot)
    {
        ActiveQuickplaySlot = slot;
    }

    [RelayCommand]
    private void CloseQuickplayEditor()
    {
        ActiveQuickplaySlot = null;
    }

    [RelayCommand]
    private void SelectQuickplayChampion(ChampionTileViewModel tile)
    {
        if (ActiveQuickplaySlot is null)
        {
            return;
        }

        ActiveQuickplaySlot.SelectedChampionTile = tile;
    }

    [RelayCommand]
    private void SelectQuickplayChampionRole(ChampionRoleOptionViewModel role)
    {
        SelectedQuickplayChampionRole = role;
    }

    [RelayCommand]
    private void SelectChampionRole(ChampionRoleOptionViewModel role)
    {
        SelectedChampionRole = role;
    }

    [RelayCommand(CanExecute = nameof(CanPickChampionCommand))]
    private Task PickAsync() => RunChampionCommandAsync(_lcu.PickChampionAsync, "Pick submitted");

    [RelayCommand]
    private async Task PickChampionTileAsync(ChampionTileViewModel tile)
    {
        SelectedChampionTile = tile;
        if (!CanPickChampionCommand)
        {
            return;
        }

        await RunChampionCommandAsync(_lcu.PickChampionAsync, "Pick submitted");
    }

    [RelayCommand(CanExecute = nameof(CanBanChampionCommand))]
    private Task BanAsync() => RunChampionCommandAsync(_lcu.BanChampionAsync, "Ban submitted");

    [RelayCommand]
    private async Task BanChampionTileAsync(ChampionTileViewModel tile)
    {
        _suppressDeclare = true;
        SelectedChampionTile = tile;
        _suppressDeclare = false;
        if (!CanBanChampionCommand)
        {
            return;
        }

        await RunChampionCommandAsync(_lcu.BanChampionAsync, "Ban submitted");
    }

    [RelayCommand(CanExecute = nameof(HasSelectedChampion))]
    private void ToggleFavorite()
    {
        TogglePreference(_preferences.Favorites);
        RefreshChampions();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedChampion))]
    private void ToggleQuickBan()
    {
        TogglePreference(_preferences.QuickBans);
        RefreshQuickBans();
    }

    [RelayCommand(CanExecute = nameof(CanPickAramChampion))]
    private Task PickAramChampionAsync() => RunChampionCommandAsync(_lcu.PickChampionAsync, "ARAM champion selected");

    [RelayCommand(CanExecute = nameof(CanSwapBenchChampion))]
    private Task SwapBenchChampionAsync() => RunChampionCommandAsync(_lcu.SwapBenchChampionAsync, "Swapped bench champion");

    [RelayCommand]
    private Task AcceptTradeAsync(ChampionTradeRequest trade) =>
        RunCommandAsync(token => _lcu.AcceptTradeAsync(trade.Id, token), "Accepted trade");

    [RelayCommand]
    private Task DeclineTradeAsync(ChampionTradeRequest trade) =>
        RunCommandAsync(token => _lcu.DeclineTradeAsync(trade.Id, token), "Declined trade");

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
                Apply(new(AppPhase.WaitingForLeagueClient, null, null, null, null, null, [], [], [], [], "Waiting for League Client"));
            }
            catch (Exception exception)
            {
                _log.Error("Polling failed", exception);
                Apply(new(AppPhase.Error, null, null, null, null, null, [], [], [], [], "Polling failed; see diagnostic log"));
            }

            await Task.Delay(PollingDelayFor(Phase, Screen, _currentLobbyState is not null || LobbyMembers.Count > 0), cancellationToken).ConfigureAwait(false);
        }
    }

    public static TimeSpan PollingDelayFor(AppPhase phase, string screen, bool hasLobby) =>
        phase is AppPhase.ReadyCheck or AppPhase.ChampionSelect ? TimeSpan.FromSeconds(1) :
        phase == AppPhase.ActiveGame ? TimeSpan.FromSeconds(1) :
        screen == "Profile" ? TimeSpan.FromSeconds(8) :
        screen == "Ready" && hasLobby ? TimeSpan.FromSeconds(2) :
        phase == AppPhase.Connected ? TimeSpan.FromSeconds(8) :
        TimeSpan.FromSeconds(3);

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

    private async Task LoadSummonerSpellIconsAsync(CancellationToken cancellationToken)
    {
        try
        {
            foreach (var spell in SummonerSpellOptions)
            {
                var icon = await _summonerSpellIconCache.LoadAsync(spell.ImageFileName, cancellationToken).ConfigureAwait(false);
                if (icon is not null)
                {
                    Dispatcher.UIThread.Post(() => spell.Icon = icon);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _log.Error("Summoner spell icon loading failed", exception);
        }
    }

    private async Task LoadGameModeIconsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var summonersRift = await _gameModeIconCache.LoadAsync("map11.png", "Summoner's Rift", cancellationToken).ConfigureAwait(false);
            var aram = await _gameModeIconCache.LoadAsync("map12.png", "ARAM", cancellationToken).ConfigureAwait(false);
            Dispatcher.UIThread.Post(() =>
            {
                SummonersRiftIcon = summonersRift;
                AramIcon = aram;
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _log.Error("Game mode icon loading failed", exception);
        }
    }

    private async Task LoadLoLClassicIconAsync(GameMode? mode, CancellationToken cancellationToken)
    {
        if (mode is null)
        {
            return;
        }

        try
        {
            var icon = await _gameModeIconCache.LoadLcuAsync(_lcu, mode, cancellationToken).ConfigureAwait(false);
            if (icon is not null)
            {
                Dispatcher.UIThread.Post(() => LoLClassicIcon = icon);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _log.Error("LoL Classic icon loading failed", exception);
        }
    }

    private async Task LoadGameModesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var modes = await _lcu.GetQueuesAsync(cancellationToken).ConfigureAwait(false);
            if (modes.Count == 0)
            {
                return;
            }

            _allGameModes = modes;
            OnPropertyChanged(nameof(HasLoLClassicMode));
            RefreshModeGroups();
            RefreshGameModes();
            await LoadLoLClassicIconAsync(modes.FirstOrDefault(mode => mode.IsLoLClassic), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _log.Error("Game mode loading failed", exception);
        }
    }

    private void RefreshGameModes()
    {
        GameModes = _allGameModes
            .Where(mode => mode.DisplayGroup == SelectedModeGroup)
            .ToList();
        SelectedGameMode = GameModes.FirstOrDefault();
    }

    private static bool IsAram(GameMode mode) => mode.QueueId is 450 or 2400;

    private void RefreshModeGroups()
    {
        ModeGroups = _allGameModes
            .Select(mode => mode.DisplayGroup)
            .Distinct()
            .OrderBy(group => group == "Summoner's Rift" ? 0 : group == "LoL Classic" ? 1 : group == "Co-op vs AI" ? 2 : group == "ARAM" ? 3 : group == "Other" ? 4 : group == "TFT" ? 5 : 6)
            .ThenBy(group => group)
            .ToList();
        if (!ModeGroups.Contains(SelectedModeGroup))
        {
            SelectedModeGroup = ModeGroups.FirstOrDefault() ?? "Summoner's Rift";
        }
    }

    private IEnumerable<Champion> FilterForCurrentChampionSelect(IReadOnlyList<Champion> champions)
    {
        if (_currentChampionSelectSession is null)
        {
            return champions;
        }

        var banned = _currentChampionSelectSession.BannedChampionIds;
        var blocked = _disabledChampionIds.Concat(banned).ToHashSet();
        if (IsRandomCardChampionSelect)
        {
            return [];
        }

        if (_championAvailability is not null && !IsCurrentChampionPoolReady())
        {
            return [];
        }

        var currentAction = _currentChampionSelectSession.IsPlanning
            && _currentChampionSelectSession.OpenAction("pick") is not null
            ? _currentChampionSelectSession.OpenAction("pick")
            : _currentChampionSelectSession.CurrentLocalAction();
        if (_championAvailability is null)
        {
            return string.Equals(currentAction?.Type, "pick", StringComparison.OrdinalIgnoreCase) && _pickableChampionIds.Count > 0
                ? champions.Where(champion => _pickableChampionIds.Contains(champion.ChampionId) && !blocked.Contains(champion.ChampionId))
                : champions.Where(champion => !blocked.Contains(champion.ChampionId));
        }

        if (string.Equals(currentAction?.Type, "ban", StringComparison.OrdinalIgnoreCase))
        {
            return champions.Where(champion => _bannableChampionIds.Contains(champion.ChampionId)
                && IsAccountEligible(champion.ChampionId)
                && !blocked.Contains(champion.ChampionId));
        }

        if (string.Equals(currentAction?.Type, "pick", StringComparison.OrdinalIgnoreCase))
        {
            return champions.Where(champion => _pickableChampionIds.Contains(champion.ChampionId)
                && IsAccountEligible(champion.ChampionId)
                && !blocked.Contains(champion.ChampionId));
        }

        return _championAvailability is null
            ? champions.Where(champion => !blocked.Contains(champion.ChampionId))
            : [];
    }

    private bool IsAccountEligible(int championId) =>
        _championAvailability is null || _accountEligibleChampionIds.Contains(championId);

    private bool IsCurrentChampionPoolReady()
    {
        if (_championAvailability is null)
        {
            return true;
        }

        var planningPick = _currentChampionSelectSession?.IsPlanning == true
            && _currentChampionSelectSession.OpenAction("pick") is not null;
        return !planningPick
            && string.Equals(_currentChampionSelectSession?.CurrentLocalAction()?.Type, "ban", StringComparison.OrdinalIgnoreCase)
            ? _championAvailability.IsBannableReady
            : _championAvailability.IsReady;
    }

    private void ApplyPreferencesToTiles()
    {
        var favorites = _preferences.Favorites.ToHashSet();
        var quickBans = _preferences.QuickBans.ToHashSet();
        foreach (var tile in _allChampionTiles)
        {
            tile.IsFavorite = favorites.Contains(tile.Champion.ChampionId);
            tile.IsQuickBan = quickBans.Contains(tile.Champion.ChampionId);
        }
    }

    private void TogglePreference(List<int> championIds)
    {
        if (SelectedChampion is null)
        {
            return;
        }

        if (!championIds.Remove(SelectedChampion.ChampionId))
        {
            championIds.Add(SelectedChampion.ChampionId);
        }

        ApplyPreferencesToTiles();
        _preferenceStore.Save(_preferences);
    }

    private void RefreshQuickBans()
    {
        if (!string.Equals(_currentChampionSelectSession?.CurrentLocalAction()?.Type, "ban", StringComparison.OrdinalIgnoreCase))
        {
            QuickBanChampions = [];
            return;
        }

        QuickBanChampions = FilterForCurrentChampionSelect(_championCatalog.All)
            .Where(champion => _preferences.QuickBans.Contains(champion.ChampionId))
            .OrderBy(champion => _preferences.QuickBans.IndexOf(champion.ChampionId))
            .Select(champion => _championTilesById[champion.ChampionId])
            .ToList();
    }

    private void ApplyChampionSelectState(LcuSnapshot snapshot)
    {
        _currentChampionSelectSession = snapshot.ChampionSelect;
        _championAvailability = snapshot.ChampionAvailability
            ?? (IsLoLClassic(snapshot.Gameflow)
                ? ChampionSelectAvailability.Unavailable("Đang tải bể tướng…")
                : null);
        _pickableChampionIds = (_championAvailability?.PickableChampionIds ?? snapshot.PickableChampionIds).ToHashSet();
        _bannableChampionIds = (_championAvailability?.BannableChampionIds ?? snapshot.PickableChampionIds).ToHashSet();
        _disabledChampionIds = (_championAvailability?.DisabledChampionIds ?? snapshot.DisabledChampionIds).ToHashSet();
        _accountEligibleChampionIds = (_championAvailability?.AccountEligibleChampionIds ?? _championCatalog.All.Select(champion => champion.ChampionId)).ToHashSet();
        ChampionPoolStatus = snapshot.ChampionSelect is null
            ? ""
            : _championAvailability is null || IsCurrentChampionPoolReady()
                ? ""
                : _championAvailability.ErrorMessage ?? "Đang tải bể tướng…";
        _allyHoveredChampionIds = snapshot.ChampionSelect?.AllyHoveredChampionIds ?? new HashSet<int>();
        IsAramChampionSelect = IsAram(snapshot.Gameflow);
        IsAramMayhemChampionSelect = IsAramMayhem(snapshot.Gameflow);
        IsRandomCardChampionSelect = IsRandomCardChampionSelectSession(snapshot);
        RandomCardChampionSelectLabel = IsAramChampionSelect ? "ARAM" : "Random Pick";
        ChampionSelectTimeline = GetChampionSelectTimeline(snapshot.ChampionSelect);
        TradeRequests = snapshot.TradeRequests.Where(trade => trade.IsPending).ToList();

        var localChampionId = snapshot.ChampionSelect?.LocalPlayer?.ChampionId ?? 0;
        _currentChampionSelectChampionId = localChampionId;
        CurrentChampion = localChampionId > 0 && _championTilesById.TryGetValue(localChampionId, out var localChampion)
            ? localChampion.Name
            : "No champion selected";

        AramAvailableChampions = localChampionId > 0 || !IsCurrentChampionPoolReady()
            ? []
            : ToChampionTiles(_pickableChampionIds);
        AramBenchChampions = ToChampionTiles(snapshot.ChampionSelect?.AvailableBenchChampionIds ?? []);

        RefreshChampions();
        RefreshQuickBans();
        RefreshAramMayhemAugmentStats();
        RefreshBanWarning();
        OnPropertyChanged(nameof(CanChampionCommand));
        OnPropertyChanged(nameof(CanPickChampionCommand));
        OnPropertyChanged(nameof(CanBanChampionCommand));
        OnPropertyChanged(nameof(HasChampionPoolStatus));
        OnPropertyChanged(nameof(CanPickAramChampion));
        OnPropertyChanged(nameof(CanSwapBenchChampion));
        PickAramChampionCommand.NotifyCanExecuteChanged();
        SwapBenchChampionCommand.NotifyCanExecuteChanged();
        PickCommand.NotifyCanExecuteChanged();
        BanCommand.NotifyCanExecuteChanged();
    }

    private static string GetChampionSelectTimeline(ChampionSelectSession? session)
    {
        if (session is null)
        {
            return "Waiting";
        }

        var currentAction = session.IsPlanning && session.OpenAction("pick") is not null
            ? session.OpenAction("pick")
            : session.CurrentLocalAction();
        if (string.Equals(currentAction?.Type, "ban", StringComparison.OrdinalIgnoreCase))
        {
            return "Banning";
        }

        if (string.Equals(currentAction?.Type, "pick", StringComparison.OrdinalIgnoreCase))
        {
            return session.LocalPlayer?.ChampionId > 0 ? "Picking" : "Declaring";
        }

        var localActions = session.Actions.SelectMany(group => group).Where(action => action.ActorCellId == session.LocalPlayerCellId).ToList();
        return localActions.Count > 0 && localActions.All(action => action.Completed) ? "Completed" : "Waiting";
    }

    private void RefreshBanWarning()
    {
        BanWarning = SelectedChampion is not null && _allyHoveredChampionIds.Contains(SelectedChampion.ChampionId)
            ? "Ally is hovering this champion"
            : "";
    }

    [RelayCommand]
    private async Task PickAramChampionTileAsync(ChampionTileViewModel tile)
    {
        SelectedChampionTile = tile;
        if (!CanPickAramChampion)
        {
            return;
        }

        await RunChampionCommandAsync(_lcu.PickChampionAsync, $"{RandomCardChampionSelectLabel} champion selected");
    }

    [RelayCommand]
    private async Task SwapBenchChampionTileAsync(ChampionTileViewModel tile)
    {
        SelectedChampionTile = tile;
        if (!CanSwapBenchChampion)
        {
            return;
        }

        await RunChampionCommandAsync(_lcu.SwapBenchChampionAsync, "Swapped bench champion");
    }

    private IReadOnlyList<ChampionTileViewModel> ToChampionTiles(IEnumerable<int> championIds) =>
        championIds
            .Distinct()
            .Where(championId => IsAccountEligible(championId))
            .Where(championId => !_disabledChampionIds.Contains(championId)
                && !(_currentChampionSelectSession?.BannedChampionIds.Contains(championId) ?? false))
            .Where(championId => _championTilesById.ContainsKey(championId))
            .Select(championId => _championTilesById[championId])
            .ToList();

    private static bool IsAram(GameflowSession? gameflow)
    {
        var queue = gameflow?.GameData?.Queue;
        var queueId = queue?.QueueId ?? queue?.Id;
        return queueId is 450 or 2400
            || (queue?.GameMode?.Contains("ARAM", StringComparison.OrdinalIgnoreCase) == true);
    }

    private static bool IsLoLClassic(GameflowSession? gameflow)
    {
        var queue = gameflow?.GameData?.Queue;
        var queueId = queue?.QueueId ?? queue?.Id;
        return queueId is 4310 or 3262
            || string.Equals(queue?.GameMode, "JADE", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAramMayhem(GameflowSession? gameflow)
    {
        var queue = gameflow?.GameData?.Queue;
        var queueId = queue?.QueueId ?? queue?.Id;
        return queueId == 2400;
    }

    private static bool IsRandomCardChampionSelectSession(LcuSnapshot snapshot)
    {
        var session = snapshot.ChampionSelect;
        return IsAram(snapshot.Gameflow)
            || session?.AllowSubsetChampionPicks == true
            || session?.AvailableBenchChampionIds.Count > 0;
    }

    private static string ChampionSelectModeLabel(LcuSnapshot snapshot) =>
        IsAram(snapshot.Gameflow) ? "ARAM" :
        IsRandomCardChampionSelectSession(snapshot) ? "Random Pick" :
        "5v5";

    private void Apply(LcuSnapshot snapshot)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => Apply(snapshot));
            return;
        }

        Phase = snapshot.Phase;
        Status = snapshot.Message;
        _currentSummoner = snapshot.Summoner;
        EnsureCurrentSummonerMatchSample(snapshot.Summoner);
        _currentLobbyState = snapshot.Lobby;
        OnPropertyChanged(nameof(HasCurrentLobby));
        OnPropertyChanged(nameof(IsAramMayhemLobby));
        RefreshAramMayhemLobbyHistory();
        RefreshStatusCtaBindings();
        Summoner = snapshot.Summoner?.Name ?? "Not connected";
        CurrentLobby = DescribeLobby(snapshot.Lobby, snapshot.LobbyMembers);
        ApplyLobbySetupState(snapshot.Lobby);
        var membersNeedingIcons = ApplyLobbyMembers(snapshot);
        if (membersNeedingIcons.Count > 0)
        {
            _ = Task.Run(() => LoadLobbyMemberIconsAsync(membersNeedingIcons, _polling.Token));
        }
        ApplyScreenForSnapshot(snapshot);
        ReadyCheck = snapshot.ReadyCheck is null
            ? "No ready check"
            : $"{snapshot.ReadyCheck.State} / {snapshot.ReadyCheck.PlayerResponse}";
        ChampionSelect = snapshot.ChampionSelect is null
            ? "Not in champion select"
            : $"{ChampionSelectModeLabel(snapshot)} - Allies: {snapshot.ChampionSelect.MyTeam.Count}, enemies: {snapshot.ChampionSelect.TheirTeam.Count}";
        ApplyChampionSelectState(snapshot);
        ApplyActiveGameState(snapshot.ActiveGame);
        AcceptCommand.NotifyCanExecuteChanged();
        DeclineCommand.NotifyCanExecuteChanged();
        OpenCurrentSummonerProfileCommand.NotifyCanExecuteChanged();
        AddFriendCommand.NotifyCanExecuteChanged();
        CreateLobbyCommand.NotifyCanExecuteChanged();
        StartMatchmakingCommand.NotifyCanExecuteChanged();
        CancelMatchmakingCommand.NotifyCanExecuteChanged();
        LeaveLobbyCommand.NotifyCanExecuteChanged();
        PickCommand.NotifyCanExecuteChanged();
        BanCommand.NotifyCanExecuteChanged();
    }

    private void ApplyLobbySetupState(LobbyState? lobby)
    {
        var local = lobby?.LocalMember ?? lobby?.Members?.FirstOrDefault(member => member.SummonerId == _currentSummoner?.SummonerId);
        if (!_positionEditDirty && local is not null)
        {
            _hydratingPositions = true;
            SelectedPrimaryPosition = string.IsNullOrWhiteSpace(local.FirstPositionPreference) ? SelectedPrimaryPosition : local.FirstPositionPreference!;
            SelectedSecondaryPosition = string.IsNullOrWhiteSpace(local.SecondPositionPreference) ? SelectedSecondaryPosition : local.SecondPositionPreference!;
            _hydratingPositions = false;
        }

        LobbySetupStatus =
            lobby?.GameConfig?.ShowQuickPlaySlotSelection == true
                ? DescribeQuickplaySetup(local, lobby)
                : lobby?.GameConfig?.ShowPositionSelector == true
                    ? "Choose primary and secondary lanes before matchmaking."
                    : "";
        ApplyQuickplaySlots(lobby?.GameConfig?.ShowQuickPlaySlotSelection == true ? local?.PlayerSlots ?? [] : []);
        GameModeSetupSections = BuildGameModeSetup(lobby);

        OnPropertyChanged(nameof(IsPositionSelectorVisible));
        OnPropertyChanged(nameof(IsQuickplaySetupVisible));
        OnPropertyChanged(nameof(CanSavePositions));
        SavePositionsCommand.NotifyCanExecuteChanged();
        OnQuickplaySlotChanged();
    }

    private IReadOnlyList<GameModeSetupSectionViewModel> BuildGameModeSetup(LobbyState? lobby)
    {
        var sections = new List<GameModeSetupSectionViewModel>();
        if (lobby?.GameConfig?.ShowPositionSelector == true)
        {
            sections.Add(new(
                "Lane Preferences",
                [
                    GameModeSetupFieldViewModel.TextLine("Choose primary and secondary lanes before matchmaking."),
                    GameModeSetupFieldViewModel.Select("Primary", PositionOptions, SelectedPrimaryPosition, value => SelectedPrimaryPosition = value),
                    GameModeSetupFieldViewModel.Select("Secondary", PositionOptions, SelectedSecondaryPosition, value => SelectedSecondaryPosition = value)
                ],
                [new("Save Lanes", GameModeSetupCommandId.SaveLanePreferences)]));
        }

        if (lobby?.GameConfig?.ShowQuickPlaySlotSelection == true)
        {
            sections.Add(new(
                "Quickplay Slots",
                [
                    GameModeSetupFieldViewModel.TextLine(LobbySetupStatus),
                    GameModeSetupFieldViewModel.RowList("Slots", QuickplaySlots),
                    GameModeSetupFieldViewModel.Toggle("Override Quickplay spells", OverrideQuickplaySpells, value => OverrideQuickplaySpells = value),
                    GameModeSetupFieldViewModel.Select("Spell 1", SummonerSpellOptions, SelectedQuickplaySpell1, value => SelectedQuickplaySpell1 = value),
                    GameModeSetupFieldViewModel.Select("Spell 2", SummonerSpellOptions, SelectedQuickplaySpell2, value => SelectedQuickplaySpell2 = value)
                ],
                [
                    new("Save Spell Settings", GameModeSetupCommandId.SaveQuickplaySpellSettings),
                    new("Save Quickplay", GameModeSetupCommandId.SaveQuickplaySlots)
                ]));
        }

        return sections;
    }

    private IReadOnlyList<QuickplaySlotViewModel> ToQuickplaySlots(IEnumerable<LobbyPlayerSlot> slots) =>
        slots.Select((slot, index) =>
            new QuickplaySlotViewModel(
                $"Slot {index + 1}",
                slot,
                _championTilesById.TryGetValue(slot.ChampionId, out var champion) ? champion : null,
                string.IsNullOrWhiteSpace(slot.PositionPreference) ? PositionOptions[0] : slot.PositionPreference!,
                OnQuickplaySlotChanged))
            .ToList();

    private void ApplyQuickplaySlots(IReadOnlyList<LobbyPlayerSlot> slots)
    {
        if (slots.Count == 0)
        {
            if (QuickplaySlots.Count > 0)
            {
                QuickplaySlots = [];
            }

            return;
        }

        if (QuickplaySlots.Count == slots.Count
            && QuickplaySlots.Select(slot => slot.OriginalSlot).SequenceEqual(slots))
        {
            return;
        }

        ActiveQuickplaySlot = null;
        QuickplaySlots = ToQuickplaySlots(slots);
    }

    private void OnQuickplaySlotChanged()
    {
        OnPropertyChanged(nameof(CanSaveQuickplaySlots));
        SaveQuickplaySlotsCommand.NotifyCanExecuteChanged();
        RefreshQuickplayChampions();
    }

    private void RefreshQuickplayChampions()
    {
        if (ActiveQuickplaySlot is null)
        {
            QuickplayChampions = [];
            return;
        }

        var query = QuickplaySearch.Trim().ToLowerInvariant();
        QuickplayChampions = _championCatalog.All
            .Where(champion => MatchesQuickplayLane(champion, ActiveQuickplaySlot.SelectedPosition))
            .Where(champion => SelectedQuickplayChampionRole.Name == "All" || champion.Tags.Contains(SelectedQuickplayChampionRole.Name))
            .Where(champion => string.IsNullOrWhiteSpace(query) || champion.SearchText.Contains(query))
            .OrderBy(champion => champion.Name)
            .Select(champion => _championTilesById[champion.ChampionId])
            .ToList();
    }

    private static bool MatchesQuickplayLane(Champion champion, string position) =>
        position switch
        {
            "TOP" => champion.Tags.Any(tag => tag is "Fighter" or "Tank"),
            "JUNGLE" => champion.Tags.Any(tag => tag is "Fighter" or "Tank" or "Assassin"),
            "MIDDLE" => champion.Tags.Any(tag => tag is "Mage" or "Assassin"),
            "BOTTOM" => champion.Tags.Any(tag => tag is "Marksman" or "Mage"),
            "UTILITY" => champion.Tags.Any(tag => tag is "Support" or "Mage"),
            _ => true
        };

    private async Task<LobbyPlayerSlot> ToQuickplaySlotAsync(QuickplaySlotViewModel slot, CancellationToken cancellationToken)
    {
        var championId = slot.SelectedChampionTile?.Champion.ChampionId ?? 0;
        if (championId <= 0 || !PositionOptions.Contains(slot.SelectedPosition))
        {
            throw new InvalidOperationException("Select a champion and lane for both Quickplay slots.");
        }

        var unchanged = championId == slot.OriginalSlot.ChampionId
            && string.Equals(slot.SelectedPosition, slot.OriginalSlot.PositionPreference, StringComparison.OrdinalIgnoreCase);
        if (unchanged)
        {
            return OverrideQuickplaySpells
                ? slot.OriginalSlot with { Spell1 = SelectedQuickplaySpell1.Id, Spell2 = SelectedQuickplaySpell2.Id }
                : slot.OriginalSlot;
        }

        var perks = await _lcu.GetQuickplayPerksAsync(championId, slot.SelectedPosition, cancellationToken);
        if (string.IsNullOrWhiteSpace(perks))
        {
            throw new InvalidOperationException($"Recommended runes are unavailable for {slot.SelectedChampionTile!.Name} {slot.SelectedPosition}.");
        }

        return new LobbyPlayerSlot(
            championId,
            slot.SelectedPosition,
            championId * 1000,
            perks,
            QuickplaySpell1For(slot.SelectedPosition),
            QuickplaySpell2For());
    }

    private ulong QuickplaySpell1For(string position) =>
        OverrideQuickplaySpells ? SelectedQuickplaySpell1.Id :
        position == "JUNGLE" ? 11UL :
        14UL;

    private ulong QuickplaySpell2For() =>
        OverrideQuickplaySpells ? SelectedQuickplaySpell2.Id : 4UL;

    private static string DescribeQuickplaySetup(LobbyMember? local, LobbyState lobby)
    {
        var slots = local?.PlayerSlots is { Count: > 0 }
            ? $"{local.PlayerSlots.Count} Quickplay slot{(local.PlayerSlots.Count == 1 ? "" : "s")} selected"
            : "No Quickplay slots selected";
        var scarce = lobby.ScarcePositions is { Count: > 0 }
            ? $" Scarce lanes: {string.Join(", ", lobby.ScarcePositions)}."
            : "";
        return $"{slots}. Complete champion and lane setup in League Client for this mode.{scarce}";
    }

    private static bool CanAddFriend(LobbyMemberViewModel? member) => member?.CanAddFriend == true;

    private IReadOnlyList<LobbyMemberViewModel> ApplyLobbyMembers(LcuSnapshot snapshot)
    {
        var nextByKey = new Dictionary<string, LobbyMemberViewModel>();
        var nextMembers = new List<LobbyMemberViewModel>(snapshot.LobbyMembers.Count);
        var needsIcons = new List<LobbyMemberViewModel>();

        foreach (var member in snapshot.LobbyMembers)
        {
            var isLocal = member.SummonerId is not null && member.SummonerId == snapshot.Summoner?.SummonerId;
            var key = LobbyMemberKey(member);
            var row = key is not null && _lobbyMembersByKey.TryGetValue(key, out var existing)
                ? existing
                : new LobbyMemberViewModel(member, isLocal);

            if (row.Update(member, isLocal) || row.Icon is null)
            {
                needsIcons.Add(row);
            }

            if (key is not null)
            {
                nextByKey[key] = row;
            }

            nextMembers.Add(row);
        }

        _lobbyMembersByKey = nextByKey;
        LobbyMembers = nextMembers;
        return needsIcons;
    }

    private static string? LobbyMemberKey(LobbyMember member) =>
        !string.IsNullOrWhiteSpace(member.Puuid) ? $"puuid:{member.Puuid}" :
        member.SummonerId is not null ? $"summoner:{member.SummonerId}" :
        null;

    private static string SwiftplayRestrictionMessage(LobbyState lobby)
    {
        var code = lobby.Restrictions?.FirstOrDefault()?.RestrictionCode;
        if (string.Equals(code, "QPPlayerScarcePositionCoverageRestriction", StringComparison.OrdinalIgnoreCase))
        {
            return "Swiftplay needs valid Quickplay champion/position slots before matchmaking";
        }

        return string.IsNullOrWhiteSpace(code)
            ? "Swiftplay is not ready for matchmaking"
            : $"Swiftplay matchmaking blocked: {code}";
    }

    private async Task LoadLobbyMemberIconsAsync(IReadOnlyList<LobbyMemberViewModel> members, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var member in members.Where(member => member.IconId is not null))
            {
                var icon = await _summonerIconCache.LoadAsync(member.IconId, cancellationToken).ConfigureAwait(false);
                if (icon is not null)
                {
                    Dispatcher.UIThread.Post(() => member.Icon = icon);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _log.Error("Summoner icon loading failed", exception);
        }
    }

    private void ApplyScreenForSnapshot(LcuSnapshot snapshot)
    {
        if (Screen == "Profile" || snapshot.Phase is AppPhase.ReadyCheck or AppPhase.ChampionSelect or AppPhase.ActiveGame)
        {
            return;
        }

    }

    private void ApplyActiveGameState(ActiveGameState? activeGame)
    {
        if (activeGame is null)
        {
            ActiveGame = "No active game";
            ActiveGamePlayer = "Player unavailable";
            ActiveGameRunes = "Runes unavailable";
            ActiveGameTeammates = [];
            ActiveGameEnemies = [];
            return;
        }

        ActiveGame = $"{activeGame.Stats?.GameMode ?? "In game"} - {FormatGameTime(activeGame.Stats?.GameTime ?? 0)}";
        var level = activeGame.ActivePlayer?.ChampionStats?.Level;
        var gold = activeGame.ActivePlayer?.CurrentGold ?? 0;
        ActiveGamePlayer = $"{activeGame.ActivePlayer?.SummonerName ?? "You"} - Level {(level > 0 ? level : "?")} - Gold {gold:0}";
        IEnumerable<LiveRune?> runes = [activeGame.ActivePlayer?.FullRunes?.Keystone];
        ActiveGameRunes = string.Join(", ", runes
            .Concat(activeGame.ActivePlayer?.FullRunes?.GeneralRunes ?? [])
            .Where(rune => rune is not null && !string.IsNullOrWhiteSpace(rune.DisplayName))
            .Select(rune => rune!.DisplayName!)
            .Distinct()
            .Take(4));
        if (string.IsNullOrWhiteSpace(ActiveGameRunes))
        {
            ActiveGameRunes = "Runes unavailable";
        }

        var localTeam = activeGame.Players
            .FirstOrDefault(player => string.Equals(player.SummonerName, activeGame.ActivePlayer?.SummonerName, StringComparison.OrdinalIgnoreCase))
            ?.Team;
        ActiveGameTeammates = activeGame.Players
            .Where(player => string.Equals(player.Team, localTeam, StringComparison.OrdinalIgnoreCase))
            .Select(ActiveGamePlayerViewModel.From)
            .ToList();
        ActiveGameEnemies = activeGame.Players
            .Where(player => !string.Equals(player.Team, localTeam, StringComparison.OrdinalIgnoreCase))
            .Select(ActiveGamePlayerViewModel.From)
            .ToList();
    }

    private static string FormatGameTime(double seconds) =>
        TimeSpan.FromSeconds(Math.Max(0, seconds)).ToString(@"m\:ss");

    private string ChampionName(int championId) =>
        _championTilesById.TryGetValue(championId, out var tile) ? tile.Name : $"Champion {championId}";

    private void EnsureCurrentSummonerMatchSample(CurrentSummoner? summoner)
    {
        var puuid = summoner?.Puuid;
        if (string.IsNullOrWhiteSpace(puuid))
        {
            _currentSummonerMatchSample = [];
            _currentSummonerMatchSamplePuuid = null;
            _currentSummonerMatchSampleLoading = false;
            RefreshAramMayhemAugmentStats();
            RefreshAramMayhemLobbyHistory();
            RefreshAramMayhemGlobalStats();
            return;
        }

        if (_currentSummonerMatchSamplePuuid == puuid)
        {
            return;
        }

        _currentSummonerMatchSamplePuuid = puuid;
        _currentSummonerMatchSample = [];
        _currentSummonerMatchSampleLoading = true;
        RefreshAramMayhemAugmentStats();
        RefreshAramMayhemLobbyHistory();
        RefreshAramMayhemGlobalStats();
        _ = Task.Run(() => LoadCurrentSummonerMatchSampleAsync(puuid, _polling.Token));
    }

    private async Task LoadCurrentSummonerMatchSampleAsync(string puuid, CancellationToken cancellationToken)
    {
        try
        {
            var matches = await _lcu.GetMatchHistoryAsync(puuid, cancellationToken).ConfigureAwait(false);
            Dispatcher.UIThread.Post(() =>
            {
                if (_currentSummonerMatchSamplePuuid != puuid)
                {
                    return;
                }

                _currentSummonerMatchSample = matches;
                _currentSummonerMatchSampleLoading = false;
                RefreshAramMayhemAugmentStats();
                RefreshAramMayhemLobbyHistory();
                RefreshAramMayhemGlobalStats();
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _log.Error("ARAM Mayhem augment sample loading failed", exception);
            Dispatcher.UIThread.Post(() =>
            {
                if (_currentSummonerMatchSamplePuuid == puuid)
                {
                    _currentSummonerMatchSampleLoading = false;
                    RefreshAramMayhemAugmentStats();
                    RefreshAramMayhemLobbyHistory();
                    RefreshAramMayhemGlobalStats();
                }
            });
        }
    }

    private void RefreshAramMayhemGlobalStats()
    {
        var selectedChampionId = SelectedAramMayhemGlobalChampion?.Champion.Champion.ChampionId;
        var participants = AramMayhemParticipants(_currentSummonerMatchSample).ToList();
        var samples = participants
            .GroupBy(participant => participant.ChampionId)
            .Select(group => new
            {
                ChampionId = group.Key,
                Samples = group.Count(),
                Wins = group.Count(participant => participant.Win),
                Placements = group.Select(participant => participant.Placement).Where(placement => placement > 0).ToList(),
                Name = ChampionName(group.Key)
            })
            .Where(group => _championTilesById.ContainsKey(group.ChampionId))
            .OrderByDescending(group => group.Samples)
            .ThenByDescending(group => group.Wins / (double)group.Samples)
            .ThenBy(group => group.Name)
            .ToList();

        selectedChampionId = samples.Any(sample => sample.ChampionId == selectedChampionId)
            ? selectedChampionId
            : samples.FirstOrDefault()?.ChampionId;

        AramMayhemGlobalChampionStats = samples
            .Select(sample => new AramMayhemGlobalChampionViewModel(
                _championTilesById[sample.ChampionId],
                sample.Samples,
                sample.Wins,
                sample.Placements.Count == 0 ? null : sample.Placements.Average(),
                sample.ChampionId == selectedChampionId))
            .ToList();
        SelectedAramMayhemGlobalChampion = AramMayhemGlobalChampionStats.FirstOrDefault(sample => sample.IsSelected);
        RefreshAramMayhemGlobalBindings();
    }

    private void RefreshAramMayhemGlobalAugmentStats()
    {
        var championId = SelectedAramMayhemGlobalChampion?.Champion.Champion.ChampionId;
        var participants = AramMayhemParticipants(_currentSummonerMatchSample)
            .Where(participant => championId is null || participant.ChampionId == championId)
            .Where(participant => participant.AugmentIds is { Count: > 0 })
            .ToList();

        AramMayhemGlobalAugmentTitle = championId is null
            ? "ARAM Mayhem Augments"
            : $"Top Augments for {ChampionName(championId.Value)}";
        AramMayhemGlobalAugmentStats = StableAugmentStats(AramMayhemGlobalAugmentStats, participants
            .SelectMany(participant => participant.AugmentIds!.Distinct())
            .GroupBy(id => id)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Take(8)
            .Select(group => AramMayhemAugmentSampleViewModel.From(WithAugmentCatalog(new MatchPickrateStat(group.Key, $"Augment {group.Key}", group.Count(), participants.Count))))
            .ToList());
    }

    private void RefreshAramMayhemGlobalBindings()
    {
        OnPropertyChanged(nameof(HasAramMayhemGlobalChampionStats));
        OnPropertyChanged(nameof(HasNoAramMayhemGlobalStats));
        OnPropertyChanged(nameof(HasAramMayhemGlobalAugmentStats));
        OnPropertyChanged(nameof(HasNoAramMayhemGlobalAugmentStats));
        OnPropertyChanged(nameof(AramMayhemGlobalEmptyText));
    }

    private static IEnumerable<MatchHistoryEntry> AramMayhemMatches(IEnumerable<MatchHistoryEntry> matches) =>
        matches.Where(match => string.Equals(match.Queue, "ARAM Mayhem", StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<MatchParticipantPerformance> AramMayhemParticipants(IEnumerable<MatchHistoryEntry> matches) =>
        AramMayhemMatches(matches).SelectMany(match => match.Participants ?? []);

    private void RefreshAramMayhemLobbyHistory()
    {
        var selectedChampionId = SelectedAramMayhemChampionSample?.Champion.Champion.ChampionId;
        var samples = _currentSummonerMatchSample
            .Where(match => string.Equals(match.Queue, "ARAM Mayhem", StringComparison.OrdinalIgnoreCase))
            .GroupBy(match => match.ChampionId)
            .Select(group => new { ChampionId = group.Key, Matches = group.Count(), Name = ChampionName(group.Key) })
            .Where(group => _championTilesById.ContainsKey(group.ChampionId))
            .OrderByDescending(group => group.Matches)
            .ThenBy(group => group.Name)
            .Take(8)
            .ToList();

        selectedChampionId = samples.Any(sample => sample.ChampionId == selectedChampionId)
            ? selectedChampionId
            : samples.FirstOrDefault()?.ChampionId;

        AramMayhemChampionSamples = samples
            .Select(sample => new AramMayhemChampionSampleViewModel(_championTilesById[sample.ChampionId], sample.Matches, sample.ChampionId == selectedChampionId))
            .ToList();
        SelectedAramMayhemChampionSample = AramMayhemChampionSamples.FirstOrDefault(sample => sample.IsSelected);
        RefreshAramMayhemLobbyHistoryBindings();
    }

    private void RefreshAramMayhemLobbyAugmentStats()
    {
        var championId = SelectedAramMayhemChampionSample?.Champion.Champion.ChampionId ?? 0;
        AramMayhemLobbyAugmentTitle = championId > 0
            ? $"Popular ARAM Mayhem Augments for {ChampionName(championId)}"
            : "Popular ARAM Mayhem Augments";
        AramMayhemLobbyAugmentStats = StableAugmentStats(AramMayhemLobbyAugmentStats, championId > 0
            ? MatchPickrateCalculator.FromAugments(_currentSummonerMatchSample, championId)
                .Select(WithAugmentCatalog)
                .Select(AramMayhemAugmentSampleViewModel.From)
                .ToList()
            : []);
        RefreshAramMayhemLobbyHistoryBindings();
    }

    private void RefreshAramMayhemLobbyHistoryBindings()
    {
        OnPropertyChanged(nameof(IsAramMayhemLobbyHistoryVisible));
        OnPropertyChanged(nameof(HasAramMayhemChampionSamples));
        OnPropertyChanged(nameof(HasNoAramMayhemChampionSamples));
        OnPropertyChanged(nameof(HasAramMayhemLobbyAugmentStats));
        OnPropertyChanged(nameof(HasNoAramMayhemLobbyAugmentStats));
        OnPropertyChanged(nameof(AramMayhemLobbyHistoryEmptyText));
    }

    private void RefreshAramMayhemAugmentStats()
    {
        AramMayhemAugmentTitle = _currentChampionSelectChampionId > 0
            ? $"Popular ARAM Mayhem Augments for {ChampionName(_currentChampionSelectChampionId)}"
            : "Popular ARAM Mayhem Augments";
        AramMayhemAugmentStats = StableAugmentStats(AramMayhemAugmentStats, IsAramMayhemAugmentPanelVisible
            ? MatchPickrateCalculator.FromAugments(_currentSummonerMatchSample, _currentChampionSelectChampionId)
                .Select(WithAugmentCatalog)
                .Select(AramMayhemAugmentSampleViewModel.From)
                .ToList()
            : []);
        RefreshAramMayhemAugmentBindings();
    }

    private MatchPickrateStat WithAugmentCatalog(MatchPickrateStat stat) =>
        new(stat.Id, AramMayhemAugmentCatalog.Name(stat.Id), stat.Picks, stat.Total);

    private static IReadOnlyList<AramMayhemAugmentSampleViewModel> StableAugmentStats(
        IReadOnlyList<AramMayhemAugmentSampleViewModel> current,
        IReadOnlyList<AramMayhemAugmentSampleViewModel> next)
    {
        if (current.Count != next.Count)
        {
            return next;
        }

        for (var i = 0; i < current.Count; i++)
        {
            if (current[i].Id != next[i].Id
                || current[i].Name != next[i].Name
                || current[i].Picks != next[i].Picks
                || current[i].Total != next[i].Total)
            {
                return next;
            }
        }

        return current;
    }

    private void QueueAramMayhemAugmentCatalogLoad()
    {
        if (_aramMayhemAugmentCatalogLoadAttempted || _aramMayhemAugmentCatalogLoading || AramMayhemAugmentCatalog.IsLoaded)
        {
            return;
        }

        _aramMayhemAugmentCatalogLoadAttempted = true;
        _aramMayhemAugmentCatalogLoading = true;
        _ = Task.Run(() => LoadAramMayhemAugmentCatalogAsync(_polling.Token));
    }

    private async Task LoadAramMayhemAugmentCatalogAsync(CancellationToken cancellationToken)
    {
        await AramMayhemAugmentCatalog.EnsureLoadedAsync(_log, cancellationToken).ConfigureAwait(false);
        Dispatcher.UIThread.Post(() =>
        {
            _aramMayhemAugmentCatalogLoading = false;
            if (!AramMayhemAugmentCatalog.IsLoaded)
            {
                return;
            }

            RefreshAramMayhemAugmentStats();
            RefreshAramMayhemLobbyAugmentStats();
            RefreshAramMayhemGlobalAugmentStats();
        });
    }

    private async Task LoadAramMayhemLobbyAugmentIconsAsync(IReadOnlyList<AramMayhemAugmentSampleViewModel> stats, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var stat in stats)
            {
                var icon = await _aramMayhemAugmentIconCache.LoadAsync(stat.Id, cancellationToken).ConfigureAwait(false);
                if (icon is not null)
                {
                    Dispatcher.UIThread.Post(() => stat.Icon = icon);
                }

                if (!stat.HasDescription
                    && await AramMayhemAugmentCatalog.EnsureDescriptionAsync(stat.Id, _log, cancellationToken).ConfigureAwait(false))
                {
                    Dispatcher.UIThread.Post(stat.RefreshDetails);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _log.Error("ARAM Mayhem augment icon loading failed", exception);
        }
    }

    private void RefreshAramMayhemAugmentBindings()
    {
        OnPropertyChanged(nameof(IsAramMayhemAugmentPanelVisible));
        OnPropertyChanged(nameof(HasAramMayhemAugmentStats));
        OnPropertyChanged(nameof(HasNoAramMayhemAugmentStats));
        OnPropertyChanged(nameof(AramMayhemAugmentEmptyText));
    }

    private async Task OpenProfileAsync(SummonerProfile profile)
    {
        if (Screen != "Profile")
        {
            _profileBackScreen = Screen;
        }

        SelectedProfile = ProfilePanelViewModel.From(profile, "Ranked unavailable");
        ProfileMatches = [];
        SelectedProfileMatch = null;
        ProfileMatchDetail = null;
        Screen = "Profile";
        _ = Task.Run(() => LoadSelectedProfileIconAsync(profile.ProfileIconId, _polling.Token));

        if (profile.SummonerId is not { } summonerId)
        {
            return;
        }

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var fetched = await _lcu.GetSummonerProfileAsync(summonerId, timeout.Token) ?? profile;
            RankedSummary? ranked = null;
            try
            {
                ranked = await _lcu.GetRankedSummaryAsync(summonerId, timeout.Token);
            }
            catch (Exception exception)
            {
                _log.Error("Ranked profile lookup failed", exception);
            }

            SelectedProfile = ProfilePanelViewModel.From(fetched, ranked?.Text ?? "Ranked unavailable");
            _ = Task.Run(() => LoadSelectedProfileIconAsync(fetched.ProfileIconId, _polling.Token));
            var matchHistory = string.IsNullOrWhiteSpace(fetched.Puuid)
                ? []
                : await _lcu.GetMatchHistoryAsync(fetched.Puuid, timeout.Token);
            ProfileMatches = matchHistory
                .Select(entry => ProfileMatchViewModel.From(entry, _championTilesById, SummonerSpellOptions))
                .ToList();
            if (ProfileMatches.Count > 0)
            {
                _ = Task.Run(() => LoadProfileMatchItemIconsAsync(ProfileMatches, _polling.Token));
            }
        }
        catch (Exception exception)
        {
            _log.Error("Summoner profile lookup failed", exception);
        }
    }

    private async Task LoadSelectedProfileIconAsync(int? iconId, CancellationToken cancellationToken)
    {
        try
        {
            var icon = await _summonerIconCache.LoadAsync(iconId, cancellationToken).ConfigureAwait(false);
            if (icon is not null)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (SelectedProfile is { } selected && selected.ProfileIconId == iconId)
                    {
                        SelectedProfile = selected with { IconImage = icon };
                    }
                });
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _log.Error("Profile icon loading failed", exception);
        }
    }

    private async Task LoadProfileMatchItemIconsAsync(IReadOnlyList<ProfileMatchViewModel> matches, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var item in matches.SelectMany(match => match.Items))
            {
                var icon = await _itemIconCache.LoadAsync(item.Id, cancellationToken).ConfigureAwait(false);
                if (icon is not null)
                {
                    Dispatcher.UIThread.Post(() => item.Icon = icon);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _log.Error("Match item icon loading failed", exception);
        }
    }

    private string DescribeLobby(LobbyState? lobby, IReadOnlyList<LobbyMember> members)
    {
        if (lobby is null && members.Count == 0)
        {
            return "No lobby";
        }

        var queueId = lobby?.GameConfig?.QueueId;
        var mode = queueId is not null
            ? _gameModeCatalog.All.FirstOrDefault(gameMode => gameMode.QueueId == queueId)?.Name
            : null;

        mode ??= lobby?.GameConfig?.GameMode ?? "Current lobby";
        return $"{mode} - {members.Count} member{(members.Count == 1 ? "" : "s")}";
    }

    private string ForcedScreenOr(string fallback) =>
        Phase == AppPhase.ChampionSelect ? "ChampionSelect" :
        Phase == AppPhase.ReadyCheck ? "Ready" :
        fallback;

    private async Task RunChampionCommandAsync(Func<int, CancellationToken, Task> command, string success)
    {
        if (SelectedChampion is null)
        {
            LastCommandResult = "Select a champion first";
            return;
        }

        await RunCommandAsync(token => command(SelectedChampion.ChampionId, token), success);
    }

    private async Task DeclareChampionAsync(int championId)
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _lcu.DeclareChampionAsync(championId, timeout.Token);
            LastCommandResult = "Champion declared";
        }
        catch (Exception exception)
        {
            _log.Error("Champion declare failed", exception);
        }
    }

    private async Task RunCommandAsync(Func<CancellationToken, Task> command, string success, bool refreshSnapshot = false)
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await command(timeout.Token);
            LastCommandResult = success;
            if (refreshSnapshot)
            {
                await RefreshSnapshotAsync();
            }
        }
        catch (Exception exception)
        {
            LastCommandResult = exception.Message;
        }
    }

    private async Task RefreshSnapshotAsync()
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            Apply(await _lcu.GetSnapshotAsync(timeout.Token));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _log.Error("Command refresh failed", exception);
        }
    }
}

public sealed class LobbyMemberViewModel
    : ViewModelBase
{
    private bool _isLocal;

    public LobbyMemberViewModel(LobbyMember member, bool isLocal)
    {
        Member = member;
        _isLocal = isLocal;
    }

    public LobbyMember Member { get; private set; }
    public int? IconId => Member.IconId;
    public Bitmap? Icon
    {
        get => _icon;
        set
        {
            if (!SetProperty(ref _icon, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasIcon));
            OnPropertyChanged(nameof(HasNoIcon));
        }
    }

    public string Name => Member.Name;
    public string Position => Member.PlayerSlots is { Count: > 0 }
        ? string.Join(", ", Member.PlayerSlots.Select(slot => slot.PositionPreference).Where(position => !string.IsNullOrWhiteSpace(position)))
        : Member.Position;
    public string Badges => string.Join(" ", new[] { _isLocal ? "You" : "", Member.IsLeader ? "Leader" : "", Member.Ready ? "Ready" : "" }.Where(value => value.Length > 0));
    public bool HasBadges => Badges.Length > 0;
    public bool HasIcon => Icon is not null;
    public bool HasNoIcon => Icon is null;
    public bool CanAddFriend => !_isLocal
        && !string.IsNullOrWhiteSpace(Member.GameName)
        && !string.IsNullOrWhiteSpace(Member.TagLine);

    private Bitmap? _icon;

    public bool Update(LobbyMember member, bool isLocal)
    {
        var iconChanged = member.IconId != IconId;
        if (Member == member && _isLocal == isLocal)
        {
            return false;
        }

        Member = member;
        _isLocal = isLocal;
        if (iconChanged)
        {
            Icon = null;
        }

        OnPropertyChanged(nameof(Member));
        OnPropertyChanged(nameof(IconId));
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Position));
        OnPropertyChanged(nameof(Badges));
        OnPropertyChanged(nameof(HasBadges));
        OnPropertyChanged(nameof(CanAddFriend));
        return iconChanged;
    }
}

public partial class QuickplaySlotViewModel : ViewModelBase
{
    private readonly Action _changed;

    public QuickplaySlotViewModel(string slot, LobbyPlayerSlot originalSlot, ChampionTileViewModel? selectedChampionTile, string selectedPosition, Action changed)
    {
        Slot = slot;
        OriginalSlot = originalSlot;
        _selectedChampionTile = selectedChampionTile;
        _selectedPosition = selectedPosition;
        _changed = changed;
    }

    public string Slot { get; }
    public LobbyPlayerSlot OriginalSlot { get; }
    public string Champion => SelectedChampionTile?.Name ?? $"Champion {OriginalSlot.ChampionId}";
    public string Position => SelectedPosition;

    [ObservableProperty]
    private ChampionTileViewModel? _selectedChampionTile;

    [ObservableProperty]
    private string _selectedPosition;

    [ObservableProperty]
    private bool _isActive;

    partial void OnSelectedChampionTileChanged(ChampionTileViewModel? value)
    {
        OnPropertyChanged(nameof(Champion));
        _changed();
    }

    partial void OnSelectedPositionChanged(string value)
    {
        OnPropertyChanged(nameof(Position));
        _changed();
    }
}

public sealed class AramMayhemChampionSampleViewModel : ViewModelBase
{
    private bool _isSelected;

    public AramMayhemChampionSampleViewModel(ChampionTileViewModel champion, int matches, bool isSelected)
    {
        Champion = champion;
        Matches = matches;
        _isSelected = isSelected;
    }

    public ChampionTileViewModel Champion { get; }
    public int Matches { get; }
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string Name => Champion.Name;
    public Bitmap? Icon => Champion.Icon;
    public bool HasIcon => Champion.HasIcon;
    public bool HasNoIcon => Champion.HasNoIcon;
    public string MatchesText => $"{Matches} sampled match{(Matches == 1 ? "" : "es")}";
}

public sealed class AramMayhemGlobalChampionViewModel : ViewModelBase
{
    private bool _isSelected;

    public AramMayhemGlobalChampionViewModel(ChampionTileViewModel champion, int samples, int wins, double? averagePlacement, bool isSelected)
    {
        Champion = champion;
        Samples = samples;
        Wins = wins;
        AveragePlacement = averagePlacement;
        _isSelected = isSelected;
    }

    public ChampionTileViewModel Champion { get; }
    public int Samples { get; }
    public int Wins { get; }
    public double? AveragePlacement { get; }
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string Name => Champion.Name;
    public Bitmap? Icon => Champion.Icon;
    public bool HasIcon => Champion.HasIcon;
    public bool HasNoIcon => Champion.HasNoIcon;
    public string SamplesText => $"{Samples} picks";
    public string WinrateText => Samples == 0 ? "0% win" : $"{Wins / (double)Samples:P0} win";
    public string PlacementText => AveragePlacement is null ? "Placement n/a" : $"Avg place {AveragePlacement:0.0}";
}

public sealed class AramMayhemAugmentSampleViewModel(int id, string name, int picks, int total)
    : ViewModelBase
{
    private Bitmap? _icon;

    public int Id { get; } = id;
    public string Name { get; } = name;
    public int Picks { get; } = picks;
    public int Total { get; } = total;
    public Bitmap? Icon
    {
        get => _icon;
        set
        {
            if (!SetProperty(ref _icon, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasIcon));
            OnPropertyChanged(nameof(HasNoIcon));
        }
    }

    public bool HasIcon => Icon is not null;
    public bool HasNoIcon => Icon is null;
    public string Rarity => AramMayhemAugmentCatalog.Rarity(Id);
    public bool HasRarity => !string.IsNullOrWhiteSpace(Rarity);
    public string RarityColor => Rarity switch
    {
        "Prismatic" => "#a855f7",
        "Gold" => "#f59e0b",
        "Silver" => "#94a3b8",
        "Bronze" => "#b45309",
        "EventChoice" => "#06b6d4",
        _ => "#3f3858"
    };
    public string RarityBackground => Rarity switch
    {
        "Prismatic" => "#3b145f",
        "Gold" => "#4a2c05",
        "Silver" => "#273244",
        "Bronze" => "#3d230d",
        "EventChoice" => "#073847",
        _ => "#242033"
    };
    public string Description => AramMayhemAugmentCatalog.Description(Id);
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    public string Badge => string.IsNullOrWhiteSpace(Name) ? "A" : Name[..1].ToUpperInvariant();
    public string Details => string.IsNullOrWhiteSpace(AramMayhemAugmentCatalog.Rarity(Id))
        ? Name
        : $"{Name}\n{AramMayhemAugmentCatalog.Rarity(Id)}";

    public void RefreshDetails()
    {
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(HasDescription));
        OnPropertyChanged(nameof(Details));
    }

    public static AramMayhemAugmentSampleViewModel From(MatchPickrateStat stat) =>
        new(stat.Id, AramMayhemAugmentCatalog.Name(stat.Id), stat.Picks, stat.Total);
}

public enum GameModeSetupCommandId
{
    SaveLanePreferences,
    OpenQuickplaySlot,
    SaveQuickplaySlots,
    SaveQuickplaySpellSettings
}

public enum GameModeSetupFieldKind
{
    Text,
    Select,
    Toggle,
    RowList
}

public sealed record GameModeSetupSectionViewModel(
    string Title,
    IReadOnlyList<GameModeSetupFieldViewModel> Fields,
    IReadOnlyList<GameModeSetupActionViewModel> Actions);

public sealed class GameModeSetupFieldViewModel : ViewModelBase
{
    private readonly Action<object?>? _selectChanged;
    private readonly Action<bool>? _toggleChanged;
    private object? _selectedValue;
    private bool _isChecked;

    private GameModeSetupFieldViewModel(
        GameModeSetupFieldKind kind,
        string label,
        string text = "",
        IReadOnlyList<object>? options = null,
        object? selectedValue = null,
        Action<object?>? selectChanged = null,
        bool isChecked = false,
        Action<bool>? toggleChanged = null,
        IReadOnlyList<QuickplaySlotViewModel>? rowItems = null)
    {
        Kind = kind;
        Label = label;
        Text = text;
        Options = options ?? [];
        _selectedValue = selectedValue;
        _selectChanged = selectChanged;
        _isChecked = isChecked;
        _toggleChanged = toggleChanged;
        RowItems = rowItems ?? [];
    }

    public GameModeSetupFieldKind Kind { get; }
    public string Label { get; }
    public string Text { get; }
    public IReadOnlyList<object> Options { get; }
    public IReadOnlyList<QuickplaySlotViewModel> RowItems { get; }
    public bool IsText => Kind == GameModeSetupFieldKind.Text;
    public bool IsSelect => Kind == GameModeSetupFieldKind.Select;
    public bool IsToggle => Kind == GameModeSetupFieldKind.Toggle;
    public bool IsRowList => Kind == GameModeSetupFieldKind.RowList;

    public object? SelectedValue
    {
        get => _selectedValue;
        set
        {
            if (!SetProperty(ref _selectedValue, value))
            {
                return;
            }

            _selectChanged?.Invoke(value);
        }
    }

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (!SetProperty(ref _isChecked, value))
            {
                return;
            }

            _toggleChanged?.Invoke(value);
        }
    }

    public static GameModeSetupFieldViewModel TextLine(string text) =>
        new(GameModeSetupFieldKind.Text, "", text);

    public static GameModeSetupFieldViewModel Select(string label, IEnumerable<object> options, object? selectedValue, Action<object?> changed) =>
        new(GameModeSetupFieldKind.Select, label, options: options.ToList(), selectedValue: selectedValue, selectChanged: changed);

    public static GameModeSetupFieldViewModel Select(string label, IEnumerable<string> options, string selectedValue, Action<string> changed) =>
        Select(label, options.Cast<object>(), selectedValue, value =>
        {
            if (value is string text)
            {
                changed(text);
            }
        });

    public static GameModeSetupFieldViewModel Select(string label, IEnumerable<SummonerSpellOptionViewModel> options, SummonerSpellOptionViewModel selectedValue, Action<SummonerSpellOptionViewModel> changed) =>
        Select(label, options.Cast<object>(), selectedValue, value =>
        {
            if (value is SummonerSpellOptionViewModel spell)
            {
                changed(spell);
            }
        });

    public static GameModeSetupFieldViewModel Toggle(string label, bool isChecked, Action<bool> changed) =>
        new(GameModeSetupFieldKind.Toggle, label, isChecked: isChecked, toggleChanged: changed);

    public static GameModeSetupFieldViewModel RowList(string label, IReadOnlyList<QuickplaySlotViewModel> rowItems) =>
        new(GameModeSetupFieldKind.RowList, label, rowItems: rowItems);
}

public sealed record GameModeSetupActionViewModel(string Label, GameModeSetupCommandId CommandId)
{
    public bool IsSaveLanePreferences => CommandId == GameModeSetupCommandId.SaveLanePreferences;
    public bool IsOpenQuickplaySlot => CommandId == GameModeSetupCommandId.OpenQuickplaySlot;
    public bool IsSaveQuickplaySlots => CommandId == GameModeSetupCommandId.SaveQuickplaySlots;
    public bool IsSaveQuickplaySpellSettings => CommandId == GameModeSetupCommandId.SaveQuickplaySpellSettings;

    public static bool IsWhitelisted(GameModeSetupCommandId commandId) =>
        Enum.IsDefined(commandId);
}

public partial class SummonerSpellOptionViewModel(string name, ulong id, string imageFileName) : ViewModelBase
{
    public string Name { get; } = name;
    public ulong Id { get; } = id;
    public string ImageFileName { get; } = imageFileName;
    public string Label => $"{Name} ({Id})";
    public bool HasIcon => Icon is not null;
    public bool HasNoIcon => Icon is null;
    public override string ToString() => Name;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasIcon))]
    [NotifyPropertyChangedFor(nameof(HasNoIcon))]
    private Bitmap? _icon;
}

public sealed record ProfilePanelViewModel(
    string Name,
    string Level,
    int? ProfileIconId,
    string SummonerId,
    string Puuid,
    string Ranked,
    Bitmap? IconImage = null)
{
    public bool HasIcon => IconImage is not null;
    public bool HasNoIcon => IconImage is null;

    public static ProfilePanelViewModel From(SummonerProfile profile, string ranked) =>
        new(
            profile.Name,
            profile.SummonerLevel is null ? "Level unknown" : $"Level {profile.SummonerLevel}",
            profile.ProfileIconId,
            profile.SummonerId is null ? "Summoner ID unavailable" : $"Summoner ID {profile.SummonerId}",
            string.IsNullOrWhiteSpace(profile.Puuid) ? "PUUID unavailable" : $"PUUID {profile.Puuid}",
            ranked);
}

public partial class ProfileMatchViewModel : ViewModelBase
{
    public ProfileMatchViewModel(
        string champion,
        Bitmap? championIcon,
        string queue,
        string result,
        string kda,
        string scoreLine,
        string economyLine,
        string combatLine,
        IReadOnlyList<MatchAssetViewModel> spells,
        IReadOnlyList<MatchAssetViewModel> items,
        IReadOnlyList<MatchAwardViewModel> awards,
        MatchAwardViewModel? viewerAward,
        string duration,
        string playedAt)
    {
        Champion = champion;
        ChampionIcon = championIcon;
        Queue = queue;
        Result = result;
        Kda = kda;
        ScoreLine = scoreLine;
        EconomyLine = economyLine;
        CombatLine = combatLine;
        Spells = spells;
        Items = items;
        Awards = awards;
        ViewerAward = viewerAward;
        Duration = duration;
        PlayedAt = playedAt;
    }

    public string Champion { get; }
    public Bitmap? ChampionIcon { get; }
    public string Queue { get; }
    public string Result { get; }
    public string Kda { get; }
    public string ScoreLine { get; }
    public string EconomyLine { get; }
    public string CombatLine { get; }
    public IReadOnlyList<MatchAssetViewModel> Spells { get; }
    public IReadOnlyList<MatchAssetViewModel> Items { get; }
    public IReadOnlyList<MatchAwardViewModel> Awards { get; }
    public MatchAwardViewModel? ViewerAward { get; }
    public IReadOnlyList<MatchAwardViewModel> TeammateAwards => Awards.Where(award => award.IsTeammate).ToList();
    public IReadOnlyList<MatchAwardViewModel> EnemyAwards => Awards.Where(award => award.IsEnemy).ToList();
    public IReadOnlyList<MatchAwardGroupViewModel> AwardGroups =>
        IsArena
            ? ArenaAwardGroups()
            : HasTeamSplit
            ?
            [
                new("Teammates", TeammateAwards),
                new("Enemies", EnemyAwards)
            ]
            : [new("Players", Awards)];
    public string Duration { get; }
    public string PlayedAt { get; }
    public bool HasChampionIcon => ChampionIcon is not null;
    public bool HasNoChampionIcon => ChampionIcon is null;
    public bool IsWin => Result == "Win";
    public bool IsArena => string.Equals(Queue, "Arena", StringComparison.OrdinalIgnoreCase);
    public bool HasItems => Items.Count > 0;
    public bool HasAwards => Awards.Count > 0;
    public bool HasTeamSplit => TeammateAwards.Count > 0 || EnemyAwards.Count > 0;
    public bool HasViewerAward => ViewerAward is not null;
    public string ItemsLine => HasItems ? "Items" : "Items unavailable";
    public string AwardsLine => HasAwards ? "Danh hieu tran" : "Danh hieu chua du";

    [ObservableProperty]
    private bool _isExpanded;

    public static ProfileMatchViewModel From(MatchHistoryEntry entry, IReadOnlyDictionary<int, ChampionTileViewModel> champions, IReadOnlyList<SummonerSpellOptionViewModel> spellOptions)
    {
        var champion = champions.TryGetValue(entry.ChampionId, out var tile) ? tile : null;
        var spells = new[]
            {
                SpellAsset(entry.Spell1Id, spellOptions),
                SpellAsset(entry.Spell2Id, spellOptions)
            }
            .Where(asset => asset is not null)
            .Select(asset => asset!)
            .ToList();
        var items = entry.ItemIds
            .Select(itemId => new MatchAssetViewModel(itemId, $"Item {itemId}"))
            .ToList();
        var viewer = entry.Participants?
            .FirstOrDefault(participant => participant.ChampionId == entry.ChampionId
                && participant.Win == entry.Win
                && participant.Kills == entry.Kills
                && participant.Deaths == entry.Deaths
                && participant.Assists == entry.Assists);
        var viewerKey = viewer?.PlayerKey;
        var awards = MatchAwardScorer.Score(entry.Participants ?? [])
            .Select(award => MatchAwardViewModel.From(award, champions, viewer?.TeamId))
            .ToList();
        var viewerAward = string.IsNullOrWhiteSpace(viewerKey)
            ? null
            : awards.FirstOrDefault(award => award.PlayerKey == viewerKey);

        return new(
            champion?.Name ?? entry.Champion,
            champion?.Icon,
            entry.Queue,
            entry.Win ? "Win" : "Loss",
            $"{entry.Kills}/{entry.Deaths}/{entry.Assists}",
            $"KDA {KdaRatio(entry.Kills, entry.Deaths, entry.Assists)}",
            $"CS {entry.CreepScore} · Gold {entry.GoldEarned:N0}",
            $"Damage {entry.DamageDealt:N0} · Multi {entry.LargestMultiKill}",
            spells,
            items,
            awards,
            viewerAward,
            entry.Duration,
            entry.PlayedAt);
    }

    private static string KdaRatio(int kills, int deaths, int assists) =>
        deaths == 0 ? "Perfect" : ((kills + assists) / (double)deaths).ToString("0.00");

    private IReadOnlyList<MatchAwardGroupViewModel> ArenaAwardGroups() =>
        Awards
            .GroupBy(award => award.TeamId)
            .OrderBy(group => group.Min(award => award.Placement > 0 ? award.Placement : int.MaxValue))
            .ThenBy(group => group.Key)
            .Select(group =>
            {
                var placement = group.Where(award => award.Placement > 0).Select(award => award.Placement).DefaultIfEmpty(0).Min();
                var title = placement > 0 ? $"Rank {placement} - Team {group.Key}" : $"Team {group.Key}";
                return new MatchAwardGroupViewModel(title, group.OrderBy(award => award.Rank).ToList());
            })
            .ToList();

    private static MatchAssetViewModel? SpellAsset(int spellId, IReadOnlyList<SummonerSpellOptionViewModel> spellOptions)
    {
        if (spellId <= 0)
        {
            return null;
        }

        var spell = spellOptions.FirstOrDefault(option => option.Id == (ulong)spellId);
        return spell is null
            ? new MatchAssetViewModel(spellId, $"Spell {spellId}")
            : new MatchAssetViewModel(spellId, spell.Name, spell.Icon);
    }
}

public sealed record MatchAwardViewModel(
    string Kind,
    int Rank,
    string PlayerKey,
    string PlayerName,
    string Champion,
    Bitmap? ChampionIcon,
    int TeamId,
    string Team,
    int Placement,
    bool HasKnownSide,
    bool IsTeammate,
    string Result,
    string Kda,
    string Economy,
    string Damage,
    int Score,
    string Line,
    string Reason)
{
    public bool HasChampionIcon => ChampionIcon is not null;
    public bool HasNoChampionIcon => ChampionIcon is null;
    public bool IsEnemy => HasKnownSide && !IsTeammate;

    public static MatchAwardViewModel From(MatchAward award, IReadOnlyDictionary<int, ChampionTileViewModel> champions, int? viewerTeamId = null)
    {
        var tile = champions.GetValueOrDefault(award.Player.ChampionId);
        var champion = tile?.Name ?? award.Player.Champion;
        return new(
            award.Kind,
            award.Rank,
            award.Player.PlayerKey,
            award.Player.PlayerName,
            champion,
            tile?.Icon,
            award.Player.TeamId,
            $"Team {award.Player.TeamId}",
            award.Player.Placement,
            viewerTeamId is not null,
            viewerTeamId is not null && award.Player.TeamId == viewerTeamId,
            award.Player.Win ? "Win" : "Loss",
            $"{award.Player.Kills}/{award.Player.Deaths}/{award.Player.Assists}",
            $"CS {award.Player.CreepScore} - Gold {award.Player.GoldEarned:N0}",
            $"Damage {award.Player.DamageDealt:N0}",
            award.Score,
            $"{award.Kind} - {award.Player.PlayerName}",
            $"{champion} - Score {award.Score:N0} - {award.Reason}");
    }
}

public sealed record MatchAwardGroupViewModel(string Title, IReadOnlyList<MatchAwardViewModel> Awards);

public sealed record ActiveGamePlayerViewModel(
    string Name,
    string Champion,
    string Level,
    string Kda,
    string Farm,
    string Items)
{
    public static ActiveGamePlayerViewModel From(LivePlayer player) =>
        new(
            string.IsNullOrWhiteSpace(player.SummonerName) ? "Unknown player" : player.SummonerName!,
            string.IsNullOrWhiteSpace(player.ChampionName) ? "Unknown champion" : player.ChampionName!,
            player.Level > 0 ? $"Level {player.Level}" : "Level ?",
            player.Scores is null ? "KDA unavailable" : $"{player.Scores.Kills}/{player.Scores.Deaths}/{player.Scores.Assists}",
            player.Scores is null ? "CS ?" : $"CS {player.Scores.CreepScore}",
            player.Items is { Count: > 0 } items ? $"{items.Count} items" : "Items unavailable");
}

public partial class MatchAssetViewModel(int id, string label, Bitmap? icon = null) : ViewModelBase
{
    public int Id { get; } = id;
    public string Label { get; } = label;
    public bool HasIcon => Icon is not null;
    public bool HasNoIcon => Icon is null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasIcon))]
    [NotifyPropertyChangedFor(nameof(HasNoIcon))]
    private Bitmap? _icon = icon;
}
