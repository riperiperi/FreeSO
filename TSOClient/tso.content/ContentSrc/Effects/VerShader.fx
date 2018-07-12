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
	float2 TexCoord: TEXCOORD1;
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
	Output.TexCoord = float2(0, 0);
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

//object vertex shader
float DepthBias;

struct ObjVertexIn
{
	float4 position : SV_Position0;
	float2 texCoord : TEXCOORD0;
	float3 normal : TEXCOORD1;
};

struct ObjVertexOut
{
	float4 position : SV_Position0;
	float2 texCoord : TEXCOORD0;
	float3 vPos : TEXCOORD1;
	float3 normal : TEXCOORD2;
	float3 shadPos : TEXCOORD3;
};
float4x4 ObjModel;
float HeightVScale;

float3 ShadPosObj(ObjVertexIn Input) {
	float4 LightPos = GetPositionFromLight(mul(Input.position, ObjModel));

	float3 result = float3(0, 0, 0);
	result.xy = 0.5*(LightPos.xy / LightPos.w) + float2(0.5, 0.5);
	result.y = (1.0f - result.y); //position of vertex on shadow map

	result.z = 1 - (LightPos.z / LightPos.w); //feed depth relative to light to compare against shadow map depth
	return result;
}

ObjVertexOut CityObjNoShadVS(ObjVertexIn Input)
{
	ObjVertexOut Output = (ObjVertexOut)0;
	Output.position = mul(mul(Input.position, ObjModel), BaseMatrix);
	Output.position.z += (DepthBias/ Output.position.z);
	Output.texCoord = Input.texCoord;
	Output.normal = Input.normal;
	return Output;
}

ObjVertexOut CityObjVS(ObjVertexIn Input)
{
	ObjVertexOut Output = CityObjNoShadVS(Input);

	Output.shadPos = ShadPosObj(Input);
	Output.vPos = float3(0, 0, 0);
	return Output;
}

ObjVertexOut CityObjFogVS(ObjVertexIn Input)
{
	ObjVertexOut Output = CityObjNoShadVS(Input);

	Output.shadPos = ShadPosObj(Input);
	float4 pos = mul(mul(Input.position, ObjModel), MV);
	pos.z += pos.w / 100000000;
	Output.vPos = pos.xyz; //
	return Output;
}

VertexToShad ShadObjVS(ObjVertexIn Input)
{
	VertexToShad Output = (VertexToShad)0;
	Output.Position = GetPositionFromLight(mul(Input.position, ObjModel));
	Output.Depth.x = 1 - (Output.Position.z / Output.Position.w);
	Output.TexCoord = Input.texCoord;
	return Output;
}

ObjVertexOut TreeVS(ObjVertexIn Input)
{
	float3 relOffset = Input.normal; //normal contains a translation
	relOffset.y *= HeightVScale;

	float4 opos = mul(Input.position, ObjModel) + float4(relOffset, 0);
	ObjVertexOut Output = (ObjVertexOut)0;
	Output.position = mul(opos, BaseMatrix);
	Output.position.z += (DepthBias / Output.position.z);
	Output.texCoord = Input.texCoord;
	Output.normal = float3(0, 1, 0);

	Input.position = opos;
	Output.shadPos = ShadPosObj(Input);
	float4 pos = mul(opos, MV);
	pos.z += pos.w / 100000000;
	Output.vPos = pos.xyz; //
	return Output;

	return CityObjFogVS(Input);
}

technique RenderCityObj
{
	pass Final
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_1 CityObjVS();
#else
		VertexShader = compile vs_3_0 CityObjVS();
#endif;
	}

	pass ShadowMap
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_1 ShadObjVS();
#else
		VertexShader = compile vs_3_0 ShadObjVS();
#endif;
	}

	pass FinalNoShad
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_1 CityObjNoShadVS();
#else
		VertexShader = compile vs_3_0 CityObjNoShadVS();
#endif;
	}

	pass FinalFog
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_1 CityObjFogVS();
#else
		VertexShader = compile vs_3_0 CityObjFogVS();
#endif;
	}

	pass FinalFogShadow
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_1 CityObjFogVS();
#else
		VertexShader = compile vs_3_0 CityObjFogVS();
#endif;
	}

	pass TreeVS
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_1 TreeVS();
#else
		VertexShader = compile vs_3_0 TreeVS();
#endif;
	}
}

//new city geometry
struct CityVertex
{
	float4 VertexPosition : SV_Position0;
	float4 TextureCoords : TEXCOORD0; //base texture xy, mask zw
	float4 NormalTrans : TEXCOORD1; //normal xyz, transparency w
};

struct CityVertexOut
{
	float4 VertexPosition : SV_Position0;

