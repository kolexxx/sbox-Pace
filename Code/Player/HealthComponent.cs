using Sandbox;
using Sandbox.Diagnostics;
using System;

namespace Pace;

public sealed class HealthComponent : Component, Component.ITriggerListener
{
    [Property] public Player Player { get; private set; }
    [Property] public float MaxHealth { get; private set; } = 100f;
    [Property, ReadOnly, Sync( SyncFlags.FromHost )] public float Health { get; private set; } = 100f;
    [Property] public SoundEvent DamageTakenSound { get; private set; }
    public DamageInfo LastDamage { get; private set; }

    protected override void OnAwake()
    {
        Health = MaxHealth;
    }

    public void TakeDamage( DamageInfo info )
    {
        if ( Health <= 0f )
            return;

        if ( !Networking.IsHost )
        {
            RequestDamage( info.Attacker, info.Weapon, info.Damage, info.Flags, info.Position, info.Force );

            return;
        }

        if ( info.Flags.HasFlag( DamageFlags.Critical ) )
            info.Damage *= 1.5f;

        Health = MathF.Max( 0f, Health - info.Damage );
        BroadcastDamage( info.Attacker, info.Weapon, info.Damage, info.Flags, info.Position, info.Force );

        if ( Health <= 0f )
            Player?.OnKilled();
    }

    [Rpc.Host]
    private void RequestDamage( Component attacker, Component weapon, float damage, DamageFlags flags, Vector3 position, Vector3 force )
    {
        var damageInfo = new DamageInfo
        {
            Attacker = attacker,
            Weapon = weapon,
            Damage = damage,
            Flags = flags,
            Position = position,
            Force = force
        };

        TakeDamage( damageInfo );
    }

    [Rpc.Broadcast]
    private void BroadcastDamage( Component attacker, Component weapon, float damage, DamageFlags flags, Vector3 position, Vector3 force )
    {
        LastDamage = new DamageInfo
        {
            Attacker = attacker,
            Weapon = weapon,
            Victim = Player,
            Damage = damage,
            Flags = flags,
            Position = position,
            Force = force,
            TimeSince = 0
        };

        if ( attacker == Player.Local )
            Sound.Play( DamageTakenSound );
    }

    public void Heal( float amount )
    {
        Assert.True( Networking.IsHost );

        Health = MathF.Min( Health + amount, MaxHealth );
    }

    void ITriggerListener.OnTriggerEnter( Collider other )
    {
        if ( !Networking.IsHost )
            return;

        if ( !other.Components.TryGet<HealthPickup>( out var pickup ) )
            return;

        if ( Health >= 100f )
            return;

        Heal( pickup.HealAmount );
        pickup.OnPickedUp();
    }
}
