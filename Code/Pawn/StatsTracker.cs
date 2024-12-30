using Sandbox;
using Sandbox.Diagnostics;

namespace Pace;

/// <summary>
/// Keeps track of the player's stats.
/// </summary>
public sealed class StatsTracker : Component
{
    [Property, ReadOnly, Sync( SyncFlags.FromHost )] public int Kills { get; set; }
    [Property, ReadOnly, Sync( SyncFlags.FromHost )] public int Assists { get; set; }
    [Property, ReadOnly, Sync( SyncFlags.FromHost )] public int Deaths { get; set; }
    [Property, ReadOnly, Sync( SyncFlags.FromHost )] public int Captures { get; set; }

    public void Clear()
    {
        Assert.True( Networking.IsHost );

        Kills = 0;
        Assists = 0;
        Deaths = 0;
        Captures = 0;
    }
}
