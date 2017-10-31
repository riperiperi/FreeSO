float4x4 World;
float4x4 View;
float4x4 Projection;

float ObjectID;
float4 AmbientLight;
float4x4 SkelBindings[50];
bool SoftwareDepth;
bool depthOutMode;

//LIGHTING
float4 OutsideLight;
float4 OutsideDark;
float4 MaxLight;
float2 MinAvg;
float3 WorldToLightFactor;
float2 LightOffset;
float2 MapLayout;
float Level;
//END LIGHTING

Texture MeshTex;
sampler TexSampler = sampler_state {
	texture = <MeshTex> ; 
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
};

texture depthMap : Diffuse;

sampler depthMapSampler = sampler_state {
    texture = <depthMap>;
    AddressU = CLAMP; AddressV = CLAMP; AddressW = CLAMP;
    MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};

texture advancedLight : Diffuse;
sampler advLightSampler = sampler_state {
	texture = <advancedLight>;
	AddressU = WRAP; AddressV = WRAP; AddressW = WRAP;
	MIPFILTER = LINEAR; MINFILTER = LINEAR; MAGFILTER = LINEAR;
};

struct VitaVertexIn
{
    float4 position : SV_Position0;
	float2 texCoord : TEXCOORD0;
	float3 bvPosition : TEXCOORD1;
	float3 params : TEXCOORD2;
    float3 normal : NORMAL0;
};

struct VitaVertexOut
{
    float4 position : SV_Position0;
	float2 texCoord : TEXCOORD0;
    float4 screenPos : TEXCOORD1;
	float3 normal : TEXCOORD2;
	float4 modelPos : TEXCOORD3;
};

float4 packObjID(float d) {
    float4 enc = float4(1.0, 255.0, 65025.0, 0.0) * d;
    enc = frac(enc);
    enc -= enc.yzww * float4(1.0 / 255.0, 1.0 / 255.0, 1.0 / 255.0, 0.0);
    enc.a = 1;

    return enc; //float4(byteFloor(d%1.0), byteFloor((d*256.0) % 1.0), byteFloor((d*65536.0) % 1.0), 1); //most sig in r, least in b
}

float unpackDepth(float4 d) {
    return dot(d, float4(1.0, 1 / 255.0, 1 / 65025.0, 0)); //d.r + (d.g / 256.0) + (d.b / 65536.0);
}

float4 lightColor(float4 intensities) {
	return float4(intensities.rgb, 1);
}

float4 lightColorFloor(float4 intensities) {
	// RGBA: LightIntensity, OutdoorsIntensity, LightIntensityShad, OutdoorsIntensityShad

	float avg = (intensities.r + intensities.g + intensities.b) / 3;
	//floor shadow is how much less than average the alpha component is

	float fshad = intensities.a / avg;

	return lerp(OutsideDark, float4(intensities.rgb, 1), (fshad - MinAvg.x) * MinAvg.y);
}

float4 lightColorI(float4 intensities, float i) {
	// RGBA: LightIntensity, OutdoorsIntensity, LightIntensityShad, OutdoorsIntensityShad

	float avg = (intensities.r + intensities.g + intensities.b) / 3;
	//floor shadow is how much less than average the alpha component is

	float fshad = intensities.a / avg;
	fshad = lerp(fshad, 1, i);

	return lerp(OutsideDark, float4(intensities.rgb, 1), (fshad - MinAvg.x) * MinAvg.y);
}

float4 lightProcess(float4 inPosition) {
	float2 orig = inPosition.x;
	inPosition.xyz *= WorldToLightFactor;
	inPosition.xz += LightOffset;

	inPosition.xz += 1 / MapLayout * floor(float2(Level % MapLayout.x, Level / MapLayout.x));

	float4 lTex = tex2D(advLightSampler, inPosition.xz);
	return lightColor(lTex);
}

