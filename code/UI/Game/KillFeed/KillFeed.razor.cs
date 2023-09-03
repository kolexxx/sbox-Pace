using Sandbox;

namespace Pace.UI;

public partial class KillFeed
{
	[ClientRpc]
	public static void AddEntry(IClient left, WeaponDefinition weapon, IClient right)
	{
		Instance.AddChild( new KillFeedEntry( left.Name, weapon, right.Name ) );
	}
}
