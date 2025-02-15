using Sandbox;
using System;

namespace Pace;

public sealed class LookCamera : Component
{
    [RequireComponent] public CameraComponent Camera { get; private set; }

    protected override void OnAwake()
    {
        WorldPosition = Player.Local.WorldPosition + Vector3.Up * 64f + Settings.Plane.Normal * 1000f;
    }

    protected override void OnPreRender()
    {
        var position = Player.Local.WorldPosition + Vector3.Up * 64f;
        var offset = (Player.Local.MousePosition - position) / 2f;
        var targetPosition = position + offset.ClampLength( 150f ) + Settings.Plane.Normal * 1000f;

        Camera.FieldOfView = Screen.CreateVerticalFieldOfView( 30f );
        WorldRotation = Rotation.LookAt( -Settings.Plane.Normal );
        WorldPosition = Vector3.Lerp( targetPosition, WorldPosition, MathF.Exp( -25f * Time.Delta ) );
    }
}