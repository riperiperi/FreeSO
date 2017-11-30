#include "LightingCommon.fx"

float4x4 Projection;
float4x4 View;
float4x4 World;

float2 ScreenSize;
float4 LightGreen;
float4 DarkGreen;
float4 LightBrown;
float4 DarkBrown;
float4 DiffuseColor;
float2 ScreenOffset;
float GrassProb;
float GrassFadeMul;

float2 TexOffset;
float4 TexMatrix;

float2 TileSize;

bool depthOutMode;
float3 CamPos;
float3 LightVec;
float GrassShininess;
bool UseTexture;
bool IgnoreColor;
texture BaseTex;
sampler TexSampler = sampler_state {
	texture = <BaseTex>;
	AddressU = Wrap;
	AddressV = Wrap;
	MIPFILTER = LINEAR; MINFILTER = LINEAR; MAGFILTER = LINEAR;
};

sampler AnisoTexSampler = sampler_state {
	texture = <BaseTex>;
	AddressU = Wrap;
	AddressV = Wrap;

	MipFilter = Anisotropic;
	MagFilter = Anisotropic;
	MinFilter = Anisotropic;
	MaxAnisotropy = 16;
};

texture RoomMap : Diffuse;
sampler RoomMapSampler = sampler_state {
	texture = <RoomMap>;
	AddressU = WRAP; AddressV = WRAP; AddressW = WRAP;
	MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};

texture RoomLight : Diffuse;
sampler RoomLightSampler = sampler_state {
	texture = <RoomLight>;
	AddressU = WRAP; AddressV = WRAP; AddressW = WRAP;
	MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};

texture TerrainNoise : Diffuse;
sampler TerrainNoiseSampler = sampler_state {
	texture = <TerrainNoise>;
	AddressU = WRAP; AddressV = WRAP; AddressW = WRAP;
	MIPFILTER = NONE; MINFILTER = POINT; MAGFILTER = POINT;
	MaxLOD = 0;
};

texture TerrainNoiseMip : Diffuse;
sampler TerrainNoiseMipSampler = sampler_state {
	texture = <TerrainNoiseMip>;
	AddressU = WRAP; AddressV = WRAP; AddressW = WRAP;
	MipFilter = Anisotropic;
	MagFilter = Anisotropic;
	MinFilter = Anisotropic;
	MaxAnisotropy = 16;
};

#if SIMPLE
texture depthMap : Diffuse;
sampler depthMapSampler = sampler_state {
	texture = <depthMap>;
	AddressU = CLAMP; AddressV = CLAMP; AddressW = CLAMP;
	MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};
#endif

struct GrassVTX
{
    float4 Position : SV_Position0;
    float4 Color : COLOR0;
    float4 GrassInfo : TEXCOORD0; //x is liveness, yz is position
	float3 Normal : TEXCOORD1;
};

struct GrassPSVTX {
    float4 Position : SV_Position0;
    float4 Color : COLOR0;
    float4 GrassInfo : TEXCOORD0; //x is liveness, yz is position
    float3 ScreenPos : TEXCOORD1;
	float4 ModelPos : TEXCOORD2;
	float3 Normal : TEXCOORD3;
};

// from shadertoy.
// https://www.shadertoy.com/view/4djSRW


float nrand(float2 n){
	return frac(sin(dot(n.xy, float2(12.9898, 78.233)))* 43758.5453);
}

float2 nrand2(float2 n) {
	return float2(nrand(n), nrand(n*float2(4, 3)));
}

float2 hash22(float2 p)
{
	float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx+19.19);
    return frac(float2((p3.x + p3.y)*p3.z, (p3.x+p3.z)*p3.y));
}

/*
old shader code for grass noise. now we use a texture.

float2 iterhash22(in float2 uv) {
    float2 a = float2(0,0);
    for (int t = 0; t < 2; t++)
    {
        float v = float(t+1)*.132;
        a += hash22(uv*v);
    }
    return a / 2.0;
}*/

float2 iterhash22(float2 uv) {
	return tex2D(TerrainNoiseSampler, uv / 512.0).xy;
}

float4 packDepth(float d) {
    float4 enc = float4(1.0, 255.0, 65025.0, 0.0) * d;
    enc = frac(enc);
    enc -= enc.yzww * float4(1.0 / 255.0, 1.0 / 255.0, 1.0 / 255.0, 0.0);
    enc.a = 1;

    return enc; //float4(byteFloor(d%1.0), byteFloor((d*256.0) % 1.0), byteFloor((d*65536.0) % 1.0), 1); //most sig in r, least in b
}

float unpackDepth(float4 d) {
    return dot(d, float4(1.0, 1 / 255.0, 1 / 65025.0, 0)); //d.r + (d.g / 256.0) + (d.b / 65536.0);
}

