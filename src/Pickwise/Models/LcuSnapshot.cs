using System.Text.Json.Serialization;

namespace Pickwise.Models;

public sealed record LcuSnapshot(
    AppPhase Phase,
    CurrentSummoner? Summoner,
    ReadyCheckState? ReadyCheck,
    ChampionSelectSession? ChampionSelect,
    GameflowSession? Gameflow,
    LobbyState? Lobby,
    IReadOnlyList<LobbyMember> LobbyMembers,
    IReadOnlyList<int> PickableChampionIds,
    IReadOnlyList<int> DisabledChampionIds,
    IReadOnlyList<ChampionTradeRequest> TradeRequests,
    string Message);

public sealed record CurrentSummoner(
    [property: JsonPropertyName("summonerId")] long? SummonerId,
    [property: JsonPropertyName("displayName")] string? DisplayName,
    [property: JsonPropertyName("gameName")] string? GameName,
    [property: JsonPropertyName("tagLine")] string? TagLine,
    [property: JsonPropertyName("summonerLevel")] long? SummonerLevel,
    [property: JsonPropertyName("profileIconId")] int? ProfileIconId,
    [property: JsonPropertyName("puuid")] string? Puuid)
{
    public string Name => !string.IsNullOrWhiteSpace(GameName)
        ? $"{GameName}#{TagLine}"
        : DisplayName ?? "Unknown summoner";
}

public sealed record LobbyState(
    [property: JsonPropertyName("canStartActivity")] bool CanStartActivity,
    [property: JsonPropertyName("gameConfig")] LobbyGameConfig? GameConfig,
    [property: JsonPropertyName("members")] List<LobbyMember>? Members,
    [property: JsonPropertyName("restrictions")] List<LobbyRestriction>? Restrictions)
{
    [JsonPropertyName("localMember")]
    public LobbyMember? LocalMember { get; init; }

    [JsonPropertyName("scarcePositions")]
    public List<string>? ScarcePositions { get; init; }

    [JsonPropertyName("warnings")]
    public List<LobbyRestriction>? Warnings { get; init; }

    public bool IsSwiftplay => GameConfig?.QueueId == 480
        || string.Equals(GameConfig?.GameMode, "SWIFTPLAY", StringComparison.OrdinalIgnoreCase);

    public bool BlocksSwiftplayMatchmaking => GameConfig?.ShowQuickPlaySlotSelection == true
        && (CanStartActivity == false || Restrictions is { Count: > 0 });
}

public sealed record LobbyGameConfig(
    [property: JsonPropertyName("queueId")] int? QueueId,
    [property: JsonPropertyName("gameMode")] string? GameMode,
    [property: JsonPropertyName("showPositionSelector")] bool ShowPositionSelector = false,
    [property: JsonPropertyName("showQuickPlaySlotSelection")] bool ShowQuickPlaySlotSelection = false,
    [property: JsonPropertyName("isCustom")] bool IsCustom = false);

public sealed record LobbyRestriction(
    [property: JsonPropertyName("restrictionCode")] string? RestrictionCode);

public sealed record LobbyPlayerSlot(
    [property: JsonPropertyName("championId")] int ChampionId,
    [property: JsonPropertyName("positionPreference")] string? PositionPreference,
    [property: JsonPropertyName("skinId")] int SkinId = 0,
    [property: JsonPropertyName("perks")] string? Perks = "",
    [property: JsonPropertyName("spell1")] ulong Spell1 = 0,
    [property: JsonPropertyName("spell2")] ulong Spell2 = 0);

