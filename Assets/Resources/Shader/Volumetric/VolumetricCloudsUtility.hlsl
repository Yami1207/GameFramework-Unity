#ifndef __VOLUMETRIC_CLOUDS_UTILITY_HLSL__
#define __VOLUMETRIC_CLOUDS_UTILITY_HLSL__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#include "VolumetricCloudsTypes.hlsl"

#define LIGHT_MARCH_STEP 8

// 多散射阶数
#define NUM_MULTI_SCATTERING_OCTAVES 2

#define CLOUD_DENSITY_TRESHOLD 0.01

#define MIN_EROSION_DISTANCE 3000.0
#define MAX_EROSION_DISTANCE 100000.0

#define CLOUD_LUT_MIP_OFFSET 1.0

#define CLOUD_DETAIL_MIP_OFFSET 0.0

// 噪声缩放因子
#define NOISE_TEXTURE_NORMALIZATION_FACTOR 0.00001

inline float Remap(float x, float a, float b, float c, float d)
{
    return c + (((x - a) / (b - a)) * (d - c));
}

// HG相函数
// http://www.pbr-book.org/3ed-2018/Volume_Scattering/Phase_Functions.html
inline float HenyeyGreensteinPhase(float g, float cosTheta)
{
    float g2 = g * g;
    float numer = 1.0 - g2;
    float denom = 1.0 + g2 + 2.0 * g * cosTheta;
    return numer / (12.56637 * PositivePow(denom, 1.5));
}

// 双瓣相位函数
inline float SamplePhaseFunction(float cosTheta, float phaseG, float phaseG2, float phaseBlend, float factor)
{
    // 前向与后向散射相位进行插值
    phaseG = clamp(phaseG, -0.999, 0.999);
    phaseG2 = clamp(phaseG2, -0.999, 0.999);
    float forwardPhase = HenyeyGreensteinPhase(factor * phaseG, cosTheta);
    float backwardPhase = HenyeyGreensteinPhase(factor * phaseG2, cosTheta);
    //return forwardPhase + backwardPhase;
    return lerp(forwardPhase, backwardPhase, phaseBlend);
}

// SIG2015的“Powder Effect”(糖粉效应)
inline float PowderEffect(float density, float cosAngle, float intensity)
{
    //float powder = 1.0 - exp(-density * 2.0);
    //return lerp(1.0, powder, saturate((-cosAngle * 0.5) + 0.5));
    
    float powderEffect = 1.0 - exp(-density * 4.0);
    powderEffect = saturate(powderEffect * 2.0);
    return lerp(1.0, lerp(1.0, powderEffect, smoothstep(0.5, -0.5, cosAngle)), intensity);
}

// Ray-Sphere相交
inline int RayIntersectSphereSolution(float3 origin, float3 direction, float4 sphere, out float2 solutions)
{
    solutions = (float2) 0;
    int numSolutions = 0;
    float3 pos = origin - sphere.xyz;
    
    // 二次方程式参数
    float3 quadraticCoef;
    quadraticCoef.x = dot(direction, direction);
    quadraticCoef.y = 2 * dot(direction, pos);
    quadraticCoef.z = dot(pos, pos) - (sphere.w * sphere.w);
    
    // 解二次方程式
    float discriminant = quadraticCoef.y * quadraticCoef.y - 4.0 * quadraticCoef.x * quadraticCoef.z;
    if (discriminant >= 0.0)
    {
        numSolutions = 2;
        
        float sqrtDiscriminant = sqrt(discriminant);
        solutions = (-quadraticCoef.y + float2(-1, 1) * sqrtDiscriminant) / (2 * quadraticCoef.x);
        solutions = float2(min(solutions.x, solutions.y), max(solutions.x, solutions.y));
        if (solutions.x < 0.0)
        {
            solutions.x = solutions.y;
            --numSolutions;
        }
        
        if (solutions.y < 0.0)
            --numSolutions;
    }
    return numSolutions;
}

