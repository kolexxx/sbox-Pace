using Sandbox;
using System;

namespace Pace;

public sealed class LookCamera : Component
{
    protected override void OnStart()
    {
        WorldPosition = Pawn.Local.Head.WorldPosition + Settings.Plane.Normal * 1000f;
    }

    protected override void OnPreRender()
    {
        var camera = Components.Get<CameraComponent>( FindMode.InSelf );

        if ( camera is null )
            return;

        var position = Pawn.Local.Head.WorldPosition;
        var offset = (Pawn.Local.MousePosition - position) / 2f;
        var targetPosition = position + offset.ClampLength( 150f ) + Settings.Plane.Normal * 1000f;

        camera.FieldOfView = Screen.CreateVerticalFieldOfView( 30f );
        WorldRotation = (-Settings.Plane.Normal).EulerAngles;
        WorldPosition = Vector3.Lerp( targetPosition, camera.WorldPosition, MathF.Exp( -25f * Time.Delta ) );
    }
}