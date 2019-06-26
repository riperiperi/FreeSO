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

float3 RGBToHSV(float3 rgb)
{
	float4 k = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
	float4 p = lerp(float4(rgb.bg, k.wz), float4(rgb.gb, k.xy), step(rgb.b, rgb.g));
	float4 q = lerp(float4(p.xyw, rgb.r), float4(rgb.r, p.yzx), step(p.x, rgb.r));

	float d = q.x - min(q.w, q.y);
	float e = 1.0e-10;
	return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

float3 HSVToRGB(float3 hsv)
{
	float4 k = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
	float3 p = abs(frac(hsv.xxx + k.xyz) * 6.0 - k.www);

	return hsv.z * lerp(k.xxx, clamp(p - k.xxx, 0.0, 1.0), hsv.y);
}

float2 SVToSL(float2 sv) {
	float e = 1.0e-10;
	float l = sv.y - sv.y * sv.x / 2.0;
	return float2((sv.y - l) / (min(l, 1 - l) + e), l);
	/*
	float2 sl = float2(sv.x * sv.y, (2.0 - sv.x) * sv.y);
	sl.x /= (sl.y <= 1.0) ? sl.y : (2 - sl.y);
	sl.y /= 2.0;
	return sl;
	*/
}

float2 SLToSV(float2 sl) {
	float e = 1.0e-10;
	float v = sl.y + sl.x * min(sl.y, 1 - sl.y);
	return float2(2 - 2 * sl.y / (v + e), v);
	/*
	float2 sv = float2((sl.y <= 0.5) ? sl.y : 1 - sl.y, sl.x + sl.y);
	sv.x = 2 * sv.x / (sl.y + sv.x);
	return sv;
	*/
	
}

float Highlight;

float4 hsvMod(float4 position : SV_Position, float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
	float4 result = tex2D(TextureSampler, texCoord);
	result.xyz = RGBToHSV(result.rgb);
	result.yz = SVToSL(result.yz);
	result.x = (result.x + color.x) % 1;
	float3 mul = color.yzw;
	mul.y += Highlight;
	result.yzw *= mul;
	result.yz = SLToSV(result.yz);
	result.rgb = HSVToRGB(result.xyz) * result.a;
	return result;
}

technique HSVEffect
{
	pass Pass1
	{
		PixelShader = compile PS_SHADERMODEL hsvMod();
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

float2 blurAmount;
float2 heightMultiplier;
float2 hardenBias;
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
			float shad = tex2D(TextureSampler, uv + Offsets[i]*blurAmount.x, 0, 0).x;
			if (shad > 0) {
				numSamples++;
				sumSamples += shad;
			}
		}
	}

	//the more light samples there are, the blurrier the result should be.
	//(lighter samples are further off the ground)
	float blurStrength = min(1, max(0, (1 - sumSamples / numSamples) * heightMultiplier.x - hardenBias.x) / (1 - hardenBias.x));

	//bool highDetail = (blurStrength > 0.5);
	blurStrength *= blurAmount.x;
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

static const float Gaussian23[23] = { 0.000169, 0.000538, 0.001532, 0.003907, 0.008922, 0.018249, 0.033435, 0.054872, 0.080666, 0.106223, 0.125294, 0.132384, 0.125294, 0.106223, 0.080666, 0.054872, 0.033435, 0.018249, 0.008922, 0.003907, 0.001532, 0.000538, 0.000169 };

float4 OutdoorsPCFStage1(float4 position : SV_Position, float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
	//rg unfiltered shadows -> rg, ba (wall/object avg, wall/object count)

	//find the average shadow strength in the working region
	//this determines how much we should blur the result by
	//(unshadowed pixels are ignored in this calculation)
	float2 numSamples = 0;
	float2 sumSamples = 0;
	{
		for (int i = -11; i < 12; i++) {
#if SIMPLE
			float shad = tex2D(TextureSampler, uv + float2(blurAmount.x*i, 0)).x;
			float shad2 = tex2D(TextureSampler, uv + float2(blurAmount.y*i, 0)).y;
#else
			float shad = tex2D(TextureSampler, uv + float2(blurAmount.x*i, 0), 0, 0).x;
			float shad2 = tex2D(TextureSampler, uv + float2(blurAmount.y*i, 0), 0, 0).y;
#endif
			float effect = Gaussian23[i + 11] / 0.132384;
			if (shad > 0) {
				numSamples.x += effect;
				sumSamples.x += shad * effect;
			}
			if (shad2 > 0) {
				numSamples.y += effect;
				sumSamples.y += shad2 * effect;
			}
		}
	}

	numSamples = max(numSamples, float2(0.0001, 0.0001)); //prevent divide by 0

	return float4(sumSamples / numSamples, numSamples/23);
}

float4 OutdoorsPCFStage2(float4 position : SV_Position, float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
	//rg, ba (wall/object avg, wall/object count) -> ba (wall/object avg 2d)

	//find the average shadow strength in the working region
	//this determines how much we should blur the result by
	//(unshadowed pixels are ignored in this calculation)
	float2 numSamples = 0;
	float2 sumSamples = 0;
	{
		for (int i = -11; i < 12; i++) {
			float4 shad;
#if SIMPLE
			shad.xz = tex2D(TextureSampler, uv + float2(0, blurAmount.x*i)).xz;
			shad.yw = tex2D(TextureSampler, uv + float2(0, blurAmount.y*i)).yw;
#else
			shad.xz = tex2D(TextureSampler, uv + float2(0, blurAmount.x*i), 0, 0).xz;
			shad.yw = tex2D(TextureSampler, uv + float2(0, blurAmount.y*i), 0, 0).yw;
#endif
			shad.zw *= 23;
			numSamples += shad.zw;
			sumSamples += shad.xy * shad.zw;
		}
	}

	numSamples = max(numSamples, float2(0.0001, 0.0001)); //prevent divide by 0

	return float4(0, 0, sumSamples / numSamples);
}

float4 OutdoorsPCFStage3(float4 position : SV_Position, float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
	//rg, ba (unfiltered wall/object, wall/object avg 2d) -> rg, ba (wall/object 1d filtered, wall/object avg 2d)

	float4 info = tex2D(TextureSampler, uv);

	if (info.z == 0 && info.w == 0) return float4(0, 0, info.zw);
	if (info.z == 1 && info.w == 1) return float4(1, 1, info.zw);

	float2 blurStrength = min(1, max(0, (1 - info.zw) * heightMultiplier - hardenBias) / (1-hardenBias));

	//bool highDetail = (blurStrength > 0.5);
	blurStrength *= blurAmount;
	float2 sum = 0;

	float noiseOff = ((tex2D(NoiseSampler, uv * 8).x) - 0.5);
	float2 bluruvx = uv + float2(noiseOff * blurStrength.x, 0);
	float2 bluruvy = uv + float2(noiseOff * blurStrength.y, 0);
	/*
	float4 result = tex2D(TextureSampler, uv);
	if (numSamples == 0 || numSamples == 37) {
		result.r = ceil(result.r);
		return result;
	}*/
	{
		for (int i = -11; i < 12; i++) {
			float2 shad;
#if SIMPLE
			shad.x = tex2D(TextureSampler, bluruvx + float2(i*blurStrength.x, 0)).x;
			shad.y = tex2D(TextureSampler, bluruvy + float2(i*blurStrength.y, 0)).y;
#else
			shad.x = tex2D(TextureSampler, bluruvx + float2(i*blurStrength.x, 0), 0, 0).x;
			shad.y = tex2D(TextureSampler, bluruvy + float2(i*blurStrength.y, 0), 0, 0).y;
#endif
			sum += Gaussian23[i+11] * ceil(shad);
		}
	}

	return float4(sum, info.zw);
}

float4 OutdoorsPCFStage4(float4 position : SV_Position, float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
	//rg, ba (unfiltered wall/object, wall/object avg 2d) -> rg, ba (wall/object 1d filtered, wall/object avg 2d)

	float4 info = tex2D(TextureSampler, uv);

	if (info.z == 0 && info.w == 0) return float4(0, 0, info.zw);
	if (info.z == 1 && info.w == 1) return float4(1, 1, info.zw);

	float2 blurStrength = min(1, max(0, (1 - info.zw) * heightMultiplier - hardenBias) / (1 - hardenBias));

	//bool highDetail = (blurStrength > 0.5);
	blurStrength *= blurAmount;
	float2 sum = 0;
	/*
	float4 result = tex2D(TextureSampler, uv);
	if (numSamples == 0 || numSamples == 37) {
	result.r = ceil(result.r);
	return result;
	}*/
	float noiseOff = ((tex2D(NoiseSampler, uv * 8).y) - 0.5);
	float2 bluruvx = uv + float2(0, noiseOff * blurStrength.x);
	float2 bluruvy = uv + float2(0, noiseOff * blurStrength.y);

	{
		for (int i = -11; i < 12; i++) {
			float2 shad;
#if SIMPLE
			shad.x = tex2D(TextureSampler, bluruvx + float2(0, i*blurStrength.x)).x;
			shad.y = tex2D(TextureSampler, bluruvy + float2(0, i*blurStrength.y)).y;
#else
			shad.x = tex2D(TextureSampler, bluruvx + float2(0, i*blurStrength.x), 0, 0).x;
			shad.y = tex2D(TextureSampler, bluruvy + float2(0, i*blurStrength.y), 0, 0).y;
#endif
			sum += Gaussian23[i + 11] * (shad);
		}
	}

	return float4(sum, 0, 1);
}

technique ShadowBlurBlit
{
	pass OneDir
	{
		PixelShader = compile PS_SHADERMODEL3 OutdoorsPCFBlit();
	}
}

technique ShadowSeparableBlit1
{
	pass Pass1
	{
		PixelShader = compile PS_SHADERMODEL3 OutdoorsPCFStage1();
	}
}

technique ShadowSeparableBlit2
{
	pass Pass1
	{
		PixelShader = compile PS_SHADERMODEL3 OutdoorsPCFStage2();
	}
}

technique ShadowSeparableBlit3
{
	pass Pass3
	{
		PixelShader = compile PS_SHADERMODEL3 OutdoorsPCFStage3();
	}
}

technique ShadowSeparableBlit4
{
	pass Pass4
	{
		PixelShader = compile PS_SHADERMODEL3 OutdoorsPCFStage4();
	}
}

float easeInSine(float t) {
	return 1 - cos(t * (3.141592 / 2));
};

float stickyOffset;
float stickyPersp;
float4 StickyEffectPS(float4 position : SV_Position, float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
	float2 z = float2(easeInSine(pow(texCoord.y, 1.2)), 0);
	z.x = z.x * stickyOffset + (texCoord.x - 0.5) * z.x * stickyPersp;
	return tex2D(TextureSampler, texCoord + z) * color;
}

technique StickyEffect
{
	pass Pass4
	{
		PixelShader = compile PS_SHADERMODEL StickyEffectPS();
	}
}
