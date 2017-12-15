//Vertex shader output structure
struct VertexToPixel
{
	float4 VertexPosition : SV_Position0;

	float4 ABTextureCoord : TEXCOORD0;
	float3 shadPos : TEXCOORD1;
	float2 CTextureCoord : TEXCOORD2;
	float2 BlendTextureCoord : TEXCOORD3;
	float2 RoadTextureCoord : TEXCOORD4;
	float2 RoadCTextureCoord : TEXCOORD5;
	float3 vPos : TEXCOORD6;
	float3 Normal : TEXCOORD7;
};

struct VertexToShad
{
	float4 Position : SV_Position0;
    float Depth : TEXCOORD0;
};

texture2D VertexColorTex;
sampler2D USampler = sampler_state
{
	Texture = <VertexColorTex>;
	AddressU = CLAMP;
	AddressV = CLAMP;
	MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
};

texture2D ShadowMap;
sampler2D ShadSampler = sampler_state
{
	Texture = <ShadowMap>;
	AddressU = CLAMP;
	AddressV = CLAMP;
	MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
};

texture2D TextureAtlasTex;
sampler2D USamplerTex = sampler_state
{
	Texture = <TextureAtlasTex>;
	AddressU = CLAMP;
	AddressV = CLAMP;
	MipFilter = POINT;
    MinFilter = POINT;
    MagFilter = POINT;
};

texture2D TransAtlasTex;
sampler2D USamplerBlend = sampler_state
{
	Texture = <TransAtlasTex>;
	AddressU = CLAMP;
	AddressV = CLAMP;
	MipFilter = POINT;
    MinFilter = POINT;
    MagFilter = POINT;
};

texture2D RoadAtlasTex;
sampler2D RSamplerTex = sampler_state
{
	Texture = <RoadAtlasTex>;
	AddressU = CLAMP;
	AddressV = CLAMP;
	MipFilter = POINT;
    MinFilter = POINT;
    MagFilter = POINT;
};

texture2D RoadAtlasCTex;
sampler2D RCSamplerTex = sampler_state
{
	Texture = <RoadAtlasCTex>;
	AddressU = CLAMP;
	AddressV = CLAMP;
	MipFilter = POINT;
    MinFilter = POINT;
    MagFilter = POINT;
};
float4 LightCol;
float4 LightVec;
float2 ShadSize;
float ShadowMult;

float FogMaxDist;
float4 FogColor;

float4 GetCityColor(VertexToPixel Input)
{
	float4 BlendA = tex2D(USamplerBlend, Input.BlendTextureCoord);
	float4 Base = tex2D(USamplerTex, Input.ABTextureCoord.zw);
	float4 Blend = tex2D(USamplerTex, Input.CTextureCoord);
	float4 Road = tex2D(RSamplerTex, Input.RoadTextureCoord);
	float4 RoadC = tex2D(RCSamplerTex, Input.RoadCTextureCoord);
	
	float A = BlendA.x;
	float InvA = 1.0 - A;
	
	Base = Base*InvA + Blend*A;
	Base *= tex2D(USampler, Input.ABTextureCoord.xy);
	
	Base = Base*(1.0-Road.w) + Road*Road.w;
	Base = Base*(1.0-RoadC.w) + RoadC*RoadC.w;
	return Base * LightCol;
}

float shadowCompare(sampler2D map, float2 pos, float compare) {
	float depth = (float)tex2D(map, pos);
	return step(depth, compare);
}

float shadowLerp(sampler2D depths, float2 size, float2 uv, float compare){
	float2 texelSize = float2(1.0, 1.0)/size;
	float2 f = frac(uv*size+0.5);
	float2 centroidUV = floor(uv*size+0.5)/size;

	float lb = shadowCompare(depths, centroidUV+texelSize*float2(0.0, 0.0), compare);
	float lt = shadowCompare(depths, centroidUV+texelSize*float2(0.0, 1.0), compare);
	float rb = shadowCompare(depths, centroidUV+texelSize*float2(1.0, 0.0), compare);
	float rt = shadowCompare(depths, centroidUV+texelSize*float2(1.0, 1.0), compare);
	float a = lerp(lb, lt, f.y);
	float b = lerp(rb, rt, f.y);
	float c = lerp(a, b, f.x);
	return c;
}

float4 CityPS(VertexToPixel Input) : COLOR0
{
	float4 BCol = GetCityColor(Input);
	float depth = Input.shadPos.z;
	float diffuse = dot(normalize(Input.Normal.xyz), LightVec.xyz);
	if (diffuse < 0) diffuse *= 0.5;

	return float4(BCol.xyz*lerp(ShadowMult, 1, min(diffuse, shadowLerp(ShadSampler, ShadSize, Input.shadPos.xy, depth+0.003*(2048.0/ShadSize.x)))), 1);
}

float4 CityPSNoShad(VertexToPixel Input) : COLOR0
{
	float4 BCol = GetCityColor(Input);
	float diffuse = dot(normalize(Input.Normal.xyz), LightVec.xyz);
	if (diffuse < 0) diffuse *= 0.5;
	return float4(BCol.xyz*lerp(ShadowMult, 1, diffuse), 1);
}

float4 CityPSFog(VertexToPixel Input) : COLOR0
{
	float4 BCol = GetCityColor(Input);
	float diffuse = dot(normalize(Input.Normal.xyz), LightVec.xyz);
	if (diffuse < 0) diffuse *= 0.5;

	float4 fogDistance = min(1, length(Input.vPos)/FogMaxDist);
	BCol = float4(BCol.xyz*lerp(ShadowMult, 1, diffuse), 1);
	return lerp(BCol, FogColor, fogDistance);
}

float4 CityPSFogShad(VertexToPixel Input) : COLOR0
{
	float4 BCol = GetCityColor(Input);
	float depth = Input.shadPos.z;
	float diffuse = dot(normalize(Input.Normal.xyz), LightVec.xyz);
	if (diffuse < 0) diffuse *= 0.5;

	BCol = float4(BCol.xyz*lerp(ShadowMult, 1, min(diffuse, shadowLerp(ShadSampler, ShadSize, Input.shadPos.xy, depth + 0.003*(2048.0 / ShadSize.x)))), 1);

	float4 fogDistance = min(1, length(Input.vPos)/FogMaxDist);
	return lerp(BCol, FogColor, fogDistance);
}

float4 ShadowMapPS(VertexToShad Input) : COLOR0
{
	return float4(Input.Depth.x, 0, 0, 1);
}

technique RenderCity
{
	pass Final
	{
#if SM4
        PixelShader = compile ps_4_0_level_9_1 CityPS();
#else
        PixelShader = compile ps_3_0 CityPS();
#endif;
	}
	
	pass ShadowMap
	{
#if SM4
        PixelShader = compile ps_4_0_level_9_1 ShadowMapPS();
#else
        PixelShader = compile ps_3_0 ShadowMapPS();
#endif;
	}
	
	pass FinalNoShadow
	{
#if SM4
        PixelShader = compile ps_4_0_level_9_1 CityPSNoShad();
#else
        PixelShader = compile ps_3_0 CityPSNoShad();
#endif;
	}

	pass FinalFog
	{
#if SM4
		PixelShader = compile ps_4_0_level_9_1 CityPSFog();
#else
		PixelShader = compile ps_3_0 CityPSFog();
#endif;
	}

	pass FinalFogShadow
	{
#if SM4
		PixelShader = compile ps_4_0_level_9_1 CityPSFogShad();
#else
		PixelShader = compile ps_3_0 CityPSFogShad();
#endif;
	}
	//
}
