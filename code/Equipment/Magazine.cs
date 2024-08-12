using Sandbox;
using System;

namespace Pace;

public class Magazine : Component
{
    [Property, Group( "Stats" )] public float ReloadTime { get; set; } = 0.6f;
    [Property, Group( "Stats" )] public int ClipSize { get; set; } = 30;

    /// <summary>
    /// How much ammo is currently in the clip.
    /// </summary>
    public int AmmoInClip { get; set; }

    /// <summary>
    /// How much ammo is in reserve.
    /// </summary>
    public int ReserveAmmo { get; set; }

    /// <summary>
    /// How long since we started reloading.
    /// </summary>
    public TimeSince TimeSinceReload { get; private set; }

    /// <summary>
    /// Are we still reloading this weapon?
    /// </summary>
    public bool IsReloading { get; private set; }

    protected override void OnAwake()
    {
        AmmoInClip = ClipSize;
        ReserveAmmo = 2 * ClipSize;
    }

    protected override void OnFixedUpdate()
    {
        if ( IsProxy )
            return;

        if ( CanReload() )
        {
            Reload();
            return;
        }

        if ( IsReloading && TimeSinceReload >= ReloadTime )
            OnReloadFinish();
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

        GameObject.Root.Components.Get<Pawn>()?.Body.Components.Get<SkinnedModelRenderer>()?.Set( "b_reload", true );
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

    protected void OnReloadFinish()
    {
        IsReloading = false;
        TakeAmmo( ClipSize - AmmoInClip );
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