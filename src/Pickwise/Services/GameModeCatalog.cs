using Pickwise.Models;

namespace Pickwise.Services;

public sealed class GameModeCatalog
{
    public IReadOnlyList<GameMode> All { get; } =
    [
        new("Normal Draft 5v5", 400),
        new("ARAM", 450),
        new("ARAM Mayhem", 2400)
    ];
}