VitaVertexOut vsVitaboy(VitaVertexIn v) {
    VitaVertexOut result;
    float4 position = (1.0-v.params.z) * mul(v.position, SkelBindings[int(v.params.x)]) + v.params.z * mul(float4(v.bvPosition, 1.0), SkelBindings[int(v.params.y)]);
    result.texCoord = v.texCoord;

	float3 normal = mul(v.normal, (float3x3)SkelBindings[int(v.params.x)]);

	float4 wPos = mul(position, World);
    float4 finalPos = mul(wPos, mul(View, Projection));
    result.position = finalPos;
	result.modelPos = wPos;
	result.normal = mul(normal, (float3x3)World);
    result.screenPos = float4(finalPos.xy*float2(0.5, -0.5) + float2(0.5, 0.5), finalPos.zw);

    return result;
}

float4 aaTex(VitaVertexOut v) {
	float2 texDDX = ddx(v.texCoord);
	float2 texDDY = ddy(v.texCoord);
	float4 color = float4(0, 0, 0, 0);

	for (int x = -1; x < 2; x++)
	{
		for (int y = -1; y < 2; y++)
		{
			float2 texCoord = v.texCoord + (texDDX * x / 3) + (texDDY * y / 3);
			color += tex2D(TexSampler, texCoord);
		}
	}
	color /= 9;
	return color;
}

float4 psVitaboy(VitaVertexOut v) : COLOR0
{
    float depth = v.screenPos.z / v.screenPos.w;
    if (depthOutMode == true) {
        return packObjID(depth);
    }
    else {
        //SOFTWARE DEPTH
        if (SoftwareDepth == true && depthOutMode == false && unpackDepth(tex2D(depthMapSampler, v.screenPos.xy)) < depth) discard;
		float4 color = aaTex(v)*AmbientLight; 
		color.rgb *= pow((dot(normalize(v.normal), float3(0, 1, 0)) + 1) / 2, 0.5)*0.5 + 0.5f;
        return color;
    }
}

float4 psVitaboyNoSSAA(VitaVertexOut v) : COLOR0
{
	float depth = v.screenPos.z / v.screenPos.w;
	if (depthOutMode == true) {
		return packObjID(depth);
	}
	else {
		//SOFTWARE DEPTH
		if (SoftwareDepth == true && depthOutMode == false && unpackDepth(tex2D(depthMapSampler, v.screenPos.xy)) < depth) discard;
		float4 color = tex2D(TexSampler, v.texCoord) * AmbientLight;
		color.rgb *= pow((dot(normalize(v.normal), float3(0, 1, 0)) + 1) / 2, 0.5)*0.5 + 0.5f;
		return color;
	}
}

float4 psVitaboyAdv(VitaVertexOut v) : COLOR0
{
	float depth = v.screenPos.z / v.screenPos.w;
	if (depthOutMode == true) {
		return packObjID(depth);
	}
	else {
		//SOFTWARE DEPTH
		if (SoftwareDepth == true && depthOutMode == false && unpackDepth(tex2D(depthMapSampler, v.screenPos.xy)) < depth) discard;
		float4 color = aaTex(v) * lightProcess(v.modelPos) * AmbientLight;
		color.rgb *= pow((dot(normalize(v.normal), float3(0, 1, 0)) + 1) / 2, 0.5)*0.5 + 0.5f;
		return color;
	}
}

float4 psObjID(VitaVertexOut v) : COLOR0
{
    return packObjID(ObjectID);
}

technique NoSSAA
{
    pass Pass1
    {
#if SM4
        VertexShader = compile vs_4_0_level_9_1 vsVitaboy();
        PixelShader = compile ps_4_0_level_9_1 psVitaboyNoSSAA();
#else
        VertexShader = compile vs_3_0 vsVitaboy();
        PixelShader = compile ps_3_0 psVitaboyNoSSAA();
#endif;
    }
}

technique ObjIDMode
{
    pass Pass1
    {
#if SM4
        VertexShader = compile vs_4_0_level_9_1 vsVitaboy();
        PixelShader = compile ps_4_0_level_9_1 psObjID();
#else
        VertexShader = compile vs_3_0 vsVitaboy();
        PixelShader = compile ps_3_0 psObjID();
#endif;
    }
}


technique AdvancedLighting
{
	pass Pass1
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_1 vsVitaboy();
		PixelShader = compile ps_4_0_level_9_3 psVitaboyAdv();
#else
		VertexShader = compile vs_3_0 vsVitaboy();
		PixelShader = compile ps_3_0 psVitaboyAdv();
#endif;
	}
}

