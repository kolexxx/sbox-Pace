using Sandbox;

namespace Pace.UI;

public partial class KillFeed
{
	[ClientRpc]
	public static void AddEntry(string left, WeaponDefinition weapon, string right)
	{
		Instance.AddToFeed( new KillFeedEntry( left, right, weapon ) );
	}
}
