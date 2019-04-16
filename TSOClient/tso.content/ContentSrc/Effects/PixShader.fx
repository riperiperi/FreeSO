#include "LightingCommon.fx"

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
	float2 TexCoord: TEXCOORD1;
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

	return gammaMul1(float4(BCol.xyz, 1), lerp(ShadowMult, 1, min(diffuse, shadowLerp(ShadSampler, ShadSize, Input.shadPos.xy, depth+0.003*(2048.0/ShadSize.x)))));
}

float4 CityPSNoShad(VertexToPixel Input) : COLOR0
{
	float4 BCol = GetCityColor(Input);
	float diffuse = dot(normalize(Input.Normal.xyz), LightVec.xyz);
	if (diffuse < 0) diffuse *= 0.5;
	return gammaMul1(float4(BCol.xyz, 1), lerp(ShadowMult, 1, diffuse));
}

float4 CityPSFog(VertexToPixel Input) : COLOR0
{
	float4 BCol = GetCityColor(Input);
	float diffuse = dot(normalize(Input.Normal.xyz), LightVec.xyz);
	if (diffuse < 0) diffuse *= 0.5;

	float fogDistance = min(1, length(Input.vPos)/FogMaxDist);
	BCol = gammaMul1(float4(BCol.xyz, 1), lerp(ShadowMult, 1, diffuse));
	return lerp(BCol, FogColor, fogDistance);
}

float4 CityPSFogShad(VertexToPixel Input) : COLOR0
{
	float4 BCol = GetCityColor(Input);
	float depth = Input.shadPos.z;
	float diffuse = dot(normalize(Input.Normal.xyz), LightVec.xyz);
	if (diffuse < 0) diffuse *= 0.5;

	BCol = gammaMul1(float4(BCol.xyz, 1), lerp(ShadowMult, 1, min(diffuse, shadowLerp(ShadSampler, ShadSize, Input.shadPos.xy, depth + 0.003*(2048.0 / ShadSize.x)))));

	float fogDistance = min(1, length(Input.vPos)/FogMaxDist);
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
        PixelShader = compile ps_4_0_level_9_3 CityPS();
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
		PixelShader = compile ps_4_0_level_9_3 CityPSFogShad();
#else
		PixelShader = compile ps_3_0 CityPSFogShad();
#endif;
	}
	//
}

//object pixel shader

struct ObjVertexOut
{
	float4 position : SV_Position0;
	float2 texCoord : TEXCOORD0;
	float3 vPos : TEXCOORD1;
	float3 normal : TEXCOORD2;
	float3 shadPos : TEXCOORD3;
};

texture2D ObjTex;
sampler2D ObjSampler = sampler_state
{
	Texture = <ObjTex>;
	AddressU = CLAMP;
	AddressV = CLAMP;
	MipFilter = LINEAR;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
};

float4 GetObjColor(ObjVertexOut Input) {
	float4 objCol = tex2D(ObjSampler, Input.texCoord);
	objCol.xyz /= objCol.w;
	return objCol;
}

float4 CityObjPS(ObjVertexOut Input) : COLOR0
{
	float4 BCol = GetObjColor(Input);
	if (BCol.a < 0.01) discard;
	float depth = Input.shadPos.z;
	float diffuse = 1;//dot(normalize(Input.normal.xyz), LightVec.xyz);
	if (diffuse < 0) diffuse *= 0.5;

	return gammaMul(float4(BCol.xyz, 1), float4(LightCol.xyz*lerp(ShadowMult, 1, min(diffuse, shadowLerp(ShadSampler, ShadSize, Input.shadPos.xy, depth + 0.003*(2048.0 / ShadSize.x)))), 1)) * BCol.a;
}

float4 CityObjPSNoShad(ObjVertexOut Input) : COLOR0
{
	float4 BCol = GetObjColor(Input);
	if (BCol.a < 0.01) discard;
	float diffuse = 1;//dot(normalize(Input.normal.xyz), LightVec.xyz);
	if (diffuse < 0) diffuse *= 0.5;
	return float4(gammaMul(float4(BCol.xyz, 1), float4(LightCol.xyz*lerp(ShadowMult, 1, diffuse),1)).rgb*BCol.a, BCol.a);
}

