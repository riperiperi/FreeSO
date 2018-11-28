#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
    #define VS_SHADERMODEL3 vs_3_0
    #define PS_SHADERMODEL3 ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1

    #define VS_SHADERMODEL3 vs_4_0_level_9_1
    #define PS_SHADERMODEL3 ps_4_0_level_9_3
#endif

sampler TextureSampler : register(s0);

// SpriteBatch expects that default vertex transform parameter will have name 'MatrixTransform'
float4x4 MatrixTransform;

void VSMain(
    inout float4 color    : COLOR0,
    inout float2 texCoord : TEXCOORD0,
    inout float4 position : SV_Position)
{
    position = mul(position, MatrixTransform);
}

float2 GaussianStep;

static const float GaussianWeights[5] = {0.2270270270, 0.1945945946, 0.1216216216, 0.0540540541, 0.0162162162};

float4 gauss(float4 position : SV_Position, float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    // Look up the texture color.

	float4 fragC = tex2D(TextureSampler, texCoord) * GaussianWeights[0];
	for (int i = 1; i < 5; i++) {
		fragC += tex2D(TextureSampler, texCoord+GaussianStep*i) * GaussianWeights[i];
		fragC += tex2D(TextureSampler, texCoord+GaussianStep*(-i)) * GaussianWeights[i];
	}
    
    return fragC;
}


technique Gaussian
{
    pass OneDir
    {
		//VertexShader = compile VS_SHADERMODEL VSMain();
        PixelShader = compile PS_SHADERMODEL gauss();
    }
}

float AllowedValue;
float4 noise(float4 position : SV_Position, float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
	float value = round(tex2D(TextureSampler, texCoord).r * 255);
    if (value != AllowedValue) discard;
    
    return color;
}

technique NoiseAccum
{
    pass OneDir
    {
		//VertexShader = compile VS_SHADERMODEL VSMain();
        PixelShader = compile PS_SHADERMODEL noise();
    }
}

/*
static const float GaussianKernel7[49] = {
	0,  0,   0,   5,   0,  0, 0,
	0,  5,  18,  32,  18,  5, 0,
	0, 18,  64, 100,  64, 18, 0,
	5, 32, 100, 100, 100, 32, 5,
	0, 18,  64, 100,  64, 18, 0,
	0,  5,  18,  32,  18,  5, 0,
	0,  0,   0,   5,   0,  0, 0,
};*/

static const float GaussianKernel7[37] = {
	0.001446, 0.002291, 0.001446,
	0.003676, 0.014662, 0.023226, 0.014662, 0.003676,
	0.001446, 0.014662, 0.058488, 0.092651, 0.058488, 0.014662, 0.001446,
	0.002291, 0.023226, 0.092651, 0.146768, 0.092651, 0.023226, 0.002291,
	0.001446, 0.014662, 0.058488, 0.092651, 0.058488, 0.014662, 0.001446,
	0.003676, 0.014662, 0.023226, 0.014662, 0.003676,
	0.001446, 0.002291, 0.001446,
};

static const float2 Offsets[37] = {
	float2(-1, -3), float2(0, -3), float2(1, -3),
	float2(-2, -2), float2(-1, -2), float2(0, -2), float2(1, -2), float2(2, -2),
	float2(-3, -1), float2(-2, -1), float2(-1, -1), float2(0, -1), float2(1, -1), float2(2, -1), float2(3, -1),
	float2(-3, 0), float2(-2, 0), float2(-1, 0), float2(0, 0), float2(1, 0), float2(2, 0), float2(3, 0),
	float2(-3, 1), float2(-2, 1), float2(-1, 1), float2(0, 1), float2(1, 1), float2(2, 1), float2(3, 1),
	float2(-2, 2), float2(-1, 2), float2(0, 2), float2(1, 2), float2(2, 2),
	float2(-1, 3), float2(0, 3), float2(1, 3),
};

float blurAmount;
float heightMultiplier;
texture noiseTexture : Diffuse;

sampler NoiseSampler : register(s1) = sampler_state {
	texture = <noiseTexture>;
	AddressU = WRAP; AddressV = WRAP; AddressW = WRAP;
	MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};


float4 OutdoorsPCFBlit(float4 position : SV_Position, float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
	float2 noiseOff = ((tex2D(NoiseSampler, uv*8, 0, 0).xy) - 0.5);
	//find the average shadow strength in the working region
	//this determines how much we should blur the result by
	//(unshadowed pixels are ignored in this calculation)
	float numSamples = 0;
	float sumSamples = 0;
	{
		for (int i = 0; i < 36; i++) {
			float shad = tex2D(TextureSampler, uv + Offsets[i]*blurAmount, 0, 0).x;
			if (shad > 0) {
				numSamples++;
				sumSamples += shad;
			}
		}
	}

	//the more light samples there are, the blurrier the result should be.
	//(lighter samples are further off the ground)
	float blurStrength = min(1, max(0, (1 - sumSamples / numSamples) * heightMultiplier - 0.07) / 0.93);

	//bool highDetail = (blurStrength > 0.5);
	blurStrength *= blurAmount;
	float sum = 0;
	float4 result = tex2D(TextureSampler, uv);
	if (numSamples == 0 || numSamples == 37) {
		result.r = ceil(result.r);
		return result;
	}

	float2 bluruv = uv + noiseOff * blurStrength;

	//even samples: always used.
	{
		for (int i = 1; i < 36; i++) {
#if OPENGL
			float shad = tex2D(TextureSampler, bluruv + Offsets[i]*blurStrength, 0, 0).x;
#else
			float shad = tex2D(TextureSampler, uv + Offsets[i] * blurStrength, 0, 0).x;
#endif
			sum += GaussianKernel7[i] * ceil(shad);
		}
	}

	/*
	//odd samples: used only for high detail
	if (highDetail == true) {
		for (int i = 1; i < 37; i += 2) {
			float shad = tex2D(TextureSampler, bluruv + Offsets[i] * blurStrength, 0, 0).x;
			sum += GaussianKernel7[i] * ceil(shad);
		}
	}

	sum /= ((highDetail == true) ? 1 : 0.500146);
	*/
	return float4(sum, result.gba);
}

technique ShadowBlurBlit
{
	pass OneDir
	{
		PixelShader = compile PS_SHADERMODEL3 OutdoorsPCFBlit();
	}
}
