using Sandbox;
using Sandbox.Citizen;

namespace Pace;

/// <summary>
/// A GameObject that can be put in a player's <see cref="Inventory"/>
/// </summary>
public sealed class Equipment : Component
{
    /// <summary>
    /// An image that will be used in UI elements.
    /// </summary>
    [Property, ImageAssetPath] public string Icon { get; private set; }

    /// <summary>
    /// A reference to our model renderer.
    /// </summary>
    [Property, Group( "Components" )] public SkinnedModelRenderer ModelRenderer { get; private set; }

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
    [Property, Group( "Stats" )] public int Slot { get; private set; } = 0;

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
    public bool IsActive => Owner?.Inventory.ActiveEquipment == this;

    /// <summary>
    /// How long since we equiped this weapon.
    /// </summary>
    public TimeSince TimeSinceDeployed { get; private set; }

    /// <summary>
    /// Have we finished deploying?
    /// </summary>
    public bool IsDeployed => IsActive && TimeSinceDeployed > DeployTime;

    protected override void OnUpdate()
    {
        ModelRenderer.Enabled = !Owner.IsValid() || IsActive;
        ModelRenderer.BoneMergeTarget = Owner.IsValid() ? Owner.BodyRenderer : null;
    }

    [Broadcast( NetPermission.OwnerOnly )]
    public void Deploy()
    {
        TimeSinceDeployed = 0f;
        Owner.BodyRenderer.Set( "b_deploy", true );
    }

    public void Holster()
    {
    }
}
