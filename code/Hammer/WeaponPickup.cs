using Editor;
using Sandbox;
using System;

namespace Pace;

[HammerEntity]
[ClassName( "weapon_spawn" )]
public partial class WeaponPickup : ModelEntity
{
	[Net, Property] public WeaponDefinition Weapon { get; set; }
	[Net] private TimeUntil TimeUntilRespawn { get; set; } = 0f;

	private SceneObject _sceneObject;

	public override void Spawn()
	{
		SetupPhysicsFromAABB( PhysicsMotionType.Static, new Vector3( -1, -40, 0 ), new Vector3( 1, 40, 80 ) );

		Tags.Add( "trigger" );
		EnableTouch = true;
		EnableTouchPersists = true;
	}

	public override void ClientSpawn()
	{
		_sceneObject = new( Game.SceneWorld, Weapon.Model, new Transform( Position, Rotation, 1.2f ) );
	}

	public override void Touch( Entity other )
	{
		if ( !Game.IsServer || other is not Pawn player || !TimeUntilRespawn )
			return;

		if ( player.Inventory[Weapon.Slot] is not null )
		{
			var weapon = player.Inventory[Weapon.Slot];

			if ( weapon.ReserveAmmo >= 2 * weapon.Definition.ClipSize )
				return;

			TimeUntilRespawn = 10f;
			weapon.ReserveAmmo = 2 * weapon.Definition.ClipSize;
			Sound.FromEntity( "pickup_weapon", player );
		}
		else
		{
			TimeUntilRespawn = 10f;
			player.Inventory.Pickup( TypeLibrary.Create<Weapon>( Weapon.ClassName ) );
		}
	}

	[GameEvent.Client.Frame]
	private void Tick()
	{
		_sceneObject.RenderingEnabled = TimeUntilRespawn;

		if ( !_sceneObject.RenderingEnabled )
			return;

		_sceneObject.Position = Position + (MathF.Sin( RealTime.Now * 2.5f ) * 10f + 45f) * Vector3.Up;
		_sceneObject.Rotation = Rotation.FromAxis( Vector3.Up, RealTime.Now * 100f );
	}
}
