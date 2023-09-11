using Sandbox;
using System;
using System.Collections.Generic;

namespace Pace;

public static class ListExtensions
{
	public static void Shuffle<T>( this IList<T> list )
	{
		var n = list.Count;
		while ( n > 1 )
		{
			n--;
			var k = Game.Random.Next( 0, n + 1 );
			(list[n], list[k]) = (list[k], list[n]);
		}
	}

	public static int HashCombine<T>( this IEnumerable<T> e, Func<T, decimal> selector )
	{
		var result = 0;

		foreach ( var el in e )
			result = HashCode.Combine( result, selector.Invoke( el ) );

		return result;
	}
}
