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
    /// <summary>
    /// A reference to our Equipment component.
    /// </summary>
    [Property, Group( "Components" )] public Equipment Equipment { get; private set; }

    /// <summary>
    /// A reference to our Ammo component, if we have it.
    /// </summary>
    [Property, Group( "Components" )] public AmmoComponent Ammo { get; private set; }

    /// <summary>
    /// Default fire mode.
    /// </summary>
    [Property, Group( "Stats" )] public FireMode FireMode { get; set; } = FireMode.Semi;

    /// <summary>
    /// How many shots are fired per second?
    /// </summary>
    [Property, Group( "Stats" )] public float FireRate { get; set; } = 10f;

    /// <summary>
    /// How much damage a single bullet does.
    /// </summary>
    [Property, Group( "Stats" )] public float Damage { get; set; } = 20;

    /// <summary>
    /// The angle of the cone in which shots will deviate.
    /// </summary>
    [Property, Group( "Stats" )] public float Spread { get; set; } = 0f;

    /// <summary>
    /// How many bullets are fired per shot.
    /// </summary>
    [Property, Group( "Stats" )] public int BulletsPerFire { get; set; } = 1;

    /// <summary>
    /// Played when firing.
    /// </summary>
    [Property, Group( "Effects" )] public SoundEvent ShootSound { get; set; }

    /// <summary>
    /// Visual representation of a bullet.
    /// </summary>
    [Property, Group( "Effects" )] public ParticleSystem Tracer { get; set; }

    /// <summary>
    /// Created from the muzzle when shooting.
    /// </summary>
    [Property, Group( "Effects" )] public GameObject MuzzleFlash { get; set; }

    /// <summary>
    /// We will fire our tracers and muzzle flashes from here.
    /// </summary>
    [Property, Group( "GameObjects" )] public GameObject Muzzle { get; set; }

    /// <summary>
    /// How long since we last shot this weapon.
    /// </summary>
    public TimeSince TimeSinceFire { get; private set; }

    /// <summary>
    /// Has enough time passed between shots?
    /// </summary>
    public bool IsOnCooldown => TimeSinceFire < 1f / FireRate;

    protected override void OnFixedUpdate()
    {
        if ( IsProxy )
            return;

        if ( !Equipment.IsDeployed )
            return;

        if ( !CanShoot() )
            return;

        TimeSinceFire = 0f;
        Ammo.LoadedAmmo--;
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

        if ( IsOnCooldown )
            return false;

        if ( Ammo.IsValid() && (Ammo.IsReloading || Ammo.LoadedAmmo <= 0) )
            return false;

        return true;
    }

    private void ShootBullet()
    {
        var ray = new Ray( Muzzle.WorldPosition, (Equipment.Owner.MousePosition - Muzzle.WorldPosition).Normal );
        ray.Forward *= Rotation.FromAxis( Settings.Plane.Normal, Game.Random.Float( -0.5f, 0.5f ) * Spread );

        foreach ( var tr in TraceBullet( ray, 5000f, 1f ) )
        {
            HitEffects( tr.EndPosition, tr.Normal );
            TracerEffects( tr.EndPosition );

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

            if ( tr.Hitbox is not null )
                damage.Flags = damage.Flags.WithFlag( DamageFlags.Critical, tr.Hitbox.Tags.Has( "head" ) );

            foreach ( var damageable in tr.GameObject.Components.GetAll<HealthComponent>( FindMode.InSelf ) )
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
            .IgnoreGameObjectHierarchy( Equipment.Owner.GameObject )
            .UseHitboxes()
            .Radius( radius );

        var tr = trace.Run();

        if ( tr.StartedSolid && tr.GameObject.Tags.Has( "barrier" ) )
            tr = trace.IgnoreGameObject( tr.GameObject ).Run();

        if ( tr.Hit )
            yield return tr;
    }

    [Rpc.Broadcast]
    private void ShootEffects()
    {
        if ( ShootSound is not null )
        {
            Sound.Play( ShootSound, WorldPosition );
        }

        if ( Muzzle.IsValid() && MuzzleFlash.IsValid() )
        {
            MuzzleFlash.Clone( new CloneConfig
            {
                Transform = Muzzle.WorldTransform,
                StartEnabled = true,
            } );
        }

        Equipment.Owner.Renderer.Set( "b_attack", true );
    }

    [Rpc.Broadcast]
    private void HitEffects( Vector3 hitPosition, Vector3 normal )
    {
        GameObject.Clone( "/effects/impact_default.prefab", new Transform( hitPosition + normal * 2.0f, Rotation.LookAt( normal ) ) );
    }

    [Rpc.Broadcast]
    private void TracerEffects( Vector3 endPos )
    {
        if ( !Muzzle.IsValid() || !Tracer.IsValid() )
            return;

        var gameObject = Scene.CreateObject();
        gameObject.Name = $"Particle: {Equipment.GameObject}";
        gameObject.WorldTransform = Muzzle.WorldTransform;

        var p = gameObject.Components.Create<LegacyParticleSystem>();
        p.Particles = Tracer;
        gameObject.Transform.ClearInterpolation();

        p.SceneObject.SetControlPoint( 0, Muzzle.WorldPosition );
        p.SceneObject.SetControlPoint( 1, endPos );

        p.Invoke( 0.1f, p.DestroyGameObject );
    }
}
