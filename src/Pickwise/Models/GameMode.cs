namespace Pickwise.Models;

public sealed record GameMode(
    string Name,
    int QueueId,
    string Category = "PvP",
    string ModeGroup = "kSummonersRift",
    string GameModeCode = "CLASSIC",
    string Type = "NORMAL",
    int MapId = 11,
    string PickMode = "",
    bool IsCustom = false,
    bool ShowPositionSelector = false,
    bool ShowQuickPlaySlotSelection = false,
    int MaxLobbySize = 5,
    LcuGameTypeConfig? GameTypeConfig = null)
{
    public string Label => $"{Name} ({QueueId})";
    public string DisplayGroup =>
        IsBot ? "Co-op vs AI" :
        IsCustom ? "Custom" :
        ModeGroup == "kSummonersRift" ? "Summoner's Rift" :
        ModeGroup == "kARAM" ? "ARAM" :
        ModeGroup == "kTeamfightTactics" ? "TFT" :
        "Other";

    public bool IsTft => string.Equals(GameModeCode, "TFT", StringComparison.OrdinalIgnoreCase)
        || Type.Contains("TFT", StringComparison.OrdinalIgnoreCase);
    public bool IsBot => string.Equals(Category, "VersusAi", StringComparison.OrdinalIgnoreCase)
        || Type.Contains("BOT", StringComparison.OrdinalIgnoreCase);
}

public sealed record LcuGameTypeConfig(
    long Id,
    string Name,
    int MaxAllowableBans,
    bool AllowTrades,
    bool ExclusivePick,
    bool DuplicatePick,
    bool TeamChampionPool,
    bool CrossTeamChampionPool,
    bool AdvancedLearningQuests,
    bool BattleBoost,
    bool DeathMatch,
    bool DoNotRemove,
    bool LearningQuests,
    bool OnboardCoopBeginner,
    bool Reroll,
    int MainPickTimerDuration,
    int PostPickTimerDuration,
    int BanTimerDuration,
    string PickMode,
    string BanMode,
    string? GameModeOverride,
    int? NumPlayersPerTeamOverride);