public sealed record LobbyMember(
    [property: JsonPropertyName("summonerId")] long? SummonerId,
    [property: JsonPropertyName("summonerName")] string? SummonerName,
    [property: JsonPropertyName("gameName")] string? GameName,
    [property: JsonPropertyName("tagLine")] string? TagLine,
    [property: JsonPropertyName("puuid")] string? Puuid,
    [property: JsonPropertyName("summonerIconId")] int? SummonerIconId,
    [property: JsonPropertyName("profileIconId")] int? ProfileIconId,
    [property: JsonPropertyName("summonerLevel")] long? SummonerLevel,
    [property: JsonPropertyName("isLeader")] bool IsLeader,
    [property: JsonPropertyName("ready")] bool Ready,
    [property: JsonPropertyName("teamPosition")] string? TeamPosition,
    [property: JsonPropertyName("firstPositionPreference")] string? FirstPositionPreference,
    [property: JsonPropertyName("playerSlots")] List<LobbyPlayerSlot>? PlayerSlots,
    [property: JsonPropertyName("secondPositionPreference")] string? SecondPositionPreference = null,
    [property: JsonPropertyName("thirdPositionPreference")] string? ThirdPositionPreference = null,
    [property: JsonPropertyName("fourthPositionPreference")] string? FourthPositionPreference = null,
    [property: JsonPropertyName("fifthPositionPreference")] string? FifthPositionPreference = null)
{
    public string Name => !string.IsNullOrWhiteSpace(GameName)
        ? string.IsNullOrWhiteSpace(TagLine) ? GameName! : $"{GameName}#{TagLine}"
        : SummonerName ?? "Unknown summoner";

    public string Position => !string.IsNullOrWhiteSpace(TeamPosition)
        ? TeamPosition!
        : string.Join(", ", new[] { FirstPositionPreference, SecondPositionPreference }
            .Where(position => !string.IsNullOrWhiteSpace(position)));

    public int? IconId => ProfileIconId ?? SummonerIconId;

    public bool NeedsProfileEnrichment => string.IsNullOrWhiteSpace(GameName)
        && string.IsNullOrWhiteSpace(SummonerName)
        && SummonerId is not null;

    public LobbyMember WithProfile(SummonerProfile profile) =>
        this with
        {
            SummonerName = profile.DisplayName ?? SummonerName,
            GameName = profile.GameName ?? GameName,
            TagLine = profile.TagLine ?? TagLine,
            ProfileIconId = profile.ProfileIconId ?? ProfileIconId,
            SummonerLevel = profile.SummonerLevel ?? SummonerLevel,
            Puuid = profile.Puuid ?? Puuid
        };
}

public sealed record SummonerProfile(
    [property: JsonPropertyName("summonerId")] long? SummonerId,
    [property: JsonPropertyName("displayName")] string? DisplayName,
    [property: JsonPropertyName("gameName")] string? GameName,
    [property: JsonPropertyName("tagLine")] string? TagLine,
    [property: JsonPropertyName("summonerLevel")] long? SummonerLevel,
    [property: JsonPropertyName("profileIconId")] int? ProfileIconId,
    [property: JsonPropertyName("puuid")] string? Puuid)
{
    public string Name => !string.IsNullOrWhiteSpace(GameName)
        ? string.IsNullOrWhiteSpace(TagLine) ? GameName! : $"{GameName}#{TagLine}"
        : DisplayName ?? "Unknown summoner";

    public static SummonerProfile FromCurrent(CurrentSummoner summoner) =>
        new(summoner.SummonerId, summoner.DisplayName, summoner.GameName, summoner.TagLine, summoner.SummonerLevel, summoner.ProfileIconId, summoner.Puuid);

    public static SummonerProfile FromLobbyMember(LobbyMember member) =>
        new(member.SummonerId, member.SummonerName, member.GameName, member.TagLine, member.SummonerLevel, member.ProfileIconId ?? member.SummonerIconId, member.Puuid);
}

public sealed record RankedSummary(string Text);

public sealed record ReadyCheckState(
    [property: JsonPropertyName("state")] string? State,
    [property: JsonPropertyName("playerResponse")] string? PlayerResponse)
{
    public bool NeedsResponse => string.Equals(State, "InProgress", StringComparison.OrdinalIgnoreCase)
        && string.Equals(PlayerResponse, "None", StringComparison.OrdinalIgnoreCase);
}

