/**
 * THE PURPOSE OF THIS EFFECT IS TO RENDER HOUSE SCENES. TO DO THIS
 * A PIXEL SHADER IS USED WHICH ALLOWS EVERY PIXEL'S DEPTH TO BE
 * INDIVIDUALLY WRITTEN TO THE DEPTH BUFFER.
 * 
 * THE HOUSE BATCH CLASS WILL SELECT THE TECHNIQUE TO USE BASED ON THE
 * DRAWING MODE
 */ 
float4x4 viewProjection  : ViewProjection;
texture diffuseTexture : Diffuse <string ResourceName = "default_color.dds";>;
texture depthTexture : Diffuse <string ResourceName = "default_depth.dds";>;


sampler TextureSampler = sampler_state 
{
    texture = <diffuseTexture>;
    AddressU  = CLAMP;
    AddressV  = CLAMP;
    AddressW  = CLAMP;
    MIPFILTER = POINT;
    MINFILTER = POINT;
    MAGFILTER = POINT;
};

sampler ZSampler = sampler_state 
{
    texture = <depthTexture>;
    AddressU  = CLAMP;
    AddressV  = CLAMP;
    AddressW  = CLAMP;
    MIPFILTER = POINT;
    MINFILTER = POINT;
    MAGFILTER = POINT;
};

struct Vertex
{
    float4 Position: POSITION;
    float4 Color: COLOR;
    float2 TexCoord : TEXCOORD0;
};

struct PixelSimple {
	float4 Color: COLOR;
};

struct Pixel {
	float4 Color: COLOR;
	float Depth: DEPTH0;
};


Vertex vsWithDepth(Vertex v)
{
    Vertex result;
    //  2D vertices are already pre-multiplied times the world matrix.
    result.Position = mul(v.Position, viewProjection);
    result.Color = v.Color;
    result.TexCoord = v.TexCoord;
    return result;
}

//-----------------------------------
Pixel psWithDepth(Vertex v)
{
	Pixel output = (Pixel)0;
	
    float4 diffuseTexture = tex2D( TextureSampler, v.TexCoord);
    output.Color = diffuseTexture;
    output.Depth = tex2D(ZSampler, v.TexCoord).r;
    
    return output;
}


technique drawWithDepth
{
   pass p0
   {
		AlphaBlendEnable = TRUE;
        DestBlend = INVSRCALPHA;
        SrcBlend = SRCALPHA;
        
        ZEnable = true;
        ZWriteEnable = true;
        CullMode = CCW;
        
        VertexShader = compile vs_1_1 vsWithDepth();
        PixelShader  = compile ps_2_0 psWithDepth();
   }
}


Vertex vsSimple(Vertex v)
{
    Vertex result;
    //  2D vertices are already pre-multiplied times the world matrix.
    result.Position = mul(v.Position, viewProjection);
    result.Color = v.Color;
    result.TexCoord = v.TexCoord;
    return result;
}

//-----------------------------------
PixelSimple psSimple(Vertex v)
{
	PixelSimple output = (PixelSimple)0;
	
    float4 diffuseTexture = tex2D( TextureSampler, v.TexCoord);
    output.Color = diffuseTexture;
    
    return output;
}

technique drawSimple
{
   pass p0
   {
		AlphaBlendEnable = TRUE;
        DestBlend = INVSRCALPHA;
        SrcBlend = SRCALPHA;
        
        CullMode = CCW;
        
        VertexShader = compile vs_1_1 vsSimple();
        PixelShader  = compile ps_2_0 psSimple();
   }
}