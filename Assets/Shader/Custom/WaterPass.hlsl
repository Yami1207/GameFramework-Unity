#ifndef __WATER_PASS_HLSL__
#define __WATER_PASS_HLSL__

#include "WaterInput.hlsl"

inline float2 WaterDirection()
{
    return -(_Time.y * _WaterSpeed * _WaterDirection);
}

inline float DepthDistance(float3 positionWS, float4 positionSS, float3 normalWS)
{
    float2 screenUV = positionSS.xy / positionSS.w;
    float sceneZ = LinearEyeDepth(SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV), _ZBufferParams);
    float deltaDepth = sceneZ - positionSS.w;
    return deltaDepth;
}

inline float4 PackedUV(float2 uv, float2 direction, half2 tiling, half speed, half subTiling, half subSpeed)
{
    float2 baseSpeed = speed * tiling;
    float2 uv1 = uv * tiling.xy + direction * baseSpeed;
    
    float2 tiling2 = tiling * subTiling;
    float2 uv2 = (uv * tiling2) + (direction * (speed * subSpeed * tiling2));
    
    return float4(uv1, uv2);
}

inline float3 PerPixelNormal(float2 uv, float2 direction)
{
    float4 texUV = PackedUV(uv, direction, _NormalTiling.xy, _NormalSpeed, _NormalSubTiling, _NormalSubSpeed);
    return 0.5 * (UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, texUV.xy)).rgb + UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, texUV.zw)).rgb);
}

inline half SampleFoam(float2 uv, float2 direction)
{
    // SampleFoamTexture((uv + foamDistortion.xy), _FoamTiling, _FoamSubTiling, TIME, _FoamSpeed, _FoamSubSpeed, foamSlopeMask, _SlopeSpeed, _SlopeStretching, enableSlopeFoam);
    
    float4 texUV = PackedUV(uv, direction, _FoamTiling.xy, _FoamSpeed, _FoamSubTiling, _FoamSubSpeed);
    half foam = SAMPLE_TEXTURE2D(_FoamMaskMap, sampler_FoamMaskMap, texUV.xy).r + SAMPLE_TEXTURE2D(_FoamMaskMap, sampler_FoamMaskMap, texUV.zw).r;
    return saturate(foam);
}

inline half SampleIntersection(float2 uv, float deltaDepth, float2 direction, half3 normalWS)
{
    // 根据深度计算出浪花强度
    float depthDifference01 = 1 - saturate(deltaDepth / _IntersectionDistance);
    
    float2 foamDirection = direction * _IntersectionSpeed;
    float2 foamUV = uv * _IntersectionTiling + foamDirection;
    half noise = SAMPLE_TEXTURE2D(_IntersectionNoiseMap, sampler_IntersectionNoiseMap, foamUV).r;
    
    float dist = saturate(depthDifference01 / _IntersectionThreshold);
    float s = sin(direction.y - depthDifference01) * _IntersectionRippleStrength;
    noise = saturate((noise + s) * dist + dist);
    return floor(1 + (noise - _IntersectionClipping));
}

Varyings vert(Attributes input)
{
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

    Varyings output;
    output.positionCS = vertexInput.positionCS;
    output.positionWSAndFog = float4(vertexInput.positionWS, ComputeFogFactor(vertexInput.positionCS.z));
    output.positionSS = vertexInput.positionNDC;
    output.texcoord = input.texcoord;
    output.normalWS = half3(0, 1, 0);
    
    output.shadowCoord = GetShadowCoord(vertexInput.positionWS, output.positionCS);
    output.vertexSH = SampleSHVertex(output.normalWS);

    return output;
}