public sealed record ChampionSelectSession(
    [property: JsonPropertyName("localPlayerCellId")] int LocalPlayerCellId,
    [property: JsonPropertyName("actions")] List<List<ChampionSelectAction>> Actions,
    [property: JsonPropertyName("myTeam")] List<ChampionSelectPlayer> MyTeam,
    [property: JsonPropertyName("theirTeam")] List<ChampionSelectPlayer> TheirTeam,
    [property: JsonPropertyName("bans")] ChampionSelectBans? Bans,
    [property: JsonPropertyName("benchChampionIds")] List<int>? BenchChampionIds,
    [property: JsonPropertyName("benchEnabled")] bool BenchEnabled,
    [property: JsonPropertyName("allowRerolling")] bool AllowRerolling,
    [property: JsonPropertyName("allowSubsetChampionPicks")] bool AllowSubsetChampionPicks)
{
    [JsonPropertyName("benchChampions")]
    public List<ChampionSelectBenchChampion>? BenchChampions { get; init; }

    public ChampionSelectPlayer? LocalPlayer => MyTeam.FirstOrDefault(player => player.CellId == LocalPlayerCellId);

    public IReadOnlyList<int> AvailableBenchChampionIds => BenchChampions is { Count: > 0 }
        ? BenchChampions.Select(champion => champion.ChampionId).Where(championId => championId > 0).ToList()
        : BenchChampionIds ?? [];

    public IReadOnlySet<int> BannedChampionIds => (Bans?.MyTeamBans ?? [])
        .Concat(Bans?.TheirTeamBans ?? [])
        .Where(championId => championId > 0)
        .ToHashSet();

    public IReadOnlySet<int> AllyHoveredChampionIds => MyTeam
        .Where(player => player.CellId != LocalPlayerCellId && player.ChampionId > 0)
        .Select(player => player.ChampionId)
        .Concat(Actions
            .SelectMany(group => group)
            .Where(action =>
                action.ActorCellId != LocalPlayerCellId
                && action.ChampionId > 0
                && !action.Completed
                && string.Equals(action.Type, "pick", StringComparison.OrdinalIgnoreCase))
            .Select(action => action.ChampionId))
        .ToHashSet();

    public ChampionSelectAction? CurrentAction(string type) => Actions
        .SelectMany(group => group)
        .FirstOrDefault(action =>
            action.ActorCellId == LocalPlayerCellId
            && action.IsInProgress
            && !action.Completed
            && string.Equals(action.Type, type, StringComparison.OrdinalIgnoreCase));

    public ChampionSelectAction? OpenAction(string type) => Actions
        .SelectMany(group => group)
        .FirstOrDefault(action =>
            action.ActorCellId == LocalPlayerCellId
            && !action.Completed
            && string.Equals(action.Type, type, StringComparison.OrdinalIgnoreCase));
}

public sealed record ChampionSelectBans(
    [property: JsonPropertyName("myTeamBans")] List<int> MyTeamBans,
    [property: JsonPropertyName("theirTeamBans")] List<int> TheirTeamBans);

public sealed record ChampionSelectBenchChampion(
    [property: JsonPropertyName("championId")] int ChampionId);

public sealed record ChampionSelectAction(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("actorCellId")] int ActorCellId,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("championId")] int ChampionId,
    [property: JsonPropertyName("completed")] bool Completed,
    [property: JsonPropertyName("isInProgress")] bool IsInProgress);

public sealed record ChampionSelectPlayer(
    [property: JsonPropertyName("cellId")] int CellId,
    [property: JsonPropertyName("championId")] int ChampionId,
    [property: JsonPropertyName("summonerId")] long SummonerId,
    [property: JsonPropertyName("assignedPosition")] string? AssignedPosition);

public sealed record ChampionTradeRequest(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("state")] string? State,
    [property: JsonPropertyName("cellId")] int CellId,
    [property: JsonPropertyName("championId")] int ChampionId)
{
    public bool IsPending => string.Equals(State, "AVAILABLE", StringComparison.OrdinalIgnoreCase)
        || string.Equals(State, "RECEIVED", StringComparison.OrdinalIgnoreCase)
        || string.Equals(State, "SENT", StringComparison.OrdinalIgnoreCase);
}

public sealed record GameflowSession(
    [property: JsonPropertyName("gameData")] GameflowGameData? GameData);

public sealed record GameflowGameData(
    [property: JsonPropertyName("queue")] GameflowQueue? Queue);

public sealed record GameflowQueue(
    [property: JsonPropertyName("id")] int? Id,
    [property: JsonPropertyName("queueId")] int? QueueId,
    [property: JsonPropertyName("gameMode")] string? GameMode);
