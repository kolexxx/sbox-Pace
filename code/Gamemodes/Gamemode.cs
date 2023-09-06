using Sandbox;
using System;
using System.Linq;

namespace Pace;

public enum State
{
	WaitingForPlayers,
	Countdown,
	Playing,
	GameOver
}

public abstract partial class Gamemode : Entity
{
	[Net, Change] public State State { get; protected set; }
	[Net] public RealTimeUntil TimeUntilNextState { get; protected set; }
	public int PlayerCount { get; set; }

	/// <summary>
	/// Decides whether or not players can move
	/// </summary>
	public virtual bool AllowMovement => State != State.Countdown;

	public virtual string GetTimeLeftLabel()
	{
		return State == State.WaitingForPlayers ? "Waiting" : TimeSpan.FromSeconds( MathF.Max( 0, TimeUntilNextState.Relative.CeilToInt() ) ).ToString( @"mm\:ss" );
	}

	public override void Spawn()
	{
		Transmit = TransmitType.Always;
	}

	/// <summary>
	/// Called when a client joins the game
	/// </summary>
	/// <param name="player"></param>
	public virtual void OnClientJoined( Pawn player )
	{
		PlayerCount++;
		VerifyEnoughPlayers();
	}

	/// <summary>
	/// Called when a client leaves the game
	/// </summary>
	/// <param name="player"></param>
	/// <param name="reason"></param>
	public virtual void OnClientLeft( Pawn player, NetworkDisconnectionReason reason )
	{
		PlayerCount--;
		VerifyEnoughPlayers();
	}

	/// <summary>
	/// Called when a player dies.
	/// </summary>
	public virtual void OnPlayerKilled( Pawn player ) { }

	/// <summary>
	/// Called when a player respawns.
	/// </summary>
	/// <param name="player"></param>
	public virtual void OnPlayerSpawned( Pawn player ) { }

	public void MoveToSpawnpoint( Pawn player )
	{
		var spawnpoint = Entity.All.OfType<SpawnPoint>()
			.OrderByDescending( x => GetSpawnpointWeight( player, x ) )
			.FirstOrDefault();

		if ( spawnpoint is null )
			return;

		var tx = spawnpoint.Transform;
		tx.Position = tx.Position + Vector3.Up * 10f;
		player.Transform = tx;
	}

	public float GetSpawnpointWeight( Pawn pawn, Entity spawnpoint )
	{
		// We want to find the closest player (worst weight)
		var distance = float.MaxValue;

		foreach ( var client in Game.Clients )
		{
			if ( client.Pawn is not Pawn player ) continue;
			if ( player == pawn ) continue;
			if ( player.LifeState != LifeState.Alive ) continue;

			var spawnDist = (spawnpoint.Position - client.Pawn.Position).LengthSquared;
			distance = MathF.Min( distance, spawnDist );
		}

		return distance;
	}

	protected void RespawnAllPlayers()
	{
		foreach ( var client in Game.Clients )
			((Pawn)client.Pawn).Respawn();
	}

	protected void VerifyEnoughPlayers()
	{
		if ( State == State.WaitingForPlayers )
		{
			if ( PlayerCount >= 2 )
				SetState( State.Countdown );
		}
		else
		{
			if ( PlayerCount < 2 )
				SetState( State.WaitingForPlayers );
		}
	}

	public override void BuildInput()
	{
		if ( AllowMovement )
			return;

		Input.AnalogMove = Vector3.Zero;
		Input.ClearActions();
		Input.StopProcessing = true;
	}

	public virtual void ResetStats()
	{
		foreach ( var client in Game.Clients )
		{
			client.SetInt( "kills", 0 );
			client.SetInt( "deaths", 0 );
			client.SetInt( "captures", 0 );
		}
	}

	protected void SetState( State state )
	{
		Game.AssertServer();

		var oldState = State;
		State = state;

		OnStateChanged( oldState, state );
	}

	protected virtual void OnStateChanged( State before, State after ) { }

	[GameEvent.Tick.Server]
	protected void EventServerTick()
	{
		Tick();
	}

	protected virtual void Tick() { }
}
