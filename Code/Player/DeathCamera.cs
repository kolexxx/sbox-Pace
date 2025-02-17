using Sandbox;
using Sandbox.Utility;

namespace Pace;

/// <summary>
/// Zoom into the killer after some time.
/// </summary>
public sealed class DeathCamera : Component
{
    private UI.DeathInfo _panel;
    private const float HoldTime = 0.5f;
    private const float ArrivalTime = 2f;
    private Player _killer;
    private Vector3 _startPos;
    private RealTimeSince _timeSinceDeath;

    protected override void OnStart()
    {
        _killer = Player.Local.HealthComponent.LastDamage.Attacker as Player;
        _startPos = WorldPosition;
        _timeSinceDeath = 0f;
    }

    protected override void OnDestroy()
    {
        _panel.Delete();
    }

    protected override void OnPreRender()
    {
        if ( _timeSinceDeath <= HoldTime )
            return;

        var frac = (_timeSinceDeath.Relative - HoldTime) / ArrivalTime;
        var targetPos = _killer.Head.WorldPosition + Settings.Plane.Normal * 500f;

        if ( frac >= 0.75f && _panel is null )
            _panel = UI.Hud.RootPanel.AddChild<UI.DeathInfo>();

        if ( frac >= 1 )
        {
            WorldPosition = targetPos;
            return;
        }

        var remap = Easing.EaseInOut( frac );
        WorldPosition = Vector3.Lerp( _startPos, targetPos, remap );
    }
}