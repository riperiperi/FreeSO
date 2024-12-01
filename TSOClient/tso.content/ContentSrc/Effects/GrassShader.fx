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

float2 ScreenRotCenter;
float4 ScreenMatrix;

bool ScreenAlignUV;
float2 TexSize;

float2 TileSize;

bool depthOutMode;
bool Water;
float3 CamPos;
float3 LightVec;
float Alpha;
float GrassShininess;
bool UseTexture;
bool IgnoreColor;
bool Ceiling;
float Bias = -999;

float ParallaxHeight = 1.0;
float4 ParallaxUVTexMat;

texture BaseTex;
texture ParallaxTex;
texture NormalMapTex;
texture RoomMap : Diffuse;
texture RoomLight : Diffuse;
texture TerrainNoise : Diffuse;
texture TerrainNoiseMip : Diffuse;

sampler TexSampler = sampler_state {
	texture = <BaseTex>;
	AddressU = Wrap;
	AddressV = Wrap;
	MIPFILTER = LINEAR; MINFILTER = LINEAR; MAGFILTER = LINEAR;
};

sampler ParallaxTexSampler = sampler_state {
	texture = <ParallaxTex>;
	AddressU = Wrap;
	AddressV = Wrap;
	MIPFILTER = LINEAR; MINFILTER = LINEAR; MAGFILTER = LINEAR;
};

sampler NormalMapSampler = sampler_state {
	texture = <NormalMapTex>;
	AddressU = Wrap;
	AddressV = Wrap;
	MIPFILTER = LINEAR; MINFILTER = LINEAR; MAGFILTER = LINEAR;
};

#if SM4
sampler AnisoTexSampler = sampler_state {
	texture = <BaseTex>;
	AddressU = Wrap;
	AddressV = Wrap;

	MipFilter = Anisotropic;
	MagFilter = Anisotropic;
	MinFilter = Anisotropic;
	MaxAnisotropy = 16;
};
#endif

sampler RoomMapSampler = sampler_state {
	texture = <RoomMap>;
	AddressU = WRAP; AddressV = WRAP; AddressW = WRAP;
	MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};


sampler RoomLightSampler = sampler_state {
	texture = <RoomLight>;
	AddressU = WRAP; AddressV = WRAP; AddressW = WRAP;
	MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};

sampler TerrainNoiseSampler = sampler_state {
	texture = <TerrainNoise>;
	AddressU = WRAP; AddressV = WRAP; AddressW = WRAP;
	MIPFILTER = NONE; MINFILTER = POINT; MAGFILTER = POINT;
	MaxLOD = 0;
};

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

	if (ScreenAlignUV == true) {
		// results in sharper textures for flat surfaces in 2d mode. may slightly distort sloped surfaces.
		// otherwise, even flat surfaces would not identically match their flat sprites as there would be a subpixel offset 
		// (and we use linear blend, so it mushes together).
		output.GrassInfo.yz = round((output.GrassInfo.yz * TexSize) + output.ScreenPos.xy);
		output.GrassInfo.yz = (output.GrassInfo.yz - output.ScreenPos.xy) / TexSize; //reverse operation
	}

	output.ScreenPos.xy -= ScreenRotCenter;
	output.ScreenPos.xy = output.ScreenPos.xy*ScreenMatrix.xw + output.ScreenPos.yx*ScreenMatrix.zy;
	output.ScreenPos.xy += ScreenRotCenter;

    //if (output.GrassInfo.x == -1.2 && output.GrassInfo.y == -1.2 && output.GrassInfo.z == -1.2 && output.GrassInfo.w < -1.0 && output.ScreenPos.x < -200 && output.ScreenPos.y < -300) output.Color *= 0.5; 

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
		color = gammaMul(lerp(green, brown, input.GrassInfo.x), lightProcessFloor(input.ModelPos) * LightDot(input.Normal));//DiffuseColor;
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
		color = gammaMul(lerp(green, brown, input.GrassInfo.x), SimpleLight(input.ModelPos.xz) * LightDot(input.Normal));
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
	color = gammaMad(lerp(green, brown, input.GrassInfo.x), lightProcessFloor(input.ModelPos) * LightDot(input.Normal), LightSpecular(input.Normal, input.ModelPos));
	color.a = a;
	color.a *= Alpha;
}

