/**
 * Various effects for rendering the 2D world.
 */
float4x4 viewProjection : ViewProjection;
float4x4 worldViewProjection;
float worldUnitsPerTile = 2.5;
float3 dirToFront;
float4 offToBack;

texture pixelTexture : Diffuse;
texture depthTexture : Diffuse;
texture maskTexture : Diffuse;

sampler pixelSampler = sampler_state {
    texture = <pixelTexture>;
    AddressU  = CLAMP; AddressV  = CLAMP; AddressW  = CLAMP;
    MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};

sampler depthSampler = sampler_state {
    texture = <depthTexture>;
    AddressU  = CLAMP; AddressV  = CLAMP; AddressW  = CLAMP;
    MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};

sampler maskSampler = sampler_state {
    texture = <maskTexture>;
    AddressU  = CLAMP; AddressV  = CLAMP; AddressW  = CLAMP;
    MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};


/**
 * SIMPLE EFFECT
 *   This effect simply draws the pixel texture onto the screen.
 *   Args:
 *		pixelTexture - Texture to sample for the pixel output
 */

struct SimpleVertex {
    float4 position: POSITION;
    float2 texCoords : TEXCOORD0;
    float objectID : TEXCOORD1;
};

SimpleVertex vsSimple(SimpleVertex v){
    SimpleVertex result;
    result.position = mul(v.position, viewProjection);
    result.texCoords = v.texCoords;
    result.objectID = v.objectID;
    return result;
}

void psSimple(SimpleVertex v, out float4 color: COLOR0){
	color = tex2D( pixelSampler, v.texCoords);
	if (color.a == 0) discard;
}

technique drawSimple {
   pass p0 {
		AlphaBlendEnable = TRUE; DestBlend = INVSRCALPHA; SrcBlend = SRCALPHA;
		
        ZEnable = false; ZWriteEnable = false;
        CullMode = CCW;
        
        VertexShader = compile vs_3_0 vsSimple();
        PixelShader  = compile ps_3_0 psSimple();
   }
}

void psIDSimple(SimpleVertex v, out float4 color: COLOR0){
	color = float4(v.objectID, 0.0, 0.0, min(tex2D( pixelSampler, v.texCoords).a*255.0, 1.0));
	if (color.a == 0) discard;
}

technique drawSimpleID {
   pass p0 {
		AlphaBlendEnable = TRUE; DestBlend = INVSRCALPHA; SrcBlend = SRCALPHA;
		
        ZEnable = false; ZWriteEnable = false;
        CullMode = CCW;
        
        VertexShader = compile vs_3_0 vsSimple();
        PixelShader  = compile ps_3_0 psIDSimple();
   }
}


/**
 * SPRITE ZBUFFER EFFECT
 *   This effect draws the pixels from the pixel sampler with depth provided by a zbuffer sprite.
 *   The depth buffer is used along with the sprites world coordinates to determine an absolute
 *   depth value.
 *   
 *   Args:
 *		pixelTexture - Texture to sample for the pixel output
 *		depthTexture - Texture to sample for the zbuffer values
 *		worldPosition - Position of the object in the world
 */

struct ZVertexIn {
	float4 position: POSITION;
    float2 texCoords : TEXCOORD0;
    float3 worldCoords : TEXCOORD1;
    float objectID : TEXCOORD2;
};

struct ZVertexOut {
	float4 position: POSITION;
    float2 texCoords : TEXCOORD0;
    float backDepth: TEXCOORD1;
    float frontDepth: TEXCOORD2;
    float objectID: TEXCOORD3;
};

ZVertexOut vsZSprite(ZVertexIn v){
    ZVertexOut result;
    result.position = mul(v.position, viewProjection);
    result.texCoords = v.texCoords;
	result.objectID = v.objectID;
    
    float4 backPosition = float4(v.worldCoords.x, v.worldCoords.y, v.worldCoords.z, 0)+offToBack;
    float4 frontPosition = backPosition;
    frontPosition.x += dirToFront.x;
    frontPosition.z += dirToFront.z;
    
    float4 backProjection = mul(backPosition, worldViewProjection);
    float4 frontProjection = mul(frontPosition, worldViewProjection);
    
    result.backDepth = backProjection.z / backProjection.w;
    result.frontDepth = frontProjection.z / frontProjection.w;
    result.frontDepth -= result.backDepth;   
    
    return result;
}

void psZSprite(ZVertexOut v, out float4 color:COLOR, out float depth:DEPTH0) {
    color = tex2D(pixelSampler, v.texCoords);
    
    float difference = ((1-tex2D(depthSampler, v.texCoords).r)/0.4);
    depth = v.backDepth + (difference*v.frontDepth);
	if (color.a == 0) discard;
}

//walls work the same as z sprites, except with an additional mask texture.

void psZWall(ZVertexOut v, out float4 color:COLOR, out float depth:DEPTH0) {
    color = tex2D(pixelSampler, v.texCoords);
    color.a = tex2D(maskSampler, v.texCoords).a;
    
    float difference = ((1-tex2D(depthSampler, v.texCoords).r)/0.4);
    depth = v.backDepth + (difference*v.frontDepth);
	if (color.a == 0) discard;
}


technique drawZSprite {
   pass p0 {
		AlphaBlendEnable = TRUE; DestBlend = INVSRCALPHA; SrcBlend = SRCALPHA;
        
        ZEnable = true; ZWriteEnable = true;
        CullMode = CCW;
        
        VertexShader = compile vs_3_0 vsZSprite();
        PixelShader  = compile ps_3_0 psZSprite();
        
   }
}


