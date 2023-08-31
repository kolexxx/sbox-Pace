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

	private SceneObject _sceneModel;

	public override void Spawn()
	{
		SetupPhysicsFromAABB( PhysicsMotionType.Static, new Vector3( -1, -40, 0 ), new Vector3( 1, 40, 80 ) );

		Tags.Add( "trigger" );

		EnableAllCollisions = true;
		EnableTouchPersists = true;
	}

	public override void ClientSpawn()
	{
		_sceneModel = new( Game.SceneWorld, Weapon.Model, new Transform( Position, Rotation, 1.2f ) );
	}

	public override void Touch( Entity other )
	{
		Log.Info( $"{other} has touched a pickup" );

		if ( !Game.IsServer || other is not Pawn player || !TimeUntilRespawn )
			return;

		if ( player.Inventory[Weapon.Slot] is not null )
			return;

		TimeUntilRespawn = 10f;
		player.Inventory.Pickup( TypeLibrary.Create<Weapon>( Weapon.ClassName ) );
	}

	[GameEvent.Client.Frame]
	private void Tick()
	{
		_sceneModel.RenderingEnabled = TimeUntilRespawn;

		if ( !_sceneModel.RenderingEnabled )
			return;

		_sceneModel.Position = Position + (MathF.Sin( RealTime.Now * 2.5f ) * 10f + 45f) * Vector3.Up;
		_sceneModel.Rotation = Rotation.FromAxis( Vector3.Up, RealTime.Now * 100f );
	}
}
