//Vertex shader output structure
struct VertexToPixel
{
	float4 VertexPosition : POSITION;
	float2 ATextureCoord : TEXCOORD0;
	float2 BTextureCoord : TEXCOORD1;
	float2 CTextureCoord : TEXCOORD2;
	float2 BlendTextureCoord : TEXCOORD3;
};

float4x4 ModelViewMatrix;
float4x4 ProjectionViewMatrix;

VertexToPixel VertexShaderFunction(VertexToPixel Input)
{
	float4x4 Temp4x4= mul(ModelViewMatrix, ProjectionViewMatrix);

	VertexToPixel Output = (VertexToPixel)0;
	Output.VertexPosition = mul(Input.VertexPosition, Temp4x4);
	Output.ATextureCoord = Input.ATextureCoord;
	Output.BTextureCoord = Input.BTextureCoord;
	Output.CTextureCoord = Input.CTextureCoord;
	Output.BlendTextureCoord = Input.CTextureCoord;
	
	return Output;
}

technique Technique1
{
	pass Pass1
	{
		VertexShader = compile vs_2_0 VertexShaderFunction();
	}
}
