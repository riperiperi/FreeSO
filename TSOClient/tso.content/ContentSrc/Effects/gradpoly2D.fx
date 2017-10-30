float4x4 Projection;

struct VertexShaderInput
{
    float2 Position : SV_Position0;
    float4 Color : COLOR0;
    float4 Color2 : COLOR1;
    float2 StartPos : TEXCOORD0;
    float2 EndPos : TEXCOORD1;
    float4 Params : TEXCOORD2;
	float4 EllipseDat : TEXCOORD3;
};

struct VertexShaderOutput
{
    float4 Position : SV_Position0;
    float4 Color : COLOR0;
    float4 Color2 : COLOR1;
    float2 StartPos : TEXCOORD0;
    float2 EndPos : TEXCOORD1;
    float4 Params : TEXCOORD2;
	float4 EllipseDat : TEXCOORD3;
    float2 IntPos : TEXCOORD4;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;
    output.Position = mul(float4(input.Position.xy, 0, 1), Projection);
    output.Color = input.Color;
    output.Color2 = input.Color2;
    output.StartPos = input.StartPos + float2(0.0000001, 0.0000001);
    output.EndPos = input.EndPos + float2(0.0000001, 0.0000001);
    output.Params = input.Params;
	output.EllipseDat = input.EllipseDat;
	output.IntPos = input.Position.xy + float2(0.0000001, 0.0000001);

    return output;
}

float4 Gradient(VertexShaderOutput input)
{
    float mode = input.Params.x;
    if (mode == 0) {
	    //solid
		return input.Color;
	}
	else if (mode == 2) {
		//cone
		float2 coneVec = normalize(input.EndPos - input.StartPos);
		float2 myVec = normalize(input.IntPos - input.StartPos);

		float angle = acos(dot(coneVec, myVec)) / input.Params.y; //clamp(, 0, 1);
		return lerp(input.Color2, input.Color, angle);
	}
	return input.Color;
}

float EllipseMultiplier(VertexShaderOutput input) {
	float2 ellipseVec1 = input.EllipseDat.xy;
	float2 ellipseVec2 = input.EllipseDat.zw; //the long one ;)
	float2 ellipsePos = input.Params.zw;

	float2 relPos = input.IntPos - ellipsePos;
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

float LinearMultiplier(VertexShaderOutput input) {
	float2 linearVec = input.EllipseDat.zw; //the long one ;)
	float2 linearPos = input.Params.zw;

	float2 relPos = input.IntPos - linearPos;
	float length2 = input.EllipseDat.x;

	float dt = dot(relPos, linearVec);
	if (dt < 0) return 0;
	return 1 - clamp(dt/length2, 0, 1);
}

float4 PSNormal(VertexShaderOutput input) : COLOR0
{
	return Gradient(input);
}

float4 PSEllipse(VertexShaderOutput input) : COLOR0
{
	return Gradient(input) * EllipseMultiplier(input);
}

float4 PSLinear(VertexShaderOutput input) : COLOR0
{
	return Gradient(input) * LinearMultiplier(input);
}

technique Draw2D
{
    pass MainPass
    {

#if SM4
        VertexShader = compile vs_4_0_level_9_1 VertexShaderFunction();
        PixelShader = compile ps_4_0_level_9_1 PSNormal();
#else
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PSNormal();
#endif;

    }

	pass EllipsePass
	{

#if SM4
		VertexShader = compile vs_4_0_level_9_1 VertexShaderFunction();
		PixelShader = compile ps_4_0_level_9_1 PSEllipse();
#else
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PSEllipse();
#endif;

	}

	pass LinearPass
	{

#if SM4
		VertexShader = compile vs_4_0_level_9_1 VertexShaderFunction();
		PixelShader = compile ps_4_0_level_9_1 PSLinear();
#else
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PSLinear();
#endif;

	}
}
