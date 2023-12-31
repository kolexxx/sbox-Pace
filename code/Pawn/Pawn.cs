using Sandbox;
using System.ComponentModel;

namespace Pace;

public partial class Pawn : AnimatedEntity
{
	/// <summary>
	/// The direction we want to move.
	/// </summary>
	[ClientInput] public Vector3 InputDirection { get; set; }

	/// <summary>
	/// The mouse's position on the game's plane.
	/// </summary>
	[ClientInput] public Vector3 MousePosition { get; set; }

	/// <summary>
	/// The weapon we want to equip.
	/// </summary>
	[ClientInput] public Weapon ActiveChildInput { get; set; }

	/// <summary>
	/// Position a player should be looking from in world space.
	/// </summary>
	[Browsable( false )]
	public Vector3 EyePosition
	{
		get => Transform.PointToWorld( EyeLocalPosition );
		set => EyeLocalPosition = Transform.PointToLocal( value );
	}

	/// <summary>
	/// Position a player should be looking from in local to the entity coordinates.
	/// </summary>
	[Net, Predicted, Browsable( false )]
	public Vector3 EyeLocalPosition { get; set; }

	/// <summary>
	/// Rotation of the entity's "eyes", i.e. rotation for the camera when this entity is used as the view entity.
	/// </summary>
	[Browsable( false )]
	public Rotation EyeRotation
	{
		get => Transform.RotationToWorld( EyeLocalRotation );
		set => EyeLocalRotation = Transform.RotationToLocal( value );
	}

	/// <summary>
	/// Rotation of the entity's "eyes", i.e. rotation for the camera when this entity is used as the view entity. In local to the entity coordinates.
	/// </summary>
	[Net, Predicted, Browsable( false )]
	public Rotation EyeLocalRotation { get; set; }

	/// <summary>
	/// The position and direction we are aiming from.
	/// </summary>
	public override Ray AimRay => new( EyePosition, EyeRotation.Forward );

	public BBox Hull
	{
		get => new
		(
			new Vector3( -16, -16, 0 ),
			new Vector3( 16, 16, 72 )
		);
	}

	[BindComponent] public PawnController Controller { get; }
	[BindComponent] public PawnAnimator Animator { get; }
	[BindComponent] public Inventory Inventory { get; }
	[BindComponent] public ProjectileSimulator Projectiles { get; }
	[Net] public TimeSince TimeSinceDeath { get; private set; }
	public DamageInfo LastDamage { get; private set; }

	/// <summary>
	/// Called when the entity is first created.
	/// </summary>
	public override void Spawn()
	{
		SetModel( "models/citizen/citizen.vmdl" );
		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, new Vector3( -16, -16, 0 ), new Vector3( 16, 16, 72 ) );

		Tags.Add( "ignorereset", "player", "solid" );
		Transmit = TransmitType.Always;
		LifeState = LifeState.Respawnable;
		TimeSinceDeath = 0f;
		EnableAllCollisions = false;
		EnableDrawing = false;
		EnableHitboxes = true;
		EnableLagCompensation = true;

		Components.Create<PawnController>();
		Components.Create<PawnAnimator>();
		Components.Create<Inventory>();
		Components.Create<ProjectileSimulator>();
	}

	/// <summary>
	/// Called when the entity is first created clientside.
	/// </summary>
	public override void ClientSpawn()
	{
		Components.Create<UI.InfoPlate.Component>();
	}

	public void Respawn()
	{
		Game.AssertServer();

		Health = 100f;
		LifeState = LifeState.Alive;
		Velocity = Vector3.Zero;
		EnableAllCollisions = true;
		EnableDrawing = true;
		Inventory.DeleteContents();
		ResetInterpolation();

		MyGame.Instance.Gamemode.OnPlayerSpawned( this );
	}

	public void DressFromClient( IClient cl )
	{
		var c = new ClothingContainer();
		c.LoadFromClient( cl );
		c.DressEntity( this );
	}

	public override void Simulate( IClient cl )
	{
		Projectiles?.Simulate( cl );

		if ( LifeState == LifeState.Respawning )
		{
			if ( Game.IsServer && TimeSinceDeath > 5f )
				Respawn();
			else
				return;
		}

		if ( LifeState != LifeState.Alive )
			return;

		SimulateRotation();
		Controller?.Simulate( cl );
		Animator?.Simulate();
		Inventory?.Simulate( cl );
		EyeLocalPosition = Vector3.Up * (64f * Scale);
	}

	public override void BuildInput()
	{
		InputDirection = Input.AnalogMove * MyGame.Plane.Normal - Input.AnalogMove;
		MousePosition = MyGame.Plane.Trace( Camera.Main.GetRay( Mouse.Position ) ) ?? Position;
	}

	public override void FrameSimulate( IClient cl )
	{
		SimulateRotation();

		Camera.Rotation = (-MyGame.Plane.Normal).EulerAngles.ToRotation();
		Camera.FieldOfView = Screen.CreateVerticalFieldOfView( 75f );
		Camera.FirstPersonViewer = null;

		Camera.Position = (EyePosition + MousePosition) / 2f;

		if ( (Camera.Position - EyePosition).Length >= 150f )
			Camera.Position = EyePosition + (Camera.Position - EyePosition).Normal * 150f;

		Camera.Position += MyGame.Plane.Normal * 300f;
	}

	public override void TakeDamage( DamageInfo info )
	{
		Game.AssertServer();

		if ( LifeState != LifeState.Alive )
			return;


		if ( info.Tags.Contains( "bullet" ) )
		{
			// Headshot
			if ( info.Hitbox.HasTag( "head" ) )
				info.Damage *= 1.5f;

			Sound.FromEntity( "damage_taken", this );
		}

		LastDamage = info;
		LastAttacker = info.Attacker;
		LastAttackerWeapon = info.Weapon;
		Health -= info.Damage;

		this.ProceduralHitReaction( info );

		if ( Health <= 0f )
		{
			Health = 0f;
			OnKilled();
		}
	}

	public override void OnKilled()
	{
		LifeState = LifeState.Dead;
		TimeSinceDeath = 0;
		EnableAllCollisions = false;
		EnableDrawing = false;

		RemoveAllDecals();
		Inventory.DeleteContents();
		BecomeRagdollOnClient( Velocity, LastDamage.Position, LastDamage.Force, LastDamage.BoneIndex );

		MyGame.Instance.Gamemode.OnPlayerKilled( this );
	}

	public TraceResult TraceBBox( Vector3 start, Vector3 end, float liftFeet = 0.0f )
	{
		return TraceBBox( start, end, Hull.Mins, Hull.Maxs, liftFeet );
	}

	public TraceResult TraceBBox( Vector3 start, Vector3 end, Vector3 mins, Vector3 maxs, float liftFeet = 0.0f )
	{
		if ( liftFeet > 0 )
		{
			start += Vector3.Up * liftFeet;
			maxs = maxs.WithZ( maxs.z - liftFeet );
		}

		var tr = Trace.Ray( start, end )
					.Size( mins, maxs )
					.WithAnyTags( "solid", "playerclip", "passbullets" )
					.Ignore( this )
					.Run();

		return tr;
	}

	protected void SimulateRotation()
	{
		EyeRotation = Rotation.LookAt( MousePosition - EyePosition );
		Rotation = Rotation.LookAt( MousePosition.WithZ( 0 ) - Position.WithZ( 0 ) );
	}
}
