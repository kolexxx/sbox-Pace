using Sandbox;

namespace Pace;

public class Deathmatch : Gamemode
{
	public override void ClientSpawn()
	{
		Game.RootPanel.AddChild<UI.Leaderboard>();
	}

	public override void OnClientJoined( Pawn player )
	{
		base.OnClientJoined( player );

		player.Respawn();
	}
}