float4 CityObjPSFog(ObjVertexOut Input) : COLOR0
{
	float4 BCol = GetObjColor(Input);
	if (BCol.a < 0.01) discard;
	float diffuse = 1;//dot(normalize(Input.normal.xyz), LightVec.xyz);
	if (diffuse < 0) diffuse *= 0.5;

	float fogDistance = min(1, length(Input.vPos) / FogMaxDist);
	BCol = float4(gammaMul(float4(BCol.xyz, 1), float4(LightCol.xyz*lerp(ShadowMult, 1, diffuse), 1)).rgb, BCol.a);
	BCol.xyz = lerp(BCol.xyz, FogColor.xyz, fogDistance) * BCol.a;
	return BCol;
}

float4 CityObjPSFogShad(ObjVertexOut Input) : COLOR0
{
	float4 BCol = GetObjColor(Input);
	if (BCol.a < 0.01) discard;
	float depth = Input.shadPos.z;
	float diffuse = 1;//dot(normalize(Input.normal.xyz), LightVec.xyz);
	if (diffuse < 0) diffuse *= 0.5;

	BCol = float4(gammaMul(float4(BCol.xyz, 1), float4(LightCol.xyz * lerp(ShadowMult, 1, min(diffuse, shadowLerp(ShadSampler, ShadSize, Input.shadPos.xy, depth + 0.003*(2048.0 / ShadSize.x)))), 1)).rgb, BCol.a);

	float fogDistance = min(1, length(Input.vPos) / FogMaxDist);
	BCol.xyz = lerp(BCol.xyz, FogColor.xyz, fogDistance) * BCol.a;
	return BCol;
}

float4 ObjShadowMapPS(VertexToShad Input) : COLOR0
{
	if (tex2D(ObjSampler, Input.TexCoord).a < 0.8) discard;
	return float4(Input.Depth.x, 0, 0, 1);
}

technique RenderCityObj
{
	pass Final
	{
#if SM4
		PixelShader = compile ps_4_0_level_9_3 CityObjPS();
#else
		PixelShader = compile ps_3_0 CityObjPS();
#endif;
	}

	pass ShadowMap
	{
#if SM4
		PixelShader = compile ps_4_0_level_9_1 ObjShadowMapPS();
#else
		PixelShader = compile ps_3_0 ObjShadowMapPS();
#endif;
	}

	pass FinalNoShadow
	{
#if SM4
		PixelShader = compile ps_4_0_level_9_1 CityObjPSNoShad();
#else
		PixelShader = compile ps_3_0 CityObjPSNoShad();
#endif;
	}

	pass FinalFog
	{
#if SM4
		PixelShader = compile ps_4_0_level_9_1 CityObjPSFog();
#else
		PixelShader = compile ps_3_0 CityObjPSFog();
#endif;
	}

	pass FinalFogShadow
	{
#if SM4
		PixelShader = compile ps_4_0_level_9_3 CityObjPSFogShad();
#else
		PixelShader = compile ps_3_0 CityObjPSFogShad();
#endif;
	}
	//
}

//new city 

bool UseVertexColor;
struct CityVertexOut
{
	float4 VertexPosition : SV_Position0;

	float4 TextureCoords : TEXCOORD0; //base texture xy, mask zw
	float4 NormalTrans : TEXCOORD1; //normal xyz, transparency w
	float2 VertexCoord : TEXCOORD2;

	float3 shadPos : TEXCOORD3;
	float3 vPos : TEXCOORD4;
};

float4 GetNCityColor(CityVertexOut Input)
{
	float A = 1.0;
	if (Input.TextureCoords.z >= -0.5) {
		A = tex2D(USamplerBlend, Input.TextureCoords.zw).r;
	}
	float4 Base = tex2D(USamplerTex, Input.TextureCoords.xy);
	if (UseVertexColor == true) {
		Base *= tex2D(USampler, Input.VertexCoord.xy);
	}
	float InvA = 1.0 - A;
	return float4(Base.xyz, A*Base.w);
}

float Diffuse(CityVertexOut Input) {
	float diffuse = dot(normalize(Input.NormalTrans.xyz), LightVec.xyz);
	if (diffuse < 0) {
		diffuse *= 0.7;
		diffuse *= -diffuse;
	}
	return diffuse;
}

