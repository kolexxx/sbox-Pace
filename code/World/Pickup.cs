using Sandbox;
using System;

namespace Pace;

public class Pickup : Component, ICleanup
{
    /// <summary>
    /// The prefab we want to spawn after some time.
    /// </summary>
    [Property] public EquipmentResource EquipmentToSpawn { get; private set; }

    [RequireComponent] public BoxCollider Collider { get; private set; }

    /// <summary>
    /// The time it takes for our item to respawn.
    /// </summary>
    [Property] public float RespawnTime { get; private set; }

    /// <summary>
	/// The time until the item respawns.
	/// </summary>
	[HostSync] public TimeUntil TimeUntilRespawn { get; set; } = 0f;

    protected SceneObject Preview { get; set; }

    protected override void OnStart()
    {
        Preview = new
        (
            Game.ActiveScene.SceneWorld,
            EquipmentToSpawn.Model,
            new Transform( Transform.Position, Transform.Rotation, 1.2f )
        );
    }

    public void OnCleanup ()
    {
        TimeUntilRespawn = 0f;
    }

    protected override void OnFixedUpdate()
    {
        Collider.Enabled = TimeUntilRespawn;
    }

    protected override void OnPreRender()
    {
        Preview.RenderingEnabled = TimeUntilRespawn;

        if ( !Preview.RenderingEnabled )
            return;

        Preview.Position = Transform.Position + (MathF.Sin( Time.Now * 2.5f ) * 10f + 45f) * Vector3.Up;
        Preview.Rotation = Rotation.FromAxis( Vector3.Up, Time.Now * 100f );
    }

    public void OnPickedUp()
    {
        TimeUntilRespawn = RespawnTime;
    }
}