technique drawZWall {
   pass p0 {
		AlphaBlendEnable = TRUE; DestBlend = INVSRCALPHA; SrcBlend = SRCALPHA;
        
        ZEnable = true; ZWriteEnable = true;
        CullMode = CCW;
        
        VertexShader = compile vs_3_0 vsZSprite();
        PixelShader  = compile ps_3_0 psZWall();
        
   }
}

/**
 * SPRITE ZBUFFER EFFECT DEPTH CHANNEL
 *   Same as the sprite zbuffer effect except it draws the depth to the output render target.
 *	 This allows you to restore it at a later date.
 *   
 *   Args:
 *		pixelTexture - Texture to sample for the pixel output
 *		depthTexture - Texture to sample for the zbuffer values
 *		worldPosition - Position of the object in the world
 */

void psZDepthSprite(ZVertexOut v, out float4 color:COLOR0, out float4 depthB:COLOR1, out float depth:DEPTH0) {
	float4 pixel = tex2D(pixelSampler, v.texCoords);
    float difference = ((1-tex2D(depthSampler, v.texCoords).r)/0.4); 
    depth = v.backDepth + (difference*v.frontDepth);
	pixel.rgb = v.backDepth;
    
    color = pixel;
    depthB = depth;
    if (pixel.a > 0.01) depthB.a = 1.0;
	else discard;
}

technique drawZSpriteDepthChannel {
   pass p0 {
		AlphaBlendEnable = TRUE; DestBlend = INVSRCALPHA; SrcBlend = SRCALPHA;
        
        ZEnable = true; ZWriteEnable = true;
        CullMode = CCW;
        
        VertexShader = compile vs_3_0 vsZSprite();
        PixelShader  = compile ps_3_0 psZDepthSprite();
   }
}

void psZDepthWall(ZVertexOut v, out float4 color:COLOR0, out float4 depthB:COLOR1, out float depth:DEPTH0) {
	float4 pixel = tex2D(pixelSampler, v.texCoords);
    pixel.a = tex2D(maskSampler, v.texCoords).a;
    
    float difference = ((1-tex2D(depthSampler, v.texCoords).r)/0.4); 
    depth = v.backDepth + (difference*v.frontDepth);
    
    color = pixel;
    depthB = depth;
    if (pixel.a > 0.01) depthB.a = 1.0;
	else discard;
}

technique drawZWallDepthChannel {
   pass p0 {
		AlphaBlendEnable = TRUE; DestBlend = INVSRCALPHA; SrcBlend = SRCALPHA;
        
        ZEnable = true; ZWriteEnable = true;
        CullMode = CCW;
        
        VertexShader = compile vs_3_0 vsZSprite();
        PixelShader  = compile ps_3_0 psZDepthWall();
        
   }
}

/**
 * SPRITE ZBUFFER EFFECT OBJID
 *   Draws the object id of the sprites (with depth as a consideration) onto a buffer, so that the id of the
 *   object that the mouse is over can be selected for interaction access/highlighting.
 *   
 *   Args:
 *		pixelTexture - Texture to sample for the pixel output
 *		depthTexture - Texture to sample for the zbuffer values
 *		worldPosition - Position of the object in the world
 *		objectID - The ID of the object from 0-1 float (multiply by 65535 to get ID)
 */

void psZIDSprite(ZVertexOut v, out float4 color:COLOR, out float depth:DEPTH0) {
	float4 pixel = tex2D( pixelSampler, v.texCoords);
    float difference = ((1-tex2D(depthSampler, v.texCoords).r)/0.4); 
    depth = v.backDepth + (difference*v.frontDepth);
    
    //Copy alpha pixel so alpha test creates same result
    color.a = min(pixel.a*255.0, 1.0); //result should only be 0 or >=1 for pixels with any alpha
    color.rgb = v.objectID;
	if (color.a == 0) discard;
}

technique drawZSpriteOBJID {
   pass p0 {
        AlphaBlendEnable = FALSE;
        ZEnable = true; ZWriteEnable = true;
        CullMode = CCW;
        
        VertexShader = compile vs_3_0 vsZSprite();
        PixelShader  = compile ps_3_0 psZIDSprite();
   }
}

/**
 * SIMPLE EFFECT WITH RESTORE DEPTH
 *   Same as simple effect except the depth buffer is restored using a texture
 *   
 *   Args:
 *		pixelTexture - Texture to sample for the pixel output
 *		depthTexture - Texture to sample for absolute z-values
 */
 

void psSimpleRestoreDepth(SimpleVertex v, out float4 color: COLOR0, out float depth:DEPTH0){
	color = tex2D( pixelSampler, v.texCoords);

	if (color.a < 0.01) {
		depth = 1.0;
		discard;
	}
	else {
		float4 dS = tex2D( depthSampler, v.texCoords);
		depth = dS.r;
	}
}

technique drawSimpleRestoreDepth {
   pass p0 {
		AlphaBlendEnable = TRUE; DestBlend = INVSRCALPHA; SrcBlend = SRCALPHA;
		
        ZEnable = true; ZWriteEnable = true;
        CullMode = CCW;
        
        VertexShader = compile vs_3_0 vsSimple();
        PixelShader  = compile ps_3_0 psSimpleRestoreDepth();
   }
}






