using Sandbox;
using Sandbox.Citizen;

namespace Pace;

public class Equipment : Component
{
    /// <summary>
    /// A reference to the equipment's model renderer.
    /// </summary>
    [Property] public EquipmentResource Resource { get; private set; }
    [Property, ImageAssetPath] public string Icon { get; private set; }
    [Property, Group( "Components" )] public SkinnedModelRenderer ModelRenderer { get; private set; }
    [Property, Group( "Components" )] public HitscanBullet PrimaryAction { get; private set; }
    [Property, Group( "Animation" )] public CitizenAnimationHelper.HoldTypes HoldType { get; private set; } = CitizenAnimationHelper.HoldTypes.Rifle;
    [Property, Group( "Animation" )] public CitizenAnimationHelper.Hand Handedness { get; private set; } = CitizenAnimationHelper.Hand.Right;
    [Property, Group( "Stats" )] public int Slot { get; private set; } = 0;
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

    public void OnEquip( Pawn pawn )
    {
        Owner = pawn;
        TimeSinceDeployed = 0f;
        Owner.BodyRenderer.Set( "b_deploy", true );
    }

    public void OnHolster()
    {
    }
}
