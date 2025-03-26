using Sandbox;

namespace Pace;

public sealed class Barrier : Component, IPlayerEvent
{
    [Property] public GameObject BarrierObject { get; private set; }
    [Property] public HealthComponent HealthComponent { get; private set; }
    [Property] public ModelRenderer Renderer { get; private set; }
    [Property] public PlayerController PlayerController { get; private set; }
    [Property] public Color DefaultColor { get; private set; }
    [Property] public Color DamagedColor { get; private set; }
    [Property] public float Cooldown { get; private set; }
    [Property] public float HealingRate { get; private set; }
    [Property] public float DeployedScale { get; private set; }
    [Property] public float DeployTime { get; private set; }
    [Property, Sync] public bool IsDeployed { get; private set; }
    [Property, Sync( SyncFlags.FromHost )] public bool IsDestroyed { get; private set; }
    [Property, Sync] public bool IsDeactivating { get; private set; }
    [Sync( SyncFlags.Interpolate )] public float Scale { get; private set; }
    public TimeSince TimeSinceDeploy { get; private set; }
    public TimeSince TimeSinceDeactivation { get; private set; }
    public TimeSince TimeSinceDestroyed => HealthComponent.LastDamage.TimeSince;
    private float _lastScale;

    protected override void OnFixedUpdate()
    {
        if ( Networking.IsHost )
        {
            if ( HealthComponent.Health <= 0 )
                IsDestroyed = true;

            if ( IsDestroyed && TimeSinceDestroyed >= Cooldown )
                IsDestroyed = false;

            if ( IsDestroyed || !IsDeployed )
                HealthComponent.Heal( HealingRate * Time.Delta );
        }

        if ( !IsProxy )
        {
            if ( IsDestroyed || !IsDeployed )
            {
                IsDeployed = false;
                IsDeactivating = false;
                Scale = 0;
            }

            if ( !IsDeployed && !IsDestroyed && TimeSinceDeactivation > Cooldown && PlayerController.IsCrouching && PlayerController.IsGrounded )
            {
                IsDeployed = true;
                TimeSinceDeploy = 0;
            }
            else if ( IsDeployed && !IsDeactivating && (!PlayerController.IsCrouching || !PlayerController.IsGrounded) )
            {
                IsDeactivating = true;
                TimeSinceDeactivation = 0;
            }
            else if ( IsDeactivating && TimeSinceDeactivation > DeployTime )
            {
                IsDeployed = false;
                IsDeactivating = false;
            }

            if ( IsDeployed && !IsDeactivating )
            {
                Scale = float.Lerp( 0, DeployedScale, float.Min( TimeSinceDeploy / DeployTime, 1f ) );
                _lastScale = WorldScale.x;
            }
            else if ( IsDeactivating )
                Scale = float.Lerp( _lastScale, 0, float.Min( TimeSinceDeactivation / DeployTime, 1f ) );
        }

        BarrierObject.Enabled = IsDeployed && !IsDestroyed;
        BarrierObject.WorldScale = Scale;
    }

    protected override void OnPreRender()
    {
        var health = HealthComponent.Health;
        var color = Color.Lerp( DamagedColor, DefaultColor, health / HealthComponent.MaxHealth );

        Renderer.Tint = Renderer.Tint.LerpTo( color, 5f * Time.Delta );
    }

    void IPlayerEvent.OnPlayerSpawned( Player player )
    {
        if ( Networking.IsHost )
            HealthComponent.Heal( HealthComponent.MaxHealth );

        IsDeployed = false;
        IsDestroyed = false;
        IsDeactivating = false;
    }

    void IPlayerEvent.OnPlayerKilled( Player player )
    {
        IsDeployed = false;
        IsDestroyed = false;
        IsDeactivating = false;
    }
}