using Avalonia.Media.Imaging;
using Pickwise.Models;
using Pickwise.Services;
using Pickwise.ViewModels;
using System.Diagnostics;
using System.Text.Json;

var session = new ChampionSelectSession(
    LocalPlayerCellId: 2,
    Actions:
    [
        [new ChampionSelectAction(1, 1, "pick", 0, false, true)],
        [new ChampionSelectAction(2, 2, "ban", 0, false, true)],
        [new ChampionSelectAction(3, 2, "pick", 0, false, false)]
    ],
    MyTeam: [],
    TheirTeam: [],
    Bans: new ChampionSelectBans([103], [12]),
    BenchChampionIds: [45, 67],
    BenchEnabled: true,
    AllowRerolling: true,
    AllowSubsetChampionPicks: false);

Assert(session.CurrentAction("ban")?.Id == 2, "finds active local ban action");
Assert(session.CurrentAction("pick") is null, "ignores inactive pick action");
Assert(session.OpenAction("pick")?.Id == 3, "finds open local pick action for declare");
Assert(session.BannedChampionIds.SetEquals([103, 12]), "combines team bans");
Assert(session.BenchChampionIds!.SequenceEqual([45, 67]), "loads ARAM bench champion ids");
AssertSessionLoadsCurrentBenchShape();
AssertSessionFallsBackToLegacyBenchIds();
AssertChampionCatalogSearchWorks();
AssertGameModeCatalogWorks();
AssertChampionRoleFilterWorks();
AssertAramChampionSelectionUsesCardAndBenchLists();
AssertCustomRandomSrUsesRandomCardUi();
AssertFiveVFiveSingleClickDeclaresChampionOnly();
AssertFiveVFiveDoubleClickPicksChampion();
AssertFiveVFiveBanPhaseCanDeclareChampion();
AssertBanWarningUsesAllyHovers();
AssertChampionSelectTimelineLabels();
AssertFavoritesSortFirst();
AssertQuickBansRespectAvailability();
AssertTradeCommandsCallLcu();
AssertCurrentSummonerProfileOpensAndBackReturnsHome();
AssertLobbyRendersFromSnapshot();
AssertLobbyPayloadLoadsEmbeddedMembers();
AssertSwiftplayLobbyPayloadLoadsSlotsAndRestrictions();
AssertNoLobbyDoesNotBreakReadyScreen();
AssertLobbyMemberProfileUsesSharedProfile();
AssertRankedFailureStillOpensProfile();
AssertAddFriendUsesRiotId();
AssertAddFriendDisabledWithoutRiotIdOrForLocalPlayer();
AssertExistingLobbyOpensReadyScreen();
AssertSwiftplayRestrictionBlocksMatchmaking();
AssertBlankLobbyMemberNameCanUseProfileData();
AssertPollingPolicyUsesScreenAndPhase();
AssertLobbyRowsAreReused();
AssertLobbyRowsUpdateInPlace();
AssertSummonerIconLoadsAreDeduped();
AssertLcuModeCapabilitiesDriveModeList();
AssertLanePreferencesCanBeSaved();
AssertQuickplaySetupIsDetectOnly();
AssertQuickplayEditorSurvivesUnchangedPoll();
AssertQuickplayEditorFiltersByActiveLane();
AssertQuickplaySlotsCanBeSavedWithRecommendedRunes();
AssertCustomLobbyUsesModeCommand();
AssertBotModesUseOwnGroup();
AssertLeaveLobbyCallsLcu();
AssertCreateLobbyRefreshesSnapshotImmediately();
AssertModeScreenNavigationWorks();
AssertGameModeIconCachePathWorks();
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
    Assert(catalog.All.Any(champion => champion.ChampionId == 62 && champion.ImageFileName == "MonkeyKing.png"), "catalog keeps Data Dragon image names");
    Assert(catalog.All.Any(champion => champion.Name == "Ahri" && champion.Tags.Contains("Mage")), "catalog loads Data Dragon tags");
    Assert(catalog.Filter("", "Marksman").All(champion => champion.Tags.Contains("Marksman")), "role filter returns only matching champions");
    Assert(catalog.Filter("cait", "Marksman").Any(champion => champion.Name == "Caitlyn"), "search combines with role filter");
    Assert(!catalog.Filter("cait", "Mage").Any(champion => champion.Name == "Caitlyn"), "role filter excludes search matches in other roles");
}

static void AssertSessionLoadsCurrentBenchShape()
{
    const string json = """
        {
          "localPlayerCellId": 1,
          "actions": [],
          "myTeam": [],
          "theirTeam": [],
          "bans": { "myTeamBans": [], "theirTeamBans": [] },
          "benchChampions": [
            { "championId": 105 },
            { "championId": 711 }
          ],
          "benchEnabled": true,
          "allowRerolling": false,
          "allowSubsetChampionPicks": true
        }
        """;

    var parsed = JsonSerializer.Deserialize<ChampionSelectSession>(json)!;
    Assert(parsed.AvailableBenchChampionIds.SequenceEqual([105, 711]), "loads current LCU benchChampions shape");
}

static void AssertSessionFallsBackToLegacyBenchIds()
{
    var session = new ChampionSelectSession(
        LocalPlayerCellId: 1,
        Actions: [],
        MyTeam: [],
        TheirTeam: [],
        Bans: new ChampionSelectBans([], []),
        BenchChampionIds: [45, 67],
        BenchEnabled: true,
        AllowRerolling: false,
        AllowSubsetChampionPicks: false);

    Assert(session.AvailableBenchChampionIds.SequenceEqual([45, 67]), "loads legacy benchChampionIds shape");
}

static void AssertGameModeCatalogWorks()
{
    var catalog = new GameModeCatalog();
    Assert(catalog.All.Any(mode => mode.Name == "Normal Draft 5v5" && mode.QueueId == 400), "catalog contains Normal Draft 5v5 queue");
    Assert(catalog.All.Any(mode => mode.Name == "ARAM" && mode.QueueId == 450), "catalog contains ARAM queue");
    Assert(catalog.All.Any(mode => mode.Name == "ARAM Mayhem" && mode.QueueId == 2400), "catalog contains ARAM Mayhem queue");
}

static void AssertChampionRoleFilterWorks()
{
    var viewModel = new MainViewModel(new SlowLcuClient(), new LocalDiagnosticLog());
    Assert(viewModel.SelectedChampionRole.Name == "All", "role filter defaults to All");

    viewModel.ChampionSearch = "cait";
    viewModel.SelectChampionRoleCommand.Execute(viewModel.ChampionRoles.Single(role => role.Name == "Marksman"));
    Assert(viewModel.Champions.Any(champion => champion.Name == "Caitlyn"), "role icon filter includes matching marksmen");

    viewModel.SelectChampionRoleCommand.Execute(viewModel.ChampionRoles.Single(role => role.Name == "Mage"));
    Assert(!viewModel.Champions.Any(champion => champion.Name == "Caitlyn"), "role icon filter excludes champions outside selected role");
}