float2 RoomIDToUV(float room) {
	return float2((room % 256) / 255.5, floor(room / 256) / 255.5);
}

float GetRoomID(float2 uv) {
	float4 test = tex2D(RoomMapSampler, uv * TileSize);
	float room1 = round(test.x * 255 + (test.y * 65280));
	float room2 = round(test.z * 255 + (test.w * 65280));
	bool diagType = (room2 > 32767);
	if (diagType == true) {
		room2 -= 32768;
	}
	if (room1 != room2) {
		//diagonal mode
		if (diagType == true) {
			//horizontal diag:
			if ((uv.x % 1) + (uv.y % 1) >= 1)
				return (room2); //hi room
			else
				return (room1); //low room
		}
		else {
			//vertical diag:
			if ((uv.x % 1) - (uv.y % 1) > 0)
				return (room1); //low room
			else
				return (room2); //hi room
		}
	}
	else {
		return room1;
	}
}

float4 SimpleLight(float2 uv) {
	return tex2D(RoomLightSampler, RoomIDToUV(GetRoomID(uv / 3)));
}

GrassPSVTX GrassVS(GrassVTX input)
{
    GrassPSVTX output = (GrassPSVTX)0;
    float4x4 Temp4x4 = mul(World, mul(View, Projection));
    float4 position = mul(input.Position, Temp4x4);
    output.Position = position;
	output.ModelPos = mul(input.Position, World);
    output.Color = input.Color;
    output.GrassInfo = input.GrassInfo;
	output.GrassInfo.yz = output.GrassInfo.yz*TexMatrix.xw + output.GrassInfo.zy*TexMatrix.zy + TexOffset;
    output.GrassInfo.w = position.z / position.w;
	output.Normal = input.Normal;

	float4 position2 = mul(float4(input.Position.x, 0, input.Position.z, input.Position.w), Temp4x4);
	output.ScreenPos.xy = ((position2.xy*float2(0.5, -0.5)) + float2(0.5, 0.5)) * ScreenSize;
	output.ScreenPos.z = position.z;

    if (output.GrassInfo.x == -1.2 && output.GrassInfo.y == -1.2 && output.GrassInfo.z == -1.2 && output.GrassInfo.w < -1.0 && output.ScreenPos.x < -200 && output.ScreenPos.y < -300) output.Color *= 0.5; 

    return output;
}
	
float4 CM(float mult) {
	return float4(mult, mult, mult, 1);
}

float4 LightDot(float3 normal) {
	return CM(dot(LightVec, normalize(normal)) * 0.5f + 0.5f);
}

float4 LightSpecular(float3 normal, float4 modelpos) {
	float3 pos = normalize(CamPos - modelpos.xyz);
	float cosan = abs(dot(pos, normal));
	return DiffuseColor*(1-pow(cosan, GrassShininess));
}

