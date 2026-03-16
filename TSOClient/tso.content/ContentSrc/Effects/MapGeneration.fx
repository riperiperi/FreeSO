#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
    #define VS_SHADERMODEL3 vs_3_0
    #define PS_SHADERMODEL3 ps_3_0
    #define VS_SHADERMODEL4 vs_4_0
    #define PS_SHADERMODEL4 ps_4_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1

    #define VS_SHADERMODEL3 vs_4_0_level_9_1
    #define PS_SHADERMODEL3 ps_4_0_level_9_3
    #define VS_SHADERMODEL4 vs_5_0
    #define PS_SHADERMODEL4 ps_5_0
#endif

texture BaseTexture;
sampler TextureSampler : register(s0) = sampler_state {
	texture = <BaseTexture>;
	AddressU = CLAMP; AddressV = CLAMP; AddressW = CLAMP;
	MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};

// SpriteBatch expects that default vertex transform parameter will have name 'MatrixTransform'
float4x4 MatrixTransform;

struct VertexIn {
  float4 position : SV_Position0;
  float2 texCoord : TEXCOORD0;
};

struct VertexOut {
  float4 position : SV_Position;
  float2 texCoord : TEXCOORD0;
};

VertexOut VSMain(VertexIn v)
{
	VertexOut result;
	result.position = v.position;
	result.texCoord = v.texCoord;
	result.texCoord.y = 1 - v.texCoord.y;
	return result;
}

float2 ImageSize;
int StepSize;

int intMod(int value, int mod) {
	return value - ((value / mod) * mod);
}

float4 encodeUV(float2 uv) {
	int2 coord = int2(uv * ImageSize);

	return float4(
		float(intMod(coord.x, 256)) / 255.0,
		floor(coord.x / 256) / 255.0,
		float(intMod(coord.y, 256)) / 255.0,
		floor(coord.y / 256) / 255.0
	);
}

bool equal(float4 left, float4 right) {
	return left.x == right.x && left.y == right.y && left.z == right.z && left.w == right.w;
}

float2 decodeUV(float4 color) {
	if (equal(color.rgba, float4(1.0, 1.0, 1.0, 1.0))) {
		return float2(-1.0, -1.0);
	}

	int x = int(color.x * 255.0 + 0.5) + (int(color.y * 255.0 + 0.5) * 256);
	int y = int(color.z * 255.0 + 0.5) + (int(color.w * 255.0 + 0.5) * 256);

	float2 invSize = 1.0 / ImageSize;

	return float2(float(x) + 0.5, float(y) + 0.5) * invSize;
}

float4 jumpFloodInit(VertexOut v) : COLOR0
{
	float2 size = ImageSize;
	float2 invSize = 1.0 / size;

	float centralA = tex2D(TextureSampler, v.texCoord).a;

	if (centralA != 1.0) {
		// Alpha is 255 for city terrain type, so spread anything that isn't that.
		// Spread this pixel in the jump flood
		return encodeUV(v.texCoord);
	} else {
		return float4(1.0, 1.0, 1.0, 1.0);
	}
}

float4 jumpFloodStep(VertexOut v) : COLOR0
{
	float2 size = ImageSize;
	float2 invSize = 1.0 / size;

	float bestDist = 99999999;
	float4 bestUVe = float4(1.0, 1.0, 1.0, 1.0);

	for (int i = 0; i < 9; i++) {
		int realI = i;
		int x = intMod(realI, 3);
		int y = realI / 3;

		float2 newUV = v.texCoord + float2(float((x - 1) * StepSize), float((y - 1) * StepSize)) * invSize;
		float4 edgeUVe = tex2D(TextureSampler, newUV);
		float2 edgeUV = decodeUV(edgeUVe);

		if (edgeUV.x != -1.0) {
			float2 delta = (v.texCoord - edgeUV) * size;
			float sqDist = dot(delta, delta);

	    if (sqDist < bestDist) {
				bestDist = sqDist;
				bestUVe = edgeUVe;
			}
		}
	}

	return bestUVe;
}

technique JumpFloodInit {
	pass All
	{
		VertexShader = compile VS_SHADERMODEL3 VSMain();
		PixelShader = compile PS_SHADERMODEL3 jumpFloodInit();
	}
}