static void AssertAramChampionSelectionUsesCardAndBenchLists()
{
    var lcu = new AramLcuClient(localChampionId: 0);
    var viewModel = new MainViewModel(lcu, new LocalDiagnosticLog());
    Thread.Sleep(100);

    Assert(viewModel.IsAramChampionSelect, "ARAM snapshot switches to ARAM champion select");
    Assert(viewModel.Champions.Count == 0, "ARAM does not use the 5v5 champion grid");
    Assert(viewModel.AramAvailableChampions.Select(tile => tile.Champion.ChampionId).SequenceEqual([103, 222, 145]), "ARAM shows all available card champions");
    Assert(viewModel.AramBenchChampions.Select(tile => tile.Champion.ChampionId).SequenceEqual([222, 145]), "ARAM shows bench whenever LCU returns bench ids");

    viewModel.PickAramChampionTileCommand.Execute(viewModel.AramAvailableChampions[0]);
    Assert(lcu.PickedChampionId == 103, "single-clicking an ARAM card picks it");

    viewModel.SelectedChampionTile = viewModel.AramAvailableChampions[1];
    Assert(viewModel.CanPickAramChampion, "selecting an ARAM card enables Pick Card");

    var selectedViewModel = new MainViewModel(new AramLcuClient(localChampionId: 103), new LocalDiagnosticLog());
    Thread.Sleep(100);
    Assert(selectedViewModel.AramAvailableChampions.Count == 0, "ARAM cards disappear after a champion is selected");
    Assert(selectedViewModel.AramBenchChampions.Select(tile => tile.Champion.ChampionId).SequenceEqual([222, 145]), "bench stays visible after a champion is selected");

    var selectedLcu = new AramLcuClient(localChampionId: 103);
    var swapViewModel = new MainViewModel(selectedLcu, new LocalDiagnosticLog());
    Thread.Sleep(100);
    swapViewModel.SwapBenchChampionTileCommand.Execute(swapViewModel.AramBenchChampions[0]);
    Assert(selectedLcu.SwappedChampionId == 222, "single-clicking an ARAM bench champion swaps it");

    swapViewModel.SelectedChampionTile = swapViewModel.AramBenchChampions[1];
    Assert(!swapViewModel.CanPickAramChampion, "selecting a bench champion does not enable Pick Card");
    Assert(swapViewModel.CanSwapBenchChampion, "selecting a bench champion enables Swap Bench");
}

static void AssertCustomRandomSrUsesRandomCardUi()
{
    var lcu = new AramLcuClient(localChampionId: 0, queueId: 0, gameMode: "CLASSIC");
    var viewModel = new MainViewModel(lcu, new LocalDiagnosticLog());
    Thread.Sleep(100);

    Assert(!viewModel.IsAramChampionSelect, "custom random SR is not labeled ARAM");
    Assert(viewModel.IsRandomCardChampionSelect, "custom random SR uses random-card champion select");
    Assert(viewModel.RandomCardChampionSelectLabel == "Random Pick", "custom random SR gets generic random pick label");
    Assert(viewModel.Champions.Count == 0, "custom random SR does not use the 5v5 champion grid");
    Assert(viewModel.AramAvailableChampions.Select(tile => tile.Champion.ChampionId).SequenceEqual([103, 222, 145]), "custom random SR shows available card champions");
    Assert(viewModel.AramBenchChampions.Select(tile => tile.Champion.ChampionId).SequenceEqual([222, 145]), "custom random SR shows bench champions");

    viewModel.PickAramChampionTileCommand.Execute(viewModel.AramAvailableChampions[0]);
    Assert(lcu.PickedChampionId == 103, "single-clicking a custom random card picks it");

    viewModel.SwapBenchChampionTileCommand.Execute(viewModel.AramBenchChampions[0]);
    Assert(lcu.SwappedChampionId == 222, "single-clicking a custom random bench champion swaps it");
}

static void AssertFiveVFiveSingleClickDeclaresChampionOnly()
{
    var lcu = new FiveVFiveLcuClient();
    var viewModel = new MainViewModel(lcu, new LocalDiagnosticLog());
    Thread.Sleep(100);

    var ahri = viewModel.Champions.Single(tile => tile.Champion.ChampionId == 103);
    viewModel.SelectedChampionTile = ahri;
    Thread.Sleep(100);

    Assert(lcu.DeclaredChampionId == 103, "single-clicking a 5v5 champion declares it");
    Assert(lcu.PickedChampionId is null, "single-clicking a 5v5 champion does not lock pick");
    Assert(lcu.BannedChampionId is null, "single-clicking a 5v5 champion does not ban");
    Assert(viewModel.LastCommandResult == "Champion declared", "single-click declare reports success");

    viewModel.PickCommand.Execute(null);
    Thread.Sleep(100);
    Assert(lcu.PickedChampionId == 103, "Pick button still submits pick");

    viewModel.BanCommand.Execute(null);
    Thread.Sleep(100);
    Assert(lcu.BannedChampionId == 103, "Ban button still submits ban");
}

static void AssertFiveVFiveDoubleClickPicksChampion()
{
    var lcu = new FiveVFiveLcuClient();
    var viewModel = new MainViewModel(lcu, new LocalDiagnosticLog(), TempPreferences());
    Thread.Sleep(100);

    var ahri = viewModel.Champions.Single(tile => tile.Champion.ChampionId == 103);
    viewModel.PickChampionTileCommand.Execute(ahri);
    Thread.Sleep(100);

    Assert(lcu.PickedChampionId == 103, "double-clicking a 5v5 champion submits pick");
}

static void AssertFiveVFiveBanPhaseCanDeclareChampion()
{
    var lcu = new FiveVFiveLcuClient(localPickInProgress: false, localBanInProgress: true, pickable: [103]);
    var viewModel = new MainViewModel(lcu, new LocalDiagnosticLog(), TempPreferences());
    Thread.Sleep(100);

    viewModel.SelectedChampionTile = viewModel.Champions.Single(tile => tile.Champion.ChampionId == 103);
    Thread.Sleep(100);

    Assert(viewModel.ChampionSelectTimeline == "Banning", "test snapshot is in ban phase");
    Assert(lcu.DeclaredChampionId == 103, "single-clicking during ban phase declares hovered champion");
}


static void AssertBanWarningUsesAllyHovers()
{
    var viewModel = new MainViewModel(new FiveVFiveLcuClient(allyChampionId: 103, pickable: []), new LocalDiagnosticLog(), TempPreferences());
    Thread.Sleep(100);

    viewModel.SelectedChampionTile = viewModel.Champions.Single(tile => tile.Champion.ChampionId == 103);
    Assert(viewModel.HasBanWarning, "shows ban warning for ally-hovered champion");

    viewModel.SelectedChampionTile = viewModel.Champions.First(tile => tile.Champion.ChampionId != 103);
    Assert(!viewModel.HasBanWarning, "does not warn when selected champion is not ally-hovered");
}

static void AssertChampionSelectTimelineLabels()
{
    Assert(TimelineFor(new FiveVFiveLcuClient(localBanInProgress: true)) == "Banning", "timeline labels ban phase");
    Assert(TimelineFor(new FiveVFiveLcuClient(localPickInProgress: true, localBanInProgress: false, localChampionId: 0)) == "Declaring", "timeline labels declare phase");
    Assert(TimelineFor(new FiveVFiveLcuClient(localPickInProgress: true, localBanInProgress: false, localChampionId: 103)) == "Picking", "timeline labels pick phase");
    Assert(TimelineFor(new FiveVFiveLcuClient(completed: true)) == "Completed", "timeline labels completed phase");
    Assert(TimelineFor(new FiveVFiveLcuClient(localPickInProgress: false, localBanInProgress: false)) == "Waiting", "timeline labels waiting phase");
}

