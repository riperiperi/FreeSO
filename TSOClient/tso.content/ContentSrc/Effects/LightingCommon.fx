
texture advancedLight : Diffuse;
sampler advLightSampler = sampler_state {
	texture = <advancedLight>;
	AddressU = CLAMP; AddressV = CLAMP; AddressW = CLAMP;
	MIPFILTER = LINEAR; MINFILTER = LINEAR; MAGFILTER = LINEAR;
};

texture advancedDirection : Diffuse;
sampler advDirectionSampler = sampler_state {
	texture = <advancedDirection>;
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

float3 LightingAdjust; //should default to 1s. Used to adjust slow updating surround lighting to match main lighting.
//END LIGHTING

float4 lightColor(float4 intensities) {
	return float4(intensities.rgb * LightingAdjust, 1);
}

float4 lightColorFloor(float4 intensities) {
	// RGBA: LightIntensity, OutdoorsIntensity, LightIntensityShad, OutdoorsIntensityShad

	float avg = (intensities.r + intensities.g + intensities.b) / 3;
	//floor shadow is how much less than average the alpha component is

	float fshad = intensities.a / max(0.0001, avg);

	return lerp(OutsideDark, float4(intensities.rgb * LightingAdjust, 1), (fshad - MinAvg.x) * MinAvg.y);
}

float4 lightColorIAvg(float4 intensities, float i, float avg) {

	float fshad = intensities.a / max(0.0001, avg);
	fshad = lerp(fshad, 1, i);

	return lerp(OutsideDark, float4(intensities.rgb * LightingAdjust, 1), (fshad - MinAvg.x) * MinAvg.y);
}

float4 lightColorI(float4 intensities, float i) {

	float avg = (intensities.r + intensities.g + intensities.b) / 3;
	//floor shadow is how much less than average the alpha component is

	float fshad = intensities.a / max(0.0001, avg);
	fshad = lerp(fshad, 1, i);

	return lerp(OutsideDark, float4(intensities.rgb * LightingAdjust, 1), (fshad - MinAvg.x) * MinAvg.y);
}

float4 lightProcessLevel(float4 inPosition, float level) {
	inPosition.xyz *= WorldToLightFactor;
	inPosition.xz += LightOffset;

	inPosition.xz += 1 / MapLayout * floor(float2(level % MapLayout.x, level / MapLayout.x));

	float4 lTex = tex2D(advLightSampler, inPosition.xz);
	return lightColor(lTex);
}

float4 lightProcess(float4 inPosition) {
	return lightProcessLevel(inPosition, Level);
}

float4 lightProcessFloor(float4 inPosition) {
	inPosition.xyz *= WorldToLightFactor;
	inPosition.xz += LightOffset;

	inPosition.xz += 1 / MapLayout * floor(float2(Level % MapLayout.x, Level / MapLayout.x));

	float4 lTex = tex2D(advLightSampler, inPosition.xz);
	return lightColorFloor(lTex);
}

float4 lightProcessRoof(float4 inPosition) {
	if (Level >= 5) inPosition.xz = float2(0.001, 0.001); //hack: outdoor color is likely at this offset.
	else {
		inPosition.xyz *= WorldToLightFactor;
		inPosition.xz += LightOffset;
		inPosition.xz += 1 / MapLayout * floor(float2(Level % MapLayout.x, Level / MapLayout.x));
	}
	float4 lTex = tex2D(advLightSampler, inPosition.xz);
	return lightColorFloor(lTex);
}

float4 lightProcessRoofCeiling(float4 inPosition) {
	if (Level >= 6) inPosition.xz = float2(0.001, 0.001); //hack: outdoor color is likely at this offset.
	else {
		inPosition.xyz *= WorldToLightFactor;
		inPosition.xz += LightOffset;
		inPosition.xz += 1 / MapLayout * floor(float2((Level - 1) % MapLayout.x, (Level - 1) / MapLayout.x));
	}
	float4 lTex = tex2D(advLightSampler, inPosition.xz);
	return lightColor(lTex);
}

float4 lightInterp(float4 inPosition, float lightBleed) {
	inPosition.xyz *= WorldToLightFactor;
	inPosition.xz += LightOffset;

	float level = min(Level, floor(inPosition.y) + 0.0001);
	float belowLevel = level - 1;
	float2 iPA = inPosition.xz + 1 / MapLayout * floor(float2(belowLevel % MapLayout.x, belowLevel / MapLayout.x));
	inPosition.xz += 1 / MapLayout * floor(float2(level % MapLayout.x, level / MapLayout.x));

	float4 lTex = tex2D(advLightSampler, inPosition.xz);

	float avg = (lTex.r + lTex.g + lTex.b) / 3;
	lTex.rgb = lerp(lTex.rgb, tex2D(advLightSampler, iPA).rgb, max(0, 1 - (inPosition.y % 1) * 2) * lightBleed);

	return lightColorIAvg(lTex, clamp((inPosition.y % 1) * 3, 0, 1), avg);
}

float4 lightProcessDirectionLevel(float4 inPosition, float3 normal, float level) {
	float2 orig = inPosition.x;
	inPosition.xyz *= WorldToLightFactor;
	inPosition.xz += LightOffset;

	inPosition.xz += 1 / MapLayout * floor(float2(level % MapLayout.x, level / MapLayout.x));

	float4 lTex = tex2D(advLightSampler, inPosition.xz);
	float4 color = lightColor(lTex);
	float4 direction = tex2D(advDirectionSampler, inPosition.xz);

	float dirIntensity = length(direction.xyz) + 0.0001;
	float pctAmbient = 1 - (dirIntensity / (direction.w + 0.0001));

	//((pow((dot(normal, normalize(direction.xyz)) + 1) / 2, 2) - 0.25) / 0.75) * (1-pctAmbient) + pctAmbient;
	//pow((dot(normal, -normalize(direction.xyz)) + 1) / 2, 2);
	float lightIntensity = (sin(dot(normal, -direction.xyz/dirIntensity)*(3.141592 / 2)) + 1) / 2;
	lightIntensity = lightIntensity *(1 - pctAmbient) + pctAmbient;
	color.rgb = lerp(OutsideDark.rgb, color.rgb, lightIntensity*1.40f - 0.2f);
	return color;
}

float4 lightProcessDirection(float4 inPosition, float3 normal) {
	return lightProcessDirectionLevel(inPosition, normal, Level);
}

//coeffs from http://chilliant.blogspot.com/2012/08/srgb-approximations-for-hlsl.html.
float4 LinearToSRGB(float4 col) {
	float3 s1 = sqrt(col.rgb);
	float3 s2 = sqrt(s1);
	float3 s3 = sqrt(s2);
	col.rgb = 0.662002687 * s1 + 0.684122060 * s2 - 0.323583601 * s3 - 0.0225411470 * col.rgb;
	return col;
}

float4 SRGBToLinear(float4 col) {
	col.rgb = col.rgb * (col.rgb * (col.rgb * 0.305306011 + 0.682171111) + 0.012522878);
	return col;
}

float4 LinearToSRGBSimple(float4 col) {
	col.rgb = pow(col.rgb, 1/2.2);
	return col;
}

float4 SRGBToLinearSimple(float4 col) {
	col.rgb = pow(col.rgb, 2.2);
	return col;
}

float4 gammaMulSimple(float4 baseCol, float4 lighting) {
	return LinearToSRGBSimple(SRGBToLinearSimple(baseCol)*lighting);
}

float4 gammaMadSimple(float4 baseCol, float4 lighting, float4 add) {
	return LinearToSRGBSimple(SRGBToLinearSimple(baseCol)*lighting + add);
}

float4 gammaMul(float4 baseCol, float4 lighting) {
	return LinearToSRGB(SRGBToLinear(baseCol)*lighting);
}

float4 gammaMad(float4 baseCol, float4 lighting, float4 add) {
	return LinearToSRGB(SRGBToLinear(baseCol)*lighting + add);
}

float4 gammaMul1(float4 baseCol, float lighting) {
	return LinearToSRGB(float4(SRGBToLinear(baseCol).rgb*lighting, baseCol.a));
}

float4 gammaMad1(float4 baseCol, float lighting, float4 add) {
	return LinearToSRGB(float4(SRGBToLinear(baseCol).rgb*lighting, 1) + add);
}
