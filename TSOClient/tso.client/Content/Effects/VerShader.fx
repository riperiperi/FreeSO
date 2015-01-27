//Vertex shader output structure
struct VertexToPixel
{
	float4 VertexPosition : POSITION;
	
	float2 ATextureCoord : TEXCOORD0;
	float2 BTextureCoord : TEXCOORD1;
	float2 CTextureCoord : TEXCOORD2;
	float2 BlendTextureCoord : TEXCOORD3;
	float2 RoadTextureCoord : TEXCOORD4;
	float2 RoadCTextureCoord : TEXCOORD5;
	float2 vPos : TEXCOORD6;
	float Depth : TEXCOORD7;
};

struct VertexToShad
{
	float4 Position : POSITION;
    float Depth : TEXCOORD0;
};

float4x4 BaseMatrix;
float4x4 LightMatrix;

float4 GetPositionFromLight(float4 position)
{
    return mul(position, LightMatrix);  
}

VertexToPixel CityNoShadVS(VertexToPixel Input)
{
 	VertexToPixel Output = (VertexToPixel)0;
	Output.VertexPosition = mul(Input.VertexPosition, BaseMatrix);
	Output.ATextureCoord = Input.ATextureCoord;
	Output.BTextureCoord = Input.BTextureCoord;
	Output.CTextureCoord = Input.CTextureCoord;
	Output.BlendTextureCoord = Input.BlendTextureCoord;
	Output.RoadTextureCoord = Input.RoadTextureCoord;
	Output.RoadCTextureCoord = Input.RoadCTextureCoord;
	return Output;
}

VertexToPixel CityVS(VertexToPixel Input)
{
	VertexToPixel Output = CityNoShadVS(Input);
	
	//calculate position of vertice in relation to light, for comparison to Shadow Map
	float4 LightPos = GetPositionFromLight(Input.VertexPosition);
	
	Output.vPos = 0.5*(LightPos.xy/LightPos.w)+float2(0.5, 0.5);
	Output.vPos.y = (1.0f - Output.vPos.y); //position of vertice on shadow map
	
	Output.Depth.x = 1 - (LightPos.z/LightPos.w); //feed depth relative to light to compare against shadow map depth
	return Output;
}

VertexToShad ShadVS(VertexToPixel Input)
{
	VertexToShad Output = (VertexToShad)0;
	Output.Position = GetPositionFromLight(Input.VertexPosition);
	Output.Depth.x = 1-(Output.Position.z/Output.Position.w);
	return Output;
}

technique RenderCity
{
	pass Final
	{
		VertexShader = compile vs_2_0 CityVS();
	}
	
	pass ShadowMap
	{
		VertexShader = compile vs_2_0 ShadVS();
	}
	
	pass FinalNoShad
	{
		VertexShader = compile vs_2_0 CityNoShadVS();
	}
	
}
