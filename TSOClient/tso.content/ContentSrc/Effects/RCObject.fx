float4x4 World;
float4x4 ViewProjection;

float ObjectID;
float4 AmbientLight;

//LIGHTING
float4 OutsideLight;
float4 OutsideDark;
float4 MaxLight;
float3 WorldToLightFactor;
float2 LightOffset;
float2 MapLayout;
float Level;
//END LIGHTING

texture MeshTex;
sampler TexSampler = sampler_state {
	texture = <MeshTex>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Clamp;
	AddressV = Clamp;
};

texture AnisoTex;
sampler AnisoSampler = sampler_state {
	texture = <AnisoTex>;
	MipFilter = Anisotropic;
	MagFilter = Anisotropic;
	MinFilter = Anisotropic;
	AddressU = Clamp;
	AddressV = Clamp;
	MaxAnisotropy = 4;
};

texture MaskTex;
sampler MaskSampler = sampler_state {
	texture = <MaskTex>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Clamp;
	AddressV = Clamp;
};

float SideMask;

texture advancedLight : Diffuse;
sampler advLightSampler = sampler_state {
	texture = <advancedLight>;
	AddressU = Clamp; AddressV = Clamp; AddressW = Clamp;
	MIPFILTER = LINEAR; MINFILTER = LINEAR; MAGFILTER = LINEAR;
};

struct VertexIn
{
	float4 position : SV_Position0;
	float2 texCoord : TEXCOORD0;
};

struct VertexOut
{
	float4 position : SV_Position0;
	float2 texCoord : TEXCOORD0;
	float4 modelPos : TEXCOORD1;
};


float4 lightColor(float4 intensities) {
	// RGBA: LightIntensity, OutdoorsIntensity, LightIntensityShad, OutdoorsIntensityShad
	float lightFactor = (intensities.x == 0) ? 0 : (intensities.x * (intensities.z / intensities.x));
	float outlightFactor = (intensities.y == 0) ? 0 : (intensities.y * (intensities.w / intensities.y));

	float4 col = lerp(lerp(OutsideDark, OutsideLight, outlightFactor), MaxLight, lightFactor);
	//float4 col = lerp(lerp(float4(0.5,0.5,0.5,1), float4(1,1,1,1), outlightFactor), float4(1, 1, 1, 1), lightFactor);

	return col;
}

float4 lightProcess(float4 inPosition) {
	float2 orig = inPosition.x;
	inPosition.xyz *= WorldToLightFactor;
	inPosition.xz += LightOffset;

	inPosition.xz += 1 / MapLayout * floor(float2(Level % MapLayout.x, Level / MapLayout.x));

	float4 lTex = tex2D(advLightSampler, inPosition.xz);
	lTex = float4(lTex.x, lTex.y, lTex.x, lTex.y); //lerp(lTex, float4(lTex.x, lTex.y, lTex.x, lTex.y), clamp((inPosition.y % 1) * 3, 0, 1));
	return lightColor(lTex);
}

float4 lightInterp(float4 inPosition) {
	inPosition.xyz *= WorldToLightFactor;
	inPosition.xz += LightOffset;

	float level = floor(inPosition.y) + 0.0001; 
	float abvLevel = min(Level, level + 1);
	float2 iPA = inPosition.xz + 1 / MapLayout * floor(float2(abvLevel % MapLayout.x, abvLevel / MapLayout.x));
	inPosition.xz += 1 / MapLayout * floor(float2(level % MapLayout.x, level / MapLayout.x));

	float4 lTex = tex2D(advLightSampler, inPosition.xz);
	lTex.xz = lerp(lTex.xz, tex2D(advLightSampler, iPA).xz, max(0, (inPosition.y % 1) * 2 - 1));

	lTex = lerp(lTex, float4(lTex.x, lTex.y, lTex.x, lTex.y), clamp((inPosition.y % 1) * 3, 0, 1));
	return lightColor(lTex);
}


VertexOut vsRC(VertexIn v) {
	VertexOut result;

	result.texCoord = v.texCoord;

	float4 wPos = mul(v.position, World);
	float4 finalPos = mul(wPos, ViewProjection);
	result.position = finalPos;
	result.modelPos = wPos;

	return result;
}

float4 psRC(VertexOut v) : COLOR0
{
	float4 color = tex2D(TexSampler, v.texCoord) * lightProcess(v.modelPos);
	return color;
}


float4 psDisabledRC(VertexOut v) : COLOR0
{
	float4 color = tex2D(TexSampler, v.texCoord) * lightProcess(v.modelPos);
	float gray = dot(color.xyz, float3(0.2989, 0.5870, 0.1140));
	color = float4(gray, gray, gray, color.a);
	return color;
}

struct WallVertexIn
{
	float4 position : SV_Position0;
	float4 color : COLOR0;
	float2 texCoord : TEXCOORD0;
};

