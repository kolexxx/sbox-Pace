using Sandbox;
using System;

namespace Pace;

public abstract partial class BasePickup : ModelEntity
{
	/// <summary>
	/// The time until the item respawns.
	/// </summary>
	[Net] protected TimeUntil TimeUntilRespawn { get; set; } = 0f;

	/// <summary>
	/// The model path for our scene object.
	/// </summary>
	protected virtual string ModelPath => "";

	protected SceneObject Preview { get; set; }

	public override void Spawn()
	{
		SetupPhysicsFromAABB( PhysicsMotionType.Static, new Vector3( -1, -40, 0 ), new Vector3( 1, 40, 80 ) );

		Tags.Add( "trigger" );
		EnableTouch = true;
		EnableTouchPersists = true;
	}

	public override void ClientSpawn()
	{
		EnableAllCollisions = false;
		Preview = new( Game.SceneWorld, ModelPath, new Transform( Position, Rotation, 1.2f ) );
	}

	[GameEvent.Client.Frame]
	private void Tick()
	{
		Preview.RenderingEnabled = TimeUntilRespawn;

		if ( !Preview.RenderingEnabled )
			return;

		Preview.Position = Position + (MathF.Sin( RealTime.Now * 2.5f ) * 10f + 45f) * Vector3.Up;
		Preview.Rotation = Rotation.FromAxis( Vector3.Up, RealTime.Now * 100f );
	}
}