#if !SIMPLE
struct GrassParallaxVTX
{
	float4 Position : SV_Position0;
	float4 Color : COLOR0;
	float4 GrassInfo : TEXCOORD0; //x is liveness, yz is position
	float3 Normal : TEXCOORD1;
	float3 Tangent : TEXCOORD2;
	float3 Bitangent : TEXCOORD3;
};

struct GrassParallaxPSVTX {
	float4 Position : SV_Position0;
	float4 Color : COLOR0;
	float4 GrassInfo : TEXCOORD0; //x is liveness, yz is position
	float4 ModelPos : TEXCOORD1;
	float3 Normal : TEXCOORD2;

	float4 TangentViewPos : TEXCOORD3; //ScreenPos.z folded in here
	float3 TangentModelPos : TEXCOORD4;
	float3 TangentLightVec : TEXCOORD5;
};

GrassParallaxPSVTX GrassParallaxVS(GrassParallaxVTX input)
{
	GrassParallaxPSVTX output = (GrassParallaxPSVTX)0;
	float4x4 Temp4x4 = mul(World, mul(View, Projection));
	float4 position = mul(input.Position, Temp4x4);
	output.Position = position;
	output.ModelPos = mul(input.Position, World);
	output.Color = input.Color;
	output.GrassInfo = input.GrassInfo;
	output.GrassInfo.yz = output.GrassInfo.yz*TexMatrix.xw + output.GrassInfo.zy*TexMatrix.zy + TexOffset;
	output.GrassInfo.w = position.z / position.w;

	output.Normal = normalize(input.Normal);

	float3 T = normalize(input.Tangent);
	float3 B = normalize(input.Bitangent);
	float3 N = output.Normal;
	float3x3 TBN = float3x3(T, B, N);

	output.TangentViewPos.xyz = mul(TBN, CamPos);
	output.TangentModelPos = mul(TBN, output.ModelPos.xyz);
	output.TangentLightVec = mul(TBN, normalize(LightVec));

	float4 position2 = mul(float4(input.Position.x, 0, input.Position.z, input.Position.w), Temp4x4);
	//output.ScreenPos.xy = ((position2.xy*float2(0.5, -0.5)) + float2(0.5, 0.5)) * ScreenSize;
	output.TangentViewPos.w = position.z;

	if (output.GrassInfo.x == -1.2 && output.GrassInfo.y == -1.2 && output.GrassInfo.z == -1.2 && output.GrassInfo.w < -1.0) output.Color *= 0.5;

	return output;
}

float2 GrassParallaxMapping(float2 texCoords, float3 viewDir, float probability)
{
	const float minLayers = 4.0;
	const float maxLayers = 20.0;
	//add more layers the further we are from directly facing the surface
	float layers = round(lerp(maxLayers, minLayers, abs(dot(float3(0.0, 0.0, 1.0), viewDir))));
	//our depth range is 0 to 1 - change the slice we check by this much each iteration
	float layerDepth = 1.0 / layers;

	viewDir.xy /= abs(viewDir.z);
	//P is how much to shift tex coords by through the whole 0-1 depth space
	float2 P = viewDir.xy * ParallaxHeight;
	float2 deltaTexCoords = P / layers;

	//correction since grass repeats at a diagonal
	deltaTexCoords = float2(deltaTexCoords.x*0.7071 - deltaTexCoords.y*0.7071, deltaTexCoords.y*0.7071 + deltaTexCoords.x*0.7071);

	float2 currentTexCoords = texCoords;

	//grass is based on probability, rather than using a texture verbatim.
	float probDelta = probability * 0.5/layers;
	probability -= probDelta * layers;

	float rand = tex2D(TerrainNoiseSampler, texCoords).y;
	float currentTexDepth = step(probability, rand);
	probability += probDelta;

	[unroll(20)]
	for (float currentLayerDepth = 0; currentLayerDepth < 1; currentLayerDepth += layerDepth)
	{
		if (currentLayerDepth >= currentTexDepth) break; //are we under the surface yet?
		//shift texture coordinates for the next layer
		currentTexCoords -= deltaTexCoords;
		//depth value at new texture coordinates
		currentTexDepth = step(probability, tex2D(TerrainNoiseSampler, currentTexCoords).y);

		probability += probDelta; //probability increases the closer to the ground we are (varying grass height)
	}

	if (currentTexDepth == 1) discard;
	return currentTexCoords;
}

