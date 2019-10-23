#include "LightingCommon.fx"

/**
 * Various effects for rendering the 2D world.
 */
float4x4 viewProjection : ViewProjection;
float4x4 worldViewProjection : ViewProjection;
float4x4 rotProjection : ViewProjection;
float4x4 iWVP;
float worldUnitsPerTile = 2.5;
float3 dirToFront;
float4 offToBack;
bool depthOutMode;

float2 PxOffset;
float4 WorldOffset;

float MaxFloor;

texture pixelTexture : Diffuse;
texture depthTexture : Diffuse;
texture maskTexture : Diffuse;
texture ambientLight : Diffuse;

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

sampler ambientSampler = sampler_state {
	texture = <ambientLight>;
	AddressU = CLAMP; AddressV = CLAMP; AddressW = CLAMP;
	MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};

float dpth(float4 v) {
    #if SM4
        return v.a;
    #else
        return v.r;
    #endif
}

float4 packDepth(float d) {
    float4 enc = float4(1.0, 255.0, 65025.0, 0.0) * d;
    enc = frac(enc);
    enc -= enc.yzww * float4(1.0 / 255.0, 1.0 / 255.0, 1.0 / 255.0, 0.0);
    enc.a = 1;

    return enc; //float4(byteFloor(d%1.0), byteFloor((d*256.0) % 1.0), byteFloor((d*65536.0) % 1.0), 1); //most sig in r, least in b
}

float unpackDepth(float4 d) {
    return dot(d, float4(1.0, 1 / 255.0, 1 / 65025.0, 0)); //d.r + (d.g / 256.0) + (d.b / 65536.0);
}

float4 packObjID(float id) {
    return (packDepth(id));
}
/**
 * SIMPLE EFFECT
 *   This effect simply draws the pixel texture onto the screen.
 *   Args:
 *		pixelTexture - Texture to sample for the pixel output
 */

struct SimpleVertex {
    float4 position: SV_Position0;
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
	color.rgb *= color.a; //"pre"multiply, just here for experimentation
	if (color.a == 0) discard;
}

technique drawSimple {
   pass p0 {
#if SM4
        VertexShader = compile vs_4_0_level_9_1 vsSimple();
        PixelShader = compile ps_4_0_level_9_1 psSimple();
#else
        VertexShader = compile vs_3_0 vsSimple();
        PixelShader = compile ps_3_0 psSimple();
#endif;

   }
}

void psIDSimple(SimpleVertex v, out float4 color: COLOR0){
	color = packObjID(v.objectID.x);
    color.a = min(tex2D(pixelSampler, v.texCoords).a*255.0, 1.0);
	if (color.a == 0) discard;
}

