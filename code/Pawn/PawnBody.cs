using Sandbox;

namespace Pace;

public class PawnBody : Component
{
    [Property, Group( "Components" )] public CapsuleCollider Collider { get; private set; }
    [Property, Group( "Components" )] public ModelPhysics Physics { get; private set; }
    [Property, Group( "Components" )] public SkinnedModelRenderer Renderer { get; private set; }
    [Property, Group( "Components" )] public UI.InfoPlate InfoPlate { get; private set; }

    public void SetRagdoll( bool ragdoll )
    {
        Collider.Enabled = !ragdoll;
        Physics.Enabled = ragdoll;
        Renderer.UseAnimGraph = !ragdoll;
        InfoPlate.Enabled = !ragdoll;
        Tags.Set( "ragdoll", ragdoll );

        if ( !ragdoll )
        {
            GameObject.Transform.LocalPosition = Vector3.Zero;
            GameObject.Transform.LocalRotation = Rotation.Identity;
        }

        Transform.ClearInterpolation();
    }

    public void ApplyImpulses( Vector3 position, Vector3 force )
    {
        if ( !Physics.IsValid() || !Physics.PhysicsGroup.IsValid() )
            return;

        foreach ( var body in Physics.PhysicsGroup.Bodies )
        {
            body.ApplyImpulseAt( position, force );
        }
    }
}