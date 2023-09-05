using Sandbox;

namespace Pace;

[ClassName( "rocketlauncher" )]
public class RocketLauncher : Weapon
{
	protected override void PrimaryAttack()
	{
		Pawn.SetAnimParameter( "b_attack", true );
		Pawn.PlaySound( Definition.FireSound );
		ShootEffects();

		if ( Prediction.FirstTime )
			FireProjectile();
	}

	private void FireProjectile()
	{
		Game.SetRandomSeed( Time.Tick );

		var position = (GetAttachment( "muzzle" ) ?? new Transform()).Position;

		new Projectile( new Ray( position, (Pawn.MousePosition - position).Normal ), Pawn );
	}
}
