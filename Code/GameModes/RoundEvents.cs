using Sandbox;

namespace Pace;

public interface IRoundEvent : ISceneEvent<IRoundEvent>
{
    void OnStart() { }
    void OnEnd() { }
}