technique JumpFloodStep {
	pass All
	{
		VertexShader = compile VS_SHADERMODEL3 VSMain();
		PixelShader = compile PS_SHADERMODEL3 jumpFloodStep();
	}
}

/*
technique JumpFloodFinal {
	pass All
	{
		PixelShader = compile PS_SHADERMODEL3 jumpFloodFinal();
	}
}
*/

int EdgeValue;

bool hasEdgeValue(float2 texCoord) {
	float valueF = tex2D(TextureSampler, texCoord).a;
	int value = int(round(valueF * 255.0));

	return value == EdgeValue || value == 255;
}

float4 cityEdgeDetect(VertexOut v) : COLOR0
{
	// There's an edge if the target value is found at this pixel, but not found on all 4 adjacent ones.
	float2 texCoord = v.texCoord;
	if (hasEdgeValue(texCoord)) {
	  float2 size = ImageSize;
	  float2 invSize = 1.0 / size;
		if (!hasEdgeValue(texCoord + float2(invSize.x, 0.0)) || !hasEdgeValue(texCoord - float2(invSize.x, 0.0)) || !hasEdgeValue(texCoord + float2(0.0, invSize.y)) || !hasEdgeValue(texCoord - float2(0.0, invSize.y))) {
			float value = tex2D(TextureSampler, texCoord).a;
			return float4(value, value, value, value);
		}
	}

  return float4(0.0, 0.0, 0.0, 1.0); // 255 is "null"
}

technique CityEdgeDetect {
	pass All
	{
		VertexShader = compile VS_SHADERMODEL3 VSMain();
		PixelShader = compile PS_SHADERMODEL3 cityEdgeDetect();
	}
}

float SdfExpand;
float SdfFade;
float GradientScale;
float GradientBase;


texture TerrainType;

sampler TerrainSampler : register(s1) = sampler_state {
	texture = <TerrainType>;
	AddressU = CLAMP; AddressV = CLAMP; AddressW = CLAMP;
	MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};

texture DistToColor;

sampler DistToColorSampler : register(s2) = sampler_state {
	texture = <DistToColor>;
	AddressU = CLAMP; AddressV = CLAMP; AddressW = CLAMP;
	MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};

float getSignedDistance(float2 texCoord) {
	float2 size = ImageSize;
	float value = tex2D(TerrainSampler, texCoord).a;
	float4 closestUVe = tex2D(TextureSampler, texCoord);
	float2 closestUV = decodeUV(closestUVe);
	
	float2 delta = (texCoord - closestUV) * size;
	float dist = sqrt(dot(delta, delta));

  // Within the value the distance is negative, outside it's positive.
	return (int(round(value * 255)) == EdgeValue) ? -dist : dist;
}

float4 jumpDistFill(VertexOut v) : COLOR0
{
	float dist = getSignedDistance(v.texCoord);

  // Gradient texture is applied within the volume (negative dist -> x)
	float4 grad = tex2D(DistToColorSampler, float2(-dist / GradientScale + GradientBase, 0.5));

  float a = 1 - smoothstep(SdfExpand, SdfExpand + SdfFade, dist);

	return grad * a;
}

technique JumpDistFill {
	pass All
	{
		VertexShader = compile VS_SHADERMODEL3 VSMain();
		PixelShader = compile PS_SHADERMODEL3 jumpDistFill();
	}
}

float TerrainScale;
float3 SunDir;

float lightingTerm(float3 normal, float3 lightDir) {
	return (dot(normal, lightDir) + 1) / 2;
}

float3 posAt(float2 texCoord) {
	return float3(texCoord.x, texCoord.y, tex2D(TextureSampler, texCoord).a * TerrainScale);
}

float SpecularPower;
float SpecularIntensity;

bool isOOB(float2 texCoord) {
	float value = tex2D(TerrainSampler, texCoord).a;

	return value == 1;
}

