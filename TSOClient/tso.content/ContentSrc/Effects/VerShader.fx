//Vertex shader output structure
struct VertexToPixel
{
	float4 VertexPosition : SV_Position0;
	
	float2 ATextureCoord : TEXCOORD0;
	float2 BTextureCoord : TEXCOORD1;
	float2 CTextureCoord : TEXCOORD2;
	float2 BlendTextureCoord : TEXCOORD3;
	float2 RoadTextureCoord : TEXCOORD4;
	float2 RoadCTextureCoord : TEXCOORD5;
	float3 Normal : TEXCOORD6;
};

struct VertexToPixelOut
{
	float4 VertexPosition : SV_Position0;
	
	float4 ABTextureCoord : TEXCOORD0;
	float3 shadPos : TEXCOORD1;
	float2 CTextureCoord : TEXCOORD2;
	float2 BlendTextureCoord : TEXCOORD3;
	float2 RoadTextureCoord : TEXCOORD4;
	float2 RoadCTextureCoord : TEXCOORD5;
	float3 vPos : TEXCOORD6;
	float3 Normal : TEXCOORD7;
};

struct VertexToShad
{
	float4 Position : SV_Position0;
    float Depth : TEXCOORD0;
};

float4x4 BaseMatrix;
float4x4 MV;
float4x4 LightMatrix;

float4 GetPositionFromLight(float4 position)
{
    return mul(position, LightMatrix);  
}

VertexToPixelOut CityNoShadVS(VertexToPixel Input)
{
 	VertexToPixelOut Output = (VertexToPixelOut)0;
	Output.VertexPosition = mul(Input.VertexPosition, BaseMatrix);
	Output.ABTextureCoord.xy = Input.ATextureCoord;
	Output.ABTextureCoord.zw = Input.BTextureCoord;
	Output.CTextureCoord = Input.CTextureCoord;
	Output.BlendTextureCoord = Input.BlendTextureCoord;
	Output.RoadTextureCoord = Input.RoadTextureCoord;
	Output.RoadCTextureCoord = Input.RoadCTextureCoord;
	Output.Normal = Input.Normal;
	return Output;
}

VertexToPixelOut CityVS(VertexToPixel Input)
{
	VertexToPixelOut Output = CityNoShadVS(Input);
	
	//calculate position of vertice in relation to light, for comparison to Shadow Map
	float4 LightPos = GetPositionFromLight(Input.VertexPosition);
	
	Output.shadPos.xy = 0.5*(LightPos.xy/LightPos.w)+float2(0.5, 0.5);
	Output.shadPos.y = (1.0f - Output.shadPos.y); //position of vertex on shadow map
	
	Output.shadPos.z = 1 - (LightPos.z/LightPos.w); //feed depth relative to light to compare against shadow map depth
	Output.vPos = float3(0, 0, 0);
	return Output;
}

VertexToPixelOut CityFogVS(VertexToPixel Input)
{
	VertexToPixelOut Output = CityNoShadVS(Input);

	//calculate position of vertice in relation to light, for comparison to Shadow Map
	float4 LightPos = GetPositionFromLight(Input.VertexPosition);

	Output.shadPos.xy = 0.5*(LightPos.xy / LightPos.w) + float2(0.5, 0.5);
	Output.shadPos.y = (1.0f - Output.shadPos.y); //position of vertex on shadow map

	Output.shadPos.z = 1 - (LightPos.z / LightPos.w); //feed depth relative to light to compare against shadow map depth

	float4 pos = mul(Input.VertexPosition, MV);
	pos.z += pos.w / 100000000;
	Output.vPos = pos.xyz; //
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
#if SM4
        VertexShader = compile vs_4_0_level_9_1 CityVS();
#else
        VertexShader = compile vs_3_0 CityVS();
#endif;
	}
	
	pass ShadowMap
	{
#if SM4
        VertexShader = compile vs_4_0_level_9_1 ShadVS();
#else
        VertexShader = compile vs_3_0 ShadVS();
#endif;
	}
	
	pass FinalNoShad
	{
#if SM4
        VertexShader = compile vs_4_0_level_9_1 CityNoShadVS();
#else
        VertexShader = compile vs_3_0 CityNoShadVS();
#endif;
	}
	
	pass FinalFog
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_1 CityFogVS();
#else
		VertexShader = compile vs_3_0 CityFogVS();
#endif;
	}

	pass FinalFogShadow
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_1 CityFogVS();
#else
		VertexShader = compile vs_3_0 CityFogVS();
#endif;
	}
}
