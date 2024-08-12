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
    /// Is this equipment currently equiped by the player?
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// How long since we equiped this weapon.
    /// </summary>
    public TimeSince TimeSinceDeployed { get; private set; }

    protected override void OnUpdate()
    {
        ModelRenderer.Enabled = IsActive;
    }

    protected override void OnFixedUpdate()
    {
        if ( IsProxy )
            return;

        if ( !IsActive )
            return;

        if ( TimeSinceDeployed < DeployTime )
            return;

        if ( Input.Down( "Attack1" ) )
            PrimaryAction?.InputAction();
    }

    public void OnEquip()
    {
        IsActive = true;
        TimeSinceDeployed = 0f;

        GameObject.Root.Components.Get<Pawn>().AnimationHelper.TriggerDeploy();
		var c = GameObject.Root.Components.Get<SkinnedModelRenderer>( FindMode.InChildren );
		ModelRenderer.BoneMergeTarget = c;
    }

    public void OnHolster()
    {
        IsActive = false;
    }
}
