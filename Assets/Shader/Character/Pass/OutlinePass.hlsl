#ifndef __CHARACTER_OUTLINE_PASS_HLSL__
#define __CHARACTER_OUTLINE_PASS_HLSL__

// 实现outline的主要代码来自https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample

#include "../../Lib/Core.hlsl"

//--------------------------------------
// 材质属性
CBUFFER_START(UnityPerMaterial)
    half4 _OutlineColor;
    half _OutlineWidth;
    half _OutlineZOffset;
CBUFFER_END

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
};

// If your project has a faster way to get camera fov in shader, you can replace this slow function to your method.
// For example, you write cmd.SetGlobalFloat("_CurrentCameraFOV",cameraFOV) using a new RendererFeature in C#.
// For this tutorial shader, we will keep things simple and use this slower but convenient method to get camera fov
inline float GetCameraFOV()
{
    //https://answers.unity.com/questions/770838/how-can-i-extract-the-fov-information-from-the-pro.html
    const float Rad2Deg = 180 / 3.1415;
    float t = unity_CameraProjection._m11;
    return atan(1.0 / t) * 2.0 * Rad2Deg;
}

inline float ApplyOutlineDistanceFadeOut(float inputMulFix)
{
    //make outline "fadeout" if character is too small in camera's view
    return saturate(inputMulFix);
}

inline float GetOutlineCameraFovAndDistanceFixMultiplier(float positionVS_Z)
{
    float cameraMulFix;
    if (unity_OrthoParams.w == 0)
    {
        ////////////////////////////////
        // Perspective camera case
        ////////////////////////////////

        // keep outline similar width on screen accoss all camera distance       
        cameraMulFix = abs(positionVS_Z);

        // can replace to a tonemap function if a smooth stop is needed
        cameraMulFix = ApplyOutlineDistanceFadeOut(cameraMulFix);

        // keep outline similar width on screen accoss all camera fov
        cameraMulFix *= GetCameraFOV();
    }
    else
    {
        ////////////////////////////////
        // Orthographic camera case
        ////////////////////////////////
        float orthoSize = abs(unity_OrthoParams.y);
        orthoSize = ApplyOutlineDistanceFadeOut(orthoSize);
        cameraMulFix = orthoSize * 50; // 50 is a magic number to match perspective camera's outline width
    }

    return cameraMulFix * 0.00005; // mul a const to make return result = default normal expand amount WS
}

inline float3 TransformPositionWSToOutlinePositionWS(float3 positionWS, float positionVS_Z, float3 normalWS)
{
    // you can replace it to your own method! Here we will write a simple world space method for tutorial reason, it is not the best method!
    float outlineExpandAmount = _OutlineWidth * GetOutlineCameraFovAndDistanceFixMultiplier(positionVS_Z);
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED) || defined(UNITY_STEREO_DOUBLE_WIDE_ENABLED)
    outlineExpandAmount *= 0.5;
#endif
    return positionWS + normalWS * outlineExpandAmount;
}

inline float4 NiloGetNewClipPosWithZOffset(float4 originalPositionCS, float viewSpaceZOffsetAmount)
{
    if (unity_OrthoParams.w == 0)
    {
        ////////////////////////////////
        //Perspective camera case
        ////////////////////////////////
        float2 ProjM_ZRow_ZW = UNITY_MATRIX_P[2].zw;
        float modifiedPositionVS_Z = -originalPositionCS.w - viewSpaceZOffsetAmount; // push imaginary vertex
        float modifiedPositionCS_Z = modifiedPositionVS_Z * ProjM_ZRow_ZW[0] + ProjM_ZRow_ZW[1];
        originalPositionCS.z = modifiedPositionCS_Z * originalPositionCS.w / (-modifiedPositionVS_Z); // overwrite positionCS.z
        return originalPositionCS;
    }
    else
    {
        ////////////////////////////////
        //Orthographic camera case
        ////////////////////////////////
        originalPositionCS.z -= viewSpaceZOffsetAmount / _ProjectionParams.z; // push imaginary vertex and overwrite positionCS.z
        return originalPositionCS;
    }
}

Varyings OutlineVertex(Attributes input)
{
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    
    float3 positionWS = TransformPositionWSToOutlinePositionWS(vertexInput.positionWS, vertexInput.positionVS.z, vertexNormalInput.normalWS);
    
    Varyings output = (Varyings) 0;
    output.positionCS = TransformWorldToHClip(positionWS);
    
#if defined(CHARACTER_FACE_PASS)
    output.positionCS = NiloGetNewClipPosWithZOffset(output.positionCS, _OutlineZOffset + 0.03);
#else
    output.positionCS = NiloGetNewClipPosWithZOffset(output.positionCS, _OutlineZOffset);
#endif

    //Varyings output = (Varyings) 0;
    //output.positionCS = vertexInput.positionCS;
    
    //// 基于法线外扩
    //float3 normalNDC = TransformWorldToHClipDir(vertexNormalInput.normalWS, true);
    //float aspect = _ScreenParams.y / _ScreenParams.x;
    //normalNDC.x *= aspect;
    //output.positionCS.xy += 0.01 * _OutlineWidth * normalNDC.xy;
    
    return output;
}

half4 OutlineFragment(Varyings input) : SV_TARGET
{
    return half4(_OutlineColor.rgb, 1);
}

#endif