static string TimelineFor(ILcuClient lcu)
{
    var viewModel = new MainViewModel(lcu, new LocalDiagnosticLog(), TempPreferences());
    Thread.Sleep(100);
    return viewModel.ChampionSelectTimeline;
}

static void AssertFavoritesSortFirst()
{
    var viewModel = new MainViewModel(new FiveVFiveLcuClient(pickable: []), new LocalDiagnosticLog(), TempPreferences());
    Thread.Sleep(100);

    var originalCount = viewModel.Champions.Count;
    var favorite = viewModel.Champions[1];
    viewModel.SelectedChampionTile = favorite;
    viewModel.ToggleFavoriteCommand.Execute(null);

    Assert(viewModel.Champions[0] == favorite, "favorites sort before non-favorites");
    Assert(viewModel.Champions.Count == originalCount, "favorites do not hide non-favorites");
}

static void AssertQuickBansRespectAvailability()
{
    var viewModel = new MainViewModel(new FiveVFiveLcuClient(localBanInProgress: true, pickable: [103, 222], disabled: [222]), new LocalDiagnosticLog(), TempPreferences());
    Thread.Sleep(100);

    viewModel.SelectedChampionTile = viewModel.Champions.Single(tile => tile.Champion.ChampionId == 103);
    viewModel.ToggleQuickBanCommand.Execute(null);
    viewModel.SelectedChampionTile = viewModel.Champions.SingleOrDefault(tile => tile.Champion.ChampionId == 222);
    if (viewModel.SelectedChampionTile is not null)
    {
        viewModel.ToggleQuickBanCommand.Execute(null);
    }

    Assert(viewModel.QuickBanChampions.Select(tile => tile.Champion.ChampionId).SequenceEqual([103]), "quick bans only show available champions");
}

static void AssertTradeCommandsCallLcu()
{
    var lcu = new FiveVFiveLcuClient(trades: [new ChampionTradeRequest(7, "AVAILABLE", 2, 103)]);
    var viewModel = new MainViewModel(lcu, new LocalDiagnosticLog(), TempPreferences());
    Thread.Sleep(100);

    viewModel.AcceptTradeCommand.Execute(viewModel.TradeRequests[0]);
    viewModel.DeclineTradeCommand.Execute(viewModel.TradeRequests[0]);
    Thread.Sleep(100);

    Assert(lcu.AcceptedTradeId == 7, "accept trade calls LCU with trade id");
    Assert(lcu.DeclinedTradeId == 7, "decline trade calls LCU with trade id");
}

static void AssertCurrentSummonerProfileOpensAndBackReturnsHome()
{
    var viewModel = new MainViewModel(new ProfileLcuClient(hasLobby: false), new LocalDiagnosticLog(), TempPreferences());
    Thread.Sleep(100);

    viewModel.OpenCurrentSummonerProfileCommand.Execute(null);
    Thread.Sleep(100);

    Assert(viewModel.IsProfileScreen, "current summoner click opens profile screen");
    Assert(viewModel.SelectedProfile?.Name == "Local#NA1", "current summoner profile uses current summoner data");

    viewModel.BackCommand.Execute(null);
    Assert(viewModel.IsHomeScreen, "profile back returns home");
}

static void AssertLobbyRendersFromSnapshot()
{
    var viewModel = new MainViewModel(new ProfileLcuClient(), new LocalDiagnosticLog(), TempPreferences());
    Thread.Sleep(100);

    Assert(viewModel.CurrentLobby == "Normal Draft 5v5 - 2 members", "ready screen renders lobby queue and member count");
    Assert(viewModel.LobbyMembers.Count == 2, "ready screen renders lobby members");
    Assert(viewModel.LobbyMembers[0].Name == "Local#NA1", "lobby member row shows Riot ID");
    Assert(viewModel.LobbyMembers[0].Badges.Contains("You"), "lobby member row marks local player");
    Assert(viewModel.LobbyMembers[0].Badges.Contains("Leader"), "lobby member row marks leader");
}

static void AssertLobbyPayloadLoadsEmbeddedMembers()
{
    const string json = """
        {
          "gameConfig": { "queueId": 400, "gameMode": "CLASSIC" },
          "members": [
            {
              "summonerId": 1,
              "summonerName": "Local",
              "gameName": "Local",
              "tagLine": "NA1",
              "isLeader": true,
              "teamPosition": "middle"
            }
          ]
        }
        """;

    var lobby = JsonSerializer.Deserialize<LobbyState>(json)!;
    Assert(lobby.Members?.Single().Name == "Local#NA1", "lobby payload loads embedded members");
}

static void AssertSwiftplayLobbyPayloadLoadsSlotsAndRestrictions()
{
    const string json = """
        {
          "canStartActivity": true,
          "gameConfig": { "queueId": 480, "gameMode": "SWIFTPLAY", "showQuickPlaySlotSelection": true },
          "members": [
            {
              "summonerId": 1,
              "summonerName": "",
              "summonerIconId": 7147,
              "isLeader": true,
              "ready": true,
              "playerSlots": [
                { "championId": 64, "positionPreference": "JUNGLE" },
                { "championId": 86, "positionPreference": "TOP" }
              ]
            }
          ],
          "restrictions": [
            { "restrictionCode": "QPPlayerScarcePositionCoverageRestriction" }
          ]
        }
        """;

    var lobby = JsonSerializer.Deserialize<LobbyState>(json)!;
    Assert(lobby.IsSwiftplay, "Swiftplay lobby payload is detected");
    Assert(lobby.BlocksSwiftplayMatchmaking, "Swiftplay restrictions block matchmaking");
    Assert(lobby.Members?.Single().PlayerSlots?.Select(slot => slot.PositionPreference).SequenceEqual(["JUNGLE", "TOP"]) == true,
        "Swiftplay lobby payload loads player slots");
    var enriched = lobby.Members!.Single().WithProfile(new SummonerProfile(1, "Local", "Local", "NA1", 616, 7147, "local-puuid"));
    Assert(enriched.Name == "Local#NA1", "blank lobby member can be enriched from profile data");
}

static void AssertNoLobbyDoesNotBreakReadyScreen()
{
    var viewModel = new MainViewModel(new SlowLcuClient(), new LocalDiagnosticLog(), TempPreferences());
    Thread.Sleep(100);

    viewModel.SelectModeGroupCommand.Execute("ARAM");
    Assert(viewModel.IsReadyScreen, "ready screen opens without lobby");
    Assert(viewModel.CurrentLobby == "No lobby", "missing lobby shows no lobby");
    Assert(viewModel.LobbyMembers.Count == 0, "missing lobby has no members");
}

static void AssertLobbyMemberProfileUsesSharedProfile()
{
    var viewModel = new MainViewModel(new ProfileLcuClient(), new LocalDiagnosticLog(), TempPreferences());
    Thread.Sleep(100);

    viewModel.CheckProfileCommand.Execute(viewModel.LobbyMembers[1]);
    Thread.Sleep(100);

    Assert(viewModel.IsProfileScreen, "lobby member check profile opens profile screen");
    Assert(viewModel.SelectedProfile?.Name == "Friend#EUW", "lobby member profile loads selected member");
    Assert(viewModel.SelectedProfile?.Ranked.Contains("GOLD") == true, "lobby member profile includes ranked summary");
}

