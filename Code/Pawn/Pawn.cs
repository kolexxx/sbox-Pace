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
	[Property, Group( "Game Objects" )] public GameObject CameraPrefab { get; private set; }
	[Property, Group( "Components" )] public PawnController PawnController { get; private set; }
	[Property, Group( "Components" )] public CitizenAnimationHelper AnimationHelper { get; private set; }
	[Property, Group( "Components" )] public Inventory Inventory { get; private set; }
	[Property, Group( "Components" )] public HealthComponent HealthComponent { get; private set; }
	[Property, Group( "Components" )] public StatsTracker Stats { get; private set; }
	[Property, Group( "Components" )] public PawnBody PawnBody { get; private set; }

	public string SteamName => Network.OwnerConnection.DisplayName;

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
	[Sync] private Vector3 MousePosition { get; set; }

	/// <summary>
	/// The direction and position from where we are aiming.
	/// </summary>
	public Ray AimRay { get; private set; }

	protected override void OnStart()
	{
		if ( IsProxy )
			return;

		CameraPrefab.Clone( new CloneConfig
		{
			Parent = GameObject,
			StartEnabled = true,
			Transform = new()
		} );
	}

	[Broadcast( NetPermission.HostOnly )]
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
	}

	[Broadcast( NetPermission.HostOnly )]
	public void OnKilled()
	{
		var damage = HealthComponent.LastDamage;

		LifeState = LifeState.Dead;
		TimeSinceDeath = 0f;

		PawnBody.SetRagdoll( true );
		PawnBody.ApplyImpulses( damage );

		if ( Networking.IsHost )
			Inventory.Clear();

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

	protected override void OnPreRender()
	{
		if ( !IsProxy )
			UpdateCamera();
	}

	private void MouseInput()
	{
		var camera = Components.Get<CameraComponent>( FindMode.InChildren );

		if ( camera is null )
			return;

		var ray = camera.ScreenPixelToRay( Mouse.Position );
		var planeIntersection = Settings.Plane.Trace( ray );
		var headPosition = Head.Transform.Position;

		MousePosition = planeIntersection ?? headPosition;
		AimRay = new( headPosition, Vector3.Direction( headPosition, MousePosition ) );
	}

	private void UpdateCamera()
	{
		var camera = Components.Get<CameraComponent>( FindMode.InChildren );

		if ( camera is null )
			return;

		var position = Head.Transform.Position;
		var offset = (MousePosition - position) / 2f;
		var targetPosition = position + offset.ClampLength( 150f ) + Settings.Plane.Normal * 1000f;

		camera.FieldOfView = Screen.CreateVerticalFieldOfView( 30f );
		camera.Transform.Rotation = (-Settings.Plane.Normal).EulerAngles;
		camera.Transform.Position = Vector3.Lerp( camera.Transform.Position, targetPosition, 1 - MathF.Exp( -25f * Time.Delta ) );
	}

	private void UpdateRotation()
	{
		var targetAngles = Head.Transform.Rotation.Angles().WithPitch( 0 ).ToRotation();
		var currAngles = Body.Transform.Rotation;

		Head.Transform.Rotation = Rotation.LookAt( MousePosition - Head.Transform.Position );
		Body.Transform.Rotation = Rotation.Lerp( currAngles, targetAngles, Time.Delta * 20f );
	}

	private void Animate()
	{
		var equipment = Inventory.ActiveEquipment;

		AnimationHelper.WithWishVelocity( IsFrozen ? Vector3.Zero : PawnController.WishVelocity );
		AnimationHelper.WithVelocity( IsFrozen ? Vector3.Zero : PawnController.Velocity );
		AnimationHelper.AimAngle = Head.Transform.Rotation;
		AnimationHelper.IsGrounded = PawnController.IsGrounded || IsFrozen;
		AnimationHelper.WithLook( Head.Transform.Rotation.Forward, 1f, 0.5f, 0.5f );
		AnimationHelper.MoveStyle = CitizenAnimationHelper.MoveStyles.Run;
		AnimationHelper.DuckLevel = 0f;
		AnimationHelper.HoldType = equipment.IsValid() ? equipment.HoldType : CitizenAnimationHelper.HoldTypes.None;
		AnimationHelper.Handedness = equipment.IsValid() ? equipment.Handedness : CitizenAnimationHelper.Hand.Both;
	}
}