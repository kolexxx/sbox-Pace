using System.Dynamic;
using Sandbox;
using Sandbox.Citizen;

namespace Pace;

/// <summary>
/// A GameObject that can be put in a player's <see cref="Inventory"/>
/// </summary>
public sealed class Equipment : Component
{
    [Property] public EquipmentResource Resource { get; private set; }

    /// <summary>
    /// A name that will be used in UI elements.
    /// </summary>
    [Property, ReadOnly] public string Name { get; private set; }

    /// <summary>
    /// An image that will be used in UI elements.
    /// </summary>
    [Property, ReadOnly, ImageAssetPath] public string Icon { get; private set; }

    /// <summary>
    /// A reference to our model renderer.
    /// </summary>
    [Property, Group( "Components" )] public SkinnedModelRenderer Renderer { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    [Property, Group( "Animation" )] public GameObject Handle { get; private set; }

    /// <summary>
    /// How are we holding this equipment?
    /// </summary>
    [Property, Group( "Animation" )] public CitizenAnimationHelper.HoldTypes HoldType { get; private set; } = CitizenAnimationHelper.HoldTypes.Rifle;

    /// <summary>
    /// In which hand are we holding this equipment?
    /// </summary>
    [Property, Group( "Animation" )] public CitizenAnimationHelper.Hand Handedness { get; private set; } = CitizenAnimationHelper.Hand.Right;

    /// <summary>
    /// What inventory slot does this equipment occupy?
    /// </summary>
    [Property, ReadOnly, Group( "Stats" )] public int Slot => Resource.Slot;

    /// <summary>
    /// How long does it take to deploy this equipment?
    /// </summary>
    [Property, Group( "Stats" )] public float DeployTime { get; private set; } = 0.6f;

    /// <summary>
    /// The holder of this equipment.
    /// </summary>
    [Sync( SyncFlags.FromHost )] public Player Owner { get; set; }

    /// <summary>
    /// Is this equipment currently equiped by the player?
    /// </summary>
    public bool IsActive => Owner?.Inventory.ActiveEquipment == this;

    /// <summary>
    /// How long since we equiped this weapon.
    /// </summary>
    public TimeSince TimeSinceDeployed { get; private set; }

    /// <summary>
    /// Have we finished deploying?
    /// </summary>
    public bool IsDeployed => IsActive && TimeSinceDeployed > DeployTime;

    protected override void OnStart()
    {
        Name = Resource.Name;
        Icon = Resource.Icon;
    }

    protected override void OnPreRender()
    {
        if ( !Owner.IsValid() )
        {
            Renderer.Enabled = false;
            return;
        }

        LocalPosition = Handle.LocalPosition;
        Renderer.Enabled = IsActive;
    }

    [Rpc.Broadcast]
    public void Deploy()
    {
        TimeSinceDeployed = 0f;
        Owner?.Renderer?.Set( "b_deploy", true );
    }

    public void Holster() { }

    /// <summary>
    /// Called when added to a player's inventory.
    /// </summary>
    [Rpc.Broadcast]
    public void CarryStart( Player owner, bool makeActive = false )
    {
        if ( !IsProxy && makeActive )
            owner.Inventory.InputEquipment = this;

        Owner = owner;
    }
}