static void AssertRankedFailureStillOpensProfile()
{
    var viewModel = new MainViewModel(new ProfileLcuClient(rankedFails: true), new LocalDiagnosticLog(), TempPreferences());
    Thread.Sleep(100);

    viewModel.CheckProfileCommand.Execute(viewModel.LobbyMembers[1]);
    Thread.Sleep(100);

    Assert(viewModel.IsProfileScreen, "ranked failure still opens profile screen");
    Assert(viewModel.SelectedProfile?.Name == "Friend#EUW", "ranked failure keeps basic profile");
    Assert(viewModel.SelectedProfile?.Ranked == "Ranked unavailable", "ranked failure uses fallback text");
}

static void AssertAddFriendUsesRiotId()
{
    var lcu = new ProfileLcuClient();
    var viewModel = new MainViewModel(lcu, new LocalDiagnosticLog(), TempPreferences());
    Thread.Sleep(100);

    viewModel.AddFriendCommand.Execute(viewModel.LobbyMembers[1]);
    Thread.Sleep(100);

    Assert(lcu.FriendRequest == "Friend#EUW", "add friend sends Riot ID to LCU");
}

static void AssertAddFriendDisabledWithoutRiotIdOrForLocalPlayer()
{
    var viewModel = new MainViewModel(new ProfileLcuClient(), new LocalDiagnosticLog(), TempPreferences());
    Thread.Sleep(100);

    Assert(!viewModel.LobbyMembers[0].CanAddFriend, "add friend is disabled for local player");
    Assert(!new LobbyMemberViewModel(new LobbyMember(9, "NoTag", "NoTag", null, null, null, null, null, false, false, null, null, null), false).CanAddFriend,
        "add friend is disabled without Riot ID");
}

static void AssertExistingLobbyOpensReadyScreen()
{
    var viewModel = new MainViewModel(new ProfileLcuClient(), new LocalDiagnosticLog(), TempPreferences());
    Thread.Sleep(100);

    Assert(viewModel.IsReadyScreen, "existing lobby opens ready screen");
}

static void AssertSwiftplayRestrictionBlocksMatchmaking()
{
    var lcu = new SwiftplayLcuClient();
    var viewModel = new MainViewModel(lcu, new LocalDiagnosticLog(), TempPreferences());
    Thread.Sleep(100);

    viewModel.StartMatchmakingCommand.Execute(null);
    Thread.Sleep(100);

    Assert(lcu.StartedMatchmaking == false, "Swiftplay restriction prevents matchmaking call");
    Assert(viewModel.LastCommandResult.Contains("Swiftplay needs valid Quickplay champion/position slots"), "Swiftplay restriction is readable");
}

static void AssertBlankLobbyMemberNameCanUseProfileData()
{
    var viewModel = new MainViewModel(new SwiftplayLcuClient(), new LocalDiagnosticLog(), TempPreferences());
    Thread.Sleep(100);

    Assert(viewModel.CurrentLobby == "Swiftplay - 1 member", "Swiftplay lobby uses catalog label");
    Assert(viewModel.LobbyMembers[0].Name == "Local#NA1", "blank lobby member name uses enriched profile data");
    Assert(viewModel.LobbyMembers[0].IconId == 7147, "lobby member keeps summoner icon id");
    Assert(viewModel.LobbyMembers[0].Position == "JUNGLE, TOP", "Swiftplay row shows slot positions");
}

static void AssertPollingPolicyUsesScreenAndPhase()
{
    Assert(MainViewModel.PollingDelayFor(AppPhase.WaitingForLeagueClient, "Home", false) == TimeSpan.FromSeconds(3), "waiting polls every 3s");
    Assert(MainViewModel.PollingDelayFor(AppPhase.Error, "Home", false) == TimeSpan.FromSeconds(3), "error polls every 3s");
    Assert(MainViewModel.PollingDelayFor(AppPhase.Connected, "Home", false) == TimeSpan.FromSeconds(8), "connected home without lobby polls every 8s");
    Assert(MainViewModel.PollingDelayFor(AppPhase.Connected, "Ready", true) == TimeSpan.FromSeconds(2), "ready lobby polls every 2s");
    Assert(MainViewModel.PollingDelayFor(AppPhase.ReadyCheck, "Ready", true) == TimeSpan.FromSeconds(1), "ready check polls every 1s");
    Assert(MainViewModel.PollingDelayFor(AppPhase.ChampionSelect, "ChampionSelect", false) == TimeSpan.FromSeconds(1), "champion select polls every 1s");
    Assert(MainViewModel.PollingDelayFor(AppPhase.ReadyCheck, "Profile", true) == TimeSpan.FromSeconds(1), "profile over ready check keeps urgent polling");
    Assert(MainViewModel.PollingDelayFor(AppPhase.ChampionSelect, "Profile", true) == TimeSpan.FromSeconds(1), "profile over champion select keeps urgent polling");
    Assert(MainViewModel.PollingDelayFor(AppPhase.Connected, "Profile", true) == TimeSpan.FromSeconds(8), "profile over non-urgent phase polls every 8s");
}

static void AssertLobbyRowsAreReused()
{
    var viewModel = new MainViewModel(new SlowLcuClient(), new LocalDiagnosticLog(), TempPreferences());
    ApplySnapshot(viewModel, LobbySnapshot(ready: false, leader: true, iconId: 12, position: "top"));
    var row = viewModel.LobbyMembers[0];
    row.Icon = TinyBitmap();

    ApplySnapshot(viewModel, LobbySnapshot(ready: false, leader: true, iconId: 12, position: "top"));

    Assert(ReferenceEquals(row, viewModel.LobbyMembers[0]), "same lobby member reuses row view model");
    Assert(viewModel.LobbyMembers[0].Icon is not null, "reused lobby row keeps loaded icon");
}

static void AssertLobbyRowsUpdateInPlace()
{
    var viewModel = new MainViewModel(new SlowLcuClient(), new LocalDiagnosticLog(), TempPreferences());
    ApplySnapshot(viewModel, LobbySnapshot(ready: false, leader: false, iconId: 12, position: "top"));
    var row = viewModel.LobbyMembers[0];

    ApplySnapshot(viewModel, LobbySnapshot(ready: true, leader: true, iconId: 12, position: "middle"));

    Assert(ReferenceEquals(row, viewModel.LobbyMembers[0]), "changed lobby member data updates existing row");
    Assert(viewModel.LobbyMembers[0].Badges.Contains("Leader"), "updated row shows leader badge");
    Assert(viewModel.LobbyMembers[0].Badges.Contains("Ready"), "updated row shows ready badge");
    Assert(viewModel.LobbyMembers[0].Position == "middle", "updated row shows new position");
}

static void AssertSummonerIconLoadsAreDeduped()
{
    var requests = 0;
    var gate = new TaskCompletionSource();
    var cache = new SummonerIconCache(new LocalDiagnosticLog(), async (_, _) =>
    {
        requests++;
        await gate.Task;
        return null;
    });

    var first = cache.LoadAsync(7, CancellationToken.None);
    var second = cache.LoadAsync(7, CancellationToken.None);
    Assert(requests == 1, "same summoner icon only starts one load");
    gate.SetResult();
    Task.WaitAll(first, second);
}

