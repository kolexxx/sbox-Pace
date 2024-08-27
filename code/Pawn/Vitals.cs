using Sandbox;
using System;

namespace Pace;

public sealed class Vitals : Component, Component.IDamageable
{
    [Property] public Pawn Player { get; private set; }
    [Property, ReadOnly, HostSync] public float Health { get; set; } = 100f;
    [Property, ReadOnly, HostSync] public float Armor { get; set; }
    [Property] public SoundEvent DamageTakenSound { get; private set; }
    public DamageInfo LastDamage { get; private set; }

    public void OnDamage( in DamageInfo info )
    {
        if ( Health <= 0f )
            return;

        if ( !Networking.IsHost )
        {
            using ( Rpc.FilterInclude( Connection.Host ) )
            {
                RequestDamage( info.Attacker, info.Weapon, info.Damage, info.Position );
            }

            return;
        }

        Health = MathF.Max( 0f, Health - info.Damage );
        BroadcastDamage( info.Attacker, info.Weapon, info.Damage, info.Position );

        if ( Health <= 0f )
            Player.OnKilled();
    }

    [Broadcast]
    private void RequestDamage( GameObject attacker, GameObject weapon, float damage, Vector3 position )
    {
        var damageInfo = new DamageInfo
        {
            Attacker = attacker,
            Weapon = weapon,
            Damage = damage,
            Position = position
        };

        OnDamage( damageInfo );
    }

    [Broadcast( NetPermission.HostOnly )]
    private void BroadcastDamage( GameObject attacker, GameObject weapon, float damage, Vector3 position )
    {
        LastDamage = new DamageInfo
        {
            Attacker = attacker,
            Weapon = weapon,
            Damage = damage,
            Position = position
        };

        Sound.Play( DamageTakenSound, position );
    }
}
