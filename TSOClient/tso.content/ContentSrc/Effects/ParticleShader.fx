#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_3
#define PS_SHADERMODEL ps_4_0_level_9_3
#endif

#include "LightingCommon.fx"

float4x4 Projection;
float4x4 View;
float4x4 World;
float4x4 InvRotation;
float4x4 InvXZRotation;

float BaseAlt;
float Time;
float TimeRate;
float StopTime; //NANI
float Frequency;

float4 Parameters1;
float4 Parameters2;
float4 Parameters3;
float4 Parameters4;

float Stories;
float ClipLevel;
float2 BpSize;
float4 Color;
float4 SubColor;
float3 CameraVelocity;

texture BaseTex;
sampler TexSampler = sampler_state {
	texture = <BaseTex>;
	AddressU = Wrap;
	AddressV = Wrap;
	MIPFILTER = LINEAR; MINFILTER = LINEAR; MAGFILTER = LINEAR;
};

texture IndoorsTex;
sampler IndoorsSampler = sampler_state {
	texture = <IndoorsTex>;
	AddressU = CLAMP;
	AddressV = CLAMP;
	MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};

struct ParticleInput
{
	float4 Position : SV_POSITION;
	float3 ModelPosition : TEXCOORD0;
	float3 TexCoord : TEXCOORD1;
};

struct ParticleOutput
{
	float4 Position : SV_POSITION;
	float2 TexCoord : TEXCOORD0;
	float4 Color : TEXCOORD1;
	float4 ModelPos : TEXCOORD2;
};

float4 Billboard(float4 posIn, float4 modelPos) {
	return posIn + mul(modelPos, InvRotation);
}

float4 XZBillboard(float4 posIn, float4 modelPos) {
	return posIn + mul(modelPos, InvXZRotation);
}

float3 RotateXY(float3 posIn, float angle) {
	float s = sin(angle);
	float c = cos(angle);
	posIn.xy = float2(posIn.x * c + posIn.y * s, posIn.y * c - posIn.x * s);
	return posIn;
}

//Parameters:
//miny, yrange, fall speed, fall speed variation
//wind x, wind z, wind variation, rotation variation
//minx, xrange, minz, zrange
//scale
ParticleOutput SnowVS(in ParticleInput input)
{
	ParticleOutput output = (ParticleOutput)0;

	float4 boxCtr = mul(float4(0, 0, 0, 1), World);

	float fallSpeed = Parameters1.z + sin(input.Position.y * 1000) * Parameters1.w;
	float rotSpeed = sin(input.Position.y * 1000) * Parameters2.w;
	float repeatTime = fallSpeed / Parameters1.y;
	float realTime = (Time / repeatTime) % 1;
	float yFrac = (input.Position.y - (Parameters1.x + boxCtr.y)) / Parameters1.y;
	yFrac = frac(yFrac - realTime);

	float newY = Parameters1.x + yFrac * Parameters1.y;
	
	float2 windSpeed = Parameters2.xy + float2(sin(input.Position.x * 1000), sin(input.Position.z * 1000)) * Parameters2.z;
	float2 xz = input.Position.xz + (realTime) * windSpeed;

	float2 xzbase = (Parameters3.xz + boxCtr.xz); //test
	xz = ((xz - xzbase) % Parameters3.yw) + xzbase;

	float flakeSize = (sin(input.Position.y * 1000)*0.15 + 1) * Parameters4.x;
	float4 realCtr = float4(xz.x, newY + boxCtr.y, xz.y, 1);
	float4 realPos = Billboard(realCtr, float4(RotateXY(input.ModelPosition, rotSpeed*realTime) * flakeSize, 1));

	output.TexCoord = input.TexCoord.xy;
	output.ModelPos = realPos / 2; //not sure why i need to do this
	output.ModelPos.y -= BaseAlt;
	output.Position = mul(realPos, mul(View, Projection));
	output.Color = float4(1, 1, 1, 1) * min(1, (0.5 - abs(yFrac - 0.5)) * 20) * min(1, ClipLevel*(2.95*3) - output.ModelPos.y) * Color;

	return output;
}

//Parameters:
//miny, yrange, fall speed, fall speed variation
//wind x, wind z, wind variation, width (0.3)
//minx, xrange, minz, zrange
ParticleOutput RainVS(in ParticleInput input)
{
	ParticleOutput output = (ParticleOutput)0;

	float4 boxCtr = mul(float4(0, 0, 0, 1), World);

	float fallSpeed = Parameters1.z + sin(input.Position.y * 1000) * Parameters1.w;
	float repeatTime = fallSpeed / Parameters1.y;
	float realTime = (Time / repeatTime) % 1;
	float yFrac = (input.Position.y - (Parameters1.x + boxCtr.y)) / Parameters1.y;
	yFrac = frac(yFrac - realTime);

	float newY = Parameters1.x + yFrac * Parameters1.y;

	float2 windSpeed = Parameters2.xy + float2(sin(input.Position.x * 1000), sin(input.Position.z * 1000)) * Parameters2.z;
	float2 xz = input.Position.xz + (realTime) * windSpeed;

	float2 xzbase = (Parameters3.xz + boxCtr.xz); //test
	xz = ((xz - xzbase) % Parameters3.yw) + xzbase;

	float4 realCtr = float4(xz.x, newY + boxCtr.y, xz.y, 1);
	float2 windDelta = (windSpeed * (TimeRate / repeatTime)) / -2;
	float3 delta = float3(windDelta.x, (Parameters1.y * TimeRate / repeatTime), windDelta.y) * 4 + CameraVelocity;
	float4 lastCtr = realCtr - float4(delta, 0);

	float3 newModelPosition = input.ModelPosition;
	newModelPosition.x *= Parameters2.w;
	newModelPosition.y = input.ModelPosition.y * delta.y + Parameters2.w;

	float4 realPos = XZBillboard(realCtr, float4(newModelPosition, 1));

	realPos.xz += input.ModelPosition.y * delta.xz;

	output.TexCoord = input.TexCoord.xy;
	output.ModelPos = realPos / 2; //not sure why i need to do this
	output.ModelPos.y -= BaseAlt;
	output.Position = mul(realPos, mul(View, Projection));
	output.Color = float4(1, 1, 1, 1) * min(1, (0.5 - abs(yFrac - 0.5)) * 20) * min(1, ClipLevel*(2.95 * 3) - output.ModelPos.y) * Color;

	return output;
}

