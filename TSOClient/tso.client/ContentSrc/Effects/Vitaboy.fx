float4x4 World;
float4x4 View;
float4x4 Projection;

float ObjectID;
float4 AmbientLight;
float4x4 SkelBindings[50];

Texture MeshTex;
sampler TexSampler = sampler_state {
	texture = <MeshTex> ; 
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
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
};

VitaVertexOut vsVitaboy(VitaVertexIn v) {
    VitaVertexOut result;
    float4 position = (1.0-v.params.z) * mul(v.position, SkelBindings[int(v.params.x)]) + v.params.z * mul(float4(v.bvPosition, 1.0), SkelBindings[int(v.params.y)]);
    result.texCoord = v.texCoord;

    float4 worldPosition = mul(position, World);
    float4 viewPosition = mul(worldPosition, View);
    result.position = mul(viewPosition, Projection);

    return result;
}

float4 psVitaboy(VitaVertexOut v) : COLOR0
{
    return tex2D(TexSampler, v.texCoord)*AmbientLight;
}

float4 psObjID(VitaVertexOut v) : COLOR0
{
    return float4(ObjectID, 0.0, 0.0, 1.0);
}

technique Technique1
{
    pass Pass1
    {
#if SM4
        VertexShader = compile vs_4_0_level_9_1 vsVitaboy();
        PixelShader = compile ps_4_0_level_9_1 psVitaboy();
#else
        VertexShader = compile vs_3_0 vsVitaboy();
        PixelShader = compile ps_3_0 psVitaboy();
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