void BladesParallaxPS3D(GrassParallaxPSVTX input, out float4 color:COLOR0)
{
	float a = 2 - sqrt(input.TangentViewPos.w / (25 * GrassFadeMul));
	if (a <= 0) discard;

	float3 viewDir = normalize(input.TangentViewPos.xyz - input.TangentModelPos);
	float2 texCoords = GrassParallaxMapping(input.GrassInfo.yz * 100 / 512, viewDir, GrassProb * ((2.0 - input.GrassInfo.x) / 2));
	
	float2 rand = iterhash22(texCoords * 512); //nearest neighbour effect
	//grass blade here

	float bladeCol = rand.x*0.6;
	float4 green = lerp(LightGreen, DarkGreen, bladeCol);
	float4 brown = lerp(LightBrown, DarkBrown, bladeCol);
	color = gammaMad(lerp(green, brown, input.GrassInfo.x), lightProcessFloor(input.ModelPos) * LightDot(input.Normal), LightSpecular(input.Normal, input.ModelPos));
	color.a = a;
	color.a *= Alpha;
}

//roof parallax

float2 ParallaxMapping(float2 texCoords, float3 viewDir)
{
	const float minLayers = 4.0; 
	const float maxLayers = 16.0;
	//add more layers the further we are from directly facing the surface
	float layers = round(lerp(maxLayers, minLayers, abs(dot(float3(0.0, 0.0, 1.0), viewDir))));
	//our depth range is 0 to 1 - change the slice we check by this much each iteration
	float layerDepth = 1.0 / layers;
	viewDir.xy /= abs(viewDir.z);
	//P is how much to shift tex coords by through the whole 0-1 depth space
	float2 P = viewDir.xy * ParallaxHeight;
	float2 deltaTexCoords = P / layers; //how much to shift per layer

	deltaTexCoords = float2(deltaTexCoords.x*ParallaxUVTexMat.x + deltaTexCoords.y*ParallaxUVTexMat.y, deltaTexCoords.y*ParallaxUVTexMat.z + deltaTexCoords.x*ParallaxUVTexMat.w);

	float2 currentTexCoords = texCoords;
	float currentTexDepth = tex2D(ParallaxTexSampler, texCoords).x; //break if our current layer is beneath this

	[unroll(16)]
	for (float currentLayerDepth = 0; currentLayerDepth < 1; currentLayerDepth += layerDepth)
	{
		if (currentLayerDepth >= currentTexDepth) break; //are we under the surface yet?
		//shift texture coordinates for the next layer
		currentTexCoords -= deltaTexCoords;
		//depth value at new texture coordinates
		currentTexDepth = tex2D(ParallaxTexSampler, currentTexCoords).x;
	}

	//parallax occlusion mapping
	//we want a better estimate of where we hit the surface to avoid a layered appearance (steep mapping)
	//find the points before and after the ray collision and interpolate between them.
	float2 lastTC = currentTexCoords + deltaTexCoords;

	float postDepth = currentTexDepth - currentLayerDepth; //should be negative - how much past current layer depth we travel
	float preDepth = tex2D(ParallaxTexSampler, lastTC).x - (currentLayerDepth - layerDepth); //should be positive - how much after last layer travel we'd need to go to intersect

	//interpolate between the depth after and before collision to find a midpoint tex-coord.
	//this removes some of the "layered" effect from most front facing angles... at least the ones where we do get a before and after intersection.
	float i = postDepth / (postDepth - preDepth);
	currentTexCoords = lerp(currentTexCoords, lastTC, i);

	return currentTexCoords;
}

float4 LightDotVec(float3 normal, float3 vec) {
	return CM(dot(vec, normalize(normal)) * 0.6f + 0.4f);
}

float4 LightSpecularVec(float3 normal, float3 pos) {
	float cosan = abs(dot(pos, normal));
	return DiffuseColor * (1 - pow(cosan, GrassShininess));
}

