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

float2 TexOffset;
float4 TexMatrix;

//LIGHTING
float4 OutsideLight;
float4 OutsideDark;
float4 MaxLight;
float3 WorldToLightFactor;
float2 LightOffset;
float2 MapLayout;
//END LIGHTING

float2 TileSize;

float Level;

bool depthOutMode;
float3 LightVec;
bool UseTexture;
bool IgnoreColor;
texture BaseTex;
sampler TexSampler = sampler_state {
	texture = <BaseTex>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
};

texture advancedLight : Diffuse;
sampler advLightSampler = sampler_state {
	texture = <advancedLight>;
	AddressU = CLAMP; AddressV = CLAMP; AddressW = CLAMP;
	MIPFILTER = LINEAR; MINFILTER = LINEAR; MAGFILTER = LINEAR;
};

texture RoomMap : Diffuse;
sampler RoomMapSampler = sampler_state {
	texture = <RoomMap>;
	AddressU = CLAMP; AddressV = CLAMP; AddressW = CLAMP;
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
	MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};

texture depthMap : Diffuse;
sampler depthMapSampler = sampler_state {
	texture = <depthMap>;
	AddressU = CLAMP; AddressV = CLAMP; AddressW = CLAMP;
	MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};

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
    float2 ScreenPos : TEXCOORD1;
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

	inPosition.xz += 1 / MapLayout * float2(Level % MapLayout.x, floor(Level / MapLayout.x));

	float4 lTex = tex2D(advLightSampler, inPosition.xz);
	return lightColor(lTex);
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
	output.ScreenPos = ((position2.xy*float2(0.5, -0.5)) + float2(0.5, 0.5)) * ScreenSize;

    if (output.GrassInfo.x == -1.2 && output.GrassInfo.y == -1.2 && output.GrassInfo.z == -1.2 && output.GrassInfo.w < -1.0 && output.ScreenPos.x < -200 && output.ScreenPos.y < -300) output.Color *= 0.5; 

    return output;
}
	
float4 CM(float mult) {
	return float4(mult, mult, mult, 1);
}

float4 LightDot(float3 normal) {
	return CM(dot(LightVec, normalize(normal)) * 0.5f + 0.5f);
}

void BladesPS(GrassPSVTX input, out float4 color:COLOR0)
{
    float2 rand = iterhash22(input.ScreenPos.xy+ScreenOffset); //nearest neighbour effect
    if (rand.y > GrassProb*((2.0-input.GrassInfo.x)/2)) discard;
    //grass blade here

    float d = input.GrassInfo.w;
    float4 depthB = packDepth(d);
    if (depthOutMode == true) {
        color = depthB;
    }
    else {
		//software depth
		if (unpackDepth(tex2D(depthMapSampler, input.ScreenPos.xy / ScreenSize)) < d) discard;
        float bladeCol = rand.x*0.6;
        float4 green = lerp(LightGreen, DarkGreen, bladeCol);
        float4 brown = lerp(LightBrown, DarkBrown, bladeCol);
		color = lerp(green, brown, input.GrassInfo.x) * lightProcess(input.ModelPos) * LightDot(input.Normal);//DiffuseColor;
    }
}

void BladesPSSimple(GrassPSVTX input, out float4 color:COLOR0)
{
	float2 rand = iterhash22(input.ScreenPos.xy + ScreenOffset); //nearest neighbour effect
	if (rand.y > GrassProb*((2.0 - input.GrassInfo.x) / 2)) discard;
	//grass blade here

	float d = input.GrassInfo.w;
	float4 depthB = packDepth(d);
	if (depthOutMode == true) {
		color = depthB;
	}
	else {
		//software depth
		if (unpackDepth(tex2D(depthMapSampler, input.ScreenPos.xy / ScreenSize)) < d) discard;
		float bladeCol = rand.x*0.6;
		float4 green = lerp(LightGreen, DarkGreen, bladeCol);
		float4 brown = lerp(LightBrown, DarkBrown, bladeCol);
		color = lerp(green, brown, input.GrassInfo.x) * SimpleLight(input.ModelPos.xz) * LightDot(input.Normal);
	}
}

void GridPS(GrassPSVTX input, out float4 color:COLOR0)
{
	if (floor(input.ScreenPos.xy + ScreenOffset).x % 2 == 0) discard;
	//skip every second horizontal pixel. TODO: Original game skips 3/4 when land is sloped.
	float d = input.GrassInfo.w;
	if (depthOutMode == true) {
		discard;
	}
	else {
		//software depth
		if (unpackDepth(tex2D(depthMapSampler, input.ScreenPos.xy / ScreenSize)) < d) discard;
		color = DiffuseColor;
	}
}


void BasePS(GrassPSVTX input, out float4 color:COLOR0)
{
    
    float d = input.GrassInfo.w;
    float4 depthB = packDepth(d);
    if (depthOutMode == true) {
        color = depthB;
    }
    else {
		//software depth
		if (unpackDepth(tex2D(depthMapSampler, input.ScreenPos.xy / ScreenSize)) < d) discard;
        color = lightProcess(input.ModelPos) * LightDot(input.Normal);//*DiffuseColor;
		if (IgnoreColor == false) color *= input.Color;
		if (UseTexture == true) {
			color *= tex2D(TexSampler, input.GrassInfo.yz);
			if (color.a < 0.5) discard;
		}
    }
}

void BasePSSimple(GrassPSVTX input, out float4 color:COLOR0)
{

	float d = input.GrassInfo.w;
	float4 depthB = packDepth(d);
	if (depthOutMode == true) {
		color = depthB;
	}
	else {
		//software depth
		if (unpackDepth(tex2D(depthMapSampler, input.ScreenPos.xy / ScreenSize)) < d) discard;
		color = SimpleLight(input.ModelPos.xz) * LightDot(input.Normal);
		if (IgnoreColor == false) color *= input.Color;
		if (UseTexture == true) {
			color *= tex2D(TexSampler, input.GrassInfo.yz);
			if (color.a < 0.5) discard;
		}
	}
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
		PixelShader = compile ps_4_0_level_9_1 BasePS();
#else
		VertexShader = compile vs_3_0 GrassVS();
		PixelShader = compile ps_3_0 BasePS();
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
}
