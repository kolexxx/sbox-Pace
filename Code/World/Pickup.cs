using Sandbox;
using System;

namespace Pace;

public abstract class Pickup : Component, IRoundEvent, UI.IMinimapElement
{
    [RequireComponent] public BoxCollider Collider { get; protected set; }

    /// <summary>
    /// The time it takes for our item to respawn.
    /// </summary>
    [Property] public float RespawnTime { get; protected set; }

    /// <summary>
	/// The time until the item respawns.
	/// </summary>
	[Sync( SyncFlags.FromHost )] public TimeUntil TimeUntilRespawn { get; private set; } = 0f;

    protected SceneObject Preview { get; set; }

    Color UI.IMinimapElement.Color => Color.Yellow;

    Vector3 UI.IMinimapElement.WorldPosition => WorldPosition;

    bool UI.IMinimapElement.IsVisible => TimeUntilRespawn <= 0f;

    protected override void OnFixedUpdate()
    {
        Collider.Enabled = TimeUntilRespawn;
    }

    protected override void OnPreRender()
    {
        Preview.RenderingEnabled = TimeUntilRespawn;

        if ( !Preview.RenderingEnabled )
            return;

        Preview.Position = WorldPosition + (MathF.Sin( Time.Now * 2.5f ) * 10f + 45f) * Vector3.Up;
        Preview.Rotation = Rotation.FromAxis( Vector3.Up, Time.Now * 100f );
    }

    public void OnPickedUp()
    {
        TimeUntilRespawn = RespawnTime;
    }

    void IRoundEvent.OnStart()
    {
        TimeUntilRespawn = 0f;
    }
}