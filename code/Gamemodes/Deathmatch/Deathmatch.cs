using Sandbox;
using System.Collections.Generic;

namespace Pace;

public partial class Deathmatch : Gamemode
{
	public override void ClientSpawn()
	{
		Game.RootPanel.AddChild<UI.Leaderboard>();
	}

	public override void OnPlayerKilled( Pawn player )
	{
		player.LifeState = LifeState.Respawning;

		if ( State != State.Playing )
			return;

		if ( player.LastAttacker != player )
			player.LastAttacker?.Client?.AddInt( "kills" );

		UI.KillFeed.AddEntry( player.LastAttacker?.Client, (player.LastAttackerWeapon as Weapon)?.Definition, player.Client );
	}

	public override void OnPlayerSpawned( Pawn player )
	{
		player.Inventory.Add( new Pistol(), true );
		MoveToSpawnpoint( player );
	}

	public override void OnClientJoined( Pawn player )
	{
		base.OnClientJoined( player );

		player.Respawn();
	}

	protected override void OnStateChanged( State before, State after )
	{
		if ( Game.IsClient )
		{
			if ( after == State.GameOver )
				ShowBestPlayer();

			return;
		}

		switch ( after )
		{
			case State.WaitingForPlayers:
			{
				ResetStats();
				VerifyEnoughPlayers();
				break;
			}
			case State.Countdown:
			{
				TimeUntilNextState = 5f;

				RespawnAllPlayers();
				break;
			}
			case State.Playing:
			{
				TimeUntilNextState = 180f;
				break;
			}
			case State.GameOver:
			{
				TimeUntilNextState = 10f;
				break;
			}
		}
	}

	protected override void Tick()
	{
		Game.TimeScale = 1f;

		if ( State == State.WaitingForPlayers )
			return;

		if ( TimeUntilNextState )
		{
			// If the round counts down to 0, start the round
			if ( State == State.Countdown )
				SetState( State.Playing );
			else if ( State == State.Playing )
				SetState( State.GameOver );
			else if ( State == State.GameOver )
				SetState( State.WaitingForPlayers );
		}

		if ( State == State.GameOver )
		{
			var timeSince = 10 - TimeUntilNextState.Relative;
			Game.TimeScale = 1 - timeSince.Remap( 0, 3, 0.3f, 0.75f, true );
		}
	}

	private void ShowBestPlayer()
	{
		var clients = new List<IClient>( Game.Clients );

		clients.Sort( delegate ( IClient x, IClient y )
		{
			return y.GetInt( "kills" ).CompareTo( x.GetInt( "kills" ) );
		} );

		var winner = clients[0].GetInt( "kills" ) != clients[1].GetInt( "kills" ) ? clients[0] : null;
		Game.RootPanel.AddChild( new UI.GameOverPopup( winner ) );
	}
}
