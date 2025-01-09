using Sandbox;
using System;

namespace Pace;

public sealed class LookCamera : Component
{
    [RequireComponent] public CameraComponent Camera { get; private set; }

    protected override void OnAwake()
    {
        WorldPosition = Pawn.Local.Head.WorldPosition + Settings.Plane.Normal * 1000f;
    }

    protected override void OnPreRender()
    {
        var position = Pawn.Local.Head.WorldPosition;
        var offset = (Pawn.Local.MousePosition - position) / 2f;
        var targetPosition = position + offset.ClampLength( 150f ) + Settings.Plane.Normal * 1000f;

        Camera.FieldOfView = Screen.CreateVerticalFieldOfView( 30f );
        WorldRotation = (-Settings.Plane.Normal).EulerAngles;
        WorldPosition = Vector3.Lerp( targetPosition, WorldPosition, MathF.Exp( -25f * Time.Delta ) );
    }
}