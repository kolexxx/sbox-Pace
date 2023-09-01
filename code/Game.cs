using Sandbox;
using System;
using System.Linq;

//
// You don't need to put things in a namespace, but it doesn't hurt.
//
namespace Pace;

/// <summary>
/// This is your game class. This is an entity that is created serverside when
/// the game starts, and is replicated to the client. 
/// 
/// You can use this to create things like HUDs and declare which player class
/// to use for spawned players.
/// </summary>
public partial class MyGame : GameManager
{
	public static MyGame Instance { get; private set; }
	public static Plane Plane { get; set; } = new( Vector3.Zero, Vector3.Forward );
	[Net] public Gamemode Gamemode { get; private set; }

	/// <summary>
	/// Called when the game is created (on both the server and client)
	/// </summary>
	public MyGame()
	{
		Instance = this;

		if ( Game.IsClient )
		{
			Game.RootPanel?.Delete();
			Game.RootPanel = new UI.Hud();
		}
	}

	public override void BuildInput()
	{
		Gamemode.BuildInput();

		base.BuildInput();
	}

	/// <summary>
	/// A client has joined the server. Make them a pawn to play with
	/// </summary>
	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );

		// Create a pawn for this client to play with
		var pawn = new Pawn();
		client.Pawn = pawn;
		pawn.DressFromClient( client );

		UI.TextChat.AddInfoEntry( $"{client.Name} has joined" );
		Gamemode.OnClientJoined( pawn );
	}

	/// <summary>
	/// A client has left the server. Delete their pawn.
	/// </summary>
	public override void ClientDisconnect( IClient cl, NetworkDisconnectionReason reason )
	{
		UI.TextChat.AddInfoEntry( $"{cl.Name} has left the game ({reason})" );
		Gamemode.OnClientLeft( cl.Pawn as Pawn, reason );

		base.ClientDisconnect( cl, reason );
	}

	public override void PostLevelLoaded()
	{
		Gamemode = new Deathmatch();
	}

	public override void MoveToSpawnpoint( Entity pawn )
	{
		// Get all of the spawnpoints
		var spawnpoints = Entity.All.OfType<SpawnPoint>();

		// chose a random one
		var randomSpawnPoint = spawnpoints.OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

		// if it exists, place the pawn there
		if ( randomSpawnPoint is not null )
		{
			var tx = randomSpawnPoint.Transform;
			tx.Position = tx.Position + Vector3.Up * 10f;
			pawn.Transform = tx;
		}
	}

	public override void OnVoicePlayed( IClient client )
	{
		UI.VoiceChat.OnVoicePlayed( client );
	}
}

