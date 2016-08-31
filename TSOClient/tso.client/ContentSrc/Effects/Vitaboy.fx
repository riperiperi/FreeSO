float4x4 World;
float4x4 View;
float4x4 Projection;

float ObjectID;
float4 AmbientLight;
float4x4 SkelBindings[50];
bool SoftwareDepth;
bool depthOutMode;

Texture MeshTex;
sampler TexSampler = sampler_state {
	texture = <MeshTex> ; 
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
};

texture depthMap : Diffuse;

sampler depthMapSampler = sampler_state {
    texture = <depthMap>;
    AddressU = CLAMP; AddressV = CLAMP; AddressW = CLAMP;
    MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
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
    float4 screenPos : TEXCOORD1;
};

float4 packObjID(float d) {
    float4 enc = float4(1.0, 255.0, 65025.0, 0.0) * d;
    enc = frac(enc);
    enc -= enc.yzww * float4(1.0 / 255.0, 1.0 / 255.0, 1.0 / 255.0, 0.0);
    enc.a = 1;

    return enc; //float4(byteFloor(d%1.0), byteFloor((d*256.0) % 1.0), byteFloor((d*65536.0) % 1.0), 1); //most sig in r, least in b
}

float unpackDepth(float4 d) {
    return dot(d, float4(1.0, 1 / 255.0, 1 / 65025.0, 0)); //d.r + (d.g / 256.0) + (d.b / 65536.0);
}

VitaVertexOut vsVitaboy(VitaVertexIn v) {
    VitaVertexOut result;
    float4 position = (1.0-v.params.z) * mul(v.position, SkelBindings[int(v.params.x)]) + v.params.z * mul(float4(v.bvPosition, 1.0), SkelBindings[int(v.params.y)]);
    result.texCoord = v.texCoord;

    float4 worldPosition = mul(position, World);
    float4 viewPosition = mul(worldPosition, View);
    float4 finalPos = mul(viewPosition, Projection);
    result.position = finalPos;
    result.screenPos = float4(finalPos.xy*float2(0.5, -0.5) + float2(0.5, 0.5), finalPos.zw);

    return result;
}

float4 psVitaboy(VitaVertexOut v) : COLOR0
{
    float depth = v.screenPos.z / v.screenPos.w;
    if (depthOutMode == true) {
        return packObjID(depth);
    }
    else {
        //SOFTWARE DEPTH
        if (SoftwareDepth == true && depthOutMode == false && unpackDepth(tex2D(depthMapSampler, v.screenPos.xy)) < depth) discard;
        return tex2D(TexSampler, v.texCoord)*AmbientLight;
    }

}

float4 psObjID(VitaVertexOut v) : COLOR0
{
    return packObjID(ObjectID);
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
