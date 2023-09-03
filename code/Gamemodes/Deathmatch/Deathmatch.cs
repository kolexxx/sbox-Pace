using Sandbox;

namespace Pace;

public class Deathmatch : Gamemode
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

		player.LastAttacker?.Client?.AddInt( "kills" );
		UI.KillFeed.AddEntry( player.LastAttacker.Client, ((Weapon)player.LastAttackerWeapon).Definition, player.Client );
	}

	public override void OnClientJoined( Pawn player )
	{
		base.OnClientJoined( player );

		player.Respawn();
	}
}