void RoofParallaxPS3D(GrassParallaxPSVTX input, out float4 color:COLOR0)
{
	float3 viewDir = normalize(input.TangentViewPos.xyz - input.TangentModelPos);
	float d = input.GrassInfo.w;
	if (IgnoreColor == false) color = input.Color;
	else color = float4(1, 1, 1, 1);//*DiffuseColor;
	float2 texCoords = ParallaxMapping(input.GrassInfo.yz, viewDir);
	color *= tex2Dgrad(TexSampler, texCoords, ddx(input.GrassInfo.yz), ddy(input.GrassInfo.yz));
	if (color.a == 0) discard;

	float3 normal = tex2D(NormalMapSampler, texCoords).xyz;
	normal = normalize(normal * 2.0 - 1.0);
	normal.xy *= -1;
	float3 lightDir = normalize(input.TangentLightVec);

	color = gammaMad(color, lightProcessRoof(input.ModelPos) * LightDotVec(normal, lightDir), LightSpecularVec(normal, viewDir));
}

void FloorParallaxPS3D(GrassParallaxPSVTX input, out float4 color:COLOR0, out float4 depthB : COLOR1)
{
	float3 viewDir = normalize(input.TangentViewPos.xyz - input.TangentModelPos);
	float d = input.GrassInfo.w;
	if (IgnoreColor == false) color = input.Color;
	else color = float4(1, 1, 1, 1);//*DiffuseColor;

	depthB = packDepth(d);
	if (depthOutMode == true) {
		color = depthB;
	}
	else {
		float2 texCoords = ParallaxMapping(LoopUV(input.GrassInfo.yz), viewDir);
		color *= tex2Dgrad(TexSampler, texCoords, ddx(input.GrassInfo.yz), ddy(input.GrassInfo.yz));
		if (color.a == 0) discard;

		float3 normal = float3(0, 0, 1);
		//tex2D(NormalMapSampler, texCoords).xyz;
	//normal = normalize(normal * 2.0 - 1.0);
	//normal.xy *= -1;
		float3 lightDir = normalize(input.TangentLightVec);

		color = gammaMad(color, lightProcessRoof(input.ModelPos) * LightDotVec(normal, lightDir), LightSpecularVec(normal, viewDir));
	}
}
#endif

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
		color = DiffuseColor * Alpha;
	}
}

void GridPSTex3D(GrassPSVTX input, out float4 color:COLOR0)
{
	if (depthOutMode == true) {
		discard;
	}
	else {
		color = tex2Dbias(TexSampler, float4(input.GrassInfo.yz, 0, -0.5)) * DiffuseColor * Alpha;
	}
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
        color = float4(1,1,1,1);//*DiffuseColor;
		if (IgnoreColor == false) color *= input.Color;
		if (UseTexture == true) {
			color *= tex2Dbias(TexSampler, float4(LoopUV(input.GrassInfo.yz), 0, Bias));
			if (color.a < 0.5) discard;
		}
		color = gammaMul(color, lightProcessRoof(input.ModelPos) * LightDot(input.Normal));
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
		color = float4(1, 1, 1, 1);
		if (IgnoreColor == false) color *= input.Color;
		if (UseTexture == true) {
			color *= tex2D(TexSampler, LoopUV(input.GrassInfo.yz));
			if (color.a < 0.5) discard;
		}
		color = gammaMul(color, SimpleLight(input.ModelPos.xz) * LightDot(input.Normal));
	}
}

float MulBase = 0.2;
float MulRange = 7;
float4 BlurBounds = float4(6, 6, 68, 68);

float AvgVector(float3 vec) {
	return (vec.r + vec.b + vec.g) / 3;
}

void BasePSMul(GrassPSVTX input, out float4 color:COLOR0)
{
	float4 c = LightDot(input.Normal);
	float4 light = lightProcessFloor(input.ModelPos);
	//float diff = AvgVector(light) / AvgVector(DiffuseColor);

	float2 edgeDistXY = min(input.ModelPos.xz-BlurBounds.xy*3, BlurBounds.zw*3 - input.ModelPos.xz);
	float edgeDist = min(edgeDistXY.x, edgeDistXY.y);

	edgeDist = min(1, edgeDist / 24.0);

	//we want to mask out the terrain using its difference from the expected ground colour. 
	//close to 0 should be zero, but past 30% should be fully apparent.
	float3 expected = lerp(LightGreen.rgb, DarkGreen.rgb, 0.1);
	//c *= input.Color;
	c = gammaMul(input.Color, c * (light / DiffuseColor));
	float diff = length(expected - c.rgb);
	color = float4(1, 1, 1, 1)*max(0, min(1, (diff - MulBase) * MulRange)) * edgeDist;
}

