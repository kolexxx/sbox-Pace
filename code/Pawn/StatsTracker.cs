using Sandbox;

namespace Pace;

public sealed class StatsTracker : Component
{
    [Property, ReadOnly, HostSync] public int Kills { get; private set; }
    [Property, ReadOnly, HostSync] public int Assists { get; private set; }
    [Property, ReadOnly, HostSync] public int Deaths { get; private set; }
	[Property, ReadOnly, HostSync] public int Captures { get; private set; }
}
