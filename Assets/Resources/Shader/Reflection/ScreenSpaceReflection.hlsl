#ifndef __SCREEN_SPACE_REFLECTION_HLSL__
#define __SCREEN_SPACE_REFLECTION_HLSL__

#include "../../../Shader/Lib/Core.hlsl"
#include "../../../Shader/Lib/Utils/CameraOpaqueTexture.hlsl"

//--------------------------------------
// 顶点结构体
struct Attributes
{
    float3 positionOS : POSITION;
    half2 texcoord : TEXCOORD0;
};

//--------------------------------------
// 片元结构体
struct Varyings
{
    float4 positionCS : SV_POSITION;
    half2 texcoord : TEXCOORD0;
};

// 裁剪空间变换矩阵
uniform float4x4 _VPMatrix;

TEXTURE2D(_SSRMaskTexture);
SAMPLER(sampler_SSRMaskTexture);

TEXTURE2D_X_FLOAT(_SSRDepthTexture);
SAMPLER(sampler_SSRDepthTexture);

inline float SampleSSRDepth(half2 uv)
{
    float depth = SAMPLE_TEXTURE2D_X(_SSRDepthTexture, sampler_SSRDepthTexture, uv).r;
#if !UNITY_REVERSED_Z
    depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, depth);
#endif
    return depth;
}

Varyings vert(Attributes input)
{
    Varyings output = (Varyings) 0;
    output.positionCS = TransformObjectToHClip(input.positionOS);
    output.texcoord = input.texcoord;
    return output;
}

half4 frag(Varyings input) : SV_Target
{
    half4 maskColor = SAMPLE_TEXTURE2D(_SSRMaskTexture, sampler_SSRMaskTexture, input.texcoord);
    if (maskColor.a < 0.5)
        return half4(0, 0, 0, 0);
    float3 normalWS = 2.0 * maskColor.rgb - 1.0;

    // 获取屏幕空间
    float depth = SampleSSRDepth(input.texcoord);
    float4 positionCS = float4(2.0 * input.texcoord - 1.0, depth, 1.0);
#if UNITY_UV_STARTS_AT_TOP
	positionCS.y = -positionCS.y;
#endif
    
    // 屏幕空间转换成世界空间
    float4 positionWS = mul(UNITY_MATRIX_I_VP, positionCS);
    positionWS.xyz /= positionWS.w;

    // 计算反射向量
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS.xyz);
    half3 reflectDirWS = reflect(-viewDirWS, normalWS);

    half4 reflectColor = 0;
    UNITY_LOOP
    for (int i = 0; i < 128; ++i)
    {
        float3 reflectPosWS = positionWS.xyz + reflectDirWS * 0.3 * i;
        float4 reflectPosCS = mul(_VPMatrix, float4(reflectPosWS, 1.0f));
        float reflectDepth = reflectPosCS.w;
        
        reflectPosCS /= reflectPosCS.w;
        float2 reflectPosSS = reflectPosCS.xy * 0.5 + 0.5;
#if UNITY_UV_STARTS_AT_TOP
        reflectPosSS.y = 1.0 - reflectPosSS.y;
#endif
        // 判断是否超出屏幕
        if (reflectPosSS.x < 0.0 || reflectPosSS.y < 0.0 || reflectPosSS.x > 1.0 || reflectPosSS.y > 1.0)
            break;
        
        float eyeDepth = GetEyeDepth(reflectPosSS);
        if (reflectDepth > eyeDepth && abs(reflectDepth - eyeDepth) < 4)
        {
            reflectColor = half4(SampleScreen(reflectPosSS), 1);
            break;
        }
    }
    return reflectColor;
}

#endif
