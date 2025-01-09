using Sandbox;
using Sandbox.Citizen;
using System;
using System.Linq;

namespace Pace;

public enum LifeState
{
	Alive,
	Dead,
	Respawning
}

/// <summary>
/// A GameObject that can respawn and be killed.
/// </summary>
public interface IRespawnable
{
	public LifeState LifeState { get; }

	public void Respawn() { }
	public void OnKilled() { }
}

public sealed class Pawn : Component, IRespawnable
{
	/// <summary>
	/// A reference to the local pawn. Returns null if one does not exist (headless server or something).
	/// </summary>
	public static Pawn Local
	{
		get
		{
			if ( !_local.IsValid() )
				_local = Game.ActiveScene.GetAllComponents<Pawn>().FirstOrDefault( x => x.Network.IsOwner );

			return _local;
		}
	}
	private static Pawn _local;

	[Property, Group( "Game Objects" )] public GameObject Head { get; private set; }
	[Property, Group( "Game Objects" )] public GameObject Body { get; private set; }
	[Property, Group( "Game Objects" )] public GameObject CameraObject { get; private set; }
	[Property, Group( "Game Objects" )] public GameObject CameraPrefab { get; private set; }
	[Property, Group( "Components" )] public PawnController PawnController { get; private set; }
	[Property, Group( "Components" )] public CitizenAnimationHelper AnimationHelper { get; private set; }
	[Property, Group( "Components" )] public Inventory Inventory { get; private set; }
	[Property, Group( "Components" )] public HealthComponent HealthComponent { get; private set; }
	[Property, Group( "Components" )] public StatsTracker Stats { get; private set; }
	[Property, Group( "Components" )] public PawnBody PawnBody { get; private set; }

	public string SteamName => Network.Owner.DisplayName;

	/// <summary>
	/// The LifeState of this pawn.
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
	/// Whether or not this pawn is alive.
	/// </summary>
	public bool IsAlive => LifeState == LifeState.Alive;

	/// <summary>
	/// If true, we're not allowed to move.
	/// </summary>
	public bool IsFrozen => !IsAlive || GameMode.Current.State == GameState.Countdown;

	/// <summary>
	/// The mouse position inside the world.
	/// </summary>
	[Sync] public Vector3 MousePosition { get; private set; }

	/// <summary>
	/// The direction and position from where we are aiming.
	/// </summary>
	public Ray AimRay { get; private set; }

	protected override void OnStart()
	{
		if ( IsProxy )
			return;

		CameraObject = CameraPrefab.Clone( new CloneConfig
		{
			Parent = GameObject,
			StartEnabled = true,
			Transform = new()
		} );

		CameraObject.Components.Create<LookCamera>();
	}

	[Rpc.Broadcast]
	public void Respawn()
	{
		LifeState = LifeState.Alive;
		PawnBody.SetRagdoll( false );

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
		TimeSinceDeath = 0f;

		PawnBody.SetRagdoll( true );
		PawnBody.ApplyImpulses( damage );

		if ( Networking.IsHost )
			Inventory.Clear();

		if ( !IsProxy )
		{
			CameraObject.Components.Get<LookCamera>().Destroy();
			CameraObject.Components.Create<DeathCamera>();
		}

		GameMode.Current?.OnKill( damage.Attacker as Pawn, this );
	}

	protected override void OnUpdate()
	{
		if ( !IsProxy )
		{
			MouseInput();
		}

		UpdateRotation();
		Animate();
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy || IsFrozen )
			return;

		PawnController.Move();
	}

	private void MouseInput()
	{
		if ( !IsAlive )
		{
			MousePosition = Head.WorldPosition;
			return;
		}

		var camera = CameraObject.Components.Get<CameraComponent>();

		if ( camera is null )
			return;

		var ray = camera.ScreenPixelToRay( Mouse.Position );
		var planeIntersection = Settings.Plane.Trace( ray );
		var headPosition = Head.WorldPosition;

		MousePosition = planeIntersection ?? headPosition;
		AimRay = new( headPosition, Vector3.Direction( headPosition, MousePosition ) );
	}

	private void UpdateRotation()
	{
		var targetAngles = Head.WorldRotation.Angles().WithPitch( 0 ).ToRotation();
		var currAngles = Body.WorldRotation;

		Head.WorldRotation = Rotation.LookAt( MousePosition - Head.WorldPosition );
		Body.WorldRotation = Rotation.Lerp( currAngles, targetAngles, Time.Delta * 20f );
	}

	private void Animate()
	{
		var equipment = Inventory.ActiveEquipment;

		AnimationHelper.WithWishVelocity( IsFrozen ? Vector3.Zero : PawnController.WishVelocity );
		AnimationHelper.WithVelocity( IsFrozen ? Vector3.Zero : PawnController.Velocity );
		AnimationHelper.AimAngle = Head.WorldRotation;
		AnimationHelper.IsGrounded = PawnController.IsGrounded || IsFrozen;
		AnimationHelper.WithLook( Head.WorldRotation.Forward, 1f, 0.5f, 0.5f );
		AnimationHelper.MoveStyle = CitizenAnimationHelper.MoveStyles.Run;
		AnimationHelper.DuckLevel = 0f;
		AnimationHelper.HoldType = equipment.IsValid() ? equipment.HoldType : CitizenAnimationHelper.HoldTypes.None;
		AnimationHelper.Handedness = equipment.IsValid() ? equipment.Handedness : CitizenAnimationHelper.Hand.Both;
	}
}