using Sandbox;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Pace;

public enum FireMode
{
	Semi = 0,
	Automatic = 1,
	Burst = 2
}

[GameResource( "Weapon Definition", "weapon", "Weapon Definition", Icon = "build_circle", IconBgColor = "#4953a7", IconFgColor = "#2a3060" )]
public class WeaponDefinition : GameResource
{
	private static readonly Dictionary<string, WeaponDefinition> _collection = new( StringComparer.OrdinalIgnoreCase );

	[Category( "Important" )]
	public string ClassName { get; set; }

	[Title( "Icon" ), Category( "UI" ), ResourceType( "png" )]
	public string IconPath { get; set; } = "ui/none.png";

	[Category( "Important" )]
	public int Slot { get; set; } = 0;

	[Category( "WorldModels" )]
	public CitizenAnimationHelper.HoldTypes HoldType { get; set; } = 0;

	[Title( "Model" ), Category( "WorldModels" ), ResourceType( "vmdl" )]
	public string ModelPath { get; set; } = "";

	[Category( "Stats" )]
	public float DeployTime { get; set; } = 0.6f;

	[Category( "Stats" )]
	public float ReloadTime { get; set; } = 0.6f;

	[Category( "Sounds" ), ResourceType( "sound" )]
	public string FireSound { get; set; } = "";

	[Category( "Stats" )]
	[Description( "The amount of bullets that come out in one shot." )]
	public int BulletsPerFire { get; set; } = 1;

	[Category( "Important" )]
	public FireMode FireMode { get; set; } = FireMode.Semi;

	[Category( "Stats" )]
	public int ClipSize { get; set; } = 30;

	[Category( "Stats" )]
	public float Damage { get; set; } = 20;

	[Category( "Stats" )]
	public float Spread { get; set; } = 0f;

	[Category( "Stats" )]
	public float PrimaryRate { get; set; } = 0f;

	[Category( "VFX" ), ResourceType( "vpcf" )]
	public string EjectParticle { get; set; } = "";

	[Category( "VFX" ), ResourceType( "vpcf" )]
	public string MuzzleFlashParticle { get; set; } = "";

	[Category( "VFX" ), ResourceType( "vpcf" )]
	public string TracerParticle { get; set; } = "";

	[HideInEditor]
	[JsonIgnore]
	public Texture Icon { get; private set; }

	[HideInEditor]
	[JsonIgnore]
	public Model Model { get; private set; }

	public static WeaponDefinition Get( Type type )
	{
		if ( type is null )
			return null;

		return _collection[TypeLibrary.GetType( type ).ClassName];
	}

	protected override void PostLoad()
	{
		if ( TypeLibrary is null )
			return;

		_collection.Add( ClassName, this );

		Icon = Texture.Load( FileSystem.Mounted, IconPath );
		Model = Model.Load( ModelPath );
	}
}
