using Editor;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pace;

[HammerEntity]
public class MapPlane : Entity
{
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