float4 Fog(float4 BCol, CityVertexOut Input) {
	float fogDistance = min(1, length(Input.vPos) / FogMaxDist);
	BCol.w *= (1.0 - Input.NormalTrans.w);
	BCol.xyz = lerp(BCol.xyz, FogColor.xyz, fogDistance) * BCol.w;
	return BCol;
}

float4 ShadLight(float4 BCol, CityVertexOut Input) {
	float depth = Input.shadPos.z;

	return float4(gammaMul(float4(BCol.xyz, 1), float4(LightCol.xyz * lerp(ShadowMult, 1, min(Diffuse(Input), shadowLerp(ShadSampler, ShadSize, Input.shadPos.xy, depth + 0.003*(2048.0 / ShadSize.x)))), 1)).rgb, BCol.w);
}

float4 NCityPS(CityVertexOut Input) : COLOR0
{
	float4 BCol = GetNCityColor(Input);
	float depth = Input.shadPos.z;

	return ShadLight(BCol, Input);
}

float4 NCityPSNoShad(CityVertexOut Input) : COLOR0
{
	float4 BCol = GetNCityColor(Input);
	return gammaMul(float4(BCol.xyz, 1), float4(LightCol.xyz * lerp(ShadowMult, 1, Diffuse(Input)), 1)) * BCol.w;
}

float4 NCityPSFog(CityVertexOut Input) : COLOR0
{
	float4 BCol = GetNCityColor(Input);
	BCol = float4(gammaMul(float4(BCol.xyz, 1), float4(LightCol.xyz * lerp(ShadowMult, 1, Diffuse(Input)), 1)).rgb, BCol.w);

	return Fog(BCol, Input);
}

float4 NCityPSFogShad(CityVertexOut Input) : COLOR0
{
	float4 BCol = GetNCityColor(Input);
	float depth = Input.shadPos.z;

	return Fog(ShadLight(BCol, Input), Input);
}

technique RenderNCity
{
pass Final
{
#if SM4
	PixelShader = compile ps_4_0_level_9_3 NCityPS();
#else
	PixelShader = compile ps_3_0 NCityPS();
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
	PixelShader = compile ps_4_0_level_9_1 NCityPSNoShad();
#else
	PixelShader = compile ps_3_0 NCityPSNoShad();
#endif;
}

pass FinalFog
{
#if SM4
	PixelShader = compile ps_4_0_level_9_1 NCityPSFog();
#else
	PixelShader = compile ps_3_0 NCityPSFog();
#endif;
}

pass FinalFogShadow
{
#if SM4
	PixelShader = compile ps_4_0_level_9_3 NCityPSFogShad();
#else
	PixelShader = compile ps_3_0 NCityPSFogShad();
#endif;
}
//
}

// Water Shader

texture2D BigWTex;
sampler2D BigWSampler = sampler_state
{
	Texture = <BigWTex>;
	AddressU = WRAP;
	AddressV = WRAP;
	MipFilter = LINEAR;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
};

texture2D SmallWTex;
sampler2D SmallWSampler = sampler_state
{
	Texture = <SmallWTex>;
	AddressU = WRAP;
	AddressV = WRAP;
	MipFilter = LINEAR;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
};

float Time;
float WavePow;
float SunStrength;
float RealNormalPct;
float4x4 InvView;

float3 NTex(float4 col) {
	return col.xzy - float3(0.5, 0.5, 0.5);
}

float4 WSpecular(CityVertexOut Input) {
	float3 light = normalize(LightVec.xyz);

	float3 normal = NTex(tex2D(SmallWSampler, Input.VertexCoord*256 + Time * float2(0.62, 0.23)/15.0)) * 0.6
		+ NTex(tex2D(SmallWSampler, Input.VertexCoord * 196 + Time * float2(-0.11, -0.73) / 11.0)) * 0.6
		+ NTex(tex2D(BigWSampler, Input.VertexCoord * 5 + Time * float2(-0.93, 0.06) / 18.0))
		+ Input.NormalTrans.xyz * RealNormalPct;
	normal = normalize(normal);

	//float3 normal = normalize(Input.NormalTrans.xyz);
	float3 r = normalize(2 * dot(light, normal) * normal - light);
	float3 camVec = normalize(-mul(Input.vPos, InvView).xyz);
	float3 v = camVec;

	float dotProduct = dot(r, v);
	float SunShininess = 120;
	float SkyShininess = 0.20;
	float SpecularIntensity = 1;

	float cosan = abs(dot(camVec, normal));

	return LinearToSRGB(float4(LightCol.xyz, 1)) * SpecularIntensity * (pow(max(dotProduct, 0), SunShininess) * 0.6*SunStrength + (1 - pow(cosan, SkyShininess))*0.75);
}

