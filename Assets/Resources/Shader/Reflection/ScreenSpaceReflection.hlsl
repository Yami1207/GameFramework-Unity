#ifndef __SCREEN_SPACE_REFLECTION_HLSL__
#define __SCREEN_SPACE_REFLECTION_HLSL__

#include "../../../Shader/Lib/Core.hlsl"
#include "../../../Shader/Lib/Utils/CameraOpaqueTexture.hlsl"

#define REFLECTION_DISTANCE    50.0
#define STEP_COUNT  200

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

uniform float _Stride;
uniform float _Thickness;

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

inline float4 TransformWorldToHScreen(float3 positionWS, float2 size)
{
    float4 positionCS = mul(_VPMatrix, float4(positionWS, 1));
    float4 positionSS = float4(positionCS.xyz / positionCS.w, positionCS.w);
    positionSS.xy = 0.5 * positionSS.xy + 0.5;
#if UNITY_UV_STARTS_AT_TOP
    positionSS.y = 1 - positionSS.y;
#endif
    positionSS.xy *= size;
    return positionSS;
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
#if UNITY_UV_STARTS_AT_TOP
	input.texcoord.y = 1.0 - input.texcoord.y;
#endif
    float4 positionCS = float4(2.0 * input.texcoord - 1.0, depth, 1.0);
    
    // 屏幕空间转换成世界空间
    float4 positionWS = mul(UNITY_MATRIX_I_VP, positionCS);
    positionWS.xyz /= positionWS.w;

    // 计算反射向量
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS.xyz);
    half3 reflectDirWS = reflect(-viewDirWS, normalWS);
    
    // 起点和终点（世界空间）
    float3 startWS = positionWS.xyz;
    float3 endWS = startWS + REFLECTION_DISTANCE * reflectDirWS;
    
    // 齐次屏幕空间坐标
    float4 texSize = float4(_ScreenParams.xy, 1.0 / _ScreenParams.xy);
    float4 startSS = TransformWorldToHScreen(startWS, texSize.xy);
    float4 endSS = TransformWorldToHScreen(endWS, texSize.xy);
    
    float3 diff = endSS.xyz - startSS.xyz;
    float delta = lerp(abs(diff.x), abs(diff.y), step(abs(diff.x), abs(diff.y)));
    float3 increment = diff / delta;
    
    float3 p = startSS.xyz;
    half4 reflectColor = 0;
    UNITY_LOOP
    for (int i = 0; i < STEP_COUNT; ++i)
    {
        p += increment * _Stride;
        if (p.x < 0.0 || p.y < 0.0 || p.x >= texSize.x || p.y >= texSize.y || p.z <= 0.0 || p.z >= 0.999)
            break;
        
        float2 uv = p.xy * texSize.zw;
        float depth = SampleSceneDepth(uv);
        if (depth > p.z && depth - p.z < _Thickness)
        {
            reflectColor = half4(SampleScreen(uv), 1);
            break;
        }
    }

//    half4 reflectColor = 0;
//    UNITY_LOOP
//    for (int i = 0; i < STEP_COUNT; ++i)
//    {
//        float3 reflectPosWS = positionWS.xyz + reflectDirWS * _Stride * i;
//        float4 reflectPosCS = mul(_VPMatrix, float4(reflectPosWS, 1.0f));
//        float reflectDepth = reflectPosCS.w;
        
//        reflectPosCS /= reflectPosCS.w;
//        float2 reflectPosSS = reflectPosCS.xy * 0.5 + 0.5;
//#if UNITY_UV_STARTS_AT_TOP
//        reflectPosSS.y = 1.0 - reflectPosSS.y;
//#endif
//        // 判断是否超出屏幕
//        if (reflectPosSS.x < 0.0 || reflectPosSS.y < 0.0 || reflectPosSS.x > 1.0 || reflectPosSS.y > 1.0)
//            break;
        
//        float eyeDepth = GetEyeDepth(reflectPosSS);
//        if (reflectDepth > eyeDepth && abs(reflectDepth - eyeDepth) < _Thickness)
//        {
//            reflectColor = half4(SampleScreen(reflectPosSS), 1);
//            break;
//        }
//    }
    return reflectColor;
}

#endif
