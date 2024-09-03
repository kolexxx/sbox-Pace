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
    [Property, ReadOnly] public string Name => Resource.Name;

    /// <summary>
    /// An image that will be used in UI elements.
    /// </summary>
    [Property, ReadOnly, ImageAssetPath] public string Icon => Resource.Icon;

    /// <summary>
    /// A reference to our model renderer.
    /// </summary>
    [Property, Group( "Components" )] public SkinnedModelRenderer Renderer { get; private set; }

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
    [HostSync] public Pawn Owner { get; set; }

    /// <summary>
    /// Is this equipment currently equiped by the player?
    /// </summary>
    public bool IsActive {get; private set;}

    /// <summary>
    /// How long since we equiped this weapon.
    /// </summary>
    public TimeSince TimeSinceDeployed { get; private set; }

    /// <summary>
    /// Have we finished deploying?
    /// </summary>
    public bool IsDeployed => IsActive && TimeSinceDeployed > DeployTime;

    protected override void OnFixedUpdate()
    {
        Renderer.Enabled = !Owner.IsValid() || IsActive;
        Renderer.BoneMergeTarget = Owner.IsValid() ? Owner.Body.Renderer : null;
    }

    [Broadcast( NetPermission.OwnerOnly )]
    public void Deploy()
    {
        IsActive = true;
        TimeSinceDeployed = 0f;
        Owner?.Body.Renderer.Set( "b_deploy", true );
    }

    public void Holster()
    {
        IsActive = false;
    }

    /// <summary>
    /// Called when added to a player's inventory.
    /// </summary>
    [Broadcast( NetPermission.HostOnly )]
    public void CarryStart(Pawn owner)
    {
        Owner = owner;
    }
}
