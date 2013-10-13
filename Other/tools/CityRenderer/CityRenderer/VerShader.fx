struct VertexToPixel
{
	float3 VertexPosition : POSITION;
	float2 ATextureCoord : POSITION;
	float2 BTextureCoord : POSITION;
	float2 CTextureCoord : POSITION;
	float2 BlendTextureCoord : POSITION;
};

float4x4 ModelViewMatrix;
float4x4 ProjectionMatrix;

VertexToPixel VertexShader(VertexToPixel Input)
{
	VertexToPixel Output = new VertexToPixel();
	Output.VertexPosition = ProjectionViewMatrix * ModelViewMatrix;
	Output.ATextureCoord = Input.ATextureCoord;
	Output.BTextureCoord = Input.BTextureCoord;
	Output.CTextureCoord = Input.CTextureCoord;
	Output.BlendTextureCoord = Input.CTextureCoord;
}
