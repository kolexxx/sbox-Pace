using Sandbox;

namespace Pace;

/// <summary>
/// Keeps track of the player's stats.
/// </summary>
public sealed class StatsTracker : Component
{
    [Property, ReadOnly, HostSync] public int Kills { get; set; }
    [Property, ReadOnly, HostSync] public int Assists { get; set; }
    [Property, ReadOnly, HostSync] public int Deaths { get; set; }
    [Property, ReadOnly, HostSync] public int Captures { get; set; }
}