float4 FadeRectangle;
float FadeWidth;
float RectangleFade(float2 xz, float extend) {
	float dx = max(abs(xz.x - FadeRectangle.x) - (FadeRectangle.z + extend), 0.0);
	float dy = max(abs(xz.y - FadeRectangle.y) - (FadeRectangle.w + extend), 0.0);
	return min(sqrt(dx * dx + dy * dy) / (FadeWidth-extend), 1.0);
}

void BasePS3D(GrassPSVTX input, out float4 color:COLOR0)
{
	float d = input.GrassInfo.w;
	color = float4(1,1,1,1);
	if (IgnoreColor == false) color *= input.Color;
	if (UseTexture == true) {
		// I cannot for the life of me find out why VFACE doesn't exist on ps4.0.
		if (Ceiling == false) {
#if SIMPLE
			color *= tex2D(TexSampler, LoopUV(input.GrassInfo.yz));
#else
#if SM4
			color *= tex2Dgrad(AnisoTexSampler, LoopUV(input.GrassInfo.yz), ddx(input.GrassInfo.yz), ddy(input.GrassInfo.yz));
#else
			color *= tex2Dgrad(TexSampler, LoopUV(input.GrassInfo.yz), ddx(input.GrassInfo.yz), ddy(input.GrassInfo.yz));
#endif
#endif

			if (color.a == 0) discard;
			color = gammaMad(color, lightProcessRoof(input.ModelPos) * LightDot(input.Normal), LightSpecular(input.Normal, input.ModelPos));
			color.a *= (1 - RectangleFade(input.ModelPos.xz, FadeWidth / 2));
		} else {
			// Ceiling colour.
			color = float4(0.76, 0.78, 0.80, 1.00);

			color = gammaMad(color, lightProcessRoofCeiling(input.ModelPos) * LightDot(input.Normal), LightSpecular(input.Normal, input.ModelPos));
			color.a *= (1 - RectangleFade(input.ModelPos.xz, FadeWidth / 2));
		}
	}
	else {
		color = gammaMad(color, lightProcessRoof(input.ModelPos) * LightDot(input.Normal), LightSpecular(input.Normal, input.ModelPos));
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
			float4 bladecolor = gammaMul(lerp(green, brown, input.GrassInfo.x), lightProcessFloor(input.ModelPos) * LightDot(input.Normal));
			color = lerp(color, bladecolor, multex);
		}
		color.a *= (1 - RectangleFade(input.ModelPos.xz, 0.0));
		color.a *= Alpha;
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

#if !SIMPLE
	pass RoofParallax3D
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_3 GrassParallaxVS();
		PixelShader = compile ps_4_0_level_9_3 RoofParallaxPS3D();
#else
		VertexShader = compile vs_3_0 GrassParallaxVS();
		PixelShader = compile ps_3_0 RoofParallaxPS3D();
#endif;
	}
#endif

#if !SIMPLE
	pass FloorParallax3D
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_3 GrassParallaxVS();
		PixelShader = compile ps_4_0_level_9_3 FloorParallaxPS3D();
#else
		VertexShader = compile vs_3_0 GrassParallaxVS();
		PixelShader = compile ps_3_0 FloorParallaxPS3D();
#endif;
	}
#endif
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

	pass MainPass3DTex
	{

#if SM4
		VertexShader = compile vs_4_0_level_9_1 GrassVS();
		PixelShader = compile ps_4_0_level_9_1 GridPSTex3D();
#else
		VertexShader = compile vs_3_0 GrassVS();
		PixelShader = compile ps_3_0 GridPSTex3D();
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

#if !SIMPLE
	pass MainBladesParallax3D
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_3 GrassParallaxVS();
		PixelShader = compile ps_4_0_level_9_3 BladesParallaxPS3D();
#else
		VertexShader = compile vs_3_0 GrassParallaxVS();
		PixelShader = compile ps_3_0 BladesParallaxPS3D();
#endif;
	}
#endif

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

technique DrawMask
{
	pass MainPass
	{

#if SM4
		VertexShader = compile vs_4_0_level_9_1 GrassVS();
		PixelShader = compile ps_4_0_level_9_3 BasePSMul();
#else
		VertexShader = compile vs_3_0 GrassVS();
		PixelShader = compile ps_3_0 BasePSMul();
#endif;

	}
}