inline bool RayIntersectCloudSolution(in CloudRay ray, in CloudLayerParams cloudLayerParams, out RayMarchRange rayMarchRange)
{    
    float2 intersectionInter, intersectionOuter;
    int numInterInner = RayIntersectSphereSolution(ray.origin, ray.direction, float4(cloudLayerParams.center, cloudLayerParams.bottomRadius), intersectionInter);
    int numInterOuter = RayIntersectSphereSolution(ray.origin, ray.direction, float4(cloudLayerParams.center, cloudLayerParams.topRadius), intersectionOuter);
    bool intersect = numInterInner > 0 || numInterOuter > 0;
    
    rayMarchRange = (RayMarchRange) 0;
    if (intersect)
    {
        rayMarchRange.start = intersectionInter.x;
        rayMarchRange.distance = intersectionOuter.x - intersectionInter.x;
    }
    return intersect;
}

// 定点是否在云层内部
//inline bool PointInsideCloudVolume(float3 positionWS)
//{
//    float toEarthCenter = dot(positionWS, positionWS);
//    return toEarthCenter >= _CloudRangeSquared.x && toEarthCenter <= _CloudRangeSquared.y;
//}

// 云的相对高度(范围：0-1)
inline float EvaluateNormalizedCloudHeight(float3 positionWS, in CloudLayerParams cloudLayerParams)
{
    return saturate((length(positionWS - cloudLayerParams.center) - cloudLayerParams.bottomRadius) * cloudLayerParams.toNormAltitude);
}

inline float DensityFadeValue(float distanceToCamera)
{
    float attenuation = 1 - saturate((distanceToCamera - _FadeInStart) / (_FadeInStart + _FadeInDistance));
    return Pow4(attenuation);
}

inline float ErosionMipOffset(float distanceToCamera)
{
    return lerp(0.0, 4.0, saturate((distanceToCamera - MIN_EROSION_DISTANCE) / (MAX_EROSION_DISTANCE - MIN_EROSION_DISTANCE)));
}

inline float3 AnimateDensityNoisePosition(float3 positionWS, float3 offset)
{
    positionWS.y += (positionWS.x / 3.0 + positionWS.z / 7.0);
    return positionWS + 0.3 * offset;
}

inline float3 AnimateErosionNoisePosition(float3 positionWS, float3 offset)
{
    return positionWS - 0.1 * offset;
}

inline EnvironmentLighting EvaluateEnvironmentLighting(in CloudRay ray)
{
    EnvironmentLighting lighting;
    lighting.sunDirection = normalize(_MainLightPosition.xyz);
    lighting.sunColor = _MainLightColor.rgb * _LightIntensity;
    
    // 天光与地面光
    lighting.ambientTermTop = float3(0, 0, 0);
    lighting.ambientTermBottom = float3(0, 0, 0);
    
    // 射线与主光夹角cos值
    lighting.cosAngle = dot(ray.direction, lighting.sunDirection);
    
    // 基于亨利·格林斯坦相位函数模拟米氏散射
    lighting.phase.x = SamplePhaseFunction(lighting.cosAngle, _PhaseG, _PhaseG2, _PhaseBlend, PositivePow(_MultiScattering, 0));
    lighting.phase.y = SamplePhaseFunction(lighting.cosAngle, _PhaseG, _PhaseG2, _PhaseBlend, PositivePow(_MultiScattering, 1));

    return lighting;
}

inline void GetCloudCoverageData(float3 positionWS, out CloudCoverageData data)
{
    float2 uv = positionWS.xz * _CloudMaskUVScale;
    float4 cloudMapData = _CloudMaskTexture.SampleLevel(sampler_CloudMaskTexture, uv, 0);
    data.coverage = cloudMapData.r;
    data.cloudType = pow(max(0.001, cloudMapData.g), 0.25);
    data.rainClouds = cloudMapData.b;
    data.maxCloudHeight = cloudMapData.w;
}

inline CloudProperty EvaluateCloudProperty(float3 positionWS, float noiseMipOffset, float erosionMipOffset, float lightSampling, in CloudLayerParams cloudLayerParams)
{
    float3 offset = _Time.y * float3(_WindDirection.x, 0, _WindDirection.y);
    
    CloudProperty property = (CloudProperty) 0;    
    property.height = EvaluateNormalizedCloudHeight(positionWS, cloudLayerParams);
    
    CloudCoverageData cloudCoverageData;
    GetCloudCoverageData(positionWS, cloudCoverageData);
    
    float4 mask = _CloudLutTexture.SampleLevel(sampler_CloudLutTexture, float2(cloudCoverageData.cloudType, property.height), CLOUD_LUT_MIP_OFFSET);
    float conservativeDensity = mask.r * cloudCoverageData.coverage;
    if (conservativeDensity > CLOUD_DENSITY_TRESHOLD && cloudCoverageData.maxCloudHeight >= property.height)
    {
        float shapeFactor = lerp(0.1, 1.0, _ShapeFactor) * mask.g;
        float erosionFactor = _ErosionFactor * mask.g;

        // 密度
        float3 uvw = AnimateDensityNoisePosition(positionWS, offset) * _DensityNoiseScale * NOISE_TEXTURE_NORMALIZATION_FACTOR;
        float density = _DensityNoiseTexture.SampleLevel(sampler_DensityNoiseTexture, uvw, noiseMipOffset).r;
        density = lerp(1.0, density, shapeFactor);
        float baseCloud = 1.0 - conservativeDensity * (1.0 - shapeFactor);
        baseCloud = saturate(Remap(density, baseCloud, 1.0, 0.0, 1.0)) * cloudCoverageData.coverage * cloudCoverageData.coverage;
        
        // 侵蚀
        uvw = AnimateErosionNoisePosition(positionWS, offset) * _ErosionNoiseScale * NOISE_TEXTURE_NORMALIZATION_FACTOR;
        float erosion = 1.0 - _ErosionNoiseTexture.SampleLevel(sampler_ErosionNoiseTexture, uvw, CLOUD_DETAIL_MIP_OFFSET + erosionMipOffset).r;
        erosion = lerp(0.0, erosion, 0.75 * erosionFactor * cloudCoverageData.coverage.x);

        baseCloud = Remap(baseCloud, erosion, 1.0, 0.0, 1.0);
        baseCloud -= lightSampling * erosionFactor * 0.1;
        baseCloud = max(0, baseCloud);
        
        // 环境光遮挡权重
        property.ambientOcclusion = mask.b;
        float ambientOcclusionBlend = saturate(1.0 - max(erosionFactor, shapeFactor) * 0.5);
        property.ambientOcclusion = lerp(1.0, property.ambientOcclusion, ambientOcclusionBlend);
        property.ambientOcclusion = saturate(property.ambientOcclusion - sqrt(erosion * _ErosionOcclusion));
        
        // 
        property.density = baseCloud * _DensityMultiplier;
        property.absorption = lerp(0.04, 0.12, cloudCoverageData.rainClouds);
    }
    else
    {
        property.density = 0.0;
        property.absorption = 0.0;
    }
    
    return property;
}

// 返回消光系数
inline float3 SampleExtinctionCoefficients()
{
    return _Extinction.rgb;
}

inline float3 EvaluateLightLuminance(float3 positionWS, float powderEffect, in EnvironmentLighting lighting, in CloudLayerParams cloudLayerParams)
{
    float3 luminance = float3(0.0, 0.0, 0.0);

    // 求去交点
    float2 intersection;
    if (RayIntersectSphereSolution(positionWS, lighting.sunDirection, float4(cloudLayerParams.center, cloudLayerParams.topRadius), intersection) > 0)
    {
        const float totalLightDistance = clamp(intersection.x, 0.0, 1000.0 * LIGHT_MARCH_STEP) + 5.0;
        const float intervalSize = totalLightDistance / LIGHT_MARCH_STEP;
        const float3 extinctionCoefficients = SampleExtinctionCoefficients();
        const float noiseMipOffset = 3.0 / LIGHT_MARCH_STEP;
        
        // 计算采样点到光源总浓度
        float totalDensity = 0;
        for (int i = 0; i < LIGHT_MARCH_STEP; i++)
        {
            float dist = intervalSize * (0.25 + i);
            float3 currentPositionWS = positionWS + dist * lighting.sunDirection;
            CloudProperty cloudProperty = EvaluateCloudProperty(currentPositionWS, i * noiseMipOffset, 0.0, 1.0, cloudLayerParams);
            totalDensity += max(cloudProperty.density * cloudProperty.absorption, 1e-6);
        }

        // 多重散射
        float3 sunColorPowderEffect = lighting.sunColor * powderEffect;
        float3 extinction = intervalSize * extinctionCoefficients * totalDensity;
        for (int o = 0; o < NUM_MULTI_SCATTERING_OCTAVES; o++)
        {
            float factor = PositivePow(_MultiScattering, o);
            float3 tranmittance = exp(-extinction * factor);
            luminance += tranmittance * sunColorPowderEffect * lighting.phase[o] * factor;
        }
    }

    return luminance;
}

inline VolumetricRayResult TraceVolumetricRay(in CloudRay ray, in CloudLayerParams cloudLayerParams)
{
    VolumetricRayResult result;
    result.scattering = float3(0, 0, 0);
    result.transmittance = 1;
    
    RayMarchRange rayMarchRange;
    if (RayIntersectCloudSolution(ray, cloudLayerParams, rayMarchRange))
    {
        if (ray.maxRayLength >= rayMarchRange.start)
        {
            const float3 extinctionCoefficients = SampleExtinctionCoefficients();
            EnvironmentLighting lighting = EvaluateEnvironmentLighting(ray);
            
            // 步进起点与终点
            float totalDistance = min(rayMarchRange.distance, ray.maxRayLength - rayMarchRange.start);
            float3 rayMarchStartPos = ray.origin + rayMarchRange.start * ray.direction;
            //float3 rayMarchEndPos = rayMarchStartPos + totalDistance * ray.direction;
    
            // 步进距离
            const float stepSize = totalDistance / (float) _NumPrimarySteps;
            const float3 stepVec = stepSize * ray.direction;

            bool activeSampling = true;
            int sequentialEmptySamples = 0;
            float currentDistance = 0;
            float3 currentPositionWS = rayMarchStartPos;
            
            for (int i = 0; i < _NumPrimarySteps; ++i)
            {
                if (currentDistance >= totalDistance)
                    break;
                
                float distanceToCamera = rayMarchRange.start + currentDistance;
                float densityAttenuation = DensityFadeValue(distanceToCamera);
                float erosionMipOffset = ErosionMipOffset(distanceToCamera);
                
                if (activeSampling)
                {
                    CloudProperty cloudProperty = EvaluateCloudProperty(currentPositionWS, 0.0, erosionMipOffset, 0.0, cloudLayerParams);
                    cloudProperty.density *= densityAttenuation;
                    if (cloudProperty.density > CLOUD_DENSITY_TRESHOLD)
                    {
                        float powderEffect = PowderEffect(cloudProperty.density, lighting.cosAngle, _PowderEffectIntensity);
                        float extinction = cloudProperty.density * cloudProperty.absorption;
                        float transmittance = exp(-extinction * stepSize);
                    
                        // 光亮度
                        float3 luminance = EvaluateLightLuminance(currentPositionWS, powderEffect, lighting, cloudLayerParams);

                        // 环境光
                        luminance += lerp(lighting.ambientTermBottom, lighting.ambientTermTop, cloudProperty.height) * cloudProperty.ambientOcclusion;
                    
                        // 云的散射能量及透射率
                        float3 scattering = luminance - luminance * transmittance;
                        result.scattering += scattering * result.transmittance;
                        result.transmittance *= transmittance;
                    
                        if (result.transmittance < 0.003)
                        {
                            result.transmittance = 0;
                            break;
                        }
                        
                        sequentialEmptySamples = 0;
                    }
                    else
                    {   
                        sequentialEmptySamples++;
                        if (sequentialEmptySamples == 8)
                            activeSampling = false;
                    }
                    
                    currentPositionWS += stepVec;
                    currentDistance += stepSize;
                }
                else
                {
                    CloudProperty cloudProperty = EvaluateCloudProperty(currentPositionWS, 1.0f, 0.0, 0.0, cloudLayerParams);
                    cloudProperty.density *= densityAttenuation;
                    if (cloudProperty.density < CLOUD_DENSITY_TRESHOLD)
                    {
                        currentPositionWS += 2.0 * stepVec;
                        currentDistance += 2.0 * stepSize;
                    }
                    else
                    {
                        currentPositionWS -= stepVec;
                        currentDistance -= stepSize;
                        activeSampling = true;
                        sequentialEmptySamples = 0;
                    }
                }
            }
        }
    }

    return result;
}

#endif