static void AssertLcuModeCapabilitiesDriveModeList()
{
    var viewModel = new MainViewModel(new ModeAwareLcuClient(), new LocalDiagnosticLog(), TempPreferences());
    Thread.Sleep(100);

    Assert(viewModel.ModeGroups.Contains("Summoner's Rift"), "LCU modes include Summoner's Rift group");
    Assert(viewModel.ModeGroups.Contains("ARAM"), "LCU modes include ARAM group");
    Assert(viewModel.ModeGroups.Contains("TFT"), "LCU modes include TFT group");
    Assert(viewModel.ModeGroups.Contains("Custom"), "LCU modes include Custom group");
    viewModel.SelectModeGroupCommand.Execute("TFT");
    Assert(viewModel.GameModes.Any(mode => mode.IsTft), "TFT group shows TFT queues");
}

static void AssertLanePreferencesCanBeSaved()
{
    var lcu = new ModeAwareLcuClient(ModeAwareLcuClient.LaneLobby());
    var viewModel = new MainViewModel(lcu, new LocalDiagnosticLog(), TempPreferences());
    Thread.Sleep(100);

    Assert(viewModel.IsPositionSelectorVisible, "lane lobby shows position selector");
    Assert(viewModel.SelectedPrimaryPosition == "TOP", "primary lane hydrates from lobby");
    Assert(viewModel.SelectedSecondaryPosition == "JUNGLE", "secondary lane hydrates from lobby");

    viewModel.SelectedPrimaryPosition = "MIDDLE";
    viewModel.SelectedSecondaryPosition = "UTILITY";
    viewModel.SavePositionsCommand.Execute(null);
    Thread.Sleep(100);

    Assert(lcu.SavedPositions == "MIDDLE/UTILITY", "save lane calls LCU position endpoint");
}

static void AssertQuickplaySetupIsDetectOnly()
{
    var viewModel = new MainViewModel(new ModeAwareLcuClient(ModeAwareLcuClient.QuickplayLobby()), new LocalDiagnosticLog(), TempPreferences());
    Thread.Sleep(100);

    Assert(viewModel.IsQuickplaySetupVisible, "quickplay lobby shows setup status");
    Assert(viewModel.LobbySetupStatus.Contains("Complete champion and lane setup in League Client"), "quickplay setup is detect-only");
    Assert(viewModel.QuickplaySlots.Count == 2, "quickplay slots are shown");
    Assert(viewModel.QuickplaySlots[0].Champion == "Lee Sin", "quickplay slot shows champion name");
    Assert(viewModel.QuickplaySlots[0].Position == "JUNGLE", "quickplay slot shows lane");
}

static void AssertQuickplayEditorSurvivesUnchangedPoll()
{
    var viewModel = new MainViewModel(new ModeAwareLcuClient(ModeAwareLcuClient.QuickplayLobby()), new LocalDiagnosticLog(), TempPreferences());
    Thread.Sleep(100);

    var slot = viewModel.QuickplaySlots[1];
    slot.SelectedChampionTile = viewModel.QuickplayChampionOptions.Single(champion => champion.Champion.ChampionId == 103);
    slot.SelectedPosition = "MIDDLE";
    ApplySnapshot(viewModel, ModeAwareLcuClient.QuickplayLobby());

    Assert(ReferenceEquals(slot, viewModel.QuickplaySlots[1]), "unchanged quickplay poll reuses editor row");
    Assert(viewModel.QuickplaySlots[1].SelectedChampionTile?.Champion.ChampionId == 103, "unchanged quickplay poll keeps pending champion selection");
    Assert(viewModel.QuickplaySlots[1].SelectedPosition == "MIDDLE", "unchanged quickplay poll keeps pending lane selection");
}

static void AssertQuickplayEditorFiltersByActiveLane()
{
    var lcu = new ModeAwareLcuClient(ModeAwareLcuClient.QuickplayLobby());
    var viewModel = new MainViewModel(lcu, new LocalDiagnosticLog(), TempPreferences());
    Thread.Sleep(100);

    var slot = viewModel.QuickplaySlots[1];
    viewModel.OpenQuickplaySlotCommand.Execute(slot);
    viewModel.SelectQuickplayPositionCommand.Execute(viewModel.QuickplayPositionOptions.Single(position => position.Name == "MIDDLE"));
    Thread.Sleep(100);

    Assert(viewModel.IsQuickplayEditorOpen, "quickplay slot opens editor");
    Assert(viewModel.ActiveQuickplaySlot == slot && slot.IsActive, "quickplay editor focuses clicked slot");
    Assert(viewModel.QuickplayPositionOptions.Single(position => position.Name == "MIDDLE").IsSelected, "quickplay lane filter marks active lane");
    Assert(viewModel.QuickplayChampions.Any(champion => champion.Champion.ChampionId == 103), "middle quickplay filter includes Ahri");
    Assert(viewModel.QuickplayChampions.All(champion => champion.Champion.ChampionId != 86), "middle quickplay filter excludes Garen");
    Assert(lcu.SavedQuickplaySlots?[1].PositionPreference == "MIDDLE", "quickplay lane filter auto-saves active slot lane");

    var ahri = viewModel.QuickplayChampions.Single(champion => champion.Champion.ChampionId == 103);
    viewModel.SelectQuickplayChampionCommand.Execute(ahri);
    Thread.Sleep(100);
    Assert(slot.SelectedChampionTile == ahri, "quickplay champion picker updates active slot");
    Assert(lcu.SavedQuickplaySlots?[1].ChampionId == 103, "quickplay champion picker auto-saves active slot champion");
}

static void AssertQuickplaySlotsCanBeSavedWithRecommendedRunes()
{
    var lcu = new ModeAwareLcuClient(ModeAwareLcuClient.QuickplayLobby());
    var viewModel = new MainViewModel(lcu, new LocalDiagnosticLog(), TempPreferences());
    Thread.Sleep(100);

    viewModel.QuickplaySlots[0].SelectedChampionTile = viewModel.QuickplayChampionOptions.Single(champion => champion.Champion.ChampionId == 64);
    viewModel.QuickplaySlots[0].SelectedPosition = "JUNGLE";
    viewModel.QuickplaySlots[1].SelectedChampionTile = viewModel.QuickplayChampionOptions.Single(champion => champion.Champion.ChampionId == 103);
    viewModel.QuickplaySlots[1].SelectedPosition = "MIDDLE";
    viewModel.SaveQuickplaySlotsCommand.Execute(null);
    Thread.Sleep(100);

    Assert(lcu.SavedQuickplaySlots?.Count == 2, "quickplay save puts both slots");
    Assert(lcu.RequestedPerks == "103/MIDDLE", "changed quickplay slot fetches recommended runes");
    Assert(lcu.SavedQuickplaySlots![0].SkinId == 64004, "unchanged quickplay slot preserves skin");
    Assert(lcu.SavedQuickplaySlots[0].Spell1 == 11UL && lcu.SavedQuickplaySlots[0].Spell2 == 4UL, "unchanged quickplay slot preserves spells");
    Assert(lcu.SavedQuickplaySlots[1].ChampionId == 103, "changed quickplay slot saves new champion");
    Assert(lcu.SavedQuickplaySlots[1].PositionPreference == "MIDDLE", "changed quickplay slot saves new lane");
    Assert(lcu.SavedQuickplaySlots[1].Perks == "{\"perkIds\":[8112],\"perkStyle\":8100,\"perkSubStyle\":8000}", "changed quickplay slot uses recommended runes");
    Assert(lcu.SavedQuickplaySlots[1].SkinId == 103000, "changed quickplay slot uses base skin");
    Assert(lcu.SavedQuickplaySlots[1].Spell1 == 14UL && lcu.SavedQuickplaySlots[1].Spell2 == 4UL, "non-jungle quickplay slot defaults to ignite flash");
}

