using System.Text.Json.Serialization;

namespace Pickwise.Models;

public sealed record LcuSnapshot(
    AppPhase Phase,
    CurrentSummoner? Summoner,
    ReadyCheckState? ReadyCheck,
    ChampionSelectSession? ChampionSelect,
    string Message);

public sealed record CurrentSummoner(
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
    [property: JsonPropertyName("theirTeam")] List<ChampionSelectPlayer> TheirTeam)
{
    public ChampionSelectAction? CurrentAction(string type) => Actions
        .SelectMany(group => group)
        .FirstOrDefault(action =>
            action.ActorCellId == LocalPlayerCellId
            && action.IsInProgress
            && !action.Completed
            && string.Equals(action.Type, type, StringComparison.OrdinalIgnoreCase));
}

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
