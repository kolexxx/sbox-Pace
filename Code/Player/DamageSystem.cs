using Sandbox;
using System;

namespace Pace;

[Flags]
public enum DamageFlags
{
    None = 0,
    Bullet = 1 << 1,
    Explosive = 1 << 2,
    Burn = 1 << 3,
    Critical = 1 << 4,
    WallBang = 1 << 5
}

public record class DamageInfo
{
    /// <summary>
    /// The damage flags associated with this event
    /// </summary>
    public DamageFlags Flags { get; set; }

    /// <summary>
    /// Who dunnit?
    /// </summary>
    public Component Attacker { get; set; }

    /// <summary>
    /// What caused the damage? Weapon?
    /// </summary>
    public Component Weapon { get; set; }

    /// <summary>
    /// Who's the victim?
    /// </summary>
    public Component Victim { get; set; }

    /// <summary>
    /// How much damage?
    /// </summary>
    public float Damage { get; set; }

    /// <summary>
    /// Force
    /// </summary>
    public Vector3 Force { get; set; }

    /// <summary>
    /// Where?
    /// </summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// How long since this damage info event happened?
    /// </summary>
    public TimeSince TimeSince { get; init; } = 0;

    public override string ToString()
    {
        return $"\"{Attacker}\" - \"{Victim}\" with \"{Weapon}\" ({Damage} damage)";
    }
}