using Sandbox;

namespace Pace;

/// <summary>
/// A resource definition for a piece of equipment. This could be a weapon, or a deployable, or a gadget, or a grenade.. Anything really.
/// </summary>
[GameResource( "pace/Equipment Item", "equip", "", IconBgColor = "#5877E0", Icon = "track_changes" )]
public class EquipmentResource : GameResource
{
    public string Name { get; set; }
    public string Icon { get; set; }
    public GameObject Prefab { get; set; }
    public Model Model { get; set; }
}
