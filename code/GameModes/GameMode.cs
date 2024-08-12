using Sandbox;
using Sandbox.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pace;

public enum GameState
{
	WaitingForPlayers,
	Countdown,
	Playing,
	GameOver
}

public abstract class GameMode : Component, Component.INetworkListener
{
	/// <summary>
	/// The current gamemode being played.
	/// </summary>
	public static GameMode Current { get; private set; }

	/// <summary>
	/// Our current <see cref="GameState"> game state</see>.
	/// </summary>
	[Property, ReadOnly, HostSync] public GameState State { get; protected set; }

	/// <summary>
	/// The time until we change our game state.
	/// </summary>
	[HostSync] public TimeUntil TimeUntilNextState { get; protected set; }

	/// <summary>
	/// The prefab to spawn for the player to control.
	/// </summary>
	[Property] public GameObject PlayerPrefab { get; set; }

	/// <summary>
	/// A list of points to choose from randomly to spawn the player in. If not set, we'll spawn at the
	/// location of the NetworkHelper object.
	/// </summary>
	[Property] public List<GameObject> SpawnPoints { get; set; }

	/// <summary>
	/// A list of all the players currently in-game.
	/// </summary>
	public List<GameObject> Players { get; private set; } = new();

	public virtual string GetTimeLeftLabel()
	{
		return State == GameState.WaitingForPlayers ? "Waiting" : TimeSpan.FromSeconds( MathF.Max( 0, TimeUntilNextState.Relative.CeilToInt() ) ).ToString( @"mm\:ss" );
	}

	protected override async Task OnLoad()
	{
		if ( Scene.IsEditor )
			return;

		if ( !GameNetworkSystem.IsActive )
		{
			LoadingScreen.Title = "Creating Lobby";
			await Task.DelayRealtimeSeconds( 0.1f );
			GameNetworkSystem.CreateLobby();
		}

		Current = this;
	}

	/// <summary>
	/// Called on the host when someone successfully joins the server (including the local player)
	/// </summary>
	public void OnActive( Connection connection )
	{
		Log.Info( $"Player '{connection.DisplayName}' has joined the game" );

		if ( PlayerPrefab is null )
			return;

		// Spawn this object and make the client the owner
		var player = PlayerPrefab.Clone( global::Transform.Zero, name: $"Player - {connection.DisplayName}" );
		player.NetworkSpawn( connection );

		Players.Add( player );
		MoveToSpawnpoint( player );
	}

	public virtual void OnRespawn(GameObject gameObject)
	{
		MoveToSpawnpoint( gameObject );
	}

	public virtual void OnKill( GameObject attacker, GameObject victim ) { }

	protected virtual void OnStateChanged( GameState before, GameState after ) { }

	protected void SetState( GameState state )
	{
		var oldState = State;
		State = state;

		OnStateChanged( oldState, state );
	}

	protected void VerifyEnoughPlayers()
	{
		if ( State == GameState.WaitingForPlayers )
		{
			if ( Players.Count >= 2 )
				SetState( GameState.Countdown );
		}
		else
		{
			if ( Players.Count < 2 )
				SetState( GameState.WaitingForPlayers );
		}
	}

	protected void MoveToSpawnpoint(GameObject gameObject)
	{
		if ( SpawnPoints is null || SpawnPoints.Count <= 0 )
			return;

		var spawnpoint = SpawnPoints
			.OrderByDescending( x => GetSpawnpointWeight( gameObject, x ) )
			.FirstOrDefault();

		if ( spawnpoint is null )
			return;

		var tx = spawnpoint.Transform.World;
		gameObject.Transform.World = tx;
	}

	private float GetSpawnpointWeight( GameObject pawn, GameObject spawnpoint )
	{
		// We want to find the closest player (worst weight)
		var distance = float.MaxValue;

		foreach ( var player in Players )
		{
			if ( player == pawn ) continue;
			if ( player.Components.Get<Vitals>()?.Health <= 0 ) continue;

			var spawnDist = (spawnpoint.Transform.Position -pawn.Transform.Position).LengthSquared;
			distance = MathF.Min( distance, spawnDist );
		}

		return distance;
	}
}
