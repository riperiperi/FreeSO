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
	float2 vPos: TEXCOORD6;
	float Depth: TEXCOORD7;
};

struct VertexToShad
{
	float4 Position : POSITION;
    float Depth : TEXCOORD0;
};

texture2D VertexColorTex;
sampler2D USampler = sampler_state
{
	Texture = <VertexColorTex>;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

texture2D ShadowMap;
sampler2D ShadSampler = sampler_state
{
	Texture = <ShadowMap>;
	AddressU = CLAMP;
	AddressV = CLAMP;
	MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
};

texture2D TextureAtlasTex;
sampler2D USamplerTex = sampler_state
{
	Texture = <TextureAtlasTex>;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

texture2D TransAtlasTex;
sampler2D USamplerBlend = sampler_state
{
	Texture = <TransAtlasTex>;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

texture2D RoadAtlasTex;
sampler2D RSamplerTex = sampler_state
{
	Texture = <RoadAtlasTex>;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

texture2D RoadAtlasCTex;
sampler2D RCSamplerTex = sampler_state
{
	Texture = <RoadAtlasCTex>;
	AddressU = CLAMP;
	AddressV = CLAMP;
};
float4 LightCol;
float ShadowMult;

float4 GetCityColor(VertexToPixel Input)
{
	float4 BlendA = tex2D(USamplerBlend, Input.BlendTextureCoord);
	float4 Base = tex2D(USamplerTex, Input.BTextureCoord);
	float4 Blend = tex2D(USamplerTex, Input.CTextureCoord);
	float4 Road = tex2D(RSamplerTex, Input.RoadTextureCoord);
	float4 RoadC = tex2D(RCSamplerTex, Input.RoadCTextureCoord);
	
	float A = BlendA.x;
	float InvA = 1.0 - A;
	
	Base = Base*InvA + Blend*A;
	Base *= tex2D(USampler, Input.ATextureCoord);
	
	Base = Base*(1.0-Road.w) + Road*Road.w;
	Base = Base*(1.0-RoadC.w) + RoadC*RoadC.w;
	return Base * LightCol;
}

float4 CityPS(VertexToPixel Input) : COLOR0
{

	float4 BCol = GetCityColor(Input);
	
	float sMapD = (float)tex2D(ShadSampler, Input.vPos);
	float depth = Input.Depth.x;
	
	if (sMapD-0.002 > depth) {
		return float4(BCol.xyz*ShadowMult, 1);
	} else {
		return BCol;
	}
}

float4 CityPSNoShad(VertexToPixel Input) : COLOR0
{
	return GetCityColor(Input);
}

float4 ShadowMapPS(VertexToShad Input) : COLOR0
{
	return float4(Input.Depth.x, 0, 0, 1);
}

technique RenderCity
{
	pass Final
	{
		PixelShader = compile ps_2_0 CityPS();
	}
	
	pass ShadowMap
	{
		PixelShader = compile ps_2_0 ShadowMapPS();
	}
	
	pass FinalNoShadow
	{
		PixelShader = compile ps_2_0 CityPSNoShad();
	}
}
