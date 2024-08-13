using Sandbox;
using System;

namespace Pace;

public sealed class Vitals : Component, Component.IDamageable
{
	[Property] public Pawn Player { get; private set; }
	[Property, ReadOnly, HostSync] public float Health { get; set; } = 100f;
    [Property, ReadOnly, HostSync] public float Armor { get; set; }
    public DamageInfo LastDamage { get; private set; }

    public void OnDamage(in DamageInfo info)
    {
        if (Health <= 0f)
            return;

        Health = MathF.Max(0f, Health - info.Damage);
        LastDamage = info;
		
        if (Health <= 0f)
            Player.OnKilled();
    }
}
