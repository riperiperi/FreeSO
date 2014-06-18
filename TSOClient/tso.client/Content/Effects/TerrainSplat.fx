float4x4 xView;
float4x4 xProjection;
float4x4 xWorld;
float3 xLightDirection;
float xAmbient;
bool xEnableLighting;


/** Sheet of terrain types **/
Texture xTextureTerrain;
sampler TextureTerrain = sampler_state {
	texture = <xTextureTerrain> ; 
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
};

/** Sheet of alpha maps to blend terrain **/
Texture xTextureBlend;
sampler TextureBlend = sampler_state { 
	texture = <xTextureBlend> ; 
	MinFilter = Linear; // Minification Filter
	MagFilter = Linear; // Magnification Filter
	MipFilter = Linear; // Mip-mapping
	AddressU = Wrap; // Address Mode for U Coordinates
	AddressV = Wrap; // Address Mode for V Coordinates
};



struct TerrainVertexToPixel
{
    float4 Position         : POSITION; 
    float4 Color            : COLOR0;
    float LightingFactor	: TEXCOORD0;
    float2 TextureCoords	: TEXCOORD1;
    float4 LightDirection   : TEXCOORD2;
    float2 BlendCoords	: TEXCOORD4;
    float2 BackCoords : TEXCOORD5;
};

struct TerrainPixelToFrame
{
    float4 Color : COLOR0;
};


TerrainVertexToPixel TerrainVS( float4 inPos : POSITION, float4 inColor: COLOR, float3 inNormal: NORMAL, float2 inTexCoords: TEXCOORD0, float2 inBlendCoords: TEXCOORD1, float2 inBackCoords: TEXCOORD2)
{
	TerrainVertexToPixel Output = (TerrainVertexToPixel)0;
	float4x4 preViewProjection = mul (xView, xProjection);
	float4x4 preWorldViewProjection = mul (xWorld, preViewProjection);
    
	Output.Position = mul(inPos, preWorldViewProjection);
	Output.Color = inColor;
	Output.TextureCoords = inTexCoords;
	Output.BlendCoords = inBlendCoords;
	Output.BackCoords = inBackCoords;
	
	float3 Normal = normalize(mul(normalize(inPos), xWorld));	
	Output.LightingFactor = 1;
	if (xEnableLighting)
		Output.LightingFactor = saturate(dot(Normal, -xLightDirection));
    
	return Output;    
}

TerrainPixelToFrame TerrainPS(TerrainVertexToPixel PSIn) 
{
	TerrainPixelToFrame Output = (TerrainPixelToFrame)0;		
    
    float4 blendPixel = tex2D(TextureBlend, PSIn.BlendCoords);
    float blendAlpha = 1 - (blendPixel.y);
    
    
    float4 backColor = tex2D(TextureTerrain, PSIn.BackCoords);
    float4 frontColor = tex2D(TextureTerrain, PSIn.TextureCoords);
    Output.Color =  frontColor * PSIn.Color; /*lerp(backColor, frontColor, blendAlpha) * PSIn.Color;*/
    
    /*Output.Color.rgb *= saturate(PSIn.LightingFactor + xAmbient);*/
    
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