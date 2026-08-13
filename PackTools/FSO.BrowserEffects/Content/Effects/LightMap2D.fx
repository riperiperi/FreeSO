//fixed parameters
float4x4 Projection;
float2 TileSize; //used for position to room masking. percentage of position space (0, 1) a tile takes up.

//change between rendering light passes
float RoomTarget; //room number for room masking
float2 RoomUVRescale;
float2 RoomUVOff;

float2 LightPosition; //in position space (0,1)
float4 LightColor;
float LightSize; //in position space (0,1)
float LightPower = 2.0; //gamma correction on lights. can get some nicer distributions.
float LightIntensity = 1.0;
float TargetRoom;
float BlurMax;
float BlurMin;
float2 MapLayout;
float2 UVBase;

float3 LightDirection;
float LightHeight;

float2 ShadowPowers;
float2 SSAASize;

bool IsOutdoors;

texture roomMap : Diffuse;
texture shadowMap : Diffuse; //alpha texture containing occlusion for this light. White = full occlusion.
texture floorShadowMap : Diffuse; //same as above, but floors only.

sampler roomSampler = sampler_state {
    texture = <roomMap>;
    AddressU = CLAMP; AddressV = CLAMP; AddressW = CLAMP;
    MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};

sampler shadowSampler = sampler_state {
    texture = <shadowMap>;
    AddressU = CLAMP; AddressV = CLAMP; AddressW = CLAMP;
    MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};

sampler floorShadowSampler = sampler_state {
	texture = <floorShadowMap>;
	AddressU = CLAMP; AddressV = CLAMP; AddressW = CLAMP;
	MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};

sampler floorShadowSamplerLin = sampler_state {
	texture = <floorShadowMap>;
	AddressU = CLAMP; AddressV = CLAMP; AddressW = CLAMP;
	MIPFILTER = LINEAR; MINFILTER = LINEAR; MAGFILTER = LINEAR;
};


struct VertexShaderInput
{
    float4 Position : SV_Position0;
};

struct VertexShaderOutput
{
    float4 Position : SV_Position0;
    float2 p : TexCoord0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;
    output.Position = mul(float4(input.Position.xy, 0, 1), Projection);
    output.p = input.Position.xy * MapLayout - UVBase;
	output.p += SSAASize / 20000000;

    return output;
}

bool OutsideRoomCheck(float2 uv) {

	uv *= RoomUVRescale;
	uv += RoomUVOff;
	float4 test = tex2D(roomSampler, uv);
	float room1 = round(test.x * 255 + (test.y * 65280));
	float room2 = round(test.z * 255 + (test.w * 65280));
	bool diagType = (room2 > 32767);
	if (diagType == true) {
		room2 -= 32768;
	}
	if (room1 != room2) {
		//diagonal mode
		uv /= TileSize;
		if (diagType == true) {
			//horizontal diag:
			if ((uv.x % 1) + (uv.y % 1) >= 1)
				return (TargetRoom != room2); //hi room
			else
				return (TargetRoom != room1); //low room
		}
		else {
			//vertical diag:
			if ((uv.x % 1) - (uv.y % 1) > 0)
				return (TargetRoom != room1); //low room
			else
				return (TargetRoom != room2); //hi room
		}
	}
	else {
		return TargetRoom != room1;
	}
}

float4 LightFinal(float light, float2 shadow) {
	shadow *= ShadowPowers;
	light = pow(light, LightPower);
	light -= light*shadow.x;
	float floorLight = light - light*shadow.y;

	return float4(LightColor.rgb*light, LightColor.a * min(1, floorLight));
}

float4 DirectionFinal(float3 direction, float light, float shadow) {
	//shadow *= ShadowPowers.x;
	light = pow(light, LightPower);
	light -= light*shadow;
	light *= LightColor.a;

	return float4(normalize(direction)*light, light);
}

float4 DirectionOutdoors(float3 direction, float light) {
	light *= LightColor.a;
	return float4(normalize(direction)*light, light);
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 uv = input.p;
	if (OutsideRoomCheck(uv)) discard;
    float light = clamp(1.0 - distance(uv, LightPosition) / LightSize, 0.0, 1.0);
	float2 shadow = float2(tex2D(shadowSampler, uv).r, tex2D(floorShadowSampler, uv).g);
	return LightFinal(light, shadow);
}

float4 PixelShaderFunctionBlur(VertexShaderOutput input) : COLOR0
{
	float2 uv = input.p;
	if (OutsideRoomCheck(uv)) discard;
	float dist = distance(uv, LightPosition);
	float light = clamp(1.0 - dist / LightSize, 0.0, 1.0);

	float blurAmount = dist / LightSize;
	blurAmount = lerp(BlurMin, BlurMax, blurAmount);

	float fs = 0;
	for (int x = -2; x < 3; x++) {
		for (int y = -2; y < 3; y++) {
			fs += tex2D(floorShadowSamplerLin, uv + float2(x*blurAmount, y*blurAmount)).g;
		}
	}

	float2 shadow = float2(tex2D(shadowSampler, uv).r, fs/(5*5));
	return LightFinal(light, shadow);
}