technique drawSimpleID {
   pass p0 {
#if SM4
        VertexShader = compile vs_4_0_level_9_1 vsSimple();
        PixelShader = compile ps_4_0_level_9_1 psIDSimple();
#else
        VertexShader = compile vs_3_0 vsSimple();
        PixelShader = compile ps_3_0 psIDSimple();
#endif;

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
	float4 position: SV_Position0;
    float2 texCoords : TEXCOORD0;
    float3 worldCoords : TEXCOORD1;
    float2 objectID : TEXCOORD2;
	float2 room : TEXCOORD3;
};

struct ZVertexOut {
	float4 position: SV_Position0;
    float2 texCoords : TEXCOORD0;
    float2 objectID: TEXCOORD2; //need to use unused texcoords - or glsl recompilation fails miserably.
    float2 backDepth: TEXCOORD3;
    float2 frontDepth: TEXCOORD4;
	float2 roomVec : TEXCOORD5;
	float2 screenPos : TEXCOORD6;
};

float depthCalc(ZVertexOut v) {
	float difference = (1 - dpth(tex2D(depthSampler, v.texCoords))) / 0.4;
	return (v.backDepth.x + (difference*v.frontDepth.x));
}

float2 depthCalc2(ZVertexOut v) {
	float difference = (1 - dpth(tex2D(depthSampler, v.texCoords))) / 0.4;
	return (v.backDepth + (difference*v.frontDepth));
}

float4 lightInterp2D(float4 inPosition) {
	inPosition.xyz *= WorldToLightFactor;
	inPosition.xz += LightOffset;

	float level = max(0, floor(inPosition.y) + 0.0001); //todo: sprite defines our level (3d walls will give us more control here)
	float abvLevel = min(MaxFloor, level + 1);
	float2 iPA = inPosition.xz + 1 / MapLayout * floor(float2(abvLevel % MapLayout.x, abvLevel / MapLayout.x));
	inPosition.xz += 1 / MapLayout * floor(float2(level % MapLayout.x, level / MapLayout.x));

	float4 lTex = tex2D(advLightSampler, inPosition.xz);
	lTex.rgb = lerp(lTex.rgb, tex2D(advLightSampler, iPA).rgb, max(0, (inPosition.y % 1) * 2 - 1));

	return lightColorI(lTex, clamp((inPosition.y % 1) * 3, 0, 1));
}

ZVertexOut vsZSprite(ZVertexIn v){
    ZVertexOut result;
	float4 inPos = v.position;
	inPos.xy += PxOffset;
	float4 pos = mul(inPos, viewProjection);
    result.position = pos;
	result.screenPos = pos.xy;
    result.texCoords = v.texCoords;
	result.objectID = v.objectID;
	result.roomVec = v.room;

    //HACK: somehow prevents result.roomVec from failing to set?? Condition should never occur.
    if (v.room.x == 2.0 && v.room.y == 2.0 && v.objectID.x == -1.0) result.texCoords /= 2.0; 
    
    float4 backPosition = float4(v.worldCoords.x, v.worldCoords.y, v.worldCoords.z, 1) + WorldOffset + offToBack;
    float4 frontPosition = float4(backPosition.x, backPosition.y, backPosition.z, backPosition.w);
    frontPosition.x += dirToFront.x;
    frontPosition.z += dirToFront.z;
    
    float4 backProjection = mul(backPosition, worldViewProjection);
    float4 frontProjection = mul(frontPosition, worldViewProjection);
    
    result.backDepth.x = backProjection.z / backProjection.w - (0.00000000001*backProjection.x+0.00000000001*backProjection.y);
	if (isnan(result.backDepth.x)) result.backDepth.x = 0;
	result.backDepth.y = backProjection.w;
    result.frontDepth.x = frontProjection.z / frontProjection.w - (0.00000000001*frontProjection.x+0.00000000001*frontProjection.y);
	if (isnan(result.frontDepth.x)) result.frontDepth.x = 0;
	result.frontDepth.y = frontProjection.w;
    result.frontDepth -= result.backDepth;   
    
    return result;
}

ZVertexOut restoreZSprite(ZVertexIn v){
    ZVertexOut result;
    result.position = mul(v.position, viewProjection);
	result.screenPos = result.position.xy;
    result.texCoords = v.texCoords;
    result.objectID = v.objectID;
    result.roomVec = v.room;
    
    float4 backPosition = float4(v.worldCoords.x, v.worldCoords.y, v.worldCoords.z, 1);
    
    float4 backProjection = mul(backPosition, rotProjection);
    float4 nullProjection = mul(float4(0,0,0,1), rotProjection);

    //float4 frontPosition = float4(dirToFront.x, dirToFront.z, 0, 0);
    //float4 frontProjection = mul(frontPosition, worldViewProjection);
    
    result.backDepth.x = backProjection.z / backProjection.w - (0.00000000001*backProjection.x+0.00000000001*backProjection.y+0.00000000001*nullProjection.x+0.00000000001*nullProjection.y) - nullProjection.z / nullProjection.w;
	result.backDepth.y = 0;
    result.frontDepth = result.backDepth;   
    
    return result;
}

void psZSprite(ZVertexOut v, out float4 color:COLOR, out float depth:DEPTH0) {
	float4 pixel = tex2D(pixelSampler, v.texCoords);
	if (pixel.a == 0) discard;

	bool lastSeg = floor(v.roomVec.y * 256) == 255;
	int xRoom = floor(v.roomVec.x * 256);
	if (lastSeg == true && xRoom == 254) {
		pixel = float4(float3(1.0, 1.0, 1.0) - pixel.xyz, pixel.a);
	} else if (lastSeg == true && xRoom == 253) {
		float gray = dot(pixel.xyz, float3(0.2989, 0.5870, 0.1140));
		pixel = float4(gray, gray, gray, pixel.a);
	}
	else if (v.roomVec.x == 0.0) {
		pixel = pixel;
	}
	else {
		pixel = gammaMulSimple(pixel, tex2D(ambientSampler, v.roomVec));
	}

	pixel.rgb *= pixel.a; //"pre"multiply, just here for experimentation

	color = pixel;
	float2 d = depthCalc2(v);
	depth = d.x;
}
//walls work the same as z sprites, except with an additional mask texture.

void psZWall(ZVertexOut v, out float4 color:COLOR, out float depth:DEPTH0) {
    color = gammaMulSimple(tex2D(pixelSampler, v.texCoords), tex2D(ambientSampler, v.roomVec));
    color.a = tex2D(maskSampler, v.texCoords).a;
	if (color.a == 0) discard;
	color.rgb *= color.a; //"pre"multiply, just here for experimentation
    
	depth = depthCalc(v);
}


technique drawZSprite {
   pass p0 {
#if SM4
        VertexShader = compile vs_4_0_level_9_1 vsZSprite(); //_level_9_1
        PixelShader = compile ps_4_0_level_9_1 psZSprite();
#else
        VertexShader = compile vs_3_0 vsZSprite();
        PixelShader = compile ps_3_0 psZSprite();
#endif;

   }
}


technique drawZWall {
   pass p0 {
#if SM4
        VertexShader = compile vs_4_0_level_9_1 vsZSprite();
        PixelShader = compile ps_4_0_level_9_1 psZWall();
#else
        VertexShader = compile vs_3_0 vsZSprite();
        PixelShader = compile ps_3_0 psZWall();
#endif;
        
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
	if (pixel.a <= 0.01) discard;
	float2 d = depthCalc2(v);
	depth = d.x;
   
    depthB = packDepth(depth);
    if (depthOutMode == true) {
        color = depthB;
    } else {
		bool lastRow = floor(v.roomVec.y * 256) == 255;
		int col = floor(v.roomVec.x * 256);
		if (lastRow == true && col > 252) {
			if (col == 254) pixel = float4(float3(1.0, 1.0, 1.0) - pixel.xyz, pixel.a);
			else if (col == 253) {
				float gray = dot(pixel.xyz, float3(0.2989, 0.5870, 0.1140));
				pixel = float4(gray, gray, gray, pixel.a);
			}
			//255 does not light pixel at all.
		}
		else if (v.roomVec.x < 0.0) pixel = gammaMulSimple(pixel, tex2D(ambientSampler, v.roomVec));
		else if (v.roomVec.x != 0.0) {
			//advanced lighting mode
			float4 projection = mul(float4(v.screenPos.x, v.screenPos.y, d.x*d.y, d.y), iWVP);
			pixel = gammaMul(pixel, lightProcessLevel(projection, v.objectID.y));
			pixel.rgb += projection.yzw * 0.00000000001; //monogame keeps trying to optimise out entire matrix columns im like well played guys who needs those right
		}
		color = pixel;

        color.rgb *= max(1, v.objectID.x); //hack - otherwise v.objectID always equals 0 on intel and 1 on nvidia (yeah i don't know)
        color.rgb *= color.a; //"pre"multiply, just here for experimentation
    }
}

void psZDepthSpriteSimple(ZVertexOut v, out float4 color:COLOR0, out float4 depthB : COLOR1, out float depth : DEPTH0) {
	float4 pixel = tex2D(pixelSampler, v.texCoords);
	if (pixel.a <= 0.01) discard;
	depth = depthCalc(v);

	depthB = packDepth(depth);
	if (depthOutMode == true) {
		color = depthB;
	}
	else {
		bool lastRow = floor(v.roomVec.y * 256) == 255;
		int col = floor(v.roomVec.x * 256);
		if (lastRow == true && col == 254) pixel = float4(float3(1.0, 1.0, 1.0) - pixel.xyz, pixel.a);
		else if (lastRow == true && col == 253) {
			float gray = dot(pixel.xyz, float3(0.2989, 0.5870, 0.1140));
			pixel = float4(gray, gray, gray, pixel.a);
		}
		else if (v.roomVec.x != 0.0) {
			pixel = gammaMulSimple(pixel, tex2D(ambientSampler, v.roomVec));
		}
		color = pixel;
		color.rgb *= color.a; //"pre"multiply, just here for experimentation
	}
}
/*
void psZDepthSpriteDirLight(ZVertexOut v, out float4 color:COLOR0, out float4 depthB : COLOR1, out float depth : DEPTH0) {
	float4 pixel = tex2D(pixelSampler, v.texCoords);
	if (pixel.a <= 0.01) discard;
	float2 d = depthCalc2(v);
	depth = d.x;

	depthB = packDepth(depth);
	if (depthOutMode == true) {
		color = depthB;
	}
	else {
		bool lastRow = floor(v.roomVec.y * 256) == 255;
		int col = floor(v.roomVec.x * 256);
		if (lastRow == true && col > 252) {
			if (col == 254) pixel = float4(float3(1.0, 1.0, 1.0) - pixel.xyz, pixel.a);
			else if (col == 253) {
				float gray = dot(pixel.xyz, float3(0.2989, 0.5870, 0.1140));
				pixel = float4(gray, gray, gray, pixel.a);
			}
			//255 does not light pixel at all.
		}
		else if (v.roomVec.x < 0.0) pixel *= tex2D(ambientSampler, v.roomVec);
		else if (v.roomVec.x != 0.0) {
			//advanced lighting mode
			float4 projection = mul(float4(v.screenPos.x, v.screenPos.y, d.x*d.y, d.y), iWVP);
			float3 normal = normalize(cross(ddx(projection.xyz), -ddy(projection.xyz)));
			pixel *= lightProcessDirectionLevel(projection, normal, v.objectID.y);
			pixel.rgb += projection.yzw * 0.00000000001; //monogame keeps trying to optimise out entire matrix columns im like well played guys who needs those right
		}
		color = pixel;

		color.rgb *= max(1, v.objectID.x); //hack - otherwise v.objectID always equals 0 on intel and 1 on nvidia (yeah i don't know)
		color.rgb *= color.a; //"pre"multiply, just here for experimentation
	}
}*/

technique drawZSpriteDepthChannel {
	pass simple {
#if SM4
		VertexShader = compile vs_4_0_level_9_1 vsZSprite(); //_level_9_1
		PixelShader = compile ps_4_0_level_9_1 psZDepthSpriteSimple(); //_level_9_1
#else
		VertexShader = compile vs_3_0 vsZSprite();
		PixelShader = compile ps_3_0 psZDepthSpriteSimple();
#endif;
	}

    pass advLighting {
#if SM4
        VertexShader = compile vs_4_0_level_9_3 vsZSprite(); //_level_9_1
        PixelShader = compile ps_4_0_level_9_3 psZDepthSprite(); //_level_9_1
#else
        VertexShader = compile vs_3_0 vsZSprite();
        PixelShader = compile ps_3_0 psZDepthSprite();
#endif;
    }

	/*
	pass dirLighting {
		ZEnable = true; ZWriteEnable = true;
		CullMode = CCW;

#if SM4
		VertexShader = compile vs_4_0_level_9_3 vsZSprite(); //_level_9_1
		PixelShader = compile ps_4_0_level_9_3 psZDepthSpriteDirLight(); //_level_9_1
#else
		VertexShader = compile vs_3_0 vsZSprite();
		PixelShader = compile ps_3_0 psZDepthSpriteDirLight();
#endif;
	}*/
}

void psZDepthWall(ZVertexOut v, out float4 color:COLOR0, out float4 depthB:COLOR1, out float depth:DEPTH0) {
	float4 pixel = tex2D(pixelSampler, v.texCoords);
    pixel.a = tex2D(maskSampler, v.texCoords).a;
	if (pixel.a <= 0.01) discard;

	float2 d = depthCalc2(v);
	depth = d.x;
    
    depthB = packDepth(depth);
    if (depthOutMode == true) {
        color = depthB;
    }
    else {
		//advanced light
		float4 projection = mul(float4(v.screenPos.x, v.screenPos.y, d.x*d.y, d.y), iWVP);
		projection.y -= v.objectID.x;
		pixel = gammaMul(pixel, lightInterp2D(projection));
		pixel.rgb += projection.yzw * 0.00000000001; //monogame keeps trying to optimise out entire matrix columns im like well played guys who needs those right
		color = pixel;

        //color = pixel * tex2D(ambientSampler, v.roomVec);
        color.rgb *= color.a; //"pre"multiply, just here for experimentation
    }
}

void psZDepthWallSimple(ZVertexOut v, out float4 color:COLOR0, out float4 depthB : COLOR1, out float depth : DEPTH0) {
	float4 pixel = tex2D(pixelSampler, v.texCoords);
	pixel.a = tex2D(maskSampler, v.texCoords).a;
	if (pixel.a <= 0.01) discard;
	depth = depthCalc(v);

	depthB = packDepth(depth);
	if (depthOutMode == true) {
		color = depthB;
	}
	else {
		color = gammaMulSimple(pixel, tex2D(ambientSampler, v.roomVec));
		color.rgb *= color.a; //"pre"multiply, just here for experimentation
	}
}

technique drawZWallDepthChannel {
	pass simple {
#if SM4
		VertexShader = compile vs_4_0_level_9_1 vsZSprite(); //_level_9_1
		PixelShader = compile ps_4_0_level_9_1 psZDepthWallSimple(); //_level_9_1
#else
		VertexShader = compile vs_3_0 vsZSprite();
		PixelShader = compile ps_3_0 psZDepthWallSimple();
#endif;

	}

    pass advLighting { 
#if SM4
        VertexShader = compile vs_4_0_level_9_3 vsZSprite(); //_level_9_1
        PixelShader = compile ps_4_0_level_9_3 psZDepthWall(); //_level_9_1
#else
        VertexShader = compile vs_3_0 vsZSprite();
        PixelShader = compile ps_3_0 psZDepthWall();
#endif;
        
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
	float4 pixel = tex2D(pixelSampler, v.texCoords);
	if (pixel.a < 0.1) discard;
	depth = depthCalc(v);

    color = packObjID(v.objectID.x);
}

technique drawZSpriteOBJID {
   pass p0 {
        AlphaBlendEnable = FALSE;
        
#if SM4
        VertexShader = compile vs_4_0_level_9_1 vsZSprite();
        PixelShader = compile ps_4_0_level_9_1 psZIDSprite();
#else
        VertexShader = compile vs_3_0 vsZSprite();
        PixelShader = compile ps_3_0 psZIDSprite();
#endif
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
 

void psSimpleRestoreDepth(ZVertexOut v, out float4 color: COLOR0, out float depth:DEPTH0){
	color = tex2D( pixelSampler, v.texCoords);

	if (color.a < 0.01) {
		depth = 1.0;
		discard;
	}
	else {
		float4 dS = tex2D(depthSampler, v.texCoords);
		depth = v.backDepth.x + unpackDepth(dS);
	}
}

technique drawSimpleRestoreDepth {
   pass p0 {
#if SM4
        VertexShader = compile vs_4_0_level_9_1 restoreZSprite();
        PixelShader = compile ps_4_0_level_9_1 psSimpleRestoreDepth();
#else
        VertexShader = compile vs_3_0 restoreZSprite();
        PixelShader = compile ps_3_0 psSimpleRestoreDepth();
#endif
   }
}






