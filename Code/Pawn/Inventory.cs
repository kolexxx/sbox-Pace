using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Rendering;

namespace Pace;

public class Inventory : Component, Component.ITriggerListener
{
	/// <summary>
	/// A reference to our Pawn component.
	/// </summary>
	[Property] public Pawn Pawn { get; private set; }

	/// <summary>
	/// A sound that will be played when we pick up a weapon.
	/// </summary>
	[Property] public SoundEvent PickupSound { get; private set; }

	/// <summary>
	/// The equipment we have currently equiped.
	/// </summary>
	[Sync] public Equipment ActiveEquipment { get; private set; }

	/// <summary>
	/// The equipment we want to equip next. If null, we will holster.
	/// </summary>
	[Sync] public Equipment InputEquipment { get; set; }

	/// <summary>
	/// A list of all our inventory slots.
	/// </summary>
	[Sync( SyncFlags.FromHost )] public NetList<Equipment> Equipment { get; private set; } = new();

	/// <summary>
	/// A shorthand to our equipment list indexer.
	/// </summary>
	/// <param name="i">The slot number.</param>
	/// <returns>The equipment at the specified slot.</returns>
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

		UpdateActive();
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
			return;

		if ( InputEquipment != ActiveEquipment )
		{
			ActiveEquipment?.Holster();
			ActiveEquipment = InputEquipment;
			ActiveEquipment?.Deploy();
		}
	}

	/// <summary>
	/// Try to add equipment to our inventory.
	/// </summary>
	/// <param name="eq">Equipment we want to add.</param>
	/// <param name="makeActive">If true, equip if added.</param>
	/// <returns>True if we added equipment, false otherwise.</returns>
	public bool Add( EquipmentResource eq, bool makeActive = false )
	{
		Assert.True( Networking.IsHost );

		if ( !CanAdd( eq ) )
			return false;

		var go = eq.Prefab.Clone( new CloneConfig()
		{
			Name = eq.Name,
			Parent = GameObject,
			Transform = new Transform(),
		} );

		go.NetworkSpawn( Network.Owner );

		Equipment[eq.Slot] = go.Components.Get<Equipment>();
		Equipment[eq.Slot].CarryStart( Pawn, makeActive );

		return true;
	}

	public bool CanAdd( EquipmentResource eq )
	{
		if ( Equipment[eq.Slot].IsValid() )
			return false;

		return true;
	}

	/// <summary>
	/// Destroys every equipment in our inventory.
	/// </summary>
	public void Clear()
	{
		Assert.True( Networking.IsHost );

		foreach ( var eq in Equipment )
		{
			if ( !eq.IsValid() )
				continue;

			eq.GameObject.DestroyImmediate();
			eq.Enabled = false;
		}
	}

	/// <summary>
	/// Update's the <see cref="ActiveEquipment"/> depending on input.
	/// </summary>
	private void UpdateActive()
	{
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

	void ITriggerListener.OnTriggerEnter( Collider other )
	{
		if ( !Networking.IsHost )
			return;

		if ( !other.Components.TryGet<WeaponPickup>( out var pickup ) )
			return;

		var eq = pickup.EquipmentToSpawn;

		if ( Add( eq ) )
		{
			pickup.OnPickedUp();
			PickupEffects();
			return;
		}

		if ( !Equipment[eq.Slot].Components.TryGet<AmmoComponent>( out var ammo ) )
			return;

		if ( ammo.IsReserveFull )
			return;

		pickup.OnPickedUp();
		ammo.RefillReserve();
	}

	[Rpc.Owner]
	private void PickupEffects()
	{
		Sound.Play( PickupSound );
	}

	public static string GetInputString( int slot )
	{
		return "Slot" + slot.ToString();
	}
}