#if SIMPLE
void BladesPS(GrassPSVTX input, out float4 color:COLOR0)
{
    float4 depthB;
#else
void BladesPS(GrassPSVTX input, out float4 color:COLOR0, out float4 depthB : COLOR1)
{
#endif

    float2 rand = iterhash22(input.ScreenPos.xy+ScreenOffset); //nearest neighbour effect
    if (rand.y > GrassProb*((2.0-input.GrassInfo.x)/2)) discard;
    //grass blade here

    float d = input.GrassInfo.w;

    depthB = packDepth(d);
    if (depthOutMode == true) {
        color = depthB;
    }
    else {
#if SIMPLE
		//software depth
		if (unpackDepth(tex2D(depthMapSampler, input.ScreenPos.xy / ScreenSize)) < d) discard;
#endif
        float bladeCol = rand.x*0.6;
        float4 green = lerp(LightGreen, DarkGreen, bladeCol);
        float4 brown = lerp(LightBrown, DarkBrown, bladeCol);
		color = lerp(green, brown, input.GrassInfo.x) * lightProcessFloor(input.ModelPos) * LightDot(input.Normal);//DiffuseColor;
    }
}

#if SIMPLE
void BladesPSSimple(GrassPSVTX input, out float4 color:COLOR0)
{
	float4 depthB;
#else
void BladesPSSimple(GrassPSVTX input, out float4 color:COLOR0, out float4 depthB : COLOR1)
{
#endif
	float2 rand = iterhash22(input.ScreenPos.xy + ScreenOffset); //nearest neighbour effect
	if (rand.y > GrassProb*((2.0 - input.GrassInfo.x) / 2)) discard;
	//grass blade here

	float d = input.GrassInfo.w;
	depthB = packDepth(d);
	if (depthOutMode == true) {
		color = depthB;
	}
	else {
#if SIMPLE
		//software depth
		if (unpackDepth(tex2D(depthMapSampler, input.ScreenPos.xy / ScreenSize)) < d) discard;
#endif
		float bladeCol = rand.x*0.6;
		float4 green = lerp(LightGreen, DarkGreen, bladeCol);
		float4 brown = lerp(LightBrown, DarkBrown, bladeCol);
		color = lerp(green, brown, input.GrassInfo.x) * SimpleLight(input.ModelPos.xz) * LightDot(input.Normal);
	}
}

void BladesPS3D(GrassPSVTX input, out float4 color:COLOR0)
{
	float a = 2 - sqrt(input.ScreenPos.z / (25 * GrassFadeMul));
	if (a <= 0) discard;
	float2 rand = iterhash22(input.GrassInfo.yz*100); //nearest neighbour effect
	if (rand.y > GrassProb*((2.0 - input.GrassInfo.x) / 2)) discard;
	//grass blade here

	float bladeCol = rand.x*0.6;
	float4 green = lerp(LightGreen, DarkGreen, bladeCol);
	float4 brown = lerp(LightBrown, DarkBrown, bladeCol);
	color = lerp(green, brown, input.GrassInfo.x) * lightProcessFloor(input.ModelPos) * LightDot(input.Normal) + LightSpecular(input.Normal, input.ModelPos);
	color.a = a;
}

void GridPS(GrassPSVTX input, out float4 color:COLOR0)
{
	if (floor(input.ScreenPos.xy + ScreenOffset).x % 2 == 0) discard;
	//skip every second horizontal pixel. TODO: Original game skips 3/4 when land is sloped.
	if (depthOutMode == true) {
		discard;
	}
	else {
#if SIMPLE
		//software depth
		float d = input.GrassInfo.w;
		if (unpackDepth(tex2D(depthMapSampler, input.ScreenPos.xy / ScreenSize)) < d) discard;
#endif
		color = DiffuseColor;
	}
}

void GridPS3D(GrassPSVTX input, out float4 color:COLOR0)
{
	if (abs(floor(input.GrassInfo.y * 24)) % 2 == 0) discard;
	//checkerboard. skip odd.
	if (depthOutMode == true) {
		discard;
	}
	else {
		color = DiffuseColor;
	}
}

bool Water;

float2 LoopUV(float2 uv) {
	if (Water == false) return uv;
	uv = frac(uv);

	float dir1 = uv.x + uv.y;
	float dir2 = uv.x + (1 - uv.y);
	if (dir1 < 0.5 || dir1 > 1.5 || dir2 < 0.5 || dir2 > 1.5) {
		uv = frac(uv + float2(0.5, 0.5));
	}
	return uv;
}

#if SIMPLE
void BasePS(GrassPSVTX input, out float4 color:COLOR0)
{
	float4 depthB;
#else
void BasePS(GrassPSVTX input, out float4 color:COLOR0, out float4 depthB : COLOR1)
{
#endif
    
    float d = input.GrassInfo.w;
    depthB = packDepth(d);
    if (depthOutMode == true) {
        color = depthB;
    }
    else {
#if SIMPLE
		//software depth
		if (unpackDepth(tex2D(depthMapSampler, input.ScreenPos.xy / ScreenSize)) < d) discard;
#endif
        color = lightProcessFloor(input.ModelPos) * LightDot(input.Normal);//*DiffuseColor;
		if (IgnoreColor == false) color *= input.Color;
		if (UseTexture == true) {
			color *= tex2D(TexSampler, LoopUV(input.GrassInfo.yz));
			if (color.a < 0.5) discard;
		}
    }
}

#if SIMPLE
void BasePSSimple(GrassPSVTX input, out float4 color:COLOR0)
{
	float4 depthB;
#else
void BasePSSimple(GrassPSVTX input, out float4 color:COLOR0, out float4 depthB : COLOR1)
{
#endif

	float d = input.GrassInfo.w;
	depthB = packDepth(d);
	if (depthOutMode == true) {
		color = depthB;
	}
	else {
#if SIMPLE
		//software depth
		if (unpackDepth(tex2D(depthMapSampler, input.ScreenPos.xy / ScreenSize)) < d) discard;
#endif
		color = SimpleLight(input.ModelPos.xz) * LightDot(input.Normal);
		if (IgnoreColor == false) color *= input.Color;
		if (UseTexture == true) {
			color *= tex2D(TexSampler, LoopUV(input.GrassInfo.yz));
			if (color.a < 0.5) discard;
		}
	}
}

void BasePS3D(GrassPSVTX input, out float4 color:COLOR0)
{
	float d = input.GrassInfo.w;
	color = lightProcessFloor(input.ModelPos) * LightDot(input.Normal) + LightSpecular(input.Normal, input.ModelPos);
	if (IgnoreColor == false) color *= input.Color;
	if (UseTexture == true) {
#if SIMPLE
		color *= tex2D(TexSampler, LoopUV(input.GrassInfo.yz));
#else
		color *= tex2Dgrad(AnisoTexSampler, LoopUV(input.GrassInfo.yz), ddx(input.GrassInfo.yz), ddy(input.GrassInfo.yz));
#endif
		if (color.a < 0.5) discard;
	}
	else {
		float a = 1 - (2 - sqrt(input.ScreenPos.z / (25 * GrassFadeMul)));
		if (a > 0) {
			a = min(1, a);
			//blade mipmaps
			float2 rand = tex2D(TerrainNoiseMipSampler, input.GrassInfo.yz * 100 / 1024.0).xy;
			float multex = rand.x;
			multex *= ((2.0 - input.GrassInfo.x) / 2);
			multex = (multex - 0.5) * 2.5 + 0.5;
			multex *= a;

			float bladeCol = rand.y*0.6;
			float4 green = lerp(LightGreen, DarkGreen, bladeCol);
			float4 brown = lerp(LightBrown, DarkBrown, bladeCol);
			float4 bladecolor = lerp(green, brown, input.GrassInfo.x) * lightProcessFloor(input.ModelPos) * LightDot(input.Normal);
			color = lerp(color, bladecolor, multex);
		}
	}
}

void BasePSLMap(GrassPSVTX input, out float4 color:COLOR0)
{
	color = DiffuseColor;
}

technique DrawBase
{
    pass MainPassSimple
    {

#if SM4
        VertexShader = compile vs_4_0_level_9_1 GrassVS();
        PixelShader = compile ps_4_0_level_9_3 BasePSSimple();
#else
        VertexShader = compile vs_3_0 GrassVS();
        PixelShader = compile ps_3_0 BasePSSimple();
#endif;

    }

	pass MainPass
	{

#if SM4
		VertexShader = compile vs_4_0_level_9_1 GrassVS();
		PixelShader = compile ps_4_0_level_9_3 BasePS();
#else
		VertexShader = compile vs_3_0 GrassVS();
		PixelShader = compile ps_3_0 BasePS();
#endif;

	}

	pass MainPass3D
	{

#if SM4
		VertexShader = compile vs_4_0_level_9_1 GrassVS();
		PixelShader = compile ps_4_0_level_9_3 BasePS3D();
#else
		VertexShader = compile vs_3_0 GrassVS();
		PixelShader = compile ps_3_0 BasePS3D();
#endif;

	}
}

technique DrawGrid
{
	pass MainPass
	{

#if SM4
		VertexShader = compile vs_4_0_level_9_1 GrassVS();
		PixelShader = compile ps_4_0_level_9_1 GridPS();
#else
		VertexShader = compile vs_3_0 GrassVS();
		PixelShader = compile ps_3_0 GridPS();
#endif;

	}

	pass MainPass3D
	{

#if SM4
		VertexShader = compile vs_4_0_level_9_1 GrassVS();
		PixelShader = compile ps_4_0_level_9_1 GridPS3D();
#else
		VertexShader = compile vs_3_0 GrassVS();
		PixelShader = compile ps_3_0 GridPS3D();
#endif;

	}
}

technique DrawBlades
{
	pass MainBladesSimple
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_1 GrassVS();
		PixelShader = compile ps_4_0_level_9_3 BladesPSSimple();
#else
		VertexShader = compile vs_3_0 GrassVS();
		PixelShader = compile ps_3_0 BladesPSSimple();
#endif;
	}

	pass MainBlades
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_3 GrassVS();
		PixelShader = compile ps_4_0_level_9_3 BladesPS();
#else
		VertexShader = compile vs_3_0 GrassVS();
		PixelShader = compile ps_3_0 BladesPS();
#endif;
	}

	pass MainBlades3D
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_3 GrassVS();
		PixelShader = compile ps_4_0_level_9_3 BladesPS3D();
#else
		VertexShader = compile vs_3_0 GrassVS();
		PixelShader = compile ps_3_0 BladesPS3D();
#endif;
	}
}

technique DrawLMap
{
	pass MainPass
	{

#if SM4
		VertexShader = compile vs_4_0_level_9_1 GrassVS();
		PixelShader = compile ps_4_0_level_9_1 BasePSLMap();
#else
		VertexShader = compile vs_3_0 GrassVS();
		PixelShader = compile ps_3_0 BasePSLMap();
#endif;

	}
}
