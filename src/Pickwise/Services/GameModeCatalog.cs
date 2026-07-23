using Pickwise.Models;

namespace Pickwise.Services;

public sealed class GameModeCatalog
{
    public IReadOnlyList<GameMode> All { get; } =
    [
        new("Normal Draft 5v5", 400, ShowPositionSelector: true, PickMode: "TeamBuilderDraftPickStrategy"),
        new("Ranked Solo/Duo", 420, Type: "RANKED_SOLO_5x5", ShowPositionSelector: true, MaxLobbySize: 2, PickMode: "TeamBuilderDraftPickStrategy"),
        new("Ranked Flex", 440, Type: "RANKED_FLEX_SR", ShowPositionSelector: true, PickMode: "TeamBuilderDraftPickStrategy"),
        new("Swiftplay", 480, GameModeCode: "SWIFTPLAY", Type: "SWIFTPLAY", ShowQuickPlaySlotSelection: true),
        new("ARAM", 450, ModeGroup: "kARAM", GameModeCode: "ARAM", Type: "ARAM_UNRANKED_5x5", MapId: 12, PickMode: "AllRandomPickStrategy"),
        new("ARAM Mayhem", 2400, ModeGroup: "kARAM", GameModeCode: "KIWI", Type: "KIWI", MapId: 12, PickMode: "AllRandomPickStrategy"),
        new("Arena", 1750, ModeGroup: "kAlternativeLeagueGameModes", GameModeCode: "CHERRY", Type: "CHERRY", MapId: 30, MaxLobbySize: 18, PickMode: "SimulPickStrategy")
    ];
}
