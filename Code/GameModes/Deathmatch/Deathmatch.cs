using Sandbox;
using System.Collections.Generic;

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
        if ( State != GameState.Playing )
            return;

        victim.LifeState = LifeState.Respawning;
        UI.KillFeed.AddEntry( victim.HealthComponent.LastDamage );

        if ( !Networking.IsHost )
            return;

        if ( attacker.IsValid() && attacker != victim )
            attacker.Stats.Kills++;

        victim.Stats.Deaths++;
    }

    protected override void OnStateChanged( GameState before, GameState after )
    {
        switch ( after )
        {
            case GameState.WaitingForPlayers:
            {
                VerifyEnoughPlayers();

                if ( State == after )
                    RoundReset();

                break;
            }
            case GameState.Countdown:
            {
                TimeUntilNextState = 5f;

                RoundReset();
                break;
            }
            case GameState.Playing:
            {
                TimeUntilNextState = 180f;
                break;
            }
            case GameState.GameOver:
            {
                ShowBestPlayer();
                TimeUntilNextState = 10f;
                break;
            }
        }
    }

    [Broadcast( NetPermission.HostOnly )]
    private void ShowBestPlayer()
    {
        var players = new List<Pawn>( Players );

        players.Sort( delegate ( Pawn x, Pawn y )
        {
            return y.Stats.Kills.CompareTo( x.Stats.Kills );
        } );

        var winner = players[0].Stats.Kills != players[1].Stats.Kills ? players[0] : null;
        UI.Hud.RootPanel.AddChild( new UI.GameOverPopup( winner ) );
    }
}