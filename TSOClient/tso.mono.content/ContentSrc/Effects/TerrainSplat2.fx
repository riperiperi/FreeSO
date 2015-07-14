float4x4 xView;
float4x4 xProjection;
float4x4 xWorld;
float3 xLightDirection;
float xAmbient;
bool xEnableLighting;


/** Sheet of terrain types **/
Texture xTextureGrass;
sampler TextureGrass = sampler_state {
	texture = <xTextureGrass> ; 
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
};

Texture xTextureSnow;
sampler TextureSnow = sampler_state {
	texture = <xTextureSnow> ; 
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
};

Texture xTextureSand;
sampler TextureSand = sampler_state {
	texture = <xTextureSand> ; 
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
};

Texture xTextureRock;
sampler TextureRock = sampler_state {
	texture = <xTextureRock> ; 
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
};

Texture xTextureWater;
sampler TextureWater = sampler_state {
	texture = <xTextureWater>; 
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
};


struct TerrainVertexToPixel
{
    float4 Position         : POSITION; 
    float4 Color            : COLOR0;
    float LightingFactor	: TEXCOORD0;
    float2 TextureCoords	: TEXCOORD1;
    float4 LightDirection   : TEXCOORD2;
    float4 TextureWeight1   : TEXCOORD3;
    float4 TextureWeight2   : TEXCOORD4;
};

struct TerrainPixelToFrame
{
    float4 Color : COLOR0;
};


TerrainVertexToPixel TerrainVS( float4 inPos : POSITION, float4 inColor: COLOR, float3 inNormal: NORMAL, float2 inTexCoords:TEXCOORD0, float4 inTexWeight1: TEXCOORD1, float4 inTexWeight2: TEXCOORD2)
{
	TerrainVertexToPixel Output = (TerrainVertexToPixel)0;
	float4x4 preViewProjection = mul (xView, xProjection);
	float4x4 preWorldViewProjection = mul (xWorld, preViewProjection);
    
	Output.Position = mul(inPos, preWorldViewProjection);
	Output.Color = inColor;
	Output.TextureWeight1 = inTexWeight1;
	Output.TextureWeight2 = inTexWeight2;
	Output.TextureCoords = inTexCoords;
	
	float3 Normal = normalize(mul(normalize(inNormal), xWorld));	
	Output.LightingFactor = 1;
	if (xEnableLighting)
		Output.LightingFactor = saturate(dot(Normal, -xLightDirection));
    
	return Output;    
}

TerrainPixelToFrame TerrainPS(TerrainVertexToPixel PSIn) 
{
	TerrainPixelToFrame Output = (TerrainPixelToFrame)0;
	
	Output.Color = tex2D (TextureGrass, PSIn.TextureCoords) * PSIn.TextureWeight1.x;
	Output.Color += tex2D (TextureSnow, PSIn.TextureCoords) * PSIn.TextureWeight1.y;
	Output.Color += tex2D (TextureSand, PSIn.TextureCoords) * PSIn.TextureWeight1.z;
	Output.Color += tex2D (TextureRock, PSIn.TextureCoords) * PSIn.TextureWeight1.w;
	Output.Color += tex2D (TextureWater, PSIn.TextureCoords) * PSIn.TextureWeight2.x;

	Output.Color *= PSIn.Color;
	Output.Color.rgb *= saturate(PSIn.LightingFactor + xAmbient);
	
	return Output;
}


technique TerrainSplat
{
	pass Pass0
    {   
    	VertexShader = compile vs_1_1 TerrainVS();
        PixelShader  = compile ps_2_0 TerrainPS();
    }
}