#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_3
	#define PS_SHADERMODEL ps_4_0_level_9_3
#endif

matrix WorldViewProjection;
float2 TextureSize;
float2 ScreenRes;
float4 Color;
float PxRange;

texture GlyphTexture;
sampler glyphSampler = sampler_state
{
	Texture = (GlyphTexture);
	AddressU = CLAMP;
	AddressV = CLAMP;
	MagFilter = LINEAR;
	MinFilter = LINEAR;
	Mipfilter = LINEAR;
};

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float2 TexCoord : TEXCOORD0;
	float2 Derivative: TEXCOORD1;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float2 TexCoord : TEXCOORD0;
	float2 Derivative: TEXCOORD1;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;
	output.Position = mul(input.Position, WorldViewProjection);
	output.TexCoord = input.TexCoord;

	output.Derivative = input.Derivative;

	return output;
}

float Median(float a, float b, float c)
{
	return max(min(a, b), min(max(a, b), c));
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float2 msdfUnit = PxRange / TextureSize;
	float3 samp = tex2D(glyphSampler, input.TexCoord).rgb;

	float sigDist = Median(samp.r, samp.g, samp.b) - 0.5f;
	sigDist = sigDist * max(1.0, dot(msdfUnit, 0.5f / (input.Derivative.x + input.Derivative.y))); //fwidth(input.TexCoord)));

	float opacity = clamp(sigDist + 0.5f, 0.0f, 1.0f);
	return Color * opacity;
}

technique Text
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};