using Sandbox;
using System;

namespace Pace;

public enum ReloadType
{
    Magazine,
    Sequential,
    NoReload
}

public class AmmoComponent : Component
{
    [Property, Group( "Components" )] public Equipment Equipment { get; private set; }
    [Property, Group( "Components" )] public FireComponent FireComponent { get; private set; }
    [Property] public ReloadType ReloadType { get; private set; }
    [Property, Group( "Stats" )] public float ReloadTime { get; private set; } = 0.6f;
    [Property, Group( "Stats" )] public int MaxLoadedAmmo { get; private set; } = 30;

    /// <summary>
    /// How much ammo is currently in the clip.
    /// </summary>
    public int LoadedAmmo { get; set; }

    /// <summary>
    /// How much ammo is in reserve.
    /// </summary>
    [Sync] public int ReserveAmmo { get; private set; }

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
    public bool IsReserveFull => ReserveAmmo >= 2 * MaxLoadedAmmo;

    /// <summary>
    /// Used for sequential reloads. Do we want to stop reloading after a reload?
    /// </summary>
    private bool _requestCancel;

    protected override void OnAwake()
    {
        LoadedAmmo = MaxLoadedAmmo;
        ReserveAmmo = 2 * MaxLoadedAmmo;
    }

    protected override void OnFixedUpdate()
    {
        if ( IsProxy )
            return;

        if ( ReloadType == ReloadType.NoReload )
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

        if ( IsReloading )
        {
            if ( Input.Pressed( "attack1" ) )
                _requestCancel = true;

            if ( TimeSinceReload >= ReloadTime )
                FinishReload();
        }
    }

    [Rpc.Broadcast]
    protected void Reload()
    {
        if ( IsReloading )
            return;

        if ( !IsProxy )
        {
            TimeSinceReload = 0;
            IsReloading = true;
        }

        Equipment.Owner.Renderer.Set( "b_reload", true );
    }

    protected bool CanReload()
    {
        if ( ReloadType == ReloadType.NoReload )
            return false;

        if ( IsReloading )
            return false;

        if ( LoadedAmmo >= MaxLoadedAmmo || ReserveAmmo <= 0 )
            return false;

        if ( FireComponent.IsOnCooldown )
            return false;

        if ( LoadedAmmo == 0 )
            return true;

        return Input.Pressed( "reload" );
    }

    protected void FinishReload()
    {
        IsReloading = false;

        if ( ReloadType == ReloadType.Magazine )
        {
            TakeAmmo( MaxLoadedAmmo - LoadedAmmo );
            return;
        }

        TakeAmmo( 1 );

        if ( !_requestCancel && LoadedAmmo < MaxLoadedAmmo && ReserveAmmo > 0 )
            Reload();

        _requestCancel = false;
    }

    [Rpc.Broadcast]
    protected void CancelReload()
    {
        _requestCancel = false;
        IsReloading = false;
        Equipment.Owner.Renderer.Set( "b_reload", false );
    }

    [Rpc.Owner]
    public void RefillReserve()
    {
        if ( ReloadType != ReloadType.NoReload )
            ReserveAmmo = 2 * MaxLoadedAmmo;
        else
            LoadedAmmo = MaxLoadedAmmo;
    }

    /// <summary>
    /// Takes ammo from the reserve and adds it in our clip.
    /// </summary>
    /// <param name="amount"></param>
    protected void TakeAmmo( int amount )
    {
        var ammo = Math.Min( ReserveAmmo, amount );

        LoadedAmmo += ammo;
        ReserveAmmo -= ammo;
    }
}