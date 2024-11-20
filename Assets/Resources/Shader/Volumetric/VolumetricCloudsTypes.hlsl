#ifndef __VOLUMETRIC_CLOUDS_TYPES_HLSL__
#define __VOLUMETRIC_CLOUDS_TYPES_HLSL__

#include "VolumetricCloudsVariables.hlsl"

#define MAX_SKYBOX_VOLUMETRIC_CLOUDS_DISTANCE 200000.0

struct CloudRay
{
    // 起点
    float3 origin;
    
    // 方向
    float3 direction;
        
    // 射线最大长度
    float maxRayLength;
    
    // 是否在云层
    bool insideClouds;
};

struct EnvironmentLighting
{
    float3 sunDirection;
    
    float3 sunColor;
    
    // 天光
    float3 ambientTermTop;
    
    // 地面光
    float3 ambientTermBottom;
    
    float cosAngle;
     
    float2 phase;
};

// 射线行进范围
struct RayMarchRange
{
    // 起点值
    float start;
    
    // 行进长度
    float distance;
};

struct VolumetricRayResult
{
    // 散射光
    float3 scattering;
    
    // 透射光
    float transmittance;
};

struct CloudLayerParams
{
    float3 center;
    
    float planetRadius;
    
    float topRadius;
    
    float bottomRadius;
    
    float toNormAltitude;
};

struct CloudProperty
{
    float density;
    
    float ambientOcclusion;
    
    float height;
    
    float absorption;
};

struct CloudCoverageData
{
    float coverage;
    
    float cloudType;
    
    float rainClouds;

    float maxCloudHeight;
};

#endif
