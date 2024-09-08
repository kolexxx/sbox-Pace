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
            new Transform( Transform.Position, Transform.Rotation, 1.2f )
        );
    }
}