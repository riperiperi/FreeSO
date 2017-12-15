
texture advancedLight : Diffuse;
sampler advLightSampler = sampler_state {
	texture = <advancedLight>;
	AddressU = CLAMP; AddressV = CLAMP; AddressW = CLAMP;
	MIPFILTER = LINEAR; MINFILTER = LINEAR; MAGFILTER = LINEAR;
};

//LIGHTING
float4 OutsideDark;
float2 MinAvg;
float3 WorldToLightFactor;
float2 LightOffset;
float2 MapLayout;
float Level;
//END LIGHTING

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

float4 lightProcessFloor(float4 inPosition) {
	float2 orig = inPosition.x;
	inPosition.xyz *= WorldToLightFactor;
	inPosition.xz += LightOffset;

	inPosition.xz += 1 / MapLayout * floor(float2(Level % MapLayout.x, Level / MapLayout.x));

	float4 lTex = tex2D(advLightSampler, inPosition.xz);
	return lightColorFloor(lTex);
}

float4 lightInterp(float4 inPosition) {
	inPosition.xyz *= WorldToLightFactor;
	inPosition.xz += LightOffset;

	float level = min(Level, floor(inPosition.y) + 0.0001);
	float abvLevel = min(Level, level + 1);
	float2 iPA = inPosition.xz + 1 / MapLayout * floor(float2(abvLevel % MapLayout.x, abvLevel / MapLayout.x));
	inPosition.xz += 1 / MapLayout * floor(float2(level % MapLayout.x, level / MapLayout.x));

	float4 lTex = tex2D(advLightSampler, inPosition.xz);
	lTex.rgb = lerp(lTex.rgb, tex2D(advLightSampler, iPA).rgb, max(0, (inPosition.y % 1) * 2 - 1));

	return lightColorI(lTex, clamp((inPosition.y % 1) * 3, 0, 1));
}