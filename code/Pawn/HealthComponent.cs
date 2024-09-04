using Sandbox;
using System;

namespace Pace;

public sealed class HealthComponent : Component
{
    [Property] public Pawn Pawn { get; private set; }
    [Property, ReadOnly, HostSync] public float Health { get; set; } = 100f;
    [Property] public SoundEvent DamageTakenSound { get; private set; }
    public DamageInfo LastDamage { get; private set; }

    public void TakeDamage( DamageInfo info )
    {
        if ( Health <= 0f )
            return;

        if ( !Networking.IsHost )
        {
            using ( Rpc.FilterInclude( Connection.Host ) )
            {
                RequestDamage( info.Attacker, info.Weapon, info.Damage, info.Flags, info.Position, info.Force );
            }

            return;
        }

        Health = MathF.Max( 0f, Health - info.Damage );
        BroadcastDamage( info.Attacker, info.Weapon, info.Damage, info.Flags, info.Position, info.Force );

        if ( Health <= 0f )
            Pawn.OnKilled();
    }

    [Broadcast]
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

    [Broadcast( NetPermission.HostOnly )]
    private void BroadcastDamage( Component attacker, Component weapon, float damage, DamageFlags flags, Vector3 position, Vector3 force )
    {
        LastDamage = new DamageInfo
        {
            Attacker = attacker,
            Weapon = weapon,
            Victim = Pawn,
            Damage = damage,
            Flags = flags,
            Position = position,
            Force = force,
            TimeSince = 0
        };

        Sound.Play( DamageTakenSound, position );
    }
}
