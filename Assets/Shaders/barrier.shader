
HEADER
{
	Description = "";
}

FEATURES
{
	#include "common/features.hlsl"
}

MODES
{
	Forward();
	Depth( S_MODE_DEPTH );
	ToolsShadingComplexity( "tools_shading_complexity.shader" );
}

COMMON
{
	#include "common/shared.hlsl"
	#include "procedural.hlsl"
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
	float4 vColor : COLOR0 < Semantic( Color ); >;
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
	float3 vPositionOs : TEXCOORD14;
	float3 vNormalOs : TEXCOORD15;
	float4 vTangentUOs_flTangentVSign : TANGENT	< Semantic( TangentU_SignV ); >;
	float4 vColor : COLOR0;
	float4 vTintColor : COLOR1;
};

VS
{
	#include "common/vertex.hlsl"

	PixelInput MainVs( VertexInput v )
	{
		
		PixelInput i = ProcessVertex( v );
		i.vPositionOs = v.vPositionOs.xyz;
		i.vColor = v.vColor;
		
		ExtraShaderData_t extraShaderData = GetExtraPerInstanceShaderData( v );
		i.vTintColor = extraShaderData.vTint;
		
		VS_DecodeObjectSpaceNormalAndTangent( v, i.vNormalOs, i.vTangentUOs_flTangentVSign );
		return FinalizeVertex( i );
		
	}
}

PS
{
	#define CUSTOM_MATERIAL_INPUTS

	#include "common/pixel.hlsl"

	BoolAttribute( translucent, true );
	RenderState( DepthWriteEnable, false);
	RenderState( CullMode, NONE );
	RenderState( BlendEnable, true );
	RenderState( AlphaToCoverageEnable, true );

	CreateInputTexture2D( PatternMap, Linear, 8, "", "_pattern", "My Material,10/10", Default(1));
	Texture2D g_tPattern <Channel(RGB, Box(PatternMap), Linear ); OutputFormat( BC7 ); SrgbRead( false ); >;
	float g_flInnerAlpha <UiType( Slider); Range(0, 1); UiGroup("My Material,10/11"); >;
	float g_flFresnelPower <UiType( Slider ); Range(0, 4); UiGroup("My Material,10/12"); >;

	float4 MainPs( PixelInput i ) : SV_Target0
	{
		Material m = Material::Init();
		m.Albedo = float3( 0, 0, 0 );
		m.Normal = float3( 0, 0, 1 );
		m.Roughness = 1;
		m.Metalness = 0;
		m.AmbientOcclusion = 1;
		m.TintMask = 1;
		m.Opacity = 1;
		m.Emission = float3( 0, 0, 0 );
		m.Transmission = 0;
		m.WorldPosition = i.vPositionWithOffsetWs + g_vHighPrecisionLightingOffsetWs.xyz;
        m.WorldPositionWithOffset = i.vPositionWithOffsetWs;
        m.ScreenPosition = i.vPositionSs;

		float2 uv = i.vTextureCoords.xy;
		float3 color = i.vTintColor.xyz;
		float3 cameraDir = CalculatePositionToCameraDirWs( m.WorldPosition );
		float frontface = saturate( round( dot( normalize( i.vNormalWs ), normalize( cameraDir )) + 0.5) );
		float fresnel = saturate( pow( 1.0 - dot( normalize( i.vNormalWs ), normalize( cameraDir )), g_flFresnelPower ) );
		float scroll = saturate( sin(-g_flTime + 4 * uv.y) - 0.9 ) * 10;
		float distanceToTerrain = length(m.WorldPosition - Depth::GetWorldPosition(m.ScreenPosition));
		float intersection = smoothstep(15, 0, distanceToTerrain);
		float pattern = lerp(float3(0, 0, 0), g_tPattern.Sample(g_sAniso, uv).xyz, frontface * (scroll + fresnel)).x;		
		float alpha = saturate(frontface * (g_flInnerAlpha + pattern + fresnel) + intersection);

		m.AmbientOcclusion = saturate( m.AmbientOcclusion );
		m.Emission = color * alpha;
		m.Roughness = saturate( m.Roughness );
		m.Metalness = saturate( m.Metalness );
		m.Opacity = alpha;
		
		// Result node takes normal as tangent space, convert it to world space now
		m.Normal = TransformNormal( m.Normal, i.vNormalWs, i.vTangentUWs, i.vTangentVWs );
		
		// for some toolvis shit
		m.WorldTangentU = i.vTangentUWs;
		m.WorldTangentV = i.vTangentVWs;
		m.TextureCoords = i.vTextureCoords.xy;
				
		return ShadingModelStandard::Shade( i, m );
	}
}
