namespace Pickwise.Models;

public sealed record GameMode(string Name, int QueueId)
{
    public string Label => $"{Name} ({QueueId})";
}