static void AssertCustomLobbyUsesModeCommand()
{
    var lcu = new ModeAwareLcuClient();
    var viewModel = new MainViewModel(lcu, new LocalDiagnosticLog(), TempPreferences());
    Thread.Sleep(100);

    viewModel.SelectModeGroupCommand.Execute("Custom");
    viewModel.SelectedGameMode = viewModel.GameModes.Single(mode => mode.QueueId == 3110);
    viewModel.CreateLobbyCommand.Execute(null);
    Thread.Sleep(100);

    Assert(lcu.CreatedMode?.QueueId == 3110 && lcu.CreatedMode.IsCustom, "custom queue creates lobby with mode metadata");
}

static void AssertBotModesUseOwnGroup()
{
    var bot = new GameMode("Easy Bot", 880, Category: "VersusAi", GameModeCode: "SWIFTPLAY", Type: "RIOTSCRIPT_BOT");
    Assert(bot.DisplayGroup == "Co-op vs AI", "bot queues use their own group");
}

static void AssertLeaveLobbyCallsLcu()
{
    var lcu = new ModeAwareLcuClient(ModeAwareLcuClient.LaneLobby());
    var viewModel = new MainViewModel(lcu, new LocalDiagnosticLog(), TempPreferences());
    Thread.Sleep(100);

    Assert(viewModel.CanLeaveLobby, "lobby enables leave command");
    viewModel.LeaveLobbyCommand.Execute(null);
    Thread.Sleep(100);

    Assert(lcu.LeftLobby, "leave lobby command calls LCU");
}

static void AssertCreateLobbyRefreshesSnapshotImmediately()
{
    var lcu = new ModeAwareLcuClient();
    var viewModel = new MainViewModel(lcu, new LocalDiagnosticLog(), TempPreferences());
    Thread.Sleep(100);

    viewModel.SelectedGameMode = viewModel.GameModes.First(mode => mode.QueueId == 400);
    viewModel.CreateLobbyCommand.Execute(null);
    Thread.Sleep(100);

    Assert(lcu.SnapshotRequests >= 2, "create lobby refreshes snapshot immediately");
}

static LcuSnapshot LobbySnapshot(bool ready, bool leader, int iconId, string position)
{
    var summoner = new CurrentSummoner(1, "Local", "Local", "NA1", 30, iconId, "local-puuid");
    var lobby = new LobbyState(true, new LobbyGameConfig(400, "CLASSIC", false), null, null);
    LobbyMember[] members =
    [
        new(1, "Local", "Local", "NA1", "local-puuid", iconId, iconId, 30, leader, ready, position, null, null)
    ];

    return new LcuSnapshot(AppPhase.Connected, summoner, null, null, null, lobby, members, [], [], [], "connected");
}

static void ApplySnapshot(MainViewModel viewModel, LcuSnapshot snapshot) =>
    typeof(MainViewModel).GetMethod("Apply", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
        .Invoke(viewModel, [snapshot]);

static Bitmap TinyBitmap() =>
    (Bitmap)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Bitmap));

static ChampionPreferenceStore TempPreferences() =>
    new(Path.Combine(Path.GetTempPath(), $"pickwise-test-{Guid.NewGuid():N}.json"));

static void AssertModeScreenNavigationWorks()
{
    var viewModel = new MainViewModel(new SlowLcuClient(), new LocalDiagnosticLog());
    Assert(viewModel.IsHomeScreen, "starts on home screen");
    Assert(viewModel.SelectedGameMode?.Name == "Normal Draft 5v5", "Summoner's Rift defaults to 5v5 queue");
    Assert(viewModel.GameModes.Any(mode => mode.Name == "Swiftplay" && mode.QueueId == 480), "Summoner's Rift shows Swiftplay queue");

    viewModel.SelectModeGroupCommand.Execute("ARAM");
    Assert(viewModel.IsReadyScreen, "mode group selection opens ready screen");
    Assert(viewModel.GameModes.Count == 2, "ARAM group shows ARAM queues");
    Assert(viewModel.GameModes.All(mode => mode.QueueId is 450 or 2400), "ARAM group excludes Summoner's Rift queues");

    viewModel.BackCommand.Execute(null);
    Assert(viewModel.IsHomeScreen, "back returns home outside forced LCU phases");

    viewModel.Phase = AppPhase.ReadyCheck;
    Assert(viewModel.IsReadyScreen, "ready check phase opens ready screen");
    viewModel.BackCommand.Execute(null);
    Assert(viewModel.IsReadyScreen, "back stays on ready screen during ready check");

    viewModel.Phase = AppPhase.ChampionSelect;
    Assert(viewModel.IsChampionSelectScreen, "champion select phase opens champion select screen");
}

static void AssertGameModeIconCachePathWorks()
{
    Assert(GameModeIconCache.CacheDirectory.Contains("game-mode-icons"), "game mode icons use their own cache directory");
}

static void AssertLcuLockfileParserWorks()
{
    var connection = LcuConnection.TryParse("LeagueClient:13968:55888:secret:https");
    Assert(connection is not null, "parses valid LCU lockfile");
    Assert(connection!.BaseUri.ToString() == "https://127.0.0.1:55888/", "uses lockfile port");
    Assert(connection.Authorization.Scheme == "Basic", "uses basic auth");
    Assert(LcuConnection.TryParse("bad") is null, "rejects invalid lockfile");
}

sealed class ModeAwareLcuClient(LcuSnapshot? snapshot = null) : ILcuClient
{
    public GameMode? CreatedMode { get; private set; }
    public string? SavedPositions { get; private set; }
    public bool LeftLobby { get; private set; }
    public int SnapshotRequests { get; private set; }
    public string? RequestedPerks { get; private set; }
    public IReadOnlyList<LobbyPlayerSlot>? SavedQuickplaySlots { get; private set; }

    public Task<IReadOnlyList<GameMode>> GetQueuesAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<GameMode>>(
        [
            new("Normal Draft", 400, ShowPositionSelector: true, PickMode: "TeamBuilderDraftPickStrategy"),
            new("Swiftplay", 480, GameModeCode: "SWIFTPLAY", Type: "SWIFTPLAY", ShowQuickPlaySlotSelection: true),
            new("ARAM", 450, ModeGroup: "kARAM", GameModeCode: "ARAM", Type: "ARAM_UNRANKED_5x5", MapId: 12),
            new("TFT Normal", 1090, ModeGroup: "kTeamfightTactics", GameModeCode: "TFT", Type: "NORMAL_TFT", MapId: 22),
            new("Custom Draft", 3110, Category: "Custom", ModeGroup: "kAlternativeLeagueGameModes", IsCustom: true, ShowPositionSelector: true, GameTypeConfig: GameType()),
            new("Hidden", 9999)
        ]);

