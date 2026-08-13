//A shader for SSAA style downscaling. Currently uses box filer.

float2 SSAASize;

texture tex : Diffuse;
sampler texSampler = sampler_state {
	texture = <tex>;
	AddressU = CLAMP; AddressV = CLAMP; AddressW = CLAMP;
	MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};

struct VertexShaderInput
{
    float4 Position : SV_Position0;
    float2 Coord : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_Position0;
    float2 Coord : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;
    output.Position = input.Position;
    output.Coord = input.Coord;
	output.Coord.y = 1 - output.Coord.y;

    return output;
}

//coeffs from http://chilliant.blogspot.com/2012/08/srgb-approximations-for-hlsl.html.
float4 LinearToSRGB(float4 col) {
	float3 s1 = sqrt(col.rgb);
	float3 s2 = sqrt(s1);
	float3 s3 = sqrt(s2);
	col.rgb = 0.662002687 * s1 + 0.684122060 * s2 - 0.323583601 * s3 - 0.0225411470 * col.rgb;
	return col;
}

float4 SRGBToLinear(float4 col) {
	col.rgb = col.rgb * (col.rgb * (col.rgb * 0.305306011 + 0.682171111) + 0.012522878);
	return col;
}

float4 SSAASample4(float2 uv) {
	float4 result = float4(0, 0, 0, 0);
	uv += SSAASize / 2;
	result += SRGBToLinear(tex2D(texSampler, uv));
	result += SRGBToLinear(tex2D(texSampler, uv + float2(SSAASize.x, 0)));
	result += SRGBToLinear(tex2D(texSampler, uv + float2(0, SSAASize.y)));
	result += SRGBToLinear(tex2D(texSampler, uv + float2(SSAASize.x, SSAASize.y)));
	return LinearToSRGB(result / 4);
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    return SSAASample4(input.Coord);
}

technique DrawSSAA4
{
    pass MainPass
    {
#if SM4
        VertexShader = compile vs_4_0_level_9_1 VertexShaderFunction();
        PixelShader = compile ps_4_0_level_9_1 PixelShaderFunction();
#else
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
#endif;

    }
}
