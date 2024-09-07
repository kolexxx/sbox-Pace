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

public interface ICleanup
{
	public void OnCleanup();
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
	[Property, ReadOnly, HostSync, Change] public GameState State { get; protected set; }

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
	public IEnumerable<SpawnPoint> SpawnPoints => Scene.GetAllComponents<SpawnPoint>();

	/// <summary>
	/// A list of all the players currently in-game.
	/// </summary>
	[HostSync] public List<Pawn> Players { get; private set; } = new();

	/// <summary>
	/// Text that will be displayed in the HUD's timer.
	/// </summary>
	public string TimerString
	{
		get
		{
			if ( State is GameState.WaitingForPlayers )
				return "Waiting";

			return TimeSpan.FromSeconds( MathF.Max( 0, TimeUntilNextState.Relative.CeilToInt() ) ).ToString( @"mm\:ss" );
		}
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
		UI.TextChat.AddInfo( $"{connection.DisplayName} has joined" );

		// Spawn this object and make the client the owner
		var player = PlayerPrefab.Clone( global::Transform.Zero, name: $"Player - {connection.DisplayName}" );
		player.NetworkSpawn( connection );

		Players.Add( player.Components.Get<Pawn>() );
		Players.Last().Respawn();

		VerifyEnoughPlayers();
	}

	public void OnDisconnected( Connection connection )
	{
		Players.RemoveAll( x => x.Network.OwnerConnection == connection );
		VerifyEnoughPlayers();

		UI.TextChat.AddInfo( $"{connection.DisplayName} has left the game" );
	}

	public virtual void OnRespawn( Pawn pawn )
	{
		MoveToSpawnpoint( pawn );
	}

	public virtual void OnKill( Pawn attacker, Pawn victim ) { }

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

	/// <summary>
	/// Called when we switch to countdown or waiting for players.
	/// </summary>
	protected void RoundReset()
	{
		foreach ( var pawn in Players )
		{
			pawn.Respawn();
			pawn.Stats.Clear();
		}
	}

	protected void MoveToSpawnpoint( Pawn pawn )
	{
		if ( SpawnPoints is null )
			return;

		var spawnpoint = SpawnPoints
						.OrderByDescending( x => GetSpawnpointWeight( pawn, x ) )
						.FirstOrDefault();

		if ( spawnpoint is null )
			return;

		pawn.PawnController.Teleport( spawnpoint.Transform.Position );
	}

	private float GetSpawnpointWeight( Pawn pawn, SpawnPoint spawnpoint )
	{
		// We want to find the closest player (worst weight)
		var distance = float.MaxValue;

		foreach ( var player in Players )
		{
			if ( player == pawn ) continue;
			if ( !pawn.IsAlive ) continue;

			var spawnDist = (spawnpoint.Transform.Position - player.Transform.Position).LengthSquared;
			distance = MathF.Min( distance, spawnDist );
		}

		return distance;
	}
}
