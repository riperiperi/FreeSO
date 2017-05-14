float4x4 Projection;
float4x4 View;

struct VertexShaderInput
{
    float4 Position : SV_Position0;
    float4 Color : COLOR0;
};

struct VertexShaderOutput
{
    float4 Position : SV_Position0;
    float4 Color : COLOR0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;
    float4x4 Temp4x4 = mul(View, Projection);
    output.Position = mul(input.Position, Temp4x4);
    output.Color = input.Color;

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    return input.Color;
}

technique Draw2D
{
    pass MainPass
    {
		AlphaBlendEnable = TRUE; DestBlend = INVSRCALPHA; SrcBlend = SRCALPHA; 

#if SM4
        VertexShader = compile vs_4_0_level_9_1 VertexShaderFunction();
        PixelShader = compile ps_4_0_level_9_1 PixelShaderFunction();
#else
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
#endif;

    }
}
