using Editor;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pace;

[ClassName( "game_area" )]
[HammerEntity]
public partial class GameArea : Entity
{
	/// <summary>
	/// The gravity that is applied to the player.
	/// </summary>
	[Net, Property] public float Gravity { get; set; }

	public override void Spawn()
	{
		Transmit = TransmitType.Always;

		MyGame.Plane = new Plane( Position, Rotation.Forward );
	}

	public override void ClientSpawn()
	{
		MyGame.Plane = new Plane( Position, Rotation.Forward );
	}
}

