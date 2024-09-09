using System.Numerics;
using Sandbox;
using Sandbox.Citizen;

namespace Pace;

public class PawnController : Component
{
	[Property, Group( "Components" )] public CitizenAnimationHelper AnimationHelper { get; private set; }
	[Property, Group( "Components" )] public CharacterController CharacterController { get; private set; }
	[Property] public float MoveSpeed { get; private set; } = 320f;
	[Property] public float Gravity { get; private set; } = 500f;
	[Property] public float Friction { get; private set; } = 4f;

	/// <summary>
	/// Our current wish velocity.
	/// </summary>
	public Vector3 WishVelocity { get; private set; }

	public Vector3 Velocity => CharacterController.Velocity;
	public bool IsGrounded => CharacterController.IsOnGround;
	private bool _hasDoubleJumped;

	[Authority( NetPermission.HostOnly )]
	public void Teleport( Vector3 position )
	{
		Transform.World = new( position );
		Transform.ClearInterpolation();

		if ( CharacterController.IsValid() )
		{
			CharacterController.Velocity = Vector3.Zero;
			CharacterController.IsOnGround = true;
		}
	}

	private void CalculateWishVelocity()
	{
		WishVelocity = Vector3.Zero;
		var right = Vector3.Up.Cross( Settings.Plane.Normal );

		if ( Input.Down( "Right" ) ) WishVelocity += right;
		if ( Input.Down( "Left" ) ) WishVelocity -= right;

		WishVelocity = WishVelocity.WithZ( 0 );

		if ( !WishVelocity.IsNearlyZero() )
			WishVelocity = WishVelocity.Normal * MoveSpeed;
	}

	public void Move()
	{
		CalculateWishVelocity();

		if ( Input.Pressed( "Jump" ) )
			Jump();


		var halfGravity = Vector3.Down * Time.Delta * Gravity;

		if ( CharacterController.IsOnGround )
		{
			CharacterController.Velocity = CharacterController.Velocity.WithZ( 0 );
			CharacterController.Accelerate( WishVelocity );
			CharacterController.ApplyFriction( Friction );
		}
		else
		{
			CharacterController.Velocity += halfGravity;
			CharacterController.Accelerate( WishVelocity );
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

		if ( WishVelocity.IsNearlyZero() )
			CharacterController.Velocity = CharacterController.Velocity.WithZ( 0 );
		else
		{
			CharacterController.Velocity = Vector3.Zero;
			dir = (Vector3.Up + WishVelocity.Normal).Normal;
		}

		CharacterController.Punch( dir * 600f );
		AnimationHelper.TriggerJump();
		_hasDoubleJumped = true;
	}
}
