#include "LightingCommon.fx"

float4x4 World;
float4x4 View;
float4x4 Projection;

float ObjectID;
float4 AmbientLight;
float4x4 SkelBindings[50];
bool SoftwareDepth;
bool depthOutMode;

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

struct VitaVertexIn
{
    float4 position : SV_Position0;
	float2 texCoord : TEXCOORD0;
	float3 bvPosition : TEXCOORD1;
	float3 params : TEXCOORD2;
    float3 normal : NORMAL0;
};

struct FSOMVertexIn
{
	float4 position : SV_Position0;
	float2 texCoord : TEXCOORD0;
	float3 normal : TEXCOORD1;
};

struct VitaVertexOut
{
    float4 position : SV_Position0;
	float2 texCoord : TEXCOORD0;
    float4 screenPos : TEXCOORD1;
	float3 normal : TEXCOORD2;
	float4 modelPos : TEXCOORD3;
};

//head object parameters
float HOToonSpecThresh;
float3 HOToonSpecColor;
float3 HOCameraPosition;

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


VitaVertexOut vsHeadObject(FSOMVertexIn v) {
	VitaVertexOut result;
	//float4 position = mul(v.position, HOWorld) + float4(mul(float4(0, 0, 0, 1), SkelBindings[int(HOBone)]).xyz, 0);
	result.texCoord = v.texCoord;

	float4 wPos = mul(v.position, World);
	float4 finalPos = mul(wPos, mul(View, Projection));
	result.position = finalPos;
	result.modelPos = wPos;
	result.normal = mul(v.normal, (float3x3)(World));
	result.screenPos = float4(finalPos.xy*float2(0.5, -0.5) + float2(0.5, 0.5), finalPos.zw);

	return result;
}

float4 aaTex(VitaVertexOut v) {
#if SIMPLE
	return tex2D(TexSampler, v.texCoord);
#else
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
#endif
}


float4 psVitaboy(VitaVertexOut v) : COLOR0
{
    float depth = v.screenPos.z / v.screenPos.w;
    if (depthOutMode == true) {
        return packObjID(depth);
    }
    else {
#if SIMPLE
        //SOFTWARE DEPTH
        if (SoftwareDepth == true && depthOutMode == false && unpackDepth(tex2D(depthMapSampler, v.screenPos.xy)) < depth) discard;
#endif
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
#if SIMPLE
		//SOFTWARE DEPTH
		if (SoftwareDepth == true && depthOutMode == false && unpackDepth(tex2D(depthMapSampler, v.screenPos.xy)) < depth) discard;
#endif
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
#if SIMPLE
		//SOFTWARE DEPTH
		if (SoftwareDepth == true && depthOutMode == false && unpackDepth(tex2D(depthMapSampler, v.screenPos.xy)) < depth) discard;
#endif
		float4 color = gammaMul(aaTex(v), lightProcess(v.modelPos) * AmbientLight);
		color.rgb *= pow((dot(normalize(v.normal), float3(0, 1, 0)) + 1) / 2, 0.5)*0.5 + 0.5f;
		return color;
	}
}

float4 psVitaboyDir(VitaVertexOut v) : COLOR0
{
	float depth = v.screenPos.z / v.screenPos.w;
	if (depthOutMode == true) {
		return packObjID(depth);
	}
	else {
	#if SIMPLE
		//SOFTWARE DEPTH
		if (SoftwareDepth == true && depthOutMode == false && unpackDepth(tex2D(depthMapSampler, v.screenPos.xy)) < depth) discard;
	#endif
		float4 color = gammaMul(aaTex(v), lightProcessDirection(v.modelPos, normalize(v.normal)) * AmbientLight);
		return color;
	}
}

float4 psObjID(VitaVertexOut v) : COLOR0
{
    return packObjID(ObjectID);
}

float4 psHeadObject(VitaVertexOut v) : COLOR0
{
	float depth = v.screenPos.z / v.screenPos.w;
	if (depthOutMode == true) {
		return packObjID(depth);
	}
	else {
	#if SIMPLE
		//SOFTWARE DEPTH
		if (SoftwareDepth == true && depthOutMode == false && unpackDepth(tex2D(depthMapSampler, v.screenPos.xy)) < depth) discard;
	#endif
		float4 color = tex2D(TexSampler, v.texCoord)*AmbientLight;
		//specular component

		float4 inPosition = v.modelPos;
		float3 normal = normalize(v.normal);
		float level = Level;

		float2 orig = inPosition.x;
		inPosition.xyz *= WorldToLightFactor;
		inPosition.xz += LightOffset;

		inPosition.xz += 1 / MapLayout * floor(float2(level % MapLayout.x, level / MapLayout.x));
		float4 direction = tex2D(advDirectionSampler, inPosition.xz);
		float3 light = normalize(direction.xyz);

		float3 r = normalize(2 * dot(light, normal) * normal - light);
		float3 ve = normalize(v.modelPos.xyz - HOCameraPosition);

		float dotProduct = dot(r, ve);
		color.rgb += (saturate((dotProduct-HOToonSpecThresh)*50) + dotProduct * 0.15) * HOToonSpecColor;
		return color;
	}
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

	if (SoftwareDepth == true && depthOutMode == false && unpackDepth(tex2D(depthMapSampler, v.screenPos.xy)) < depth) discard;


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

technique AdvancedLightingDirection
{
	pass Pass1
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_1 vsVitaboy();
		PixelShader = compile ps_4_0_level_9_3 psVitaboyDir();
#else
		VertexShader = compile vs_3_0 vsVitaboy();
		PixelShader = compile ps_3_0 psVitaboyDir();
#endif;
	}
}


technique HeadObject
{
	pass Pass1
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_1 vsHeadObject();
		PixelShader = compile ps_4_0_level_9_3 psHeadObject();
#else
		VertexShader = compile vs_3_0 vsHeadObject();
		PixelShader = compile ps_3_0 psHeadObject();
#endif;
	}
}