    public Task<LcuSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        SnapshotRequests++;
        return Task.FromResult(snapshot ?? new LcuSnapshot(AppPhase.Connected, new CurrentSummoner(1, "Local", "Local", "NA1", 30, 11, "local-puuid"), null, null, null, null, [], [], [], [], "connected"));
    }

    public Task CreateLobbyAsync(GameMode mode, CancellationToken cancellationToken)
    {
        CreatedMode = mode;
        return Task.CompletedTask;
    }

    public Task UpdatePositionPreferencesAsync(string first, string second, CancellationToken cancellationToken)
    {
        SavedPositions = $"{first}/{second}";
        return Task.CompletedTask;
    }

    public Task LeaveLobbyAsync(CancellationToken cancellationToken)
    {
        LeftLobby = true;
        return Task.CompletedTask;
    }

    public Task<string?> GetQuickplayPerksAsync(int championId, string position, CancellationToken cancellationToken)
    {
        RequestedPerks = $"{championId}/{position}";
        return Task.FromResult<string?>("{\"perkIds\":[8112],\"perkStyle\":8100,\"perkSubStyle\":8000}");
    }

    public Task UpdateQuickplaySlotsAsync(IReadOnlyList<LobbyPlayerSlot> slots, CancellationToken cancellationToken)
    {
        SavedQuickplaySlots = slots;
        return Task.CompletedTask;
    }

    public static LcuSnapshot LaneLobby()
    {
        var summoner = new CurrentSummoner(1, "Local", "Local", "NA1", 30, 11, "local-puuid");
        var local = new LobbyMember(1, "Local", "Local", "NA1", "local-puuid", 11, 11, 30, true, true, null, "TOP", [], "JUNGLE");
        var lobby = new LobbyState(true, new LobbyGameConfig(400, "CLASSIC", ShowPositionSelector: true), [local], null)
        {
            LocalMember = local
        };
        return new LcuSnapshot(AppPhase.Connected, summoner, null, null, null, lobby, [local], [], [], [], "connected");
    }

    public static LcuSnapshot QuickplayLobby()
    {
        var summoner = new CurrentSummoner(1, "Local", "Local", "NA1", 30, 11, "local-puuid");
        var local = new LobbyMember(
            1,
            "Local",
            "Local",
            "NA1",
            "local-puuid",
            11,
            11,
            30,
            true,
            true,
            null,
            "",
            [new LobbyPlayerSlot(64, "JUNGLE", 64004, "{\"old\":true}", 11, 4), new LobbyPlayerSlot(86, "TOP", 86000, "{\"old\":true}", 14, 4)]);
        var lobby = new LobbyState(false, new LobbyGameConfig(480, "SWIFTPLAY", ShowQuickPlaySlotSelection: true), [local], [new LobbyRestriction("QPPlayerScarcePositionCoverageRestriction")])
        {
            LocalMember = local,
            ScarcePositions = ["UTILITY"]
        };
        return new LcuSnapshot(AppPhase.Connected, summoner, null, null, null, lobby, [local], [], [], [], "connected");
    }

    private static LcuGameTypeConfig GameType() =>
        new(18, "", 0, false, false, false, false, false, false, false, false, false, false, false, false, 0, 0, 0, "TeamBuilderDraftPickStrategy", "", null, null);

    public Task<SummonerProfile?> GetSummonerProfileAsync(long summonerId, CancellationToken cancellationToken) => Task.FromResult<SummonerProfile?>(null);
    public Task<RankedSummary?> GetRankedSummaryAsync(long summonerId, CancellationToken cancellationToken) => Task.FromResult<RankedSummary?>(null);
    public Task SendFriendRequestAsync(LobbyMember member, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task AcceptReadyCheckAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeclineReadyCheckAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task CreateLobbyAsync(int queueId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StartMatchmakingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task CancelMatchmakingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeclareChampionAsync(int championId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task PickChampionAsync(int championId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task BanChampionAsync(int championId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task SwapBenchChampionAsync(int championId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task AcceptTradeAsync(int tradeId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeclineTradeAsync(int tradeId, CancellationToken cancellationToken) => Task.CompletedTask;
}

sealed class ProfileLcuClient(bool rankedFails = false, bool hasLobby = true) : ILcuClient
{
    public string? FriendRequest { get; private set; }

    public Task<LcuSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        var summoner = new CurrentSummoner(1, "Local", "Local", "NA1", 30, 11, "local-puuid");
        var lobby = hasLobby ? new LobbyState(true, new LobbyGameConfig(400, "CLASSIC", false), null, null) : null;
        LobbyMember[] members =
        [
            new(1, "Local", "Local", "NA1", "local-puuid", 11, 11, 30, true, true, "middle", null, null),
            new(2, "Friend", "Friend", "EUW", "friend-puuid", 12, 12, 55, false, true, "top", null, null)
        ];

        return Task.FromResult(new LcuSnapshot(AppPhase.Connected, summoner, null, null, null, lobby, hasLobby ? members : [], [], [], [], "connected"));
    }

    public Task<SummonerProfile?> GetSummonerProfileAsync(long summonerId, CancellationToken cancellationToken) =>
        Task.FromResult<SummonerProfile?>(summonerId == 2
            ? new SummonerProfile(2, "Friend", "Friend", "EUW", 55, 12, "friend-puuid")
            : null);

    public Task<RankedSummary?> GetRankedSummaryAsync(long summonerId, CancellationToken cancellationToken)
    {
        if (rankedFails)
        {
            throw new InvalidOperationException("ranked down");
        }

        return Task.FromResult<RankedSummary?>(new RankedSummary("RANKED_SOLO_5x5: GOLD II - 44 LP"));
    }

    public Task SendFriendRequestAsync(LobbyMember member, CancellationToken cancellationToken)
    {
        FriendRequest = $"{member.GameName}#{member.TagLine}";
        return Task.CompletedTask;
    }

    public Task AcceptReadyCheckAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeclineReadyCheckAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task CreateLobbyAsync(int queueId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StartMatchmakingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task CancelMatchmakingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeclareChampionAsync(int championId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task PickChampionAsync(int championId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task BanChampionAsync(int championId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task SwapBenchChampionAsync(int championId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task AcceptTradeAsync(int tradeId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeclineTradeAsync(int tradeId, CancellationToken cancellationToken) => Task.CompletedTask;
}

sealed class SwiftplayLcuClient : ILcuClient
{
    public bool StartedMatchmaking { get; private set; }

    public Task<LcuSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        var summoner = new CurrentSummoner(1, "Local", "Local", "NA1", 616, 7147, "local-puuid");
        var lobby = new LobbyState(
            true,
            new LobbyGameConfig(480, "SWIFTPLAY", ShowQuickPlaySlotSelection: true),
            null,
            [new LobbyRestriction("QPPlayerScarcePositionCoverageRestriction")]);
        LobbyMember[] members =
        [
            new(
                1,
                "",
                "Local",
                "NA1",
                "local-puuid",
                7147,
                null,
                616,
                true,
                true,
                null,
                "",
                [new LobbyPlayerSlot(64, "JUNGLE"), new LobbyPlayerSlot(86, "TOP")])
        ];

        return Task.FromResult(new LcuSnapshot(AppPhase.Connected, summoner, null, null, null, lobby, members, [], [], [], "connected"));
    }

    public Task<SummonerProfile?> GetSummonerProfileAsync(long summonerId, CancellationToken cancellationToken) =>
        Task.FromResult<SummonerProfile?>(new SummonerProfile(summonerId, "Local", "Local", "NA1", 616, 7147, "local-puuid"));
    public Task<RankedSummary?> GetRankedSummaryAsync(long summonerId, CancellationToken cancellationToken) => Task.FromResult<RankedSummary?>(null);
    public Task SendFriendRequestAsync(LobbyMember member, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task AcceptReadyCheckAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeclineReadyCheckAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task CreateLobbyAsync(int queueId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StartMatchmakingAsync(CancellationToken cancellationToken)
    {
        StartedMatchmaking = true;
        return Task.CompletedTask;
    }
    public Task CancelMatchmakingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeclareChampionAsync(int championId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task PickChampionAsync(int championId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task BanChampionAsync(int championId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task SwapBenchChampionAsync(int championId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task AcceptTradeAsync(int tradeId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeclineTradeAsync(int tradeId, CancellationToken cancellationToken) => Task.CompletedTask;
}

sealed class SlowLcuClient : ILcuClient
{
    public Task<LcuSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        Thread.Sleep(1_000);
        return Task.FromResult(new LcuSnapshot(AppPhase.WaitingForLeagueClient, null, null, null, null, null, [], [], [], [], "slow"));
    }

    public Task<SummonerProfile?> GetSummonerProfileAsync(long summonerId, CancellationToken cancellationToken) => Task.FromResult<SummonerProfile?>(null);
    public Task<RankedSummary?> GetRankedSummaryAsync(long summonerId, CancellationToken cancellationToken) => Task.FromResult<RankedSummary?>(null);
    public Task SendFriendRequestAsync(LobbyMember member, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task AcceptReadyCheckAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeclineReadyCheckAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task CreateLobbyAsync(int queueId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StartMatchmakingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task CancelMatchmakingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeclareChampionAsync(int championId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task PickChampionAsync(int championId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task BanChampionAsync(int championId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task SwapBenchChampionAsync(int championId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task AcceptTradeAsync(int tradeId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeclineTradeAsync(int tradeId, CancellationToken cancellationToken) => Task.CompletedTask;
}

sealed class FiveVFiveLcuClient(
    int localChampionId = 0,
    int allyChampionId = 0,
    bool localPickInProgress = true,
    bool localBanInProgress = false,
    bool completed = false,
    IReadOnlyList<int>? pickable = null,
    IReadOnlyList<int>? disabled = null,
    IReadOnlyList<ChampionTradeRequest>? trades = null) : ILcuClient
{
    public int? DeclaredChampionId { get; private set; }
    public int? PickedChampionId { get; private set; }
    public int? BannedChampionId { get; private set; }
    public int? AcceptedTradeId { get; private set; }
    public int? DeclinedTradeId { get; private set; }

    public Task<LcuSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        var session = new ChampionSelectSession(
            LocalPlayerCellId: 1,
            Actions:
            [
                [new ChampionSelectAction(1, 1, "pick", localChampionId, completed, localPickInProgress)],
                [new ChampionSelectAction(2, 1, "ban", 0, completed, localBanInProgress)]
            ],
            MyTeam: [new ChampionSelectPlayer(1, localChampionId, 1, "middle"), new ChampionSelectPlayer(2, allyChampionId, 2, "top")],
            TheirTeam: [],
            Bans: new ChampionSelectBans([], []),
            BenchChampionIds: null,
            BenchEnabled: false,
            AllowRerolling: false,
            AllowSubsetChampionPicks: false);
        var gameflow = new GameflowSession(new GameflowGameData(new GameflowQueue(400, 400, "CLASSIC")));
        return Task.FromResult(new LcuSnapshot(AppPhase.ChampionSelect, null, null, session, gameflow, null, [], pickable ?? [103], disabled ?? [], trades ?? [], "5v5"));
    }

    public Task<SummonerProfile?> GetSummonerProfileAsync(long summonerId, CancellationToken cancellationToken) => Task.FromResult<SummonerProfile?>(null);
    public Task<RankedSummary?> GetRankedSummaryAsync(long summonerId, CancellationToken cancellationToken) => Task.FromResult<RankedSummary?>(null);
    public Task SendFriendRequestAsync(LobbyMember member, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task AcceptReadyCheckAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeclineReadyCheckAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task CreateLobbyAsync(int queueId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StartMatchmakingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task CancelMatchmakingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeclareChampionAsync(int championId, CancellationToken cancellationToken)
    {
        DeclaredChampionId = championId;
        return Task.CompletedTask;
    }
    public Task PickChampionAsync(int championId, CancellationToken cancellationToken)
    {
        PickedChampionId = championId;
        return Task.CompletedTask;
    }
    public Task BanChampionAsync(int championId, CancellationToken cancellationToken)
    {
        BannedChampionId = championId;
        return Task.CompletedTask;
    }
    public Task SwapBenchChampionAsync(int championId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task AcceptTradeAsync(int tradeId, CancellationToken cancellationToken)
    {
        AcceptedTradeId = tradeId;
        return Task.CompletedTask;
    }
    public Task DeclineTradeAsync(int tradeId, CancellationToken cancellationToken)
    {
        DeclinedTradeId = tradeId;
        return Task.CompletedTask;
    }
}

sealed class AramLcuClient(int localChampionId, int queueId = 450, string gameMode = "ARAM") : ILcuClient
{
    public int? PickedChampionId { get; private set; }
    public int? SwappedChampionId { get; private set; }

    public Task<LcuSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        var session = new ChampionSelectSession(
            LocalPlayerCellId: 1,
            Actions:
            [
                [new ChampionSelectAction(1, 1, "pick", 0, false, true)]
            ],
            MyTeam: [new ChampionSelectPlayer(1, localChampionId, 1, "")],
            TheirTeam: [],
            Bans: new ChampionSelectBans([], []),
            BenchChampionIds: null,
            BenchEnabled: true,
            AllowRerolling: false,
            AllowSubsetChampionPicks: true)
        {
            BenchChampions = [new ChampionSelectBenchChampion(222), new ChampionSelectBenchChampion(145)]
        };
        var gameflow = new GameflowSession(new GameflowGameData(new GameflowQueue(queueId, queueId, gameMode)));
        return Task.FromResult(new LcuSnapshot(AppPhase.ChampionSelect, null, null, session, gameflow, null, [], [103, 222, 145], [], [], "aram"));
    }

    public Task<SummonerProfile?> GetSummonerProfileAsync(long summonerId, CancellationToken cancellationToken) => Task.FromResult<SummonerProfile?>(null);
    public Task<RankedSummary?> GetRankedSummaryAsync(long summonerId, CancellationToken cancellationToken) => Task.FromResult<RankedSummary?>(null);
    public Task SendFriendRequestAsync(LobbyMember member, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task AcceptReadyCheckAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeclineReadyCheckAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task CreateLobbyAsync(int queueId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StartMatchmakingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task CancelMatchmakingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeclareChampionAsync(int championId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task PickChampionAsync(int championId, CancellationToken cancellationToken)
    {
        PickedChampionId = championId;
        return Task.CompletedTask;
    }
    public Task BanChampionAsync(int championId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task SwapBenchChampionAsync(int championId, CancellationToken cancellationToken)
    {
        SwappedChampionId = championId;
        return Task.CompletedTask;
    }
    public Task AcceptTradeAsync(int tradeId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeclineTradeAsync(int tradeId, CancellationToken cancellationToken) => Task.CompletedTask;
}
