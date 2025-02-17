using Sandbox;

namespace Pace;

public interface IPlayerEvent : ISceneEvent<IPlayerEvent>
{
    void OnPlayerSpawned( Player player ) { }
    void OnPlayerKilled( Player player ) { }
}