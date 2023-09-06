using Sandbox;
using Sandbox.Html;
using System;
using static Sandbox.Gizmo;

namespace Pace;

public partial class Projectile : ModelEntity
{
	public TimeUntil CanHitTime { get; set; } = 0.1f;
	public string Attachment { get; set; } = null;
	public bool ExplodeOnDestroy { get; set; } = true;

	public virtual float LifeTime => 5f;
	public virtual float Gravity => 0f;
	public virtual float Speed => 1000f;
	public virtual float Radius => 3f;
	public virtual string ModelPath => "models/weapons/rocketlauncher/rocket.vmdl";
	public virtual string ExplosionEffect => "particles/explosion.vpcf";
	public virtual string FollowEffect => "";
	public virtual string TrailEffect => "";
	public virtual string LaunchSound => "";
	public virtual string HitSound => "sounds/weapons/rocketlauncher/rl.explode.sound";

	protected Weapon Origin { get; set; }
	protected float GravityModifier { get; set; }
	protected TimeUntil DestroyTime { get; set; }
	protected SceneObject ModelEntity { get; set; }
	protected Sound Sound { get; set; }
	protected Particles Follower { get; set; }
	protected Particles Trail { get; set; }

	public Projectile() { }

	public Projectile( Ray ray, Pawn player )
	{
		if ( LifeTime > 0f )
			DestroyTime = LifeTime;

		Owner = player;
		Origin = player.Inventory.ActiveWeapon;
		EnableDrawing = false;
		Velocity = ray.Forward * Speed;
		Position = ray.Position;

		player.Projectiles.Add( this );

		if ( Game.IsServer )
		{
			using ( LagCompensation() )
			{
				// Work out the number of ticks for this client's latency that it took for us to receive this input.
				var tickDifference = ((float)(Owner.Client.Ping / 2000f) / Time.Delta).CeilToInt();

				// Advance the simulation by that number of ticks.
				for ( var i = 0; i < tickDifference; i++ )
				{
					if ( IsValid )
					{
						Simulate( Owner.Client );
					}
				}
			}
		}

		if ( IsClientOnly )
		{
			using ( Prediction.Off() )
			{
				CreateEffects();
			}
		}
	}

	public override void Spawn()
	{
		Predictable = true;
	}

	public override void ClientSpawn()
	{
		// We only want to create effects if we're NOT the server-side copy.
		if ( !IsServerSideCopy() )
			CreateEffects();
	}

	public virtual void CreateEffects()
	{
		if ( !string.IsNullOrEmpty( ModelPath ) )
			ModelEntity = new( Game.SceneWorld, ModelPath, Transform );

		if ( !string.IsNullOrEmpty( TrailEffect ) )
		{
			Trail = Particles.Create( TrailEffect, this );

			if ( !string.IsNullOrEmpty( Attachment ) )
				Trail.SetEntityAttachment( 0, this, Attachment );
			else
				Trail.SetEntity( 0, this );
		}

		if ( !string.IsNullOrEmpty( FollowEffect ) )
			Follower = Particles.Create( FollowEffect, this );

		if ( !string.IsNullOrEmpty( LaunchSound ) )
			Sound = PlaySound( LaunchSound );
	}

	public override void Simulate( IClient client )
	{
		Rotation = Rotation.LookAt( Velocity.Normal );

		var newPosition = GetTargetPosition();

		var trace = Trace.Ray( Position, newPosition )
			.UseHitboxes()
			.WithoutTags( "trigger" )
			.Size( Radius )
			.Ignore( Owner )
			.Run();

		Position = trace.EndPosition;

		if ( LifeTime > 0f && DestroyTime )
		{
			if ( ExplodeOnDestroy )
				PlayHitEffects( Vector3.Zero );

			Delete();

			return;
		}

		if ( HasHitTarget( trace ) )
		{
			if ( Game.IsServer )
				DoExplosion( Origin.Definition.Damage, 120f );

			PlayHitEffects( trace.Normal );
			Delete();
		}
	}

	public bool IsServerSideCopy()
	{
		return !IsClientOnly && Owner.IsValid() && Owner.IsLocalPawn;
	}

	protected virtual bool HasHitTarget( TraceResult trace )
	{
		return (trace.Hit && CanHitTime) || trace.StartedSolid;
	}

	protected void DoExplosion( float damage, float radius )
	{
		foreach ( var entity in Entity.FindInSphere( Position, radius ) )
		{
			var dmgPos = Position;
			var entPos = entity.WorldSpaceBounds.Center;

			var tr = Trace.Ray( dmgPos, entPos )
				.StaticOnly()
				.Ignore( this )
				.Ignore( entity )
				.Run();

			// If we hit something, we're blocked by world.
			if ( tr.Hit )
				return;

			// Use whichever is closer, absorigin or worldspacecenter
			var toWorldSpaceCenter = (Position - entPos).Length;
			var toOrigin = (Position - entity.Position).Length;

			var distance = Math.Min( toWorldSpaceCenter, toOrigin );

			// if we're outside of the radius, exit now.
			if ( radius < tr.Distance || radius == 0 )
				return;

			var maxDamage = damage;
			var minDamage = damage * 0.1f;

			damage = distance.Remap( 0, radius, maxDamage, minDamage, true );

			// If we end up doing 0 damage, exit now.
			if ( damage <= 0 )
				return;

			var damageInfo = DamageInfo.FromExplosion( Position, tr.Direction * damage, damage )
				.WithAttacker( Owner )
				.WithWeapon( Origin );

			entity.TakeDamage( damageInfo );
		}
	}

	protected virtual Vector3 GetTargetPosition()
	{
		var newPosition = Position;
		newPosition += Velocity * Time.Delta;

		GravityModifier += Gravity;
		newPosition -= new Vector3( 0f, 0f, GravityModifier * Time.Delta );

		return newPosition;
	}

	[ClientRpc]
	protected virtual void PlayHitEffects( Vector3 normal )
	{
		if ( IsServerSideCopy() )
		{
			// We don't want to play hit effects if we're the server-side copy.
			return;
		}

		if ( !string.IsNullOrEmpty( ExplosionEffect ) )
		{
			var explosion = Particles.Create( ExplosionEffect );

			if ( explosion != null )
			{
				explosion.SetPosition( 0, Position );
				explosion.SetForward( 0, normal );
			}
		}

		if ( !string.IsNullOrEmpty( HitSound ) )
			Sound.FromWorld( HitSound, Position );
	}

	[GameEvent.PreRender]
	protected virtual void PreRender()
	{
		if ( ModelEntity.IsValid() )
		{
			ModelEntity.Transform = Transform;
		}
	}

	protected override void OnDestroy()
	{
		(Owner as Pawn)?.Projectiles.Remove( this );
		RemoveEffects();

		base.OnDestroy();
	}

	private void RemoveEffects()
	{
		ModelEntity?.Delete();
		Sound.Stop();
		Follower?.Destroy();
		Trail?.Destroy();
	}
}
