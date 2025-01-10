using Sandbox;

namespace Pace.UI;

public interface IMinimapElement
{
    [Property] public Color Color { get; }
    public Vector3 WorldPosition { get; }
    public bool IsVisible { get; }
}