float4 GetWCityColor(CityVertexOut Input) {
	float4 baseCol = GetNCityColor(Input);

	//create waves when alpha is < 1, indicating we're blending into land
	//assume we're closer to land the closer land is to 0
	//currently waves take 4 seconds, and repeat every 8

	float waveTime = ((Time + Input.VertexCoord.x *64.0 + Input.VertexCoord.y *32.0)%7.0) / 7.0;

	float waveScale = max(0, (1.0 - waveTime));
	waveScale = pow(abs(waveScale), WavePow);

	float closeness = 1-smoothstep(0.0, 0.15, abs(baseCol.a - waveScale));
	float startEnd = 1 - smoothstep(0.4, 0.5, max(abs(waveTime-0.5), abs(waveScale-0.5)));
	float a = closeness*startEnd * (1.0-step(0.499, abs(baseCol.a - 0.5)));

	return lerp(baseCol + float4(WSpecular(Input).xyz, 0), LinearToSRGB(float4(LightCol.xyz, 1.0)), a);
}

float4 WCityPS(CityVertexOut Input) : COLOR0
{
	float4 BCol = GetWCityColor(Input);
	float depth = Input.shadPos.z;

	return ShadLight(BCol, Input);
}

float4 WCityPSNoShad(CityVertexOut Input) : COLOR0
{
	float4 BCol = GetWCityColor(Input);
	return gammaMul(float4(BCol.xyz, 1), float4(LightCol.xyz * lerp(ShadowMult, 1, Diffuse(Input)), 1)) * BCol.w;
}

float4 WCityPSFog(CityVertexOut Input) : COLOR0
{
	float4 BCol = GetWCityColor(Input);
	BCol = float4(gammaMul(float4(BCol.xyz, 1), float4(LightCol.xyz * lerp(ShadowMult, 1, Diffuse(Input)), 1)).rgb, BCol.w);

	return Fog(BCol, Input);
}

float4 WCityPSFogShad(CityVertexOut Input) : COLOR0
{
	float4 BCol = GetWCityColor(Input);
	float depth = Input.shadPos.z;

	return Fog(ShadLight(BCol, Input), Input);
}

technique RenderWCity
{
	pass Final
	{
#if SM4
		PixelShader = compile ps_4_0_level_9_3 WCityPS();
#else
		PixelShader = compile ps_3_0 WCityPS();
#endif;
	}

	pass ShadowMap
	{
#if SM4
		PixelShader = compile ps_4_0_level_9_3 ShadowMapPS();
#else
		PixelShader = compile ps_3_0 ShadowMapPS();
#endif;
	}

	pass FinalNoShadow
	{
#if SM4
		PixelShader = compile ps_4_0_level_9_3 WCityPSNoShad();
#else
		PixelShader = compile ps_3_0 WCityPSNoShad();
#endif;
	}

	pass FinalFog
	{
#if SM4
		PixelShader = compile ps_4_0_level_9_3 WCityPSFog();
#else
		PixelShader = compile ps_3_0 WCityPSFog();
#endif;
	}

	pass FinalFogShadow
	{
#if SM4
		PixelShader = compile ps_4_0_level_9_3 WCityPSFogShad();
#else
		PixelShader = compile ps_3_0 WCityPSFogShad();
#endif;
	}
	//
}

float4 HighlightColor;
float4 NeighHighlightPS(ObjVertexOut Input) : COLOR0
{
	return HighlightColor * tex2D(ObjSampler, Input.texCoord).r;
}

technique NeighHighlight
{
	pass Final
	{
#if SM4
		PixelShader = compile ps_4_0_level_9_1 NeighHighlightPS();
#else
		PixelShader = compile ps_3_0 NeighHighlightPS();
#endif;
	}
}