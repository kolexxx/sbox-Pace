using Sandbox;
using Sandbox.Citizen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Pace;

public class PlayerController : Component
{
    [Property, Group( "Game Objects" )] public GameObject Head { get; private set; }
    [Property, Group( "Game Objects" )] public GameObject Body { get; private set; }
    [Property, Group( "Components" )] public Player Player { get; private set; }
    [Property, Group( "Components" )] public CitizenAnimationHelper AnimationHelper { get; private set; }
    [Property, Group( "Components" )] public ModelPhysics Physics { get; private set; }
    [Property, Group( "Components" )] public List<Component> ComponentsToEnable { get; private set; }
    [Property, Group( "Components" )] public List<Component> ComponentsToDisable { get; private set; }
    [Property] public bool UseBuiltInCC { get; private set; }
    [Property] public float WalkSpeed { get; private set; } = 280f;
    [Property] public float CrouchSpeed { get; private set; } = 150f;
    [Property] public float GroundAngle { get; private set; } = 45f;
    [Property] public float StepSize { get; private set; } = 18f;
    [Property, ReadOnly, Sync] public Vector3 Velocity { get; private set; }
    [Property, ReadOnly, Sync] public Vector3 WishVelocity { get; private set; }
    [Property, ReadOnly, Sync] public bool IsCrouching { get; private set; }
    [Property, ReadOnly, Sync] public bool IsGrounded { get; private set; }
    [Property, ReadOnly, Sync] public Vector3 GroundNormal { get; private set; } = Vector3.Up;
    [Property, ReadOnly] public bool HasDoubleJumped { get; private set; }
    [Sync] public Vector3 MousePosition { get; private set; }
    [Property, ReadOnly] public bool IsRagdoll { get; private set; } = false;

    public Capsule Capsule
    {
        get => new( new Vector3( 0f, 0f, 8f ), new Vector3( 0f, 0f, IsCrouching ? 32f : 64f ), 7f );
    }

    private TimeUntil _timeUntilCanGround = 0f;

    [Rpc.Owner]
    public void Teleport( Vector3 position )
    {
        WorldPosition = new( position );
        Velocity = Vector3.Zero;
    }

    public void SetRagdoll( bool ragdoll, DamageInfo damage = null )
    {
        IsRagdoll = ragdoll;
        Tags.Set( "ragdoll", IsRagdoll );
        Transform.ClearInterpolation();

        foreach ( var component in ComponentsToEnable )
        {
            component.Enabled = !IsRagdoll;
        }

        foreach ( var component in ComponentsToDisable )
        {
            component.Enabled = IsRagdoll;
        }

        if ( !IsRagdoll )
        {
            Physics.LocalPosition = Vector3.Zero;
            Physics.LocalRotation = Rotation.Identity;
            return;
        }

        if ( damage is null || !Physics.IsValid() || !Physics.PhysicsGroup.IsValid() )
            return;

        foreach ( var body in Physics.PhysicsGroup.Bodies )
        {
            body.ApplyImpulseAt( damage.Position, damage.Force );
        }
    }

    protected override void OnPreRender()
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
        if ( IsProxy || IsRagdoll || Player.IsFrozen )
            return;

        UpdateCrouch();
        CalculateWishVelocity();

        WorldPosition = Settings.Plane.SnapToPlane( WorldPosition );
        Head.LocalPosition = Vector3.Up * (IsCrouching ? 32f : 64f);

        if ( TryUnstuck() )
            return;

        if ( Input.Pressed( "jump" ) )
            Jump();

        if ( IsGrounded )
        {
            Velocity -= Velocity.ProjectOnNormal( GroundNormal );
            Velocity = Velocity.WithAcceleration( WishVelocity, 20f * Time.Delta );
            Velocity = Velocity.WithFriction( 10f * Time.Delta );
        }
        else
        {
            Velocity += 0.5f * Scene.PhysicsWorld.Gravity * Time.Delta;
            Velocity = Velocity.WithAcceleration( WishVelocity, 3f * Time.Delta );
        }

        Move();
        CategorizePosition();

