using Sandbox;

namespace Pace;

public sealed class WeaponPickup : Pickup
{
    /// <summary>
    /// The prefab we want to spawn after some time.
    /// </summary>
    [Property] public EquipmentResource EquipmentToSpawn { get; private set; }

    protected override void OnStart()
    {
        Preview = new
        (
            Game.ActiveScene.SceneWorld,
            EquipmentToSpawn.Model,
            new Transform( WorldPosition, WorldRotation, 2f )
        );
    }
}