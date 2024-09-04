using Sandbox;
using System;

namespace Pace;

public class AmmoComponent : Component
{
    [Property, Group( "Components" )] public Equipment Equipment { get; private set; }
    [Property, Group( "Stats" )] public float ReloadTime { get; private set; } = 0.6f;
    [Property, Group( "Stats" )] public int ClipSize { get; private set; } = 30;

    /// <summary>
    /// How much ammo is currently in the clip.
    /// </summary>
    public int AmmoInClip { get; set; }

    /// <summary>
    /// How much ammo is in reserve.
    /// </summary>
    [Sync] public int ReserveAmmo { get; set; }

    /// <summary>
    /// How long since we started reloading.
    /// </summary>
    public TimeSince TimeSinceReload { get; private set; }

    /// <summary>
    /// Are we still reloading this weapon?
    /// </summary>
    public bool IsReloading { get; private set; }

    /// <summary>
    /// Can we pickup anymore ammo?
    /// </summary>
    public bool IsReserveFull => ReserveAmmo >= 2 * ClipSize;

    protected override void OnAwake()
    {
        AmmoInClip = ClipSize;
        ReserveAmmo = 2 * ClipSize;
    }

    protected override void OnFixedUpdate()
    {
        if ( IsProxy )
            return;

        if ( !Equipment.IsDeployed )
        {
            if ( IsReloading )
                CancelReload();

            return;
        }

        if ( CanReload() )
        {
            Reload();
            return;
        }

        if ( IsReloading && TimeSinceReload >= ReloadTime )
            FinishReload();
    }

    [Broadcast( NetPermission.OwnerOnly )]
    protected void Reload()
    {
        if ( IsReloading )
            return;

        if ( !IsProxy )
        {
            TimeSinceReload = 0;
            IsReloading = true;
        }

        Equipment.Owner.PawnBody.Renderer.Set( "b_reload", true );
    }

    protected bool CanReload()
    {
        if ( IsReloading )
            return false;

        if ( AmmoInClip >= ClipSize || ReserveAmmo <= 0 )
            return false;

        if ( AmmoInClip == 0 )
            return true;

        return Input.Pressed( "reload" );
    }

    protected void FinishReload()
    {
        IsReloading = false;
        TakeAmmo( ClipSize - AmmoInClip );
    }

    [Broadcast( NetPermission.OwnerOnly )]
    protected void CancelReload()
    {
        IsReloading = false;
        Equipment.Owner.PawnBody.Renderer.Set( "b_reload", false );
    }

    [Authority( NetPermission.HostOnly )]
    public void RefillReserve()
    {
        ReserveAmmo = 2 * ClipSize;
    }

    /// <summary>
    /// Takes ammo from the reserve and adds it in our clip.
    /// </summary>
    /// <param name="amount"></param>
    protected void TakeAmmo( int amount )
    {
        var ammo = Math.Min( ReserveAmmo, amount );

        AmmoInClip += ammo;
        ReserveAmmo -= ammo;
    }
}