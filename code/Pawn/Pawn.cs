using Sandbox;
using Sandbox.Citizen;
using Sandbox.Diagnostics;
using System.Linq;

namespace Pace;

public class Pawn : Component
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

	[Property] public GameObject Head { get; private set; }
	[Property] public GameObject Body { get; private set; }
	[Property, Group( "Components" )] public CharacterController CharacterController { get; private set; }
	[Property, Group( "Components" )] public CitizenAnimationHelper AnimationHelper { get; private set; }
	[Property, Group( "Components" )] public SkinnedModelRenderer BodyRenderer { get; private set; }
	[Property, Group( "Components" )] public Inventory Inventory { get; private set; }
	[Property, Group( "Components" )] public Vitals Vitals { get; private set; }
	[Property] public float MoveSpeed { get; private set; } = 200f;
	[Property] public float Friction { get; private set; } = 0.4f;

	/// <summary>
	/// The mouse position inside the world.
	/// </summary>
	[Sync] public Vector3 MousePosition { get; private set; }

	/// <summary>
	/// The direction and position from where we are aiming.
	/// </summary>
	public Ray AimRay { get; private set; }

	/// <summary>
	/// Whether or not this pawn is alive.
	/// </summary>
	public bool IsAlive => Vitals.Health >= 0;

	/// <summary>
	/// If true, we're not allowed to move.
	/// </summary>
	public bool IsFrozen => !IsAlive || GameMode.Current.State == GameState.Countdown;

	private Vector3 _wishVelocity;
	private bool _hasDoubleJumped;

	protected override void OnStart()
	{
		if ( !IsProxy )
		{
			GameObject.Clone( "templates/gameobject/camera.prefab", new CloneConfig
			{
				Parent = GameObject,
				StartEnabled = true
			} );
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy || IsFrozen )
			return;

		CalculateWishVelocity();

		if ( Input.Pressed( "Jump" ) )
			Jump();

		Move();
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

	protected override void OnPreRender()
	{
		if ( IsProxy )
			return;

		UpdateCamera();
	}

	public void Respawn()
	{
		Assert.True( Networking.IsHost );

		Transform.ClearInterpolation();
		CharacterController.Velocity = Vector3.Zero;
		Inventory.Clear();
		Vitals.Health = 100f;

		GameMode.Current?.OnRespawn( GameObject );
	}

	public void OnKilled()
	{
		Inventory.Clear();

		GameMode.Current?.OnKill( GameObject, Vitals.LastDamage.Attacker );
	}

	private void CalculateWishVelocity()
	{
		_wishVelocity = Vector3.Zero;
		var right = Vector3.Up.Cross( Settings.Plane.Normal );

		if ( Input.Down( "Right" ) ) _wishVelocity += right;
		if ( Input.Down( "Left" ) ) _wishVelocity -= right;

		_wishVelocity = _wishVelocity.WithZ( 0 );

		if ( !_wishVelocity.IsNearlyZero() )
			_wishVelocity = _wishVelocity.Normal * MoveSpeed;
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
		AimRay = new( headPosition, (MousePosition - headPosition).Normal );
	}

	private void Move()
	{
		var halfGravity = Scene.PhysicsWorld.Gravity * Time.Delta * 0.5f;

		if ( CharacterController.IsOnGround )
		{
			CharacterController.Velocity = CharacterController.Velocity.WithZ( 0 );
			CharacterController.Accelerate( _wishVelocity );
			CharacterController.ApplyFriction( Friction );
		}
		else
		{
			CharacterController.Velocity += halfGravity;
			CharacterController.Accelerate( _wishVelocity.ClampLength( 200f ) );
			CharacterController.ApplyFriction( Friction * 0.3f, 0 );
		}

		CharacterController.Move();

		if ( CharacterController.IsOnGround )
		{
			_hasDoubleJumped = false;
			CharacterController.Velocity = CharacterController.Velocity.WithZ( 0 );
		}
		else
			CharacterController.Velocity += halfGravity;

		Transform.Position = Settings.Plane.SnapToPlane( Transform.Position );
	}

	private void Jump()
	{
		if ( CharacterController.IsOnGround )
		{
			CharacterController.Punch( Vector3.Up * 500f );
			AnimationHelper.TriggerJump();
			return;
		}

		if ( _hasDoubleJumped )
			return;

		var dir = Vector3.Up;

		if ( _wishVelocity.IsNearlyZero() )
			CharacterController.Velocity = CharacterController.Velocity.WithZ( 0 );
		else
		{
			CharacterController.Velocity = Vector3.Zero;
			dir = (Vector3.Up + _wishVelocity.Normal).Normal;
		}

		CharacterController.Punch( dir * 600f );
		AnimationHelper.TriggerJump();
		_hasDoubleJumped = true;
	}

	private void UpdateCamera()
	{
		var camera = Components.Get<CameraComponent>( FindMode.InChildren );

		if ( camera is null )
			return;

		var position = AimRay.Position;
		var offset = (MousePosition - position) / 2f;

		camera.FieldOfView = Screen.CreateVerticalFieldOfView( 60f );
		camera.Transform.Rotation = (-Settings.Plane.Normal).EulerAngles;
		camera.Transform.Position = position + offset.ClampLength( 150f ) + Settings.Plane.Normal * 400f;
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
		if ( AnimationHelper is null )
			return;

		var equipment = Inventory.ActiveEquipment;

		AnimationHelper.WithWishVelocity( _wishVelocity );
		AnimationHelper.WithVelocity( CharacterController.Velocity );
		AnimationHelper.AimAngle = Head.Transform.Rotation;
		AnimationHelper.IsGrounded = CharacterController.IsOnGround;
		AnimationHelper.WithLook( Head.Transform.Rotation.Forward, 1f, 0.5f, 0.5f );
		AnimationHelper.MoveStyle = CitizenAnimationHelper.MoveStyles.Auto;
		AnimationHelper.DuckLevel = 0f;
		AnimationHelper.HoldType = equipment.IsValid() ? equipment.HoldType : CitizenAnimationHelper.HoldTypes.None;
		AnimationHelper.Handedness = equipment.IsValid() ? equipment.Handedness : CitizenAnimationHelper.Hand.Both;
	}
}