//Parameters:
//(deltax, deltay, deltaz, gravity)
//(deltavar, rotdeltavar, size, sizevel)
//(duration, fadein, fadeout, sizevar)
ParticleOutput GenericBoxVS(in ParticleInput input)
{
	ParticleOutput output = (ParticleOutput)0;

	float4 boxCtr = mul(float4(0, 0, 0, 1), World);
	float repeatTime = Parameters3.x;
	//if all particles created and died at the same time things would be pretty stupid.
	//calculate a "random" delta to phase offset this particle's animation.
	float3 rands = float3(sin(input.Position.x * 1000), sin(input.Position.y * 1000), sin(input.Position.z * 1000));

	float timeDelta = input.TexCoord.z * Frequency;
	float ltTime = ((Time + timeDelta) / Frequency) % 1.0;
	ltTime *= Frequency / repeatTime;

	float realTime = ltTime * repeatTime;

	float3 positionMod = (Parameters1.xyz + Parameters2.x*rands) * realTime;
	positionMod.y += Parameters1.w * realTime*realTime;

	float rotSpeed = sin(input.Position.y * 1000) * Parameters2.y;

	float flakeSize = sin(input.Position.y * 1245)*Parameters2.z + Parameters3.w + realTime * Parameters2.w;
	float4 realCtr = mul((input.Position + float4(positionMod, 0)), World);
	if (ltTime > 1) flakeSize = 0;
	float4 realPos = Billboard(realCtr, float4(RotateXY(input.ModelPosition, (rands.x*3.14)+rotSpeed*realTime) * flakeSize, 1));

	output.TexCoord = input.TexCoord.xy;
	output.ModelPos = realPos / 2; //not sure why i need to do this
	output.ModelPos.y -= BaseAlt;
	output.Position = mul(realPos, mul(View, Projection));

	float opacity;
	float startTime = Time - realTime;
	if (startTime > StopTime || startTime < 0.0) { //if we started after this emitter stopped, then don't show us at all.
		opacity = 0.0;
	} else if (ltTime < Parameters3.y) {
		opacity = min(1.0, ltTime / Parameters3.y);
	} else {
		opacity = min(1.0, (1-ltTime) / Parameters3.z);
	}

	float baseColInt = Parameters4.w * rands.z;
	output.Color = opacity * float4(lerp(Color.xyz, Parameters4.xyz, baseColInt + ltTime*(1.0-baseColInt)), Color.w);

	return output;
}

float dpth(float4 v) {
#if SM4
	return v.a;
#else
	return v.r;
#endif
}

float4 MainPS(ParticleOutput input) : COLOR
{
	float level = (input.ModelPos.y) / (2.95*3);
	if (level >= ClipLevel || round(dpth(tex2D(IndoorsSampler, input.ModelPos.xz / BpSize))*Stories) > level) discard;
	return gammaMul(tex2D(TexSampler, input.TexCoord)*input.Color * Color, lightInterp(input.ModelPos, 1)) - SubColor;
}

float4 SimplePS(ParticleOutput input) : COLOR
{
	return tex2D(TexSampler, input.TexCoord)*input.Color;
}

float4 RainPS(ParticleOutput input) : COLOR
{
	float level = (input.ModelPos.y) / (2.95 * 3);
	if (level >= ClipLevel || round(dpth(tex2D(IndoorsSampler, input.ModelPos.xz / BpSize))*Stories) > level) discard;
	return gammaMul(((1-cos(input.TexCoord.y*3.1415*2)) * (1 - cos(input.TexCoord.x*3.1415 * 2))/4) *input.Color, lightInterp(input.ModelPos, 1)) - SubColor;
}

float4 RainSimplePS(ParticleOutput input) : COLOR
{
	return ((1 - cos(input.TexCoord.y*3.1415 * 2)) * (1 - cos(input.TexCoord.x*3.1415 * 2)) / 4) * input.Color - SubColor;
}

float4 ParticlePS(ParticleOutput input) : COLOR
{
	if (input.Color.a == 0) discard;
	return gammaMul(tex2D(TexSampler, input.TexCoord)*input.Color, lightProcess(input.ModelPos)) - SubColor;
}

//snow particles that follow the camera
technique SnowParticle
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL SnowVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};

technique SnowParticleSimple
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL SnowVS();
		PixelShader = compile PS_SHADERMODEL SimplePS();
	}
};

//rain particles that follow the camera
technique RainParticle
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL RainVS();
		PixelShader = compile PS_SHADERMODEL RainPS();
	}
};

technique RainSimpleParticle
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL RainVS();
		PixelShader = compile PS_SHADERMODEL RainSimplePS();
	}
};

//particles with fixed start location, but various deltas and duration. eg. smoke, fire
technique GenericBoxParticle
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL GenericBoxVS();
		PixelShader = compile PS_SHADERMODEL ParticlePS();
	}
};
