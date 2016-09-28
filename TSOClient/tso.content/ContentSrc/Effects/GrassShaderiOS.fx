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

bool depthOutMode;

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
};

struct GrassPSVTX {
    float4 Position : SV_Position0;
    float4 Color : COLOR0;
    float4 GrassInfo : TEXCOORD0; //x is liveness, yz is position
    float2 ScreenPos : TEXCOORD1;
};

// from shadertoy.
// https://www.shadertoy.com/view/4djSRW

float2 hash22(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx+19.19);
    return frac(float2((p3.x + p3.y)*p3.z, (p3.x+p3.z)*p3.y));
}

float2 iterhash22(in float2 uv) {
    float2 a = float2(0,0);
    for (int t = 0; t < 2; t++)
    {
        float v = float(t+1)*.132;
        a += hash22(uv*v);
    }
    return a / 2.0;
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

GrassPSVTX GrassVS(GrassVTX input)
{
    GrassPSVTX output = (GrassPSVTX)0;
    float4x4 Temp4x4 = mul(World, mul(View, Projection));
    float4 position = mul(input.Position, Temp4x4);
    output.Position = position;
    output.Color = input.Color;
    output.GrassInfo = input.GrassInfo;
    output.GrassInfo.w = position.z / position.w;
    output.ScreenPos = ((position.xy*float2(0.5, -0.5)) + float2(0.5, 0.5)) * ScreenSize;

    if (output.GrassInfo.x == -1.2 && output.GrassInfo.y == -1.2 && output.GrassInfo.z == -1.2 && output.GrassInfo.w < -1.0 && output.ScreenPos.x < -200 && output.ScreenPos.y < -300) output.Color *= 0.5; 

    return output;
}

void BladesPS(GrassPSVTX input, out float4 color:COLOR0)
{
    float2 rand = iterhash22(floor(input.ScreenPos.xy+ScreenOffset)); //nearest neighbour effect
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
        color = lerp(green, brown, input.GrassInfo.x) * DiffuseColor;
    }

}

void GridPS(GrassPSVTX input, out float4 color:COLOR0)
{
	if (floor(input.ScreenPos.xy + ScreenOffset).x % 2 == 0) discard;
	//skip every second horizontal pixel. TODO: Original game skips 3/4 when land is sloped.
	if (depthOutMode == true) {
		discard;
	}
	else {
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
        if (depthOutMode == false && unpackDepth(tex2D(depthMapSampler, input.ScreenPos.xy / ScreenSize)) < d) discard;
        color = input.Color*DiffuseColor;
    }
}

technique DrawBase
{
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
    pass MainBlades
    {
#if SM4
        VertexShader = compile vs_4_0_level_9_1 GrassVS();
        PixelShader = compile ps_4_0_level_9_1 BladesPS();
#else
        VertexShader = compile vs_3_0 GrassVS();
        PixelShader = compile ps_3_0 BladesPS();
#endif;
    }
}
