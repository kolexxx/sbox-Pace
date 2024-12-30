using Sandbox;

namespace Pace;

public sealed class Deathcam : Component
{
    public Pawn Killer => Pawn.Local.HealthComponent.LastDamage.Attacker as Pawn;
    private bool _arrived;
    private Vector3 _velocity = 0;
    private RealTimeSince _timeSinceDeath = 0;

    protected override void OnPreRender()
    {
        if ( _timeSinceDeath < 0.5f )
            return;

        var targetPos = Killer.Head.WorldPosition + Settings.Plane.Normal * 750f;
        var currentPos = WorldPosition;
        var diff = targetPos - currentPos;

        if ( _arrived )
        {
            WorldPosition = targetPos;
            return;
        }

        var dir = diff.Normal;
        _velocity = _velocity.ProjectOnNormal( dir );

        _velocity += 2f * diff * Time.Delta;
        WorldPosition = WorldPosition + _velocity * Time.Delta;
        _velocity += 2f * diff * Time.Delta;

        if ( (targetPos - WorldPosition).Dot( dir ) < 0 )
        {
            _arrived = true;
            WorldPosition = targetPos;
        }

    }
}