struct WallVertexOut
{
	float4 position : SV_Position0;
	float4 color : COLOR0;
	float2 texCoord : TEXCOORD0;
	float4 modelPos : TEXCOORD1;
};

SamplerState g_samPoint
{
	Filter = POINT;
	AddressU = Wrap;
	AddressV = Wrap;
};

WallVertexOut vsWallRC(WallVertexIn v) {
	WallVertexOut result;

	result.texCoord = v.texCoord;

	float4 wPos = mul(v.position, World);

	/*if (v.texCoord.y > CurrentLevel + 0.1) {
		//can be subject to cutaway
		if (CutawayTex.SampleLevel(g_samPoint, wPos.xz * WorldToLightFactor.xz + CutawayOffset, 0).a > 0.5f) wPos.y -= 2.45f;
	}*/

	float4 finalPos = mul(wPos, ViewProjection);
	result.color = v.color;
	result.position = finalPos;
	result.modelPos = wPos;

	return result;
}

float4 psWallRC(WallVertexOut v) : COLOR0
{
	float4 mPos = v.modelPos;
	mPos.y = v.texCoord.y*2.95*3;
	float2 texC = v.texCoord;
	texC.x = frac(texC.x);
	texC.y = frac(((v.texCoord.y % 1)-1/240)/-1.04);
	float4 color = v.color * tex2Dgrad(AnisoSampler, texC, ddx(v.texCoord), ddy(v.texCoord)) * lightInterp(mPos); //tex2D(TexSampler, texC) * lightInterp(mPos); version for no mipmaps
	if (SideMask != 0) {
		//our mask is actually a texture of a top right wall.
		//skew the texcoord appropriately.

		texC.x = frac(texC.x);
		texC.y = frac((frac(v.texCoord.y)*0.970)*(-(1-0.1185))+(1-texC.x)*0.1185*SideMask - 0.117);
	}
	float4 maskC = tex2D(MaskSampler, texC);
	color.a *= maskC.a;
	if (color.a < 0.1) discard;
	return color;
}

WallVertexOut vsWallLMap(WallVertexIn v) {
	WallVertexOut result;

	float4 position = v.position;
	float2 tc = v.texCoord;
	//we don't care about the terrain elevation of walls in this mode, only their level...
	//first we want to remove cutaways. this is easy - ceiling the y component of the texcoord
	tc.y = ceil(tc.y - 0.001);
	position.z = tc.y; //this makes a wall's height equal to its level. of course, two 
	result.texCoord = tc;

	float4 wPos = mul(position, World);
	float4 finalPos = mul(wPos, ViewProjection);
	result.color = v.color;
	result.position = finalPos;
	result.modelPos = wPos;

	return result;
}

float4 psWallLMap(WallVertexOut v) : COLOR0
{
	float2 texC = v.texCoord;
	if (texC.y - 0.001 < Level) discard; //ignore under current level
	//fade out as we get further away from the floor.
	//of course, lightmaps for upper levels
	float4 color = float4(1, 1, 1, 1) * (1 - (texC.y - Level) / 5); 

	//still want to mask, of course...
	texC.x = frac(texC.x);
	texC.y = frac(((v.texCoord.y % 1) - 1 / 240) / -1.04);

	if (SideMask != 0) {
		//our mask is actually a texture of a top right wall.
		//skew the texcoord appropriately.

		texC.x = frac(texC.x);
		texC.y = frac((frac(v.texCoord.y)*0.970)*(-(1 - 0.1185)) + (1 - texC.x)*0.1185*SideMask - 0.117);
	}
	float4 maskC = tex2D(MaskSampler, texC);
	color.a *= maskC.a;
	if (color.a < 0.02) discard;
	return color;
}

technique Draw
{
	pass Pass1
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_3 vsRC();
		PixelShader = compile ps_4_0_level_9_3 psRC();
#else
		VertexShader = compile vs_3_0 vsRC();
		PixelShader = compile ps_3_0 psRC();
#endif;
	}
}

technique DisabledDraw
{
	pass Pass1
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_3 vsRC();
		PixelShader = compile ps_4_0_level_9_3 psDisabledRC();
#else
		VertexShader = compile vs_3_0 vsRC();
		PixelShader = compile ps_3_0 psDisabledRC();
#endif;
	}
}

technique WallDraw
{
	pass Pass1
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_3 vsWallRC();
		PixelShader = compile ps_4_0_level_9_3 psWallRC();
#else
		VertexShader = compile vs_3_0 vsWallRC();
		PixelShader = compile ps_3_0 psWallRC();
#endif;
	}
}

technique WallLMap
{
	pass Pass1
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_3 vsWallLMap();
		PixelShader = compile ps_4_0_level_9_3 psWallLMap();
#else
		VertexShader = compile vs_3_0 vsWallLMap();
		PixelShader = compile ps_3_0 psWallLMap();
#endif;
	}
}