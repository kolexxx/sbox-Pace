using Sandbox;
using System.Collections.Generic;

namespace Pace;

public class ProjectileSimulator : EntityComponent
{
	public List<Projectile> List { get; private set; } = new();

	public void Add( Projectile projectile )
	{
		List.Add( projectile );
	}

	public void Remove( Projectile projectile )
	{
		List.Remove( projectile );
	}

	public void Clear()
	{
		foreach ( var projectile in List )
		{
			projectile.Delete();
		}

		List.Clear();
	}

	public void Simulate( IClient client )
	{
		for ( var i = List.Count - 1; i >= 0; i-- )
		{
			var projectile = List[i];

			if ( !projectile.IsValid() )
			{
				List.RemoveAt( i );
				continue;
			}

			if ( Prediction.FirstTime )
				projectile.Simulate( client );
		}
	}
}
