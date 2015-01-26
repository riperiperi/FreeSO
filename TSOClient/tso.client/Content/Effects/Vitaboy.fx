float4x4 World;
float4x4 View;
float4x4 Projection;

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
    float4 position : POSITION0;
	float2 texCoord : TEXCOORD0;
	float4 bvPosition : TEXCOORD1;
	float3 params : TEXCOORD2;
};

struct VitaVertexOut
{
    float4 position : POSITION0;
	float2 texCoord : TEXCOORD0;
};

VitaVertexOut vsVitaboy(VitaVertexIn v) {
    VitaVertexOut result;
    float4 position = (1.0-v.params.z) * mul(v.position, SkelBindings[int(v.params.x)]) + v.params.z * mul(v.bvPosition, SkelBindings[int(v.params.y)]);
    result.texCoord = v.texCoord;

    float4 worldPosition = mul(position, World);
    float4 viewPosition = mul(worldPosition, View);
    result.position = mul(viewPosition, Projection);

    return result;
}

float4 psVitaboy(VitaVertexOut v) : COLOR0
{
    return tex2D(TexSampler, v.texCoord);
}

technique Technique1
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_3_0 vsVitaboy();
        PixelShader = compile ps_3_0 psVitaboy();
    }
}
