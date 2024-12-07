#ifndef __CHARACTER_FACE_PASS_HLSL__
#define __CHARACTER_FACE_PASS_HLSL__

#include "CharacterInput.hlsl"

inline FaceData GetFaceData(Varyings input)
{
    half4 faceMask = SAMPLE_TEXTURE2D(_FaceTex, sampler_FaceTex, input.texcoord.xy);
    half4 expressionMask = SAMPLE_TEXTURE2D(_FaceExpressionTex, sampler_FaceExpressionTex, input.texcoord.xy);
    
    FaceData faceData = (FaceData) 0;
    faceData.eyeMask = faceMask.r;
    faceData.ao = faceMask.g;
    faceData.noseLine = faceMask.b;
    faceData.sdf = faceMask.a;
    faceData.cheek = expressionMask.r;
    faceData.shy = expressionMask.g;
    faceData.shadow = expressionMask.b;
    return faceData;
}

inline void InitializeSurfaceData(Varyings input, FaceData faceData, HeadDirections headDirections, out CustomSurfaceData surfaceData)
{
    half4 albedoAlpha = SAMPLE_TEXTURE2D(_AlbedoTex, sampler_AlbedoTex, input.texcoord.xy) * _Color;
    
    surfaceData = (CustomSurfaceData) 0;
    surfaceData.albedo = albedoAlpha.rgb;
    surfaceData.alpha = albedoAlpha.a;
    
    // 处理鼻线
    //half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    //float3 FoV = pow(abs(dot(headDirections.forward, viewDirWS)), _NoseLinePower);
    //surfaceData.albedo = surfaceData.albedo * lerp(1, _NoseLineColor.rgb, step(1.03 - faceData.noseLine, FoV));
    
    // 处理表情
    half3 cheekColor = surfaceData.albedo * lerp(1, _ExCheekColor.rgb, faceData.cheek);
    surfaceData.albedo = lerp(surfaceData.albedo, cheekColor, _ExCheekIntensity);
    half3 shyColor = surfaceData.albedo * lerp(1, _ExShyColor.rgb, faceData.shy);
    surfaceData.albedo = lerp(surfaceData.albedo, shyColor, _ExShyIntensity);
    half3 shadowColor = surfaceData.albedo * lerp(1, _ExShadowColor.rgb, faceData.shadow);
    surfaceData.albedo = lerp(surfaceData.albedo, shadowColor, _ExShadowIntensity);
    half3 eyeShadowColor = surfaceData.albedo * lerp(1, _ExEyeShadowColor.rgb, faceData.eyeMask);
    surfaceData.albedo = lerp(surfaceData.albedo, eyeShadowColor, _ExShadowIntensity);
    
    // 自发光
#if USING_EMISSION
    surfaceData.emission = surfaceData.alpha * _EmissionIntensity;
    surfaceData.emissionColor = _EmissionColor.rgb;
#endif
    
    // 顶点色
    surfaceData.vertexColor = input.color;
}

inline void InitializeInputData(Varyings input, CustomSurfaceData surfaceData, inout CustomInputData inputData)
{
    inputData.positionWS = input.positionWS;
    inputData.normalWS = input.normalWS;

    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(inputData.positionWS);
    inputData.viewDirectionWS = viewDirWS;

	// 阴影值
    inputData.shadowCoord = GetShadowCoordInFragment(inputData.positionWS, input.shadowCoord);
    
    // 雾
    inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.fogFactorAndSH9.w);
    
    inputData.bakedGI = SampleSHPixel(input.fogFactorAndSH9.xyz, inputData.normalWS.xyz);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = SampleShadowMask();
}

inline half3 GetDiffuse(Varyings input, CustomSurfaceData surfaceData, FaceData faceData, HeadDirections headDirections, Light light)
{
    half3 shadowColor = lerp(_G_ShadowColor, 1, light.shadowAttenuation);
    
    half isRight = step(0, dot(light.direction, headDirections.right));
    half2 uv = half2(lerp(input.texcoord.x, 1.0 - input.texcoord.x, isRight), input.texcoord.y);
    half threshold = SAMPLE_TEXTURE2D(_FaceTex, sampler_FaceTex, uv).a;
    
    half FoL01 = dot(headDirections.forward, light.direction) * 0.5 + 0.5;
    half3 faceShadow = lerp(_ShadowColor.rgb, 1, floor(FoL01 + threshold) * light.shadowAttenuation); // SDF Shadow
    half3 eyeShadow = lerp(_EyeShadowColor.rgb, 1, smoothstep(0.3, 0.5, FoL01) * light.shadowAttenuation);
    half3 shadow = lerp(faceShadow, eyeShadow, faceData.eyeMask);
    return surfaceData.albedo * light.color * light.distanceAttenuation * shadow;
}

Varyings vert(Attributes input)
{
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    
    Varyings output = (Varyings) 0;
    output.positionCS = vertexInput.positionCS;
    output.texcoord = half4(input.texcoord0, input.texcoord1);
    output.positionWS = vertexInput.positionWS;
    output.normalWS = normalInput.normalWS;
    
    // sh与雾
    OUTPUT_SH(normalInput.normalWS, output.fogFactorAndSH9);
    output.fogFactorAndSH9.w = ComputeFogFactor(vertexInput.positionCS.z);
    
    // 阴影
    output.shadowCoord = GetShadowCoord(vertexInput.positionWS.xyz, output.positionCS);
    
    return output;
}

FragData frag(Varyings input)
{
    FaceData faceData = GetFaceData(input);
    HeadDirections headDirections = GetCharacterHeadDirections();

    CustomSurfaceData surfaceData;
    InitializeSurfaceData(input, faceData, headDirections, surfaceData);
    
    CustomInputData inputData = GetDefaultInputData();
    InitializeInputData(input, surfaceData, inputData);
        
    half4 shadowMask = CalculateShadowMask(inputData);
    Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, shadowMask);
    
    // 主光
    half3 mainLightColor = 0;
    {
        BxDFContext bxdfContext = GetBxDFContext(inputData, mainLight.direction);
    
        // 漫反射
        half3 diffuseColor = GetDiffuse(input, surfaceData, faceData, headDirections, mainLight);

        mainLightColor = diffuseColor;
    }
    
    // 自发光
    half3 emissionColor = 0;
#if USING_EMISSION
    {
        emissionColor = GetEmission(surfaceData, _EmissionThreshold);
    }
#endif
    
    half3 finalColor = mainLightColor + emissionColor;
    
    // 与雾混合
    finalColor = MixFog(finalColor, inputData, surfaceData);
    
    FragData output = (FragData) 0;
    output.color = half4(finalColor, surfaceData.alpha);
    output.normal = float4(input.normalWS.xyz * 0.5 + 0.5, 0.0);
    return output;
}

#endif
