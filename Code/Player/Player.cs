using Sandbox;
using Sandbox.Citizen;
using System.Linq;

namespace Pace;

public enum LifeState
{
	Alive,
	Dead,
	Respawning
}


public sealed class Player : Component, UI.IMinimapElement
{
	/// <summary>
	/// A reference to the local player. Returns null if one does not exist (headless server or something).
	/// </summary>
	public static Player Local
	{
		get
		{
			if ( !_local.IsValid() )
				_local = Game.ActiveScene.GetAllComponents<Player>().FirstOrDefault( x => x.Network.IsOwner );

			return _local;
		}
	}
	private static Player _local;

	[Property, Group( "Game Objects" )] public GameObject Head { get; private set; }
	[Property, Group( "Game Objects" )] public GameObject Hand { get; private set; }
	[Property, Group( "Game Objects" )] public GameObject CameraObject { get; private set; }
	[Property, Group( "Game Objects" )] public GameObject CameraPrefab { get; private set; }
	[Property, Group( "Components" )] public PlayerController Controller { get; private set; }
	[Property, Group( "Components" )] public Inventory Inventory { get; private set; }
	[Property, Group( "Components" )] public HealthComponent HealthComponent { get; private set; }
	[Property, Group( "Components" )] public StatsTracker Stats { get; private set; }
	[Property, Group( "Components" )] public SkinnedModelRenderer Renderer { get; private set; }
	public string SteamName => Network.Owner.DisplayName;

	/// <summary>
	/// The LifeState of this player.
	/// </summary>
	public LifeState LifeState { get; set; }

	/// <summary>
	/// How long has it been since we died?
	/// </summary>
	public TimeSince TimeSinceDeath { get; private set; }

	/// <summary>
	/// The position we last spawned at.
	/// </summary>
	public Vector3? SpawnPosition { get; set; }

	/// <summary>
	/// Whether or not this player is alive.
	/// </summary>
	public bool IsAlive => LifeState == LifeState.Alive;

	/// <summary>
	/// If true, we're not allowed to move.
	/// </summary>
	public bool IsFrozen => !IsAlive || GameMode.Current.State == GameState.Countdown;

	/// <summary>
	/// The mouse position inside the world.
	/// </summary>
	public Vector3 MousePosition => Controller.MousePosition;

	/// <summary>
	/// The direction and position from where we are aiming.
	/// </summary>
	public Ray AimRay => new( Head.WorldPosition, Head.WorldRotation.Forward );

	Color UI.IMinimapElement.Color => Local == this ? Color.White : Color.Red;

	Vector3 UI.IMinimapElement.WorldPosition => WorldPosition;

	bool UI.IMinimapElement.IsVisible => IsAlive;

	protected override void OnStart()
	{
		if ( IsProxy )
			return;

		CameraObject = CameraPrefab.Clone( new CloneConfig
		{
			Parent = Scene,
			StartEnabled = true,
			Transform = new()
		} );

		CameraObject.Components.Create<LookCamera>();
	}

	[Rpc.Broadcast]
	public void Respawn()
	{
		LifeState = LifeState.Alive;
		Renderer.Enabled = true;

		if ( Networking.IsHost )
		{
			Inventory.Clear();
			HealthComponent.Health = 100f;
			GameMode.Current?.OnRespawn( this );
		}

		if ( !IsProxy && CameraObject.IsValid() )
		{
			CameraObject.Components.Get<DeathCamera>()?.Destroy();
			CameraObject.Components.GetOrCreate<LookCamera>();
		}
	}

	[Rpc.Broadcast]
	public void OnKilled()
	{
		var damage = HealthComponent.LastDamage;

		LifeState = LifeState.Dead;
		Renderer.Enabled = false;
		TimeSinceDeath = 0f;

		if ( Networking.IsHost )
			Inventory.Clear();

		if ( !IsProxy )
		{
			CameraObject.Components.Get<LookCamera>().Destroy();
			CameraObject.Components.Create<DeathCamera>();
		}

		GameMode.Current?.OnKill( damage.Attacker as Player, this );
	}
}
