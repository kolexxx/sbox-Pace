using Sandbox;

namespace Pace;

public static partial class PaceEvent
{
	public static class Player
	{
		public const string Killed = "pace.player.killed";

		/// <summary>
		/// Called when a player has been killed.
		/// </summary>
		[MethodArguments( typeof( Pawn ) )]
		public class KilledAttribute : EventAttribute
		{
			public KilledAttribute() : base( Killed ) { }
		}
	}
}