float4 PixelShaderFunctionOutdoors(VertexShaderOutput input) : COLOR0{
	float2 uv = input.p;
	if (OutsideRoomCheck(uv)) discard;
	float light = 1;

	float2 shadow = float2(tex2D(shadowSampler, uv).r, tex2D(floorShadowSampler, uv).g);
	shadow *= ShadowPowers;

	light -= light*shadow.x;
	float floorLight = light - light*shadow.y;
	// RGBA: LightIntensity, OutdoorsIntensity, LightIntensityShad, OutdoorsIntensityShad
	// output different under diff conditions
	return float4(LightColor.rgb*light, LightColor.a * floorLight);
}

float2 SSAASample(float2 uv) {
	float2 result = float2(0, 0);
	uv += SSAASize / 2;
	result += tex2D(shadowSampler, uv).rg;
	result += tex2D(shadowSampler, uv + float2(SSAASize.x, 0)).rg;
	result += tex2D(shadowSampler, uv + float2(0, SSAASize.y)).rg;
	result += tex2D(shadowSampler, uv + float2(SSAASize.x, SSAASize.y)).rg;
	return result / 4;
}

float4 PixelShaderFunctionOutdoorsSSAA(VertexShaderOutput input) : COLOR0{
	float2 uv = input.p;
	if (OutsideRoomCheck(uv)) discard;
	float light = 1;

	float2 shadow = SSAASample(uv);
	shadow *= ShadowPowers;

	float floorLight = light - light*max(shadow.x, shadow.y);
	light -= light*shadow.x;

	return float4(LightColor.rgb*light, LightColor.a * floorLight);
}

float4 PixelShaderFunctionLightBleed(VertexShaderOutput input) : COLOR0{
	float2 uv = input.p;
	if (OutsideRoomCheck(uv)) discard;
	float light = 1;

	float2 shadow = float2(SSAASample(uv).r * 5 / 4, 0);

	light -= light*shadow.x;
	float floorLight = light - light*shadow.y;

	return float4(LightColor.rgb*light, LightColor.a * floorLight);
}

//direction functions, for light direction floating point targets.

float4 DirectionFunction(VertexShaderOutput input) : COLOR0
{
	float2 uv = input.p;
	if (OutsideRoomCheck(uv)) discard;

	float3 direction = float3(uv.x, 0, uv.y) - float3(LightPosition.x, LightHeight, LightPosition.y);

	float light = clamp(1.0 - distance(uv, LightPosition) / LightSize, 0.0, 1.0);
	float shadow = tex2D(shadowSampler, uv).r;
	return DirectionFinal(direction, light, shadow);
}

float4 DirectionFunctionOutdoors(VertexShaderOutput input) : COLOR0{
	float2 uv = input.p;
	if (OutsideRoomCheck(uv)) discard;
	float light = 1;

	float shadow = tex2D(shadowSampler, uv).r;
	shadow *= ShadowPowers.x;

	light -= light*shadow;
	// RGBA: LightIntensity, OutdoorsIntensity, LightIntensityShad, OutdoorsIntensityShad
	// output different under diff conditions
	float4 result = DirectionOutdoors(LightDirection, light);
	//result.xyz *= ShadowPowers.x;
	return result;
}


float4 DirectionFunctionOutdoorsSSAA(VertexShaderOutput input) : COLOR0{
	float2 uv = input.p;
	if (OutsideRoomCheck(uv)) discard;
	float light = 1;

	float shadow = SSAASample(uv).r;
	shadow *= ShadowPowers.x;

	light -= light*shadow;

	float4 result = DirectionOutdoors(LightDirection, light);
	//result.xyz *= ShadowPowers.x;
	return result;
}

float4 DirectionFunctionLightBleed(VertexShaderOutput input) : COLOR0{
	float2 uv = input.p;
	if (OutsideRoomCheck(uv)) discard;
	float light = 1;

	float shadow = SSAASample(uv).r * 5 / 4;

	light -= light*shadow;

	float4 result = DirectionOutdoors(LightDirection, light);
	return result;
}

float4 PixelShaderFunctionMask(VertexShaderOutput input) : COLOR0{
	float2 uv = input.p;
	if (OutsideRoomCheck(uv)) discard;
	return LightColor; //multiply light color onto the room
}

float4 DirectionFunctionMask(VertexShaderOutput input) : COLOR0{
	float2 uv = input.p;
	if (OutsideRoomCheck(uv)) discard;
	return float4(LightColor.w, LightColor.w, LightColor.w, LightColor.w); //multiply light color onto the room
}

