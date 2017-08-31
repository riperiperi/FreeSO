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

float4 SSAASample4(float2 uv) {
	float4 result = float4(0, 0, 0, 0);
	uv += SSAASize / 2;
	result += tex2D(texSampler, uv);
	result += tex2D(texSampler, uv + float2(SSAASize.x, 0));
	result += tex2D(texSampler, uv + float2(0, SSAASize.y));
	result += tex2D(texSampler, uv + float2(SSAASize.x, SSAASize.y));
	return result / 4;
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
