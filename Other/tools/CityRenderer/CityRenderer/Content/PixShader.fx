//Vertex shader output structure
struct VertexToPixel
{
	float3 VertexPosition : POSITION0;
	float2 ATextureCoord : TEXCOORD0;
	float2 BTextureCoord : TEXCOORD1;
	float2 CTextureCoord : TEXCOORD2;
	float2 BlendTextureCoord : TEXCOORD3;
};

sampler2D USampler;
sampler2D USamplerTex;
sampler2D USamplerBlend;
float4 LightCol;

float4 PixelShaderFunction(VertexToPixel Input) : COLOR0
{
	float4 BlendA = tex2D(USamplerBlend, Input.BlendTextureCoord);
	float4 Blend = tex2D(USamplerTex, Input.CTextureCoord);
	float4 Base = tex2D(USamplerTex, Input.BTextureCoord);
	float InvA = 1.0 - BlendA.x;
	
	Base.x = Base.x*InvA + Blend.x * BlendA.x;
	Base.y = Base.y*InvA + Blend.y * BlendA.x;
	Base.z = Base.z*InvA + Blend.z * BlendA.x;
	
	return Base * tex2D(USampler, Input.ATextureCoord) * LightCol;
}

technique Technique1
{
	pass Pass1
	{
		PixelShader = compile ps_2_0 PixelShaderFunction();
	}
}