	float4 TextureCoords : TEXCOORD0; //base texture xy, mask zw
	float4 NormalTrans : TEXCOORD1; //normal xyz, transparency w
	float2 VertexCoord : TEXCOORD2;

	float3 shadPos : TEXCOORD3;
	float3 vPos : TEXCOORD4;
};

CityVertexOut NCityNoShadVS(CityVertex Input)
{
	CityVertexOut Output = (CityVertexOut)0;
	Output.VertexPosition = mul(Input.VertexPosition, BaseMatrix);
	Output.VertexPosition.z += DepthBias * Output.VertexPosition.w;
	Output.TextureCoords = Input.TextureCoords;
	Output.NormalTrans = Input.NormalTrans;
	Output.VertexCoord = Input.VertexPosition.xz / 512.0;
	return Output;
}

CityVertexOut NCityVS(CityVertex Input)
{
	CityVertexOut Output = NCityNoShadVS(Input);

	//calculate position of vertice in relation to light, for comparison to Shadow Map
	float4 LightPos = GetPositionFromLight(Input.VertexPosition);

	Output.shadPos.xy = 0.5*(LightPos.xy / LightPos.w) + float2(0.5, 0.5);
	Output.shadPos.y = (1.0f - Output.shadPos.y); //position of vertex on shadow map

	Output.shadPos.z = 1 - (LightPos.z / LightPos.w); //feed depth relative to light to compare against shadow map depth
	Output.vPos = float3(0, 0, 0);
	return Output;
}

CityVertexOut NCityFogVS(CityVertex Input)
{
	CityVertexOut Output = NCityNoShadVS(Input);

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

VertexToShad NShadVS(CityVertex Input)
{
	VertexToShad Output = (VertexToShad)0;
	Output.Position = GetPositionFromLight(Input.VertexPosition);
	Output.Depth.x = 1 - (Output.Position.z / Output.Position.w);
	Output.TexCoord = float2(0, 0);
	return Output;
}

technique RenderNCity
{
	pass Final
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_1 NCityVS();
#else
		VertexShader = compile vs_3_0 NCityVS();
#endif;
	}

	pass ShadowMap
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_1 NShadVS();
#else
		VertexShader = compile vs_3_0 NShadVS();
#endif;
	}

	pass FinalNoShad
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_1 NCityNoShadVS();
#else
		VertexShader = compile vs_3_0 NCityNoShadVS();
#endif;
	}

	pass FinalFog
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_1 NCityFogVS();
#else
		VertexShader = compile vs_3_0 NCityFogVS();
#endif;
	}

	pass FinalFogShadow
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_1 NCityFogVS();
#else
		VertexShader = compile vs_3_0 NCityFogVS();
#endif;
	}
}

float2 SpriteSize;
//vertex shader for positioned sprites
ObjVertexOut CitySprNoShadVS(ObjVertexIn Input)
{
	ObjVertexOut Output = (ObjVertexOut)0;
	Output.position = mul(mul(Input.position, ObjModel), BaseMatrix);
	Output.position.z += (DepthBias / Output.position.z);
	Output.position.xy += (Input.texCoord - float2(0.5, 0.5)) * SpriteSize * Output.position.w;
	Output.texCoord = Input.texCoord;
	Output.normal = Input.normal;
	return Output;
}

ObjVertexOut CitySprVS(ObjVertexIn Input)
{
	ObjVertexOut Output = CitySprNoShadVS(Input);

	Output.shadPos = ShadPosObj(Input);
	Output.vPos = float3(0, 0, 0);
	return Output;
}

ObjVertexOut CitySprFogVS(ObjVertexIn Input)
{
	ObjVertexOut Output = CitySprNoShadVS(Input);

	Output.shadPos = ShadPosObj(Input);
	float4 pos = mul(mul(Input.position, ObjModel), MV);
	pos.z += pos.w / 100000000;
	Output.vPos = pos.xyz; //
	return Output;
}

technique RenderCitySpr
{
	pass Final
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_1 CitySprVS();
#else
		VertexShader = compile vs_3_0 CitySprVS();
#endif;
	}

	pass ShadowMap //unused
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_1 ShadObjVS();
#else
		VertexShader = compile vs_3_0 ShadObjVS();
#endif;
	}

	pass FinalNoShad
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_1 CitySprNoShadVS();
#else
		VertexShader = compile vs_3_0 CitySprNoShadVS();
#endif;
	}

	pass FinalFog
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_1 CitySprFogVS();
#else
		VertexShader = compile vs_3_0 CitySprFogVS();
#endif;
	}

	pass FinalFogShadow
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_1 CitySprFogVS();
#else
		VertexShader = compile vs_3_0 CitySprFogVS();
#endif;
	}
}