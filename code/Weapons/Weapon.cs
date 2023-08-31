using Sandbox;
using System;
using System.Collections.Generic;

namespace Pace;

[Category( "Weapon" )]
public abstract partial class Weapon : AnimatedEntity
{
	/// <summary>
	/// An accessor to grab our Pawn.
	/// </summary>
	public Pawn Pawn => Owner as Pawn;

	/// <summary>
	/// Is this weapon currently equiped by the player?
	/// </summary>
	public bool IsActive => Pawn?.Inventory.ActiveWeapon == this;

	/// <summary>
	/// Information and stats for this weapon.
	/// </summary>
	public WeaponDefinition Definition { get; set; }

	/// <summary>
	/// How long since we equiped this weapon.
	/// </summary>
	[Net, Predicted] public TimeSince TimeSinceDeployed { get; private set; }

	/// <summary>
	/// How long since we last shot this weapon.
	/// </summary>
	[Net, Predicted] public TimeSince TimeSincePrimaryAttack { get; protected set; }

	/// <summary>
	/// How much ammo is currently in the clip.
	/// </summary>
	[Net, Predicted] public int AmmoInClip { get; protected set; }

	/// <summary>
	/// How much ammo is in reserve.
	/// </summary>
	[Net, Predicted] public int ReserveAmmo { get; protected set; }

	/// <summary>
	/// How long since we started reloading.
	/// </summary>
	[Net, Predicted] public TimeSince TimeSinceReload { get; protected set; }

	/// <summary>
	/// Are we still reloading this weapon?
	/// </summary>
	[Net, Predicted] public bool IsReloading { get; protected set; }

	public override void Spawn()
	{
		EnableDrawing = false;

		Definition = WeaponDefinition.Get( GetType() );
		AmmoInClip = Definition.ClipSize;
		ReserveAmmo = 2 * Definition.ClipSize;
		Model = Definition.Model;
	}

	public override void ClientSpawn()
	{
		Definition = WeaponDefinition.Get( GetType() );
	}

	/// <summary>
	/// Called when the weapon becomes the ActiveChild of the owner.
	/// </summary>
	/// <param name="pawn"></param>
	public virtual void OnEquip( Pawn pawn )
	{
		TimeSinceDeployed = 0f;
		TimeSinceReload = 0;
		IsReloading = false;
		EnableDrawing = true;

		pawn.SetAnimParameter( "holdtype", (int)Definition.HoldType );
		pawn.SetAnimParameter( "b_deploy", true );
	}

	/// <summary>
	/// Called when the weapon is either removed from the player, or holstered.
	/// </summary>
	public virtual void OnHolster()
	{
		EnableDrawing = false;
	}

	/// <summary>
	/// Called from <see cref="Pawn.Simulate(IClient)"/>.
	/// </summary>
	/// <param name="client"></param>
	public override void Simulate( IClient client )
	{
		if ( CanReload() )
		{
			Reload();
			return;
		}

		if ( !IsReloading )
		{
			if ( CanPrimaryAttack() )
			{
				using ( LagCompensation() )
				{
					TimeSincePrimaryAttack = 0;
					PrimaryAttack();
				}
			}
		}
		else if ( TimeSinceReload >= Definition.ReloadTime )
			OnReloadFinish();
	}

	/// <summary>
	/// Called every <see cref="Simulate(IClient)"/> to see if we can shoot our gun.
	/// </summary>
	/// <returns></returns>
	protected virtual bool CanPrimaryAttack()
	{
		if ( AmmoInClip <= 0 )
			return false;

		if ( Definition.FireMode == FireMode.Semi && !Input.Pressed( InputAction.PrimaryAttack ) )
			return false;
		else if ( Definition.FireMode != FireMode.Semi && !Input.Down( InputAction.PrimaryAttack ) )
			return false;

		var rate = Definition.PrimaryRate;
		if ( rate <= 0 )
			return true;

		return TimeSincePrimaryAttack > (1 / rate);
	}

