using System.Net.Http.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using Pickwise.Models;

namespace Pickwise.Services;

public sealed class KuncLcuClient : ILcuClient, IDisposable
{
    private readonly LocalDiagnosticLog _log;
    private readonly HttpClient _http;
    private readonly Dictionary<long, SummonerProfile?> _summonerProfiles = [];

    public KuncLcuClient(LocalDiagnosticLog log)
    {
        _log = log;
        _http = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });
    }

    public async Task<LcuSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (TryGetConnection() is null)
            {
                return Disconnected();
            }

            var summoner = await GetOrNull<CurrentSummoner>("lol-summoner/v1/current-summoner", cancellationToken);
            if (summoner is null)
            {
                return Disconnected();
            }

            var ready = await GetOrNull<ReadyCheckState>("lol-matchmaking/v1/ready-check", cancellationToken);
            var lobby = await GetOrNull<LobbyState>("lol-lobby/v2/lobby", cancellationToken);
            var lobbyMembers = lobby?.Members is { Count: > 0 } members
                ? members
                : await GetListOrEmpty<LobbyMember>("lol-lobby/v2/lobby/members", cancellationToken);
            lobbyMembers = await EnrichLobbyMembersAsync(lobbyMembers, cancellationToken);
            if (ready?.NeedsResponse == true)
            {
                return new(AppPhase.ReadyCheck, summoner, ready, null, null, lobby, lobbyMembers, [], [], [], "Match found");
            }

            var championSelect = await GetOrNull<ChampionSelectSession>("lol-champ-select/v1/session", cancellationToken);
            if (championSelect is not null)
            {
                var gameflow = await GetOrNull<GameflowSession>("lol-gameflow/v1/session", cancellationToken);
                var pickable = championSelect.AllowSubsetChampionPicks
                    ? await GetListOrEmpty("lol-lobby-team-builder/champ-select/v1/subset-champion-list", cancellationToken)
                    : await GetListOrEmpty("lol-champ-select/v1/pickable-champion-ids", cancellationToken);
                var disabled = await GetListOrEmpty("lol-champ-select/v1/disabled-champion-ids", cancellationToken);
                var trades = await GetListOrEmpty<ChampionTradeRequest>("lol-champ-select/v1/session/trades", cancellationToken);
                return new(AppPhase.ChampionSelect, summoner, ready, championSelect, gameflow, lobby, lobbyMembers, pickable, disabled, trades, "Champion select");
            }

            return new(AppPhase.Connected, summoner, ready, null, null, lobby, lobbyMembers, [], [], [], "Connected");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _log.Error("LCU snapshot failed", exception);
            return new(AppPhase.Error, null, null, null, null, null, [], [], [], [], "Cannot connect to League Client");
        }
    }

    public Task<SummonerProfile?> GetSummonerProfileAsync(long summonerId, CancellationToken cancellationToken) =>
        GetOrNull<SummonerProfile>($"lol-summoner/v1/summoners/{summonerId}", cancellationToken);

    public async Task<IReadOnlyList<GameMode>> GetQueuesAsync(CancellationToken cancellationToken)
    {
        var queues = await GetListOrEmpty<LcuQueue>("lol-game-queues/v1/queues", cancellationToken);
        return queues
            .Where(queue =>
                queue.IsVisible
                && queue.IsEnabled
                && string.Equals(queue.QueueAvailability, "Available", StringComparison.OrdinalIgnoreCase))
            .OrderBy(queue => queue.GameSelectModeGroup)
            .ThenBy(queue => queue.Category)
            .ThenBy(queue => queue.Id)
            .Select(queue => queue.ToGameMode())
            .ToList();
    }

    public async Task<RankedSummary?> GetRankedSummaryAsync(long summonerId, CancellationToken cancellationToken)
    {
        var ranked = await GetOrNull<RankedStats>($"lol-ranked-stats/v1/stats/{summonerId}", cancellationToken);
        var queue = ranked?.Queues?
            .Where(queue => queue.Tier is not null || queue.Rank is not null)
            .OrderBy(queue => queue.QueueType == "RANKED_SOLO_5x5" ? 0 : 1)
            .FirstOrDefault();

        return queue is null
            ? null
            : new RankedSummary($"{queue.QueueType}: {queue.Tier} {queue.Rank} - {queue.LeaguePoints ?? 0} LP");
    }

    public async Task<IReadOnlyList<MatchHistoryEntry>> GetMatchHistoryAsync(string puuid, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(puuid))
        {
            return [];
        }

        var history = await GetOrNull<MatchHistoryResponse>($"lol-match-history/v1/products/lol/{Uri.EscapeDataString(puuid)}/matches?begIndex=0&endIndex=8", cancellationToken);
        if (history?.Games?.Games is not { Count: > 0 } games)
        {
            return [];
        }

        var entries = new List<MatchHistoryEntry>(games.Count);
        foreach (var game in games)
        {
            var detailed = game.Participants is { Count: > 1 } || game.GameId <= 0
                ? game
                : await GetOrNull<MatchHistoryGame>($"lol-match-history/v1/games/{game.GameId}", cancellationToken) ?? game;
            if (ToMatchHistoryEntry(detailed, puuid) is { } entry)
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    public async Task SendFriendRequestAsync(LobbyMember member, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(member.GameName) || string.IsNullOrWhiteSpace(member.TagLine))
        {
            throw new InvalidOperationException("Riot ID is required to send a friend request.");
        }

        using var response = await SendJsonAsync(HttpMethod.Post, "lol-chat/v2/friend-requests", new { gameName = member.GameName, tagLine = member.TagLine }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        _log.Info($"Friend request sent by Player Command: {member.GameName}#{member.TagLine}");
    }

    public async Task AcceptReadyCheckAsync(CancellationToken cancellationToken)
    {
        using var response = await SendJsonAsync(HttpMethod.Post, "lol-matchmaking/v1/ready-check/accept", new { }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        _log.Info("Ready Check accepted by Player Command");
    }

    public async Task DeclineReadyCheckAsync(CancellationToken cancellationToken)
    {
        using var response = await SendJsonAsync(HttpMethod.Post, "lol-matchmaking/v1/ready-check/decline", new { }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        _log.Info("Ready Check declined by Player Command");
    }

    public async Task CreateLobbyAsync(int queueId, CancellationToken cancellationToken)
    {
        using var response = await SendJsonAsync(HttpMethod.Post, "lol-lobby/v2/lobby", new { queueId }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        _log.Info($"Lobby created by Player Command: queueId={queueId}");
    }

    public async Task CreateLobbyAsync(GameMode mode, CancellationToken cancellationToken)
    {
        if (!mode.IsCustom)
        {
            await CreateLobbyAsync(mode.QueueId, cancellationToken);
            return;
        }

        if (mode.GameTypeConfig is null)
        {
            throw new InvalidOperationException("Custom lobby metadata is unavailable.");
        }

        var body = new
        {
            queueId = mode.QueueId,
            customGameLobby = new
            {
                lobbyName = "Pickwise Custom",
                lobbyPassword = "",
                configuration = new
                {
                    mapId = mode.MapId,
                    gameMode = mode.GameModeCode,
                    mutators = mode.GameTypeConfig,
                    gameTypeConfig = mode.GameTypeConfig,
                    spectatorPolicy = "NotAllowed",
                    teamSize = Math.Min(5, Math.Max(1, mode.MaxLobbySize)),
                    maxPlayerCount = (uint)Math.Max(1, mode.MaxLobbySize),
                    tournamentGameMode = "",
                    tournamentPassbackUrl = "",
                    tournamentPassbackDataPacket = "",
                    gameServerRegion = "",
                    spectatorDelayEnabled = false,
                    hidePublicly = false,
                    aramMapMutator = ""
                }
            }
        };

        using var response = await SendJsonAsync(HttpMethod.Post, "lol-lobby/v2/lobby", body, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        _log.Info($"Custom lobby created by Player Command: queueId={mode.QueueId}");
    }

    public async Task UpdatePositionPreferencesAsync(string first, string second, CancellationToken cancellationToken)
    {
        using var response = await SendJsonAsync(
            HttpMethod.Put,
            "lol-lobby/v2/lobby/members/localMember/position-preferences",
            new { firstPreference = first, secondPreference = second },
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        _log.Info($"Lane preferences updated by Player Command: {first}/{second}");
    }

    public async Task LeaveLobbyAsync(CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Delete, "lol-lobby/v2/lobby", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        _log.Info("Lobby left by Player Command");
    }

    public Task<string?> GetQuickplayPerksAsync(int championId, string position, CancellationToken cancellationToken) =>
        GetOrNull<string>($"lol-perks/v1/quick-play-selections/champion/{championId}/position/{Uri.EscapeDataString(position)}", cancellationToken);

    public async Task UpdateQuickplaySlotsAsync(IReadOnlyList<LobbyPlayerSlot> slots, CancellationToken cancellationToken)
    {
        using var response = await SendJsonAsync(HttpMethod.Put, "lol-lobby/v1/lobby/members/localMember/player-slots", slots, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        _log.Info("Quickplay slots updated by Player Command");
    }

    public async Task StartMatchmakingAsync(CancellationToken cancellationToken)
    {
        using var response = await SendJsonAsync(HttpMethod.Post, "lol-lobby/v2/lobby/matchmaking/search", new { }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        _log.Info("Matchmaking started by Player Command");
    }

    public async Task CancelMatchmakingAsync(CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Delete, "lol-lobby/v2/lobby/matchmaking/search", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        _log.Info("Matchmaking cancelled by Player Command");
    }

    public Task DeclareChampionAsync(int championId, CancellationToken cancellationToken) =>
        PatchChampionActionAsync("pick", championId, complete: false, requireInProgress: false, cancellationToken);

    public Task PickChampionAsync(int championId, CancellationToken cancellationToken) =>
        PatchChampionActionAsync("pick", championId, complete: true, requireInProgress: true, cancellationToken);

    public Task BanChampionAsync(int championId, CancellationToken cancellationToken) =>
        PatchChampionActionAsync("ban", championId, complete: true, requireInProgress: true, cancellationToken);

    public async Task SwapBenchChampionAsync(int championId, CancellationToken cancellationToken)
    {
        using var response = await SendJsonAsync(HttpMethod.Post, $"lol-lobby-team-builder/champ-select/v1/session/bench/swap/{championId}", new { }, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            using var fallback = await SendJsonAsync(HttpMethod.Post, $"lol-champ-select/v1/session/bench/swap/{championId}", new { }, cancellationToken);
            await EnsureSuccessAsync(fallback, cancellationToken);
        }

        _log.Info($"ARAM bench champion swapped by Player Command: championId={championId}");
    }

    public async Task AcceptTradeAsync(int tradeId, CancellationToken cancellationToken)
    {
        using var response = await SendJsonAsync(HttpMethod.Post, $"lol-champ-select/v1/session/trades/{tradeId}/accept", new { }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        _log.Info($"Champion trade accepted by Player Command: tradeId={tradeId}");
    }

    public async Task DeclineTradeAsync(int tradeId, CancellationToken cancellationToken)
    {
        using var response = await SendJsonAsync(HttpMethod.Post, $"lol-champ-select/v1/session/trades/{tradeId}/decline", new { }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        _log.Info($"Champion trade declined by Player Command: tradeId={tradeId}");
    }

    public void Dispose() => _http.Dispose();

    private static LcuSnapshot Disconnected() =>
        new(AppPhase.WaitingForLeagueClient, null, null, null, null, null, [], [], [], [], "Waiting for League Client");

    private async Task<T?> GetOrNull<T>(string endpoint, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await SendAsync(HttpMethod.Get, endpoint, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return default;
            }

            return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
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

    private async Task<IReadOnlyList<int>> GetListOrEmpty(string endpoint, CancellationToken cancellationToken) =>
        await GetOrNull<List<int>>(endpoint, cancellationToken) ?? [];

    private async Task<IReadOnlyList<T>> GetListOrEmpty<T>(string endpoint, CancellationToken cancellationToken) =>
        await GetOrNull<List<T>>(endpoint, cancellationToken) ?? [];

    private async Task<IReadOnlyList<LobbyMember>> EnrichLobbyMembersAsync(IReadOnlyList<LobbyMember> members, CancellationToken cancellationToken)
    {
        if (members.Count == 0)
        {
            return members;
        }

        var enriched = new List<LobbyMember>(members.Count);
        foreach (var member in members)
        {
            if (!member.NeedsProfileEnrichment || member.SummonerId is not { } summonerId)
            {
                enriched.Add(member);
                continue;
            }

            if (!_summonerProfiles.TryGetValue(summonerId, out var profile))
            {
                profile = await GetSummonerProfileAsync(summonerId, cancellationToken);
                _summonerProfiles[summonerId] = profile;
            }

            enriched.Add(profile is null ? member : member.WithProfile(profile));
        }

        return enriched;
    }

    private async Task PatchChampionActionAsync(string type, int championId, bool complete, bool requireInProgress, CancellationToken cancellationToken)
    {
        var session = await GetOrNull<ChampionSelectSession>("lol-champ-select/v1/session", cancellationToken);
        var action = requireInProgress
            ? session?.CurrentAction(type)
            : session?.OpenAction(type);
        if (action is null)
        {
            throw new InvalidOperationException($"No active {type} action.");
        }

        using var response = complete
            ? await SendJsonAsync(HttpMethod.Patch, $"lol-champ-select/v1/session/actions/{action.Id}", new { championId, completed = true }, cancellationToken)
            : await SendJsonAsync(HttpMethod.Patch, $"lol-champ-select/v1/session/actions/{action.Id}", new { championId }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        _log.Info(complete
            ? $"Champion {type} submitted by Player Command"
            : $"Champion {type} declared by Player Command");
    }

    private static MatchHistoryEntry? ToMatchHistoryEntry(MatchHistoryGame game, string puuid)
    {
        var namesByParticipantId = game.ParticipantIdentities?
            .Where(identity => identity.Player is not null)
            .ToDictionary(identity => identity.ParticipantId, identity => identity.Player!)
            ?? [];
        var participantId = game.ParticipantIdentities?
            .FirstOrDefault(identity => string.Equals(identity.Player?.Puuid, puuid, StringComparison.OrdinalIgnoreCase))
            ?.ParticipantId;
        var participant = participantId is null
            ? game.Participants?.FirstOrDefault()
            : game.Participants?.FirstOrDefault(candidate => candidate.ParticipantId == participantId);
        if (participant?.Stats is null)
        {
            return null;
        }

        var participants = game.Participants?
            .Where(candidate => candidate.Stats is not null)
            .Select(candidate => ToParticipantPerformance(candidate, namesByParticipantId.GetValueOrDefault(candidate.ParticipantId)))
            .ToList()
            ?? [];

        return new(
            participant.ChampionId,
            $"Champion {participant.ChampionId}",
            QueueLabel(game.QueueId),
            participant.Stats.Win,
            participant.Stats.Kills,
            participant.Stats.Deaths,
            participant.Stats.Assists,
            participant.Stats.TotalMinionsKilled + participant.Stats.NeutralMinionsKilled,
            participant.Stats.GoldEarned,
            participant.Stats.TotalDamageDealtToChampions,
            participant.Stats.LargestMultiKill,
            participant.Spell1Id,
            participant.Spell2Id,
            new[] { participant.Stats.Item0, participant.Stats.Item1, participant.Stats.Item2, participant.Stats.Item3, participant.Stats.Item4, participant.Stats.Item5, participant.Stats.Item6 }
                .Where(itemId => itemId > 0)
                .ToList(),
            TimeSpan.FromSeconds(game.GameDuration).ToString(@"m\:ss"),
            string.IsNullOrWhiteSpace(game.GameCreationDate) ? "Recent" : game.GameCreationDate!,
            participants);
    }

    private static MatchParticipantPerformance ToParticipantPerformance(MatchHistoryParticipant participant, MatchHistoryPlayer? player)
    {
        var stats = participant.Stats!;
        return new(
            string.IsNullOrWhiteSpace(player?.Puuid) ? $"participant:{participant.ParticipantId}" : player!.Puuid!,
            PlayerName(player, participant.ParticipantId),
            participant.TeamId,
            stats.Win,
            participant.ChampionId,
            $"Champion {participant.ChampionId}",
            stats.Kills,
            stats.Deaths,
            stats.Assists,
            stats.TotalMinionsKilled + stats.NeutralMinionsKilled,
            stats.GoldEarned,
            stats.TotalDamageDealtToChampions,
            stats.LargestMultiKill);
    }

    private static string PlayerName(MatchHistoryPlayer? player, int participantId) =>
        !string.IsNullOrWhiteSpace(player?.GameName)
            ? string.IsNullOrWhiteSpace(player.TagLine) ? player.GameName! : $"{player.GameName}#{player.TagLine}"
            : player?.SummonerName ?? $"Player {participantId}";

    private static string QueueLabel(int queueId) =>
        queueId switch
        {
            400 => "Normal Draft",
            420 => "Ranked Solo/Duo",
            430 => "Blind Pick",
            440 => "Ranked Flex",
            450 => "ARAM",
            480 => "Swiftplay",
            _ => $"Queue {queueId}"
        };

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(string.IsNullOrWhiteSpace(error)
            ? $"LCU request failed: {(int)response.StatusCode} {response.ReasonPhrase}"
            : error);
    }

    private Task<HttpResponseMessage> SendJsonAsync<T>(HttpMethod method, string endpoint, T body, CancellationToken cancellationToken)
    {
        var request = CreateRequest(method, endpoint);
        request.Content = JsonContent.Create(body);
        return _http.SendAsync(request, cancellationToken);
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string endpoint, CancellationToken cancellationToken) =>
        _http.SendAsync(CreateRequest(method, endpoint), cancellationToken);

    private HttpRequestMessage CreateRequest(HttpMethod method, string endpoint)
    {
        var connection = TryGetConnection() ?? throw new InvalidOperationException("League Client lockfile is not available.");
        var request = new HttpRequestMessage(method, new Uri(connection.BaseUri, endpoint));
        request.Headers.Authorization = connection.Authorization;
        return request;
    }

    private static LcuConnection? TryGetConnection()
    {
        var process = Process.GetProcessesByName("LeagueClientUx")
            .FirstOrDefault(process => !string.IsNullOrWhiteSpace(process.MainModule?.FileName));
        var directory = process is null ? null : Path.GetDirectoryName(process.MainModule?.FileName);
        var lockfilePath = directory is null ? null : Path.Combine(directory, "lockfile");

        return lockfilePath is not null && File.Exists(lockfilePath)
            ? LcuConnection.TryParse(ReadShared(lockfilePath))
            : null;
    }

    private static string ReadShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

public sealed record LcuConnection(Uri BaseUri, AuthenticationHeaderValue Authorization)
{
    public static LcuConnection? TryParse(string lockfile)
    {
        var parts = lockfile.Trim().Split(':');
        if (parts.Length != 5 || !int.TryParse(parts[2], out var port) || string.IsNullOrWhiteSpace(parts[3]))
        {
            return null;
        }

        var token = Convert.ToBase64String(Encoding.ASCII.GetBytes($"riot:{parts[3]}"));
        return new(new Uri($"https://127.0.0.1:{port}/"), new AuthenticationHeaderValue("Basic", token));
    }
}

public sealed record RankedStats(
    [property: System.Text.Json.Serialization.JsonPropertyName("queues")] List<RankedQueue>? Queues);

public sealed record RankedQueue(
    [property: System.Text.Json.Serialization.JsonPropertyName("queueType")] string? QueueType,
    [property: System.Text.Json.Serialization.JsonPropertyName("tier")] string? Tier,
    [property: System.Text.Json.Serialization.JsonPropertyName("rank")] string? Rank,
    [property: System.Text.Json.Serialization.JsonPropertyName("leaguePoints")] int? LeaguePoints);

public sealed record MatchHistoryResponse(
    [property: System.Text.Json.Serialization.JsonPropertyName("games")] MatchHistoryGames? Games);

public sealed record MatchHistoryGames(
    [property: System.Text.Json.Serialization.JsonPropertyName("games")] List<MatchHistoryGame>? Games);

public sealed record MatchHistoryGame(
    [property: System.Text.Json.Serialization.JsonPropertyName("gameId")] long GameId,
    [property: System.Text.Json.Serialization.JsonPropertyName("gameCreationDate")] string? GameCreationDate,
    [property: System.Text.Json.Serialization.JsonPropertyName("gameDuration")] int GameDuration,
    [property: System.Text.Json.Serialization.JsonPropertyName("queueId")] int QueueId,
    [property: System.Text.Json.Serialization.JsonPropertyName("participants")] List<MatchHistoryParticipant>? Participants,
    [property: System.Text.Json.Serialization.JsonPropertyName("participantIdentities")] List<MatchHistoryParticipantIdentity>? ParticipantIdentities);

public sealed record MatchHistoryParticipant(
    [property: System.Text.Json.Serialization.JsonPropertyName("participantId")] int ParticipantId,
    [property: System.Text.Json.Serialization.JsonPropertyName("teamId")] int TeamId,
    [property: System.Text.Json.Serialization.JsonPropertyName("championId")] int ChampionId,
    [property: System.Text.Json.Serialization.JsonPropertyName("spell1Id")] int Spell1Id,
    [property: System.Text.Json.Serialization.JsonPropertyName("spell2Id")] int Spell2Id,
    [property: System.Text.Json.Serialization.JsonPropertyName("stats")] MatchHistoryStats? Stats);

public sealed record MatchHistoryStats(
    [property: System.Text.Json.Serialization.JsonPropertyName("win")] bool Win,
    [property: System.Text.Json.Serialization.JsonPropertyName("kills")] int Kills,
    [property: System.Text.Json.Serialization.JsonPropertyName("deaths")] int Deaths,
    [property: System.Text.Json.Serialization.JsonPropertyName("assists")] int Assists,
    [property: System.Text.Json.Serialization.JsonPropertyName("totalMinionsKilled")] int TotalMinionsKilled,
    [property: System.Text.Json.Serialization.JsonPropertyName("neutralMinionsKilled")] int NeutralMinionsKilled,
    [property: System.Text.Json.Serialization.JsonPropertyName("goldEarned")] int GoldEarned,
    [property: System.Text.Json.Serialization.JsonPropertyName("totalDamageDealtToChampions")] int TotalDamageDealtToChampions,
    [property: System.Text.Json.Serialization.JsonPropertyName("largestMultiKill")] int LargestMultiKill,
    [property: System.Text.Json.Serialization.JsonPropertyName("item0")] int Item0,
    [property: System.Text.Json.Serialization.JsonPropertyName("item1")] int Item1,
    [property: System.Text.Json.Serialization.JsonPropertyName("item2")] int Item2,
    [property: System.Text.Json.Serialization.JsonPropertyName("item3")] int Item3,
    [property: System.Text.Json.Serialization.JsonPropertyName("item4")] int Item4,
    [property: System.Text.Json.Serialization.JsonPropertyName("item5")] int Item5,
    [property: System.Text.Json.Serialization.JsonPropertyName("item6")] int Item6);

public sealed record MatchHistoryParticipantIdentity(
    [property: System.Text.Json.Serialization.JsonPropertyName("participantId")] int ParticipantId,
    [property: System.Text.Json.Serialization.JsonPropertyName("player")] MatchHistoryPlayer? Player);

public sealed record MatchHistoryPlayer(
    [property: System.Text.Json.Serialization.JsonPropertyName("puuid")] string? Puuid,
    [property: System.Text.Json.Serialization.JsonPropertyName("summonerName")] string? SummonerName,
    [property: System.Text.Json.Serialization.JsonPropertyName("gameName")] string? GameName,
    [property: System.Text.Json.Serialization.JsonPropertyName("tagLine")] string? TagLine);

public sealed record LcuQueue(
    [property: System.Text.Json.Serialization.JsonPropertyName("id")] int Id,
    [property: System.Text.Json.Serialization.JsonPropertyName("name")] string? Name,
    [property: System.Text.Json.Serialization.JsonPropertyName("shortName")] string? ShortName,
    [property: System.Text.Json.Serialization.JsonPropertyName("category")] string? Category,
    [property: System.Text.Json.Serialization.JsonPropertyName("gameSelectModeGroup")] string? GameSelectModeGroup,
    [property: System.Text.Json.Serialization.JsonPropertyName("gameMode")] string? GameMode,
    [property: System.Text.Json.Serialization.JsonPropertyName("type")] string? Type,
    [property: System.Text.Json.Serialization.JsonPropertyName("mapId")] int MapId,
    [property: System.Text.Json.Serialization.JsonPropertyName("pickMode")] string? PickMode,
    [property: System.Text.Json.Serialization.JsonPropertyName("isCustom")] bool IsCustom,
    [property: System.Text.Json.Serialization.JsonPropertyName("isEnabled")] bool IsEnabled,
    [property: System.Text.Json.Serialization.JsonPropertyName("isVisible")] bool IsVisible,
    [property: System.Text.Json.Serialization.JsonPropertyName("queueAvailability")] string? QueueAvailability,
    [property: System.Text.Json.Serialization.JsonPropertyName("showPositionSelector")] bool ShowPositionSelector,
    [property: System.Text.Json.Serialization.JsonPropertyName("showQuickPlaySlotSelection")] bool ShowQuickPlaySlotSelection,
    [property: System.Text.Json.Serialization.JsonPropertyName("maximumParticipantListSize")] int MaximumParticipantListSize,
    [property: System.Text.Json.Serialization.JsonPropertyName("gameTypeConfig")] LcuQueueGameTypeConfig? GameTypeConfig)
{
    public GameMode ToGameMode() =>
        new(
            string.IsNullOrWhiteSpace(Name) ? $"Queue {Id}" : Name!,
            Id,
            Category ?? "",
            GameSelectModeGroup ?? "",
            GameMode ?? "",
            Type ?? "",
            MapId,
            PickMode ?? "",
            IsCustom,
            ShowPositionSelector,
            ShowQuickPlaySlotSelection,
            MaximumParticipantListSize,
            GameTypeConfig?.ToModel(PickMode ?? ""));
}

public sealed record LcuQueueGameTypeConfig(
    [property: System.Text.Json.Serialization.JsonPropertyName("id")] long Id,
    [property: System.Text.Json.Serialization.JsonPropertyName("name")] string? Name,
    [property: System.Text.Json.Serialization.JsonPropertyName("maxAllowableBans")] int MaxAllowableBans,
    [property: System.Text.Json.Serialization.JsonPropertyName("allowTrades")] bool AllowTrades,
    [property: System.Text.Json.Serialization.JsonPropertyName("exclusivePick")] bool ExclusivePick,
    [property: System.Text.Json.Serialization.JsonPropertyName("duplicatePick")] bool DuplicatePick,
    [property: System.Text.Json.Serialization.JsonPropertyName("teamChampionPool")] bool TeamChampionPool,
    [property: System.Text.Json.Serialization.JsonPropertyName("crossTeamChampionPool")] bool CrossTeamChampionPool,
    [property: System.Text.Json.Serialization.JsonPropertyName("advancedLearningQuests")] bool AdvancedLearningQuests,
    [property: System.Text.Json.Serialization.JsonPropertyName("battleBoost")] bool BattleBoost,
    [property: System.Text.Json.Serialization.JsonPropertyName("deathMatch")] bool DeathMatch,
    [property: System.Text.Json.Serialization.JsonPropertyName("doNotRemove")] bool DoNotRemove,
    [property: System.Text.Json.Serialization.JsonPropertyName("learningQuests")] bool LearningQuests,
    [property: System.Text.Json.Serialization.JsonPropertyName("onboardCoopBeginner")] bool OnboardCoopBeginner,
    [property: System.Text.Json.Serialization.JsonPropertyName("reroll")] bool Reroll,
    [property: System.Text.Json.Serialization.JsonPropertyName("mainPickTimerDuration")] int MainPickTimerDuration,
    [property: System.Text.Json.Serialization.JsonPropertyName("postPickTimerDuration")] int PostPickTimerDuration,
    [property: System.Text.Json.Serialization.JsonPropertyName("banTimerDuration")] int BanTimerDuration,
    [property: System.Text.Json.Serialization.JsonPropertyName("pickMode")] string? PickMode,
    [property: System.Text.Json.Serialization.JsonPropertyName("banMode")] string? BanMode,
    [property: System.Text.Json.Serialization.JsonPropertyName("gameModeOverride")] string? GameModeOverride,
    [property: System.Text.Json.Serialization.JsonPropertyName("numPlayersPerTeamOverride")] int? NumPlayersPerTeamOverride)
{
    public LcuGameTypeConfig ToModel(string queuePickMode) =>
        new(
            Id,
            Name ?? "",
            MaxAllowableBans,
            AllowTrades,
            ExclusivePick,
            DuplicatePick,
            TeamChampionPool,
            CrossTeamChampionPool,
            AdvancedLearningQuests,
            BattleBoost,
            DeathMatch,
            DoNotRemove,
            LearningQuests,
            OnboardCoopBeginner,
            Reroll,
            MainPickTimerDuration,
            PostPickTimerDuration,
            BanTimerDuration,
            string.IsNullOrWhiteSpace(PickMode) ? queuePickMode : PickMode!,
            BanMode ?? "",
            GameModeOverride,
            NumPlayersPerTeamOverride);
}