technique SSAA
{
	pass Pass1
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_1 vsVitaboy();
		PixelShader = compile ps_4_0_level_9_3 psVitaboy();
#else
		VertexShader = compile vs_3_0 vsVitaboy();
		PixelShader = compile ps_3_0 psVitaboy();
#endif;
	}
}

//additional technique for basic shadowing

float FloorHeight;
float2 LightPosition;

struct ShadVertexIn
{
	float4 position : SV_Position0;
	float bone : TEXCOORD0;
};

struct ShadVertexOut
{
	float4 position : SV_Position0;
	float factor : TEXCOORD0;
	float4 ellipseVec : TEXCOORD1;
	float4 ellipseBasePos : TEXCOORD2;
	float4 screenPos : TEXCOORD3;
};

ShadVertexOut vsShadow(ShadVertexIn v) {
	ShadVertexOut result;
	float4 position = mul(float4(0,0,0,1), SkelBindings[int(v.bone)]);
	result.factor = max(1-abs(position.y-0.030)*0.75, 0);
	position.y = 0.01;

	float4 wPos = mul(position, World);
	float2 eCtr = wPos.xz;

	//calculate the ellipse dimensions
	float eSize = 0.45;
	float myHeight = 1.875;
	float lightHeight = 9;
	float2 elVec = (eCtr - LightPosition);
	float height = length(elVec) * myHeight / (lightHeight - myHeight);
	if (result.factor == 0) height = 0;
	elVec = normalize(elVec);
	float2 largeDim = (eSize + height) * elVec;
	float2 smallDim = float2(largeDim.y, -largeDim.x);
	smallDim = normalize(smallDim);
	smallDim *= eSize;
	//end ellipse calculations

	result.ellipseBasePos.xy = eCtr; //center of ellipse
	//wPos.y = FloorHeight;
	wPos += float4(v.position.xyz, 0)*(eSize + height*2);
	float4 finalPos = mul(wPos, mul(View, Projection));
	result.ellipseBasePos.zw = wPos.xz; //world position of fragment
	result.position = finalPos;

	result.ellipseVec.xy = smallDim;
	result.ellipseVec.zw = largeDim;

	result.screenPos = float4(finalPos.xy*float2(0.5, -0.5) + float2(0.5, 0.5), finalPos.zw);

	return result;
}

float EllipseMultiplier(float2 ellipseVec1, float2 ellipseVec2, float2 ellipsePos, float2 basePos) {
	//ellipse vec 2 is the long one ;)

	float2 relPos = basePos - ellipsePos;
	float smallLength = length(ellipseVec1);
	float sL2 = smallLength*smallLength;
	float largeLength = length(ellipseVec2);

	float smallDot = dot(relPos, ellipseVec1) / sL2;
	float largeDot = dot(relPos, ellipseVec2) / largeLength;

	if (largeDot < 0) {
		largeDot /= smallLength;
	}
	else {
		largeDot /= largeLength;
	}

	return 1 - clamp(length(float2(smallDot, largeDot)), 0, 1);
}


float4 psShadow(ShadVertexOut v) : COLOR0
{
	if (v.factor == 0) discard;
	float depth = v.screenPos.z / v.screenPos.w;

	float light = clamp(1.0 - distance(v.ellipseBasePos.zw, LightPosition) / 30, 0.0, 1.0);
	light = pow(light, 2);

	float shadowStrength = EllipseMultiplier(v.ellipseVec.xy, v.ellipseVec.zw, v.ellipseBasePos.xy, v.ellipseBasePos.zw)*0.4f*v.factor;
	if (shadowStrength == 0) discard;
	//if (depth > -100) shadowStrength = 1;
	return float4(0,0,0,shadowStrength*light);
}

technique ShadowTech
{
	pass Pass1
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_1 vsShadow();
		PixelShader = compile ps_4_0_level_9_3 psShadow();
#else
		VertexShader = compile vs_3_0 vsShadow();
		PixelShader = compile ps_3_0 psShadow();
#endif;
	}
}