	/// <summary>
	/// Called when your gun shoots.
	/// </summary>
	protected virtual void PrimaryAttack()
	{
		AmmoInClip--;
		Pawn.SetAnimParameter( "b_attack", true );
		Pawn.PlaySound( Definition.FireSound );
		ShootEffects();
		ShootBullet( Definition.Spread, 50, Definition.Damage, 1 );
	}

	protected virtual bool CanReload()
	{
		if ( IsReloading )
			return false;

		if ( AmmoInClip >= Definition.ClipSize || ReserveAmmo <= 0 )
			return false;

		if ( AmmoInClip == 0 )
			return true;

		if ( !Input.Pressed( InputAction.Reload ) )
			return false;

		return true;
	}

	protected virtual void Reload()
	{
		if ( IsReloading )
			return;

		TimeSinceReload = 0;
		IsReloading = true;

		Pawn.SetAnimParameter( "b_reload", true );
	}

	protected virtual void OnReloadFinish()
	{
		IsReloading = false;
		TakeAmmo( Definition.ClipSize - AmmoInClip );
	}

	/// <summary>
	/// Does a trace from start to end, does bullet impact effects. Coded as an IEnumerable so you can return multiple
	/// hits, like if you're going through layers or ricocheting or something.
	/// </summary>
	public virtual IEnumerable<TraceResult> TraceBullet( Ray ray, float distance, float radius = 2.0f )
	{
		var underWater = Trace.TestPoint( ray.Position, "water" );

		var trace = Trace.Ray( ray, distance )
				.UseHitboxes()
				.WithAnyTags( "solid", "player", "npc" )
				.Ignore( this )
				.Size( radius );

		//
		// If we're not underwater then we can hit water
		//
		if ( !underWater )
			trace = trace.WithAnyTags( "water" );

		var tr = trace.Run();

		if ( tr.Hit )
			yield return tr;
	}

	/// <summary>
	/// Shoot a single bullet
	/// </summary>
	public virtual void ShootBullet( Ray ray, float spread, float force, float damage, float bulletSize )
	{
		for ( var i = 0; i < Definition.BulletsPerFire; i++ )
		{
			var newRay = ray;
			newRay.Forward *= Rotation.FromAxis( MyGame.Plane.Normal, Game.Random.Float(-1f, 1f) * spread );

			//
			// ShootBullet is coded in a way where we can have bullets pass through shit
			// or bounce off shit, in which case it'll return multiple results
			//
			foreach ( var tr in TraceBullet( newRay, 5000f, bulletSize ) )
			{
				tr.Surface.DoBulletImpact( tr );

				if ( !string.IsNullOrEmpty( Definition.TracerParticle ) )
					TracerEffect( tr.EndPosition );

				if ( !Game.IsServer )
					continue;

				if ( !tr.Entity.IsValid() )
					continue;

				//
				// We turn predictiuon off for this, so any exploding effects don't get culled etc
				//
				using ( Prediction.Off() )
				{
					var damageInfo = DamageInfo.FromBullet( tr.EndPosition, newRay.Forward * force, damage )
						.UsingTraceResult( tr )
						.WithAttacker( Owner )
						.WithWeapon( this );

					tr.Entity.TakeDamage( damageInfo );
				}
			}
		}
	}

	/// <summary>
	/// Shoot a single bullet from owners view point
	/// </summary>
	public virtual void ShootBullet( float spread, float force, float damage, float bulletSize )
	{
		Game.SetRandomSeed( Time.Tick );

		ShootBullet( Owner.AimRay, spread, force, damage, bulletSize );
	}

	protected void TakeAmmo( int amount )
	{
		var ammo = Math.Min( ReserveAmmo, amount );

		AmmoInClip += ammo;
		ReserveAmmo -= ammo;
	}

	[ClientRpc]
	protected virtual void ShootEffects()
	{
		Particles.Create( Definition.MuzzleFlashParticle, this, "muzzle" );
	}

	[ClientRpc]
	public void TracerEffect( Vector3 hitPosition )
	{
		// get the muzzle position on our effect entity - either viewmodel or world model
		var pos = GetAttachment( "muzzle" ) ?? Transform;

		var system = Particles.Create( Definition.TracerParticle );
		system?.SetPosition( 0, pos.Position );
		system?.SetPosition( 1, hitPosition );
	}
}
