using Sandbox;
using System.Collections.Generic;

namespace Pace;

public enum FireMode
{
    Semi = 0,
    Automatic = 1,
    Burst = 2
}

public sealed class FireComponent : Component
{
    [Property, Group( "Components" )] public Equipment Equipment { get; private set; }
    [Property, Group( "Components" )] public AmmoComponent Ammo { get; private set; }
    [Property, Group( "Stats" )] public FireMode FireMode { get; set; } = FireMode.Semi;
    [Property, Group( "Stats" )] public float FireRate { get; set; } = 0f;
    [Property, Group( "Stats" )] public float Damage { get; set; } = 20;
    [Property, Group( "Stats" )] public float Spread { get; set; } = 0f;
    [Property, Group( "Stats" )] public int BulletsPerFire { get; set; } = 1;
    [Property, Group( "Effects" )] public SoundEvent ShootSound { get; set; }
    [Property, Group( "Effects" )] public ParticleSystem Tracer { get; set; }
    [Property, Group( "Effects" )] public GameObject MuzzleFlash { get; set; }
    [Property, Group( "GameObjects" )] public GameObject Muzzle { get; set; }
    [Property, Group( "GameObjects" )] public GameObject EjectionPort { get; set; }

    /// <summary>
    /// How long since we last shot this weapon.
    /// </summary>
    public TimeSince TimeSinceFire { get; private set; }

    protected override void OnFixedUpdate()
    {
        if ( IsProxy )
            return;

        if ( !Equipment.IsDeployed )
            return;

        if ( !CanShoot() )
            return;

        ShootEffects();

        for ( var i = 0; i < BulletsPerFire; i++ )
            ShootBullet();
    }

    private bool CanShoot()
    {
        if ( Equipment.Owner.IsFrozen )
            return false;

        if ( FireMode == FireMode.Semi && !Input.Pressed( "Attack1" ) )
            return false;
        else if ( FireMode != FireMode.Semi && !Input.Down( "Attack1" ) )
            return false;

        if ( FireRate != 0f && TimeSinceFire < 1f / FireRate )
            return false;

        if ( Ammo.IsValid() && (Ammo.IsReloading || Ammo.AmmoInClip <= 0) )
            return false;

        return true;
    }

    private void ShootBullet()
    {
        TimeSinceFire = 0f;
        Ammo.AmmoInClip--;
        
        var ray = Equipment.Owner.AimRay;
        ray.Forward *= Rotation.FromAxis( Settings.Plane.Normal, Game.Random.Float( -0.5f, 0.5f ) * Spread );

        foreach ( var tr in TraceBullet( ray, 5000f, 1f ) )
        {
            GameObject.Clone( "/effects/impact_default.prefab", new Transform( tr.HitPosition + tr.Normal * 2.0f, Rotation.LookAt( tr.Normal ) ) );

            {
                var go = GameObject.Clone( "/effects/decal_bullet_default.prefab" );
                go.Transform.World = new Transform( tr.HitPosition + tr.Normal * 2.0f, Rotation.LookAt( -tr.Normal, Vector3.Random ), System.Random.Shared.Float( 0.8f, 1.2f ) );
                go.SetParent( tr.GameObject );
            }

            tr.Body?.ApplyImpulseAt( tr.HitPosition, tr.Direction * 200.0f * tr.Body.Mass.Clamp( 0, 200 ) );

            if ( tr.GameObject is null )
                continue;

            var damage = new DamageInfo
            {
                Attacker = Equipment.Owner,
                Weapon = Equipment,
                Damage = Damage,
                Flags = DamageFlags.Bullet,
                Position = tr.HitPosition,
                Force = tr.Direction * Damage * 100
            };

            foreach ( var damageable in tr.GameObject.Components.GetAll<HealthComponent>() )
            {
                damageable.TakeDamage( damage );
            }
        }
    }

    /// <summary>
    /// Does a trace from start to end, does bullet impact effects. Coded as an IEnumerable so you can return multiple
    /// hits, like if you're going through layers or ricocheting or something.
    /// </summary>
    private IEnumerable<SceneTraceResult> TraceBullet( Ray ray, float distance, float radius = 2.0f )
    {
        var trace = Scene.Trace.Ray( ray, distance )
            .IgnoreGameObjectHierarchy( GameObject.Parent )
            .UseHitboxes()
            .Radius( radius );

        var tr = trace.Run();

        if ( tr.Hit )
            yield return tr;
    }

    [Broadcast( NetPermission.OwnerOnly )]
    private void ShootEffects()
    {
        if ( ShootSound is not null )
        {
            Sound.Play( ShootSound, Transform.Position );
        }

        if ( Muzzle.IsValid() && MuzzleFlash.IsValid() )
        {
            MuzzleFlash.Clone( new CloneConfig
            {
                Parent = Muzzle,
                Transform = new(),
                StartEnabled = true,
            } );
        }

        Equipment.Owner.PawnBody.Renderer.Set( "b_attack", true );
    }

    [Broadcast( NetPermission.OwnerOnly )]
    private void TracerEffects( Vector3 hitPosition )
    {
        // get the muzzle position on our effect entity - either viewmodel or world model
        var pos = Components.Get<SkinnedModelRenderer>().GetAttachment( "muzzle" ) ?? Transform.World;
    }
}
