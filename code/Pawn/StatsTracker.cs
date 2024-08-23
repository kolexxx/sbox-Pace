using Sandbox;

namespace Pace;

/// <summary>
/// Keeps track of the player's stats.
/// </summary>
public sealed class StatsTracker : Component
{
    [Property, ReadOnly, HostSync] public int Kills { get; private set; }
    [Property, ReadOnly, HostSync] public int Assists { get; private set; }
    [Property, ReadOnly, HostSync] public int Deaths { get; private set; }
    [Property, ReadOnly, HostSync] public int Captures { get; private set; }
}
