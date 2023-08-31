using Sandbox;
using System.Collections.Generic;

namespace Pace;

public partial class Inventory : EntityComponent<Pawn>, ISingletonComponent
{
	[Net, Predicted] public Weapon ActiveWeapon { get; set; }
	[Net] private IList<Weapon> Weapons { get; set; }
	public Weapon this[int i] => Weapons[i];
	public int Count => Weapons.Count;
	private Weapon _lastActive;

	public Inventory()
	{
		Weapons = new List<Weapon>( new Weapon[10] );
	}

	public void Simulate( IClient cl )
	{
		if ( Entity.ActiveChildInput.IsValid() )
			SetActive( Entity.ActiveChildInput );

		if ( _lastActive != ActiveWeapon )
		{
			_lastActive?.OnHolster();
			_lastActive = ActiveWeapon;
			ActiveWeapon?.OnEquip( Entity );
		}

		if ( !ActiveWeapon.IsValid() || !ActiveWeapon.IsAuthority )
			return;

		if ( ActiveWeapon.TimeSinceDeployed > ActiveWeapon.Definition.DeployTime )
			ActiveWeapon.Simulate( cl );
	}

	public bool Add( Weapon weapon, bool makeActive = false )
	{
		Game.AssertServer();

		if ( !weapon.IsValid() )
			return false;

		if ( weapon.Owner is not null )
			return false;

		if ( !CanAdd( weapon ) )
			return false;

		weapon.Owner = Entity;
		weapon.SetParent( Entity, true );
		Weapons[weapon.Definition.Slot] = weapon;

		if ( makeActive )
			SetActive( weapon );

		return true;
	}

	public bool CanAdd( Weapon carriable )
	{
		if ( Weapons[carriable.Definition.Slot] is not null )
			return false;

		return true;
	}

	public bool Contains( Weapon entity )
	{
		return Weapons.Contains( entity );
	}

	public void Pickup( Weapon carriable )
	{
		if ( Add( carriable ) )
			Sound.FromEntity( "pickup_weapon", Entity );
	}

	public bool SetActive( Weapon carriable )
	{
		if ( ActiveWeapon == carriable )
			return false;

		if ( carriable.IsValid() && !Contains( carriable ) )
			return false;

		ActiveWeapon = carriable;

		return true;
	}

	public void DeleteContents()
	{
		Game.AssertServer();

		for ( var i = 0; i < Weapons.Count; i++ )
		{
			Weapons[i]?.Delete();
			Weapons[i] = null;
		}

		ActiveWeapon = null;
	}
}