float3 calcNormal(float2 texCoord) {
	float2 invSize = 1.0 / ImageSize;

	if (isOOB(texCoord)) {
		return float3(0, 0, 1);
	}

  // Calculate normal
	float3 posTL = posAt(texCoord);
	float3 posTR = posAt(texCoord + float2(invSize.x, 0.0));
	float3 posBL = posAt(texCoord + float2(0.0, -invSize.y));
	float3 posBR = posAt(texCoord + float2(invSize.x, -invSize.y));

	float3 normal1 = normalize(cross(posTR - posTL, posBL - posTL));
	float3 normal2 = normalize(cross(posBR - posBL, posBR - posTR));

	float3 normal = -normalize((normal1 + normal2) / 2);

	return normal;
}

float3 treatNormal(float4 col) {
	return normalize(col.xyz * 2 - float3(1, 1, 1));
}

float4 terrainLighting(VertexOut v) : COLOR0
{
	float2 texCoord = v.texCoord;
	float3 normal = treatNormal(tex2D(DistToColorSampler, texCoord));

	if (isOOB(texCoord)) {
		return float4(1, 1, 1, 1);
	}

  // Diffuse term
  float4 vertexColor = tex2D(TextureSampler, texCoord);
	float refDiffuse = lightingTerm(float3(0, 0, 1), SunDir);
	float diffuse = lightingTerm(normal, SunDir);

	return float4(vertexColor.rgb * (diffuse / refDiffuse), 1);
}

technique TerrainLighting {
	pass All
	{
		VertexShader = compile VS_SHADERMODEL3 VSMain();
		PixelShader = compile PS_SHADERMODEL3 terrainLighting();
	}
}

float4 terrainSpecular(VertexOut v) : COLOR0
{
	float2 texCoord = v.texCoord;
	float3 normal = treatNormal(tex2D(TextureSampler, texCoord));

  // Specular term (reflects white)
	float3 reflected = normalize(2 * dot(SunDir, normal) * normal - SunDir);
	float3 camDir = float3(0, 0, 1);
  float specularFactor = pow(max(0, dot(reflected, camDir)), SpecularPower) * SpecularIntensity;
	float4 specularColor = specularFactor * float4(1.0, 1.0, 1.0, 1.0);

	return specularColor;
}

technique TerrainSpecular {
	pass All
	{
		VertexShader = compile VS_SHADERMODEL3 VSMain();
		PixelShader = compile PS_SHADERMODEL3 terrainSpecular();
	}
}

float4 terrainNormal(VertexOut v) : COLOR0
{
	float3 normal = calcNormal(v.texCoord);

	return float4((normal + float3(1, 1, 1)) / 2, 1.0);
}

technique TerrainNormal {
	pass All
	{
		VertexShader = compile VS_SHADERMODEL3 VSMain();
		PixelShader = compile PS_SHADERMODEL3 terrainNormal();
	}
}

float4 Color;

float4 forestOverlay(VertexOut v) : COLOR0
{
	float2 texCoord = v.texCoord;

  float4 color = Color;
  float a = tex2D(TextureSampler, texCoord).a * 0.75;

	float value = tex2D(TerrainSampler, texCoord).a;
	int type = int(round(value * 255));

	if (!(type == 0 || type == 2)) {
		// Needs to be grass or rock.
		a = 0;
	}

	float4 result = color * a;
	result.a = min(result.a, 0.75);

	return result;
}

technique ForestOverlay {
	pass All
	{
		VertexShader = compile VS_SHADERMODEL3 VSMain();
		PixelShader = compile PS_SHADERMODEL3 forestOverlay();
	}
}

float2 GaussianStep;
int GaussianSize; // Up to 21
float GaussianWeights[21];

float4 gauss(VertexOut v) : COLOR0
{
  // Look up the texture color.
	float2 texCoord = v.texCoord;

	float4 fragC = tex2D(TextureSampler, texCoord) * GaussianWeights[0];

	for (int i = 1; i < GaussianSize; i++) {
		fragC += tex2D(TextureSampler, texCoord+GaussianStep*i) * GaussianWeights[i];
		fragC += tex2D(TextureSampler, texCoord+GaussianStep*(-i)) * GaussianWeights[i];
	}
    
  return fragC;
}

technique Gaussian
{
    pass OneDir
    {
				VertexShader = compile VS_SHADERMODEL3 VSMain();
        PixelShader = compile PS_SHADERMODEL3 gauss();
    }
}