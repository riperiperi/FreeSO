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

// from shadertoy: https://www.shadertoy.com/view/4djSRW
// this function is used instead of a noise texture as directx does not like binding 2 textures for sprite effect.

float hash12(float2 p)
{
	float3 p3 = frac(float3(p.xyx) * .1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return frac((p3.x + p3.y) * p3.z);
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
	float e = 1.0e-5;
	return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

float3 HSVToRGB(float3 hsv)
{
	float4 k = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
	float3 p = abs(frac(hsv.xxx + k.xyz) * 6.0 - k.www);

	return hsv.z * lerp(k.xxx, clamp(p - k.xxx, 0.0, 1.0), hsv.y);
}

float2 SVToSL(float2 sv) {
	float e = 1.0e-5;
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
	float e = 1.0e-5;
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

// dissolve effect

float DissolvePercent;
float2 TexSize;
float4 dissolve(float4 position : SV_Position, float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
	float noiseValue = hash12(texCoord * TexSize); //tex2D(NoiseSampler, texCoord * TexSizeByNoiseSize).r;
	if (noiseValue < DissolvePercent) discard;
	return tex2D(TextureSampler, texCoord) * color;
}

technique Dissolve
{
	pass OneDir
	{
		PixelShader = compile PS_SHADERMODEL dissolve();
	}
}

// maze 2 grid effect

float MazeHorizon = 0.5;
float MazeVanishing = 0.55;
float MazeGridScale = 1.9;
float3 MazeColor1 = float3(0.9058823529411765, 0.7294117647058823, 0.5764705882352941);
float3 MazeColor2 = float3(0.9058823529411765, 0.5333333333333333, 0.4196078431372549);

float2 MazeGridOffset;
float MazeRotation;
bool MazeUseTexture = false;

float mod(float x, float y)
{
	return x - y * floor(x / y);
}

float2 mod2(float2 x, float y)
{
	return float2(mod(x.x, y), mod(x.y, y));
}

float4 maze2Grid(float4 position : SV_Position, float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
	float3 col;
	float2 uv = float2(texCoord.x, 1.0 - texCoord.y);
	if (uv.y > MazeHorizon) {
		discard; // only the most efficient shaders draw half of the output as discard
	}
	else {
		// for this pixel, calculate the xy position on the ground that we are looking at.
		// we can use that calculated position for texture mapping or a grid pattern.
		float yFactor = (uv.y - MazeVanishing) / (1.0 - MazeVanishing);
		float2 pos = float2((uv.x - 0.5) / yFactor, 1.0 / yFactor);

		// apply rotation
		float c = cos(MazeRotation);
		float s = sin(MazeRotation);
		float2 xr = float2(c, s);
		float2 yr = float2(-s, c);

		pos = float2(dot(pos, xr), dot(pos, yr)) * MazeGridScale + MazeGridOffset;

		// pos now contains the final ground position.
		if (MazeUseTexture == true) {
			col.rgb = tex2D(TextureSampler, mod2(pos, 1.0)).rgb;
		}
		else {
			// basic grid shader.
			bool altX = mod(pos.x, 2.0) > 1.0;
			bool altY = mod(pos.y, 2.0) > 1.0;

			if (altX == altY) col.rgb = MazeColor1;
			else col.rgb = MazeColor2;
		}
	}

	return float4(col, 1.0);
}

technique Maze2Grid
{
	pass OneDir
	{
		PixelShader = compile PS_SHADERMODEL maze2Grid();
	}
}

static const float kernel9[81] = {
	0.000000, 0.000001, 0.000014, 0.000055, 0.000088, 0.000055, 0.000014, 0.000001, 0.000000,
	0.000001, 0.000036, 0.000362, 0.001445, 0.002289, 0.001445, 0.000362, 0.000036, 0.000001,
	0.000014, 0.000362, 0.003672, 0.014648, 0.023205, 0.014648, 0.003672, 0.000362, 0.000014,
	0.000055, 0.001445, 0.014648, 0.058434, 0.092566, 0.058434, 0.014648, 0.001445, 0.000055,
	0.000088, 0.002289, 0.023205, 0.092566, 0.146634, 0.092566, 0.023205, 0.002289, 0.000088,
	0.000055, 0.001445, 0.014648, 0.058434, 0.092566, 0.058434, 0.014648, 0.001445, 0.000055,
	0.000014, 0.000362, 0.003672, 0.014648, 0.023205, 0.014648, 0.003672, 0.000362, 0.000014,
	0.000001, 0.000036, 0.000362, 0.001445, 0.002289, 0.001445, 0.000362, 0.000036, 0.000001,
	0.000000, 0.000001, 0.000014, 0.000055, 0.000088, 0.000055, 0.000014, 0.000001, 0.000000
};


static const float kernel9trim[45] = {
	          0.000362, 0.001445, 0.002289, 0.001445, 0.000362,
	0.000362, 0.003672, 0.014648, 0.023205, 0.014648, 0.003672, 0.000362,
	0.001445, 0.014648, 0.058434, 0.092566, 0.058434, 0.014648, 0.001445,
	0.002289, 0.023205, 0.092566, 0.146634, 0.092566, 0.023205, 0.002289,
	0.001445, 0.014648, 0.058434, 0.092566, 0.058434, 0.014648, 0.001445,
	0.000362, 0.003672, 0.014648, 0.023205, 0.014648, 0.003672, 0.000362,
	          0.000362, 0.001445, 0.002289, 0.001445, 0.000362
};

static const float2 k9Off[45] = {
	                float2(-2, -3), float2(-1, -3), float2(0, -3), float2(1, -3), float2(2, -3),
	float2(-3, -2), float2(-2, -2), float2(-1, -2), float2(0, -2), float2(1, -2), float2(2, -2), float2(3, -2),
	float2(-3, -1), float2(-2, -1), float2(-1, -1), float2(0, -1), float2(1, -1), float2(2, -1), float2(3, -1),
	float2(-3, 0),  float2(-2, 0),  float2(-1, 0),  float2(0, 0),  float2(1, 0),  float2(2, 0),  float2(3, 0),
	float2(-3, 1),  float2(-2, 1),  float2(-1, 1),  float2(0, 1),  float2(1, 1),  float2(2, 1),  float2(3, 1),
	float2(-3, -2), float2(-2, 2),  float2(-1, 2),  float2(0, 2),  float2(1, 2),  float2(2, 2),  float2(3, 2),
	                float2(-2, 3),  float2(-1, 3),  float2(0, 3),  float2(1, 3),  float2(2, 3),
};


static const int FILTER_SIZE = 4;
static const float VARIANCE = 0.0039215686274509803921568627451 * 2;
static const float DISC_DIFF = 0.02;

float2 pixelSize = float2(0.00735294117647058823529411764706, 0.00260416666666666666666666666667);

float4 derivativeDepth(float4 position : SV_Position, float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
	float depth = tex2D(TextureSampler, uv).r;
	float4 d = float4(0,0,0,0);

	float dxp = tex2D(TextureSampler, uv + pixelSize * float2(1, 0)).r - depth;
	float dxm = depth - tex2D(TextureSampler, uv + pixelSize * float2(-1, 0)).r;

	if (abs(dxp) < DISC_DIFF) d.xz += float2(dxp, 1);
	if (abs(dxm) < DISC_DIFF) d.xz += float2(dxm, 1);

	float dyp = tex2D(TextureSampler, uv + pixelSize * float2(0, 1)).r - depth;
	float dym = depth - tex2D(TextureSampler, uv + pixelSize * float2(0, -1)).r;
	if (abs(dyp) < DISC_DIFF) d.yw += float2(dyp, 1);
	if (abs(dym) < DISC_DIFF) d.yw += float2(dym, 1);

	d.xy /= d.zw;
	return float4(depth, d.xy, 1.0);
}

float4 dequantizeDepth(float4 position : SV_Position, float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0 
{
	float4 tex = tex2D(TextureSampler, uv);
	float depth = tex.r;
/*

	float4 d = float4(0,0,0,0);

	float dxp = tex2D(TextureSampler, uv + pixelSize * float2(1, 0)).r - depth;
	float dxm = depth - tex2D(TextureSampler, uv + pixelSize * float2(-1, 0)).r;

	if (abs(dxp) < DISC_DIFF) d.xz += float2(dxp, 1);
	if (abs(dxm) < DISC_DIFF) d.xz += float2(dxm, 1);

	float dyp = tex2D(TextureSampler, uv + pixelSize * float2(0, 1)).r - depth;
	float dym = depth - tex2D(TextureSampler, uv + pixelSize * float2(0, -1)).r;
	if (abs(dyp) < DISC_DIFF) d.yw += float2(dyp, 1);
	if (abs(dym) < DISC_DIFF) d.yw += float2(dym, 1);

	d.xy /= d.zw;
	*/
	float2 d = tex.gb;

	//let's pretend that's accurate enough for now

	float sumK = 0.0;
	float sumD = 0.0;
	float2 sumDerivative = float2(0, 0);
	
	[unroll(45)] for (int i = 0; i <= 45; i++) {
		float kValue = kernel9trim[i];
		float2 pos = k9Off[i];
		float kDepth = tex2D(TextureSampler, uv + pixelSize * pos).r;
		float globalD = (kDepth - depth);
		float2 variance = abs(d.xy - globalD / pos);
		if ((pos.x == 0 || variance.x < VARIANCE) && (pos.y == 0 || variance.y < VARIANCE)) {
			sumK += kValue;
			sumD += kDepth * kValue;
			sumDerivative -= sign(pos) * globalD * kValue;
		}
	}
	/*
	[unroll(9)] for (int x = -FILTER_SIZE; x <= FILTER_SIZE; x++) {
		[unroll(9)] for (int y = -FILTER_SIZE; y <= FILTER_SIZE; y++) {
			float kValue = kernel9[(x + 4) + (y + 4) * 9];
			float2 pos = float2(x, y);
			float kDepth = tex2D(TextureSampler, uv + pixelSize * pos).r;
			float globalD = (kDepth - depth);
			float2 variance = abs(d.xy - globalD / pos);
			if ((x == 0 || variance.x < VARIANCE) && (y == 0 || variance.y < VARIANCE)) {
				sumK += kValue;
				sumD += kDepth * kValue;
				sumDerivative -= sign(pos) * globalD * kValue;
			}
		}
	}
	*/
	sumD /= sumK;
	sumDerivative /= sumK;

	//estimate new depth
	float estDepth = sumD - (sumDerivative.x + sumDerivative.y);
	return float4(estDepth, 0, 0, 1);
}

technique DerivativeDepth
{
	pass All
	{
		PixelShader = compile PS_SHADERMODEL3 derivativeDepth();
	}
}

technique DequantizeDepth
{
	pass All
	{
		PixelShader = compile PS_SHADERMODEL3 dequantizeDepth();
	}
}