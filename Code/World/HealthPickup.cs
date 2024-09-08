using Sandbox;

namespace Pace;

public sealed class HealthPickup : Pickup
{
    [Property] public float HealAmount { get; private set; } = 50f;

    protected override void OnStart()
    {
        Preview = new
        (
            Game.ActiveScene.SceneWorld,
            "models/citizen_props/balloonheart01.vmdl",
            new Transform( Transform.Position, Transform.Rotation )
        );

        Preview.ColorTint = Color.Green;
    }
}