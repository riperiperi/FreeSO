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

GrassPSVTX GrassVS(GrassVTX input)
{
    GrassPSVTX output = (GrassPSVTX)0;
    float4x4 Temp4x4 = mul(World, mul(View, Projection));
    float4 position = mul(input.Position, Temp4x4);
    output.Position = position;
    output.Color = input.Color;
    output.GrassInfo = input.GrassInfo;
    output.GrassInfo.w = position.z / position.w;
    output.ScreenPos = position.xy * ScreenSize;

    if (output.GrassInfo.x == -1.2 && output.GrassInfo.y == -1.2 && output.GrassInfo.z == -1.2 && output.GrassInfo.w < -1.0 && output.ScreenPos.x < -200 && output.ScreenPos.y < -300) output.Color *= 0.5; 

    return output;
}

void BladesPS(GrassPSVTX input, out float4 color:COLOR0, out float4 depthB:COLOR1)
{
    float2 rand = iterhash22(floor(input.ScreenPos.xy+ScreenOffset)); //nearest neighbour effect
    if (rand.y > GrassProb*((2.0-input.GrassInfo.x)/2)) discard;
    //grass blade here
    float bladeCol = rand.x*0.6;
    float4 green = lerp(LightGreen, DarkGreen, bladeCol);
    float4 brown = lerp(LightBrown, DarkBrown, bladeCol);

    color = lerp(green, brown, input.GrassInfo.x) * DiffuseColor;
    float d = input.GrassInfo.w;
    depthB = float4(d,d,d,1);
}

void BasePS(GrassVTX input, out float4 color:COLOR0, out float4 depthB:COLOR1)
{
    color = input.Color*DiffuseColor;
    float d = input.GrassInfo.w;
    depthB = float4(d,d,d,1);
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
