using Sandbox;

namespace Pace;

internal class Deathmatch : Gamemode
{
	public override void ClientSpawn()
	{
		Game.RootPanel.AddChild<UI.Leaderboard>();
	}

	protected override void OnStateChanged( State before, State after )
	{
		base.OnStateChanged( before, after );
	}
}
