using Sandbox;
using System;
using System.Collections.Generic;

namespace Pace;

public class Inventory : Component, Component.ITriggerListener
{
    public Equipment ActiveEquipment { get; private set; }
    public Equipment InputEquipment { get; private set; }
    public List<Equipment> Equipment { get; private set; } = new List<Equipment>( new Equipment[4] );
	public Equipment this[int i] => Equipment[i];

	protected override void OnEnabled()
    {
        var go = GameObject.Clone( "prefabs/pistol.prefab" );
        Add( go.Components.Get<Equipment>() );
        go = GameObject.Clone( "prefabs/rifle.prefab" );
        Add( go.Components.Get<Equipment>() );
    }

	protected override void OnUpdate()
	{
		var currentSlot = ActiveEquipment?.Slot ?? 1;

		foreach ( var eq in Equipment )
		{
			if ( !eq.IsValid() )
				continue;

			var action = GetInputString(eq.Slot);
			
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
        if ( InputEquipment != ActiveEquipment )
        {
            ActiveEquipment?.OnHolster();
            ActiveEquipment = InputEquipment;
            ActiveEquipment?.OnEquip();
        }
    }

    public bool Add( Equipment eq, bool makeActive = false )
    {
        if ( !Networking.IsHost )
            return false;

		if ( !CanAdd( eq ) )
			return false;

        eq.GameObject.SetParent( GameObject );
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

    public static string GetInputString( int slot )
    {
        return "Slot" + slot.ToString();
    }

	public override int GetHashCode()
	{
		var result = 0;

		foreach ( var eq in Equipment )
			result = HashCode.Combine( result, eq.IsValid(), eq?.IsActive );

		return result;
	}
}