        if ( !IsGrounded )
            Velocity += 0.5f * Scene.PhysicsWorld.Gravity * Time.Delta;
    }

    private void Move()
    {
        var velocity = Velocity;
        var position = WorldPosition;
        var timeRemaining = Time.Delta;

        if ( UseBuiltInCC )
        {
            var trace = Scene.Trace.Capsule( Capsule, position, position )
            .IgnoreGameObjectHierarchy( GameObject )
            .WithCollisionRules( Tags );

            var cc = new CharacterControllerHelper( trace, position, velocity )
            {
                MaxStandableAngle = GroundAngle,
                Bounce = 0.1f
            };

            if ( IsGrounded )
                cc.TryMoveWithStep( timeRemaining, StepSize );
            else
                cc.TryMove( timeRemaining );

            position = cc.Position;
            velocity = cc.Velocity;
        }
        else
        {
            for ( var i = 0; i < 3; i++ )
            {
                var tr = TraceBody( position, position + velocity * timeRemaining );

                if ( !tr.Hit )
                {
                    position = tr.EndPosition;
                    break;
                }

                position = tr.EndPosition + tr.Normal * 0.001f;
                timeRemaining -= timeRemaining * tr.Fraction;

                if ( !IsGrounded || !IsGroundNormal( tr.Normal ) )
                {
                    velocity = Vector3.VectorPlaneProject( velocity, tr.Normal );
                }
                else
                {
                    var length = velocity.Length;
                    velocity = length * Vector3.VectorPlaneProject( velocity, tr.Normal ).Normal;
                }
            }
        }

        Velocity = velocity;
        WorldPosition = position;
    }

    private int _stuckTries;

    private bool TryUnstuck()
    {
        if ( !TraceBody( WorldPosition, WorldPosition ).StartedSolid )
        {
            _stuckTries = 0;
            return false;
        }

        for ( var i = 0; i < 20; i++ )
        {
            var vector = WorldPosition + Vector3.VectorPlaneProject( Vector3.Random, Settings.Plane.Normal ).Normal * (_stuckTries / 2f);

            if ( i == 0 )
                vector = WorldPosition + Vector3.Up * 2f;

            if ( !TraceBody( vector, vector ).StartedSolid )
            {
                WorldPosition = vector;
                return false;
            }
        }

        _stuckTries++;
        return true;
    }

    private void CategorizePosition()
    {
        if ( !_timeUntilCanGround )
        {
            ClearGround();
            return;
        }

        var from = WorldPosition;
        var to = WorldPosition + Vector3.Down * 2f;
        var tr = TraceBody( from, to );

        if ( !tr.Hit )
        {
            ClearGround();
            return;
        }

        IsGrounded = IsGroundNormal( tr.Normal );
        GroundNormal = IsGrounded ? tr.Normal : Vector3.Up;
        HasDoubleJumped = !IsGrounded && HasDoubleJumped;
    }

    private void UpdateCrouch()
    {
        if ( Input.Down( "Crouch" ) )
        {
            IsCrouching = true;
            return;
        }

        if ( !IsCrouching )
            return;

        var from = WorldPosition;
        var to = WorldPosition + Vector3.Up * 32f;
        var tr = TraceBody( from, to );

        IsCrouching = tr.Hit;
    }

    private void Jump()
    {
        if ( IsGrounded )
        {
            _timeUntilCanGround = 0.2f;
            Velocity = Velocity.WithZ( 0f );
            Velocity += Vector3.Up * 500f;
            ClearGround();
            JumpEffects();
            return;
        }

        if ( HasDoubleJumped )
            return;

        var dir = (Vector3.Up + WishVelocity.Normal).Normal;
        Velocity = dir * 500f;
        HasDoubleJumped = true;
        JumpEffects();
    }

    [Rpc.Broadcast]
    private void JumpEffects()
    {
        AnimationHelper.TriggerJump();
    }

    private SceneTraceResult TraceBody( in Vector3 from, in Vector3 to )
    {
        return Scene.Trace.Capsule( Capsule, from, to )
            .IgnoreGameObjectHierarchy( GameObject )
            .WithCollisionRules( Tags )
            .Run();
    }

    private void CalculateWishVelocity()
    {
        WishVelocity = Vector3.Zero;
        var right = GroundNormal.Cross( Settings.Plane.Normal );

        if ( Input.Down( "Right" ) ) WishVelocity += right;
        if ( Input.Down( "Left" ) ) WishVelocity -= right;

        if ( !WishVelocity.IsNearlyZero() )
            WishVelocity = WishVelocity.Normal * (IsCrouching ? CrouchSpeed : WalkSpeed);
    }

    private void ClearGround()
    {
        IsGrounded = false;
        GroundNormal = Vector3.Up;
    }

    private bool IsGroundNormal( Vector3 normal )
    {
        return Vector3.GetAngle( Vector3.Up, normal ) <= GroundAngle;
    }

    private void MouseInput()
    {
        var camera = Scene.GetAllComponents<CameraComponent>().First();

        if ( camera is null )
            return;

        var ray = camera.ScreenPixelToRay( Mouse.Position );
        var planeIntersection = Settings.Plane.Trace( ray );
        var headPosition = Head.WorldPosition;

        MousePosition = planeIntersection ?? headPosition;
    }

    private void UpdateRotation()
    {
        var rotation = Rotation.From( GroundNormal.Cross( Settings.Plane.Normal ).EulerAngles );
        var targetAngles = Head.LocalRotation.Angles().WithPitch( 0 ).WithRoll( 0 ).ToRotation();
        var currAngles = Body.LocalRotation.Angles().WithPitch( 0 ).WithRoll( 0 ).ToRotation();

        WorldRotation = WorldRotation.LerpTo( rotation, 10f * Time.Delta );
        Head.WorldRotation = Rotation.LookAt( MousePosition - Head.WorldPosition, GroundNormal );
        Body.LocalRotation = Rotation.Lerp( currAngles, targetAngles, 10f * Time.Delta );
    }

    private void Animate()
    {
        var equipment = Components.Get<Inventory>()?.ActiveEquipment ?? null;

        AnimationHelper.Target.UseAnimGraph = !IsRagdoll;
        AnimationHelper.WithWishVelocity( Player.IsFrozen ? Vector3.Zero : WishVelocity );
        AnimationHelper.WithVelocity( Player.IsFrozen ? Vector3.Zero : Velocity.ClampLength( 240f ) );
        AnimationHelper.AimAngle = Head.WorldRotation;
        AnimationHelper.IsGrounded = IsGrounded || Player.IsFrozen;
        AnimationHelper.WithLook( Head.WorldRotation.Forward, 1f, 0.5f, 0.5f );
        AnimationHelper.MoveStyle = CitizenAnimationHelper.MoveStyles.Run;
        AnimationHelper.HoldType = equipment.IsValid() ? equipment.HoldType : CitizenAnimationHelper.HoldTypes.None;
        AnimationHelper.Handedness = equipment.IsValid() ? equipment.Handedness : CitizenAnimationHelper.Hand.Both;
        AnimationHelper.DuckLevel = IsCrouching ? 1f : 0f;
    }
}