half4 frag(Varyings input) : SV_Target0
{
    Light mainLight = GetMainLight();
    half3 mainLightColor = mainLight.color * lerp(_G_ShadowColor, 1, mainLight.shadowAttenuation);

    float3 positionWS = input.positionWSAndFog.xyz;
    float3 viewDirectionWS = GetWorldSpaceNormalizeViewDir(positionWS);
    
    float2 uv = positionWS.xz;
    float2 direction = WaterDirection();

    // 法线
    float3 vertexNormalWS = input.normalWS;
    float3 normalTS = PerPixelNormal(uv, direction);
    float3 noiseNormalWS = normalTS.xzy;

    float deltaDepth = DepthDistance(positionWS, input.positionSS, vertexNormalWS);
    float depthDifference01 = 1 - saturate(deltaDepth / _DepthDistance);
    half3 albedo = CosineGradient(depthDifference01, _WaterPhase, _WaterAmplitude, _WaterFrequency, _WaterOffset).rgb;

    // 折射
    half3 refracColor = 0;
#if USING_REFRACTION
    {
        float2 offset = normalTS.xy;
        float4 refractionPosition = float4(input.positionSS.xy + offset * _RefractionFactor, input.positionSS.zw);
        refracColor = SampleScreen(refractionPosition);
        albedo = lerp(albedo, refracColor, depthDifference01);
    }
#endif
    
    // 泡沫
    half foam = 0;
#if USING_FOAM
    {
        half foamMask = 1 - _FoamAmount;
        foam = smoothstep(foamMask, 1 + foamMask, SampleFoam(uv + noiseNormalWS.xy * _FoamDistortion, direction));
        albedo = lerp(albedo, _FoamColor, saturate(2 * foam));
    }
#endif
    
    // 交界处泡沫
    half intersection = 0;
#if USING_INTERSECTION
    {
        intersection = SampleIntersection(uv + noiseNormalWS.xy * _IntersectionDistortion, deltaDepth, direction, noiseNormalWS);
        noiseNormalWS = lerp(noiseNormalWS, vertexNormalWS, intersection);
        albedo = lerp(albedo, _IntersectionColor, intersection);
    }
#endif
    
    BxDFContext bxdfContext = GetBxDFContext(noiseNormalWS, viewDirectionWS, mainLight.direction);
    
    // GI
    half3 bakedGI = SampleSHPixel(input.vertexSH, noiseNormalWS);
    MixRealtimeAndBakedGI(mainLight, noiseNormalWS, bakedGI);
    half3 giColor = bakedGI;
    
    // 漫反射
    half3 diffuseColor = mainLightColor;//LightingLambert(mainLightColor, bxdfContext);
       
    // 高光
    half3 specularColor = 0;
#if USING_SPECULAR
    {
        specularColor = mainLightColor * _SpecularIntensity * _SpecularColor * (1 - 2 * foam) * (1 - intersection);
    
        CustomBRDFData brdfData = GetBRDFData(albedo, 0, _SpecularShinness);
        half specular = DirectBRDFSpecular(brdfData, bxdfContext);
        specularColor *= specular;
        //float3 H = SafeNormalize(viewDirectionWS + mainLight.direction);
        //half specular = pow(max(0.001, dot(normalWS, H)), _SpecularShinness * 128);
        //specularColor = specular * _SpecularIntensity * _SpecularColor;
    }
#endif
    
    float3 finalColor = albedo * (giColor + diffuseColor) + specularColor;
    
    // 反射
    half3 reflectedColor = 0;
#if USING_REFLECTION
    {
        half3 distortNormalWS = lerp(vertexNormalWS, noiseNormalWS, _ReflectionDistort);
    
        // 环境贴图
        half3 reflectDir = reflect(-viewDirectionWS, distortNormalWS);
        half3 environmentColor = SAMPLE_TEXTURECUBE(_ReflectionCubemap, sampler_ReflectionCubemap, reflectDir).rgb;

        // 实时反射贴图
        float2 screenUV = input.positionSS.xy / input.positionSS.w + distortNormalWS.xz;
        half4 reflectedTex = SAMPLE_TEXTURE2D(_G_ReflectionTex, sampler_G_ReflectionTex, screenUV);
    
        reflectedColor = lerp(environmentColor, reflectedTex.rgb, reflectedTex.a) * _ReflectionColor;

        half NoV = saturate(dot(vertexNormalWS, viewDirectionWS));
        half fresnel = lerp(DIELECTRIC_SPEC.r, 1, Pow4(1 - NoV));
        finalColor = lerp(finalColor, reflectedColor, fresnel * _ReflectionIntensity);
    }
#endif

    // 与雾混合
    finalColor = MixFog(finalColor, input.positionWSAndFog.w);
    
    // 透明度
    half alpha = saturate(deltaDepth / _TransparentDistance + foam + intersection);

    return half4(finalColor, alpha);
}

#endif
