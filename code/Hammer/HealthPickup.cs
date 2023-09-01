using Editor;
using Sandbox;
using System;

namespace Pace;

[Category( "Pickups" )]
[ClassName( "pickup_health" )]
[HammerEntity]
[Title( "Health Pickup" )]
public partial class HealthPickup : BasePickup
{
	protected override string ModelPath => "models/citizen_props/balloonheart01.vmdl";
	private const float Amount = 50f;

	public override void ClientSpawn()
	{
		base.ClientSpawn();

		Preview.ColorTint = Color.Green;
		Preview.Transform = Preview.Transform.WithScale( 0.8f );
	}

	public override void Touch( Entity other )
	{
		if ( !Game.IsServer || other is not Pawn player || !TimeUntilRespawn )
			return;

		if ( player.Health >= 100f )
			return;

		TimeUntilRespawn = 10f;
		player.Health = MathF.Min( 100f, player.Health + Amount );
	}
}
