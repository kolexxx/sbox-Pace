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
		var dir = (Pawn.MousePosition - position).Normal;
		var startPosition = position - dir * 50f;

		var tr = Trace.Ray( startPosition, position )
			.Ignore( this )
			.Ignore( Owner )
			.Run();

		new Projectile( new Ray( tr.EndPosition, dir ), Pawn );
	}
}
