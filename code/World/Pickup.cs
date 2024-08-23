using Sandbox;
using System;

namespace Pace;

public sealed class Pickup : Component
{
    /// <summary>
    /// The prefab we want to spawn after some time.
    /// </summary>
    [Property] public GameObject PrefabToSpawn { get; private set; }

    /// <summary>
    /// The time it takes for our item to respawn.
    /// </summary>
    [Property] public float RespawnTime { get; private set; }

    /// <summary>
	/// The time until the item respawns.
	/// </summary>
	[HostSync] public TimeUntil TimeUntilRespawn { get; set; } = 0f;

    /// <summary>
    /// A reference to our item once spawned.
    /// </summary>
    [Property, ReadOnly, HostSync] public GameObject SpawnedObject { get; private set; }

    protected override void OnFixedUpdate()
    {
        if ( !Networking.IsHost )
            return;

        if ( SpawnedObject.IsValid() && SpawnedObject.Parent != GameObject )
        {
            SpawnedObject = null;
            TimeUntilRespawn = RespawnTime;
        }

        if ( SpawnedObject.IsValid() )
        {
            SpawnedObject.Transform.LocalPosition = (MathF.Sin( Time.Now * 2.5f ) * 10f + 45f) * Vector3.Up;
            SpawnedObject.Transform.LocalRotation = Rotation.FromAxis( Vector3.Up, Time.Now * 100f );
            return;
        }

        if ( !TimeUntilRespawn )
            return;

        SpawnedObject = PrefabToSpawn.Clone( new CloneConfig
        {
            Parent = GameObject,
            StartEnabled = true,
            Transform = new Transform()
        } );

        SpawnedObject.NetworkSpawn();
    }

    public void Delete()
    {
        SpawnedObject.Destroy();
        TimeUntilRespawn = RespawnTime;
    }
}