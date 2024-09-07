using Sandbox;
using System;

namespace Pace;

public abstract class Pickup : Component, ICleanup
{
    [RequireComponent] public BoxCollider Collider { get; private set; }

    /// <summary>
    /// The time it takes for our item to respawn.
    /// </summary>
    [Property] public float RespawnTime { get; protected set; }

    /// <summary>
	/// The time until the item respawns.
	/// </summary>
	[HostSync] public TimeUntil TimeUntilRespawn { get; private set; } = 0f;

    protected SceneObject Preview { get; set; }

    public void OnCleanup()
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