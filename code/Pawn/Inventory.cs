using Sandbox;
using Sandbox.Diagnostics;

namespace Pace;

public class Inventory : Component, Component.ITriggerListener
{
	/// <summary>
	/// A reference to our Pawn component.
	/// </summary>
	[Property] public Pawn Pawn { get; private set; }

	/// <summary>
	/// The equipment we have currently equiped.
	/// </summary>
	[Sync] public Equipment ActiveEquipment { get; private set; }

	/// <summary>
	/// The equipment we want to equip next. If null, we will holster.
	/// </summary>
	[Sync, HostSync] public Equipment InputEquipment { get; private set; }

	/// <summary>
	/// A list of all our inventory slots.
	/// </summary>
	[HostSync] public NetList<Equipment> Equipment { get; private set; } = new();

	public Equipment this[int i] => Equipment[i];

	protected override void OnAwake()
	{
		for ( var i = 0; i < 4; i++ )
			Equipment.Add( null );
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		var currentSlot = ActiveEquipment?.Slot ?? 1;

		foreach ( var eq in Equipment )
		{
			if ( !eq.IsValid() )
				continue;

			var action = GetInputString( eq.Slot );

			if ( Input.Pressed( action ) )
			{
				InputEquipment = eq;
				currentSlot = eq.Slot;
			}
		}

		if ( Input.MouseWheel == 0 )
			return;

		var incr = (int)Input.MouseWheel.y.Clamp( -1, 1 );

		for ( var i = currentSlot + incr; i != currentSlot; i += incr )
		{
			if ( i < 0 )
				i = Equipment.Count - 1;

			if ( i >= Equipment.Count )
				i = 0;

			if ( !Equipment[i].IsValid() )
				continue;

			InputEquipment = Equipment[i];
			break;
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
			return;

		if ( InputEquipment != ActiveEquipment )
		{
			ActiveEquipment?.OnHolster();
			ActiveEquipment = InputEquipment;
			ActiveEquipment?.OnEquip( Pawn );
		}
	}

	/// <summary>
	/// Try to add equipment to our inventory.
	/// </summary>
	/// <param name="eq">Equipment we want to add.</param>
	/// <param name="makeActive">If true, equip if added.</param>
	/// <returns>True if we added equipment, false otherwise.</returns>
	public bool Add( Equipment eq, bool makeActive = false )
	{
		Assert.True( Networking.IsHost );

		if ( !CanAdd( eq ) )
			return false;

		eq.GameObject.SetParent( GameObject );
		eq.Network.AssignOwnership( Network.OwnerConnection );
		Equipment[eq.Slot] = eq;

		if ( makeActive )
			InputEquipment = eq;

		return true;
	}

	public bool CanAdd( Equipment eq )
	{
		if ( Equipment[eq.Slot].IsValid() )
			return false;

		return true;
	}

	public void Clear()
	{
		if ( !Networking.IsHost )
			return;

		foreach ( var eq in Equipment )
		{
			if ( !eq.IsValid() )
				continue;

			eq.GameObject.Destroy();
			eq.Enabled = false;
		}
	}

	void ITriggerListener.OnTriggerEnter( Collider other )
	{
		if ( !Networking.IsHost )
			return;

		if ( !other.Components.TryGet<Pickup>( out var pickup ) )
			return;

		if ( !pickup.SpawnedObject.IsValid() )
			return;

		if ( !pickup.SpawnedObject.Components.TryGet<Equipment>( out var eq ) )
			return;

		Add( eq );
	}

	public static string GetInputString( int slot )
	{
		return "Slot" + slot.ToString();
	}
}
