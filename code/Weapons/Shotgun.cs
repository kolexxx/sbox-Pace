using Sandbox;

namespace Pace;

[ClassName( "Shotgun" )]
public class Shotgun : Weapon 
{
	private bool _attackedDuringReload = false;

	public override void OnEquip( Pawn player )
	{
		base.OnEquip( player );

		_attackedDuringReload = false;
		TimeSinceReload = 0f;
	}

	protected override bool CanReload()
	{
		if ( !base.CanReload() )
			return false;

		var rate = Definition.PrimaryRate;
		if ( rate <= 0 )
			return true;

		return TimeSincePrimaryAttack > (1 / rate);
	}

	public override void Simulate( IClient owner )
	{
		base.Simulate( owner );

		if ( IsReloading && Input.Pressed( InputAction.PrimaryAttack ) )
			_attackedDuringReload = true;
	}

	protected override void OnReloadFinish()
	{
		IsReloading = false;
		TimeSincePrimaryAttack = 0;

		TakeAmmo( 1 );

		if ( !_attackedDuringReload && AmmoInClip < Definition.ClipSize && ReserveAmmo > 0 )
			Reload();

		_attackedDuringReload = false;
	}
}
