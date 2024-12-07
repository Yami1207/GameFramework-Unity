#ifndef __CHARACTER_TYPES_HLSL__
#define __CHARACTER_TYPES_HLSL__

struct HeadDirections
{
    float3 forward;
    
    float3 right;
    
    float3 up;
};

struct FaceData
{
    half eyeMask;
    
    half ao;
    
    half noseLine;
    
    half sdf;
    
    half cheek;
    
    half shy;
    
    half shadow;
};

struct RampDiffuseData
{
    half ramp;
    
    half ao;
};

struct RimLightData
{
    half3 color;
    
    // 屏幕上像素的偏移量
    half width;
    
    // 阈值
    half threshold;
    
    half edgeSoftness;
};

struct RimShadowData
{
    half3 color;
    
    half width;
    
    half intensity;
    
    half feather;
};

#endif
