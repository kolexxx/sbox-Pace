using Sandbox;

namespace Pace;

public sealed class Deathmatch : GameMode
{
    [Property] public EquipmentResource DefaultWeapon { get; private set; }

    protected override void OnFixedUpdate()
    {
        if ( !Networking.IsHost )
            return;

        if ( TimeUntilNextState )
        {
            // If the round counts down to 0, start the round.
            if ( State == GameState.Countdown )
                SetState( GameState.Playing );
            else if ( State == GameState.Playing )
                SetState( GameState.GameOver );
            else if ( State == GameState.GameOver )
                SetState( GameState.WaitingForPlayers );
        }

        if ( State is GameState.Countdown or GameState.GameOver )
            return;

        foreach ( var player in Players )
        {
            if ( player.IsAlive )
                continue;

            if ( player.TimeSinceDeath >= 5f )
                player.Respawn();
        }
    }

    public override void OnRespawn( Pawn pawn )
    {
        pawn.Inventory.Add( DefaultWeapon, true );

        base.OnRespawn( pawn );
    }

    public override void OnKill( Pawn attacker, Pawn victim )
    {
        if ( State == GameState.Playing )
        {
            UI.KillFeed.AddEntry( victim.HealthComponent.LastDamage );
        }

        if ( !Networking.IsHost )
            return;

        if ( attacker != victim )
            attacker.Stats.Kills++;

        victim.Stats.Deaths++;
    }

    protected override void OnStateChanged( GameState before, GameState after )
    {
        //if ( after == GameState.GameOver )
        //ShowBestPlayer();

        if ( !Networking.IsHost )
            return;

        switch ( after )
        {
            case GameState.WaitingForPlayers:
            {
                VerifyEnoughPlayers();

                if ( State == after )
                    RespawnAllPlayers();

                break;
            }
            case GameState.Countdown:
            {
                TimeUntilNextState = 5f;

                RespawnAllPlayers();
                break;
            }
            case GameState.Playing:
            {
                TimeUntilNextState = 180f;
                break;
            }
            case GameState.GameOver:
            {
                TimeUntilNextState = 10f;
                break;
            }
        }
    }
}
