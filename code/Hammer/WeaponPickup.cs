using Editor;
using Sandbox;

namespace Pace;

[Category( "Pickups" )]
[ClassName( "pickup_weapon" )]
[HammerEntity]
[Title("Weapon Pickup")]
public partial class WeaponPickup : BasePickup
{
	/// <summary>
	/// The weapon that will be spawned.
	/// </summary>
	[Net, Property] public WeaponDefinition Weapon { get; set; }

	protected override string ModelPath => Weapon.ModelPath;

	public override void Touch( Entity other )
	{
		if ( !Game.IsServer || other is not Pawn player || !TimeUntilRespawn )
			return;

		if ( player.Inventory[Weapon.Slot] is not null )
		{
			var weapon = player.Inventory[Weapon.Slot];

			if ( weapon.ReserveAmmo >= 2 * weapon.Definition.ClipSize )
				return;

			TimeUntilRespawn = 10f;
			weapon.ReserveAmmo = 2 * weapon.Definition.ClipSize;
			Sound.FromEntity( "pickup_weapon", player );
		}
		else
		{
			TimeUntilRespawn = 10f;
			player.Inventory.Pickup( TypeLibrary.Create<Weapon>( Weapon.ClassName ) );
		}
	}
}
