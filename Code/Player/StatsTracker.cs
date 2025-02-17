using Sandbox;
using Sandbox.Diagnostics;
using System.Collections.Generic;

namespace Pace;

/// <summary>
/// Keeps track of the player's stats.
/// </summary>
public sealed class StatsTracker : Component, Component.INetworkListener, IPlayerEvent, IRoundEvent
{
    [Property, ReadOnly, Sync( SyncFlags.FromHost )] public int Kills { get; set; }
    [Property, ReadOnly, Sync( SyncFlags.FromHost )] public int Assists { get; set; }
    [Property, ReadOnly, Sync( SyncFlags.FromHost )] public int Deaths { get; set; }
    [Property, ReadOnly, Sync( SyncFlags.FromHost )] public int Captures { get; set; }
    [Sync( SyncFlags.FromHost )] public NetDictionary<ulong, uint> KillsAgainstPlayer { get; set; } = new();

    protected override void OnAwake()
    {
        foreach ( var connection in Connection.All )
            KillsAgainstPlayer.TryAdd( connection.SteamId, 0u );
    }

    public void Clear()
    {
        Assert.True( Networking.IsHost );

        Kills = 0;
        Assists = 0;
        Deaths = 0;
        Captures = 0;

        foreach ( var id in KillsAgainstPlayer.Keys )
            KillsAgainstPlayer[id] = 0u;
    }

    void IPlayerEvent.OnPlayerKilled( Player player )
    {
        if ( !Networking.IsHost )
            return;

        if ( GameMode.Current.State != GameState.Playing )
            return;

        Deaths++;

        var attacker = player.HealthComponent.LastDamage.Attacker as Player;

        if ( !attacker.IsValid() || attacker == player )
            return;

        attacker.Stats.Kills++;
        attacker.Stats.KillsAgainstPlayer[Network.Owner.SteamId]++;
    }

    void IRoundEvent.OnStart()
    {
        if ( Networking.IsHost )
            Clear();
    }

    void INetworkListener.OnActive( Connection channel )
    {
        KillsAgainstPlayer.TryAdd( channel.SteamId, 0u );
    }

    void INetworkListener.OnDisconnected( Connection channel )
    {
        KillsAgainstPlayer.Remove( channel.SteamId );
    }
}