technique Draw2D
{
    pass MainPass
    {
        AlphaBlendEnable = TRUE; DestBlend = ONE; SrcBlend = ONE; BlendOp = Add; ZEnable = FALSE;

#if SM4
        VertexShader = compile vs_4_0_level_9_1 VertexShaderFunction();
        PixelShader = compile ps_4_0_level_9_3 PixelShaderFunction();
#else
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
#endif;

    }

	pass OutsidePass
	{
		AlphaBlendEnable = TRUE; DestBlend = ONE; SrcBlend = ONE; BlendOp = Add; ZEnable = FALSE;

#if SM4
		VertexShader = compile vs_4_0_level_9_1 VertexShaderFunction();
		PixelShader = compile ps_4_0_level_9_1 PixelShaderFunctionOutdoors();
#else
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunctionOutdoors();
#endif;

	}

	pass ClearPass
	{

#if SM4
		VertexShader = compile vs_4_0_level_9_1 VertexShaderFunction();
		PixelShader = compile ps_4_0_level_9_1 PixelShaderFunctionMask();
#else
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunctionMask();
#endif;

	}

	pass BleedPass
	{
		AlphaBlendEnable = TRUE; DestBlend = ONE; SrcBlend = ONE; BlendOp = Add; ZEnable = FALSE;

#if SM4
		VertexShader = compile vs_4_0_level_9_1 VertexShaderFunction();
		PixelShader = compile ps_4_0_level_9_1 PixelShaderFunctionLightBleed();
#else
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunctionLightBleed();
#endif;

	}

	pass SSAAPass
	{
		AlphaBlendEnable = TRUE; DestBlend = ONE; SrcBlend = ONE; BlendOp = Add; ZEnable = FALSE;

#if SM4
		VertexShader = compile vs_4_0_level_9_1 VertexShaderFunction();
		PixelShader = compile ps_4_0_level_9_1 PixelShaderFunctionOutdoorsSSAA();
#else
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunctionOutdoorsSSAA();
#endif;

	}

	pass MainPassBlur
	{
		AlphaBlendEnable = TRUE; DestBlend = ONE; SrcBlend = ONE; BlendOp = Add; ZEnable = FALSE;

#if SM4
		VertexShader = compile vs_4_0_level_9_1 VertexShaderFunction();
		PixelShader = compile ps_4_0_level_9_3 PixelShaderFunctionBlur();
#else
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunctionBlur();
#endif;

	}
}

technique DrawDirection
{
	pass MainPass
	{
		AlphaBlendEnable = TRUE; DestBlend = ONE; SrcBlend = ONE; BlendOp = Add; ZEnable = FALSE;

#if SM4
		VertexShader = compile vs_4_0_level_9_1 VertexShaderFunction();
		PixelShader = compile ps_4_0_level_9_3 DirectionFunction();
#else
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 DirectionFunction();
#endif;

	}

	pass OutsidePass
	{
		AlphaBlendEnable = TRUE; DestBlend = ONE; SrcBlend = ONE; BlendOp = Add; ZEnable = FALSE;

#if SM4
		VertexShader = compile vs_4_0_level_9_1 VertexShaderFunction();
		PixelShader = compile ps_4_0_level_9_1 DirectionFunctionOutdoors();
#else
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 DirectionFunctionOutdoors();
#endif;

	}

	pass ClearPass
	{

#if SM4
		VertexShader = compile vs_4_0_level_9_1 VertexShaderFunction();
		PixelShader = compile ps_4_0_level_9_1 DirectionFunctionMask();
#else
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 DirectionFunctionMask();
#endif;

	}

	pass BleedPass
	{
		AlphaBlendEnable = TRUE; DestBlend = ONE; SrcBlend = ONE; BlendOp = Add; ZEnable = FALSE;

#if SM4
		VertexShader = compile vs_4_0_level_9_1 VertexShaderFunction();
		PixelShader = compile ps_4_0_level_9_1 DirectionFunctionLightBleed();
#else
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 DirectionFunctionLightBleed();
#endif;

	}

	pass SSAAPass
	{
		AlphaBlendEnable = TRUE; DestBlend = ONE; SrcBlend = ONE; BlendOp = Add; ZEnable = FALSE;

#if SM4
		VertexShader = compile vs_4_0_level_9_1 VertexShaderFunction();
		PixelShader = compile ps_4_0_level_9_1 DirectionFunctionOutdoorsSSAA();
#else
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 DirectionFunctionOutdoorsSSAA();
#endif;

	}

	pass MainPassBlur
	{
		AlphaBlendEnable = TRUE; DestBlend = ONE; SrcBlend = ONE; BlendOp = Add; ZEnable = FALSE;

#if SM4
		VertexShader = compile vs_4_0_level_9_1 VertexShaderFunction();
		PixelShader = compile ps_4_0_level_9_1 DirectionFunction();
#else
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 DirectionFunction();
#endif;

	}
}