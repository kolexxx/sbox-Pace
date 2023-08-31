using Sandbox;

namespace Pace;

public class PawnAnimator : EntityComponent<Pawn>, ISingletonComponent
{
	public void Simulate()
	{
		var helper = new CitizenAnimationHelper( Entity );
		helper.WithVelocity( Entity.Velocity );
		helper.WithLookAt( Entity.EyePosition + Entity.EyeRotation.Forward * 100, 0, 0f, 0.05f );
		helper.HoldType = Entity.Inventory.ActiveWeapon?.Definition.HoldType ?? CitizenAnimationHelper.HoldTypes.None;
		helper.IsGrounded = Entity.GroundEntity.IsValid();

		if ( Entity.Controller.HasEvent( "jump" ) )
			helper.TriggerJump();
	}
}
