#ifndef __VOLUMETRIC_CLOUDS_VARIABLES_HLSL__
#define __VOLUMETRIC_CLOUDS_VARIABLES_HLSL__

CBUFFER_START(VolumetricCloudsVariables)
    uint2 _CameraColorTextureSize;

    // 降采样深度图宽高
    uint2 _HalfDepthTextureSize;

    // x: width y: height z: 1 / width w: 1 / height
    float4 _CloudsTextureSize;

    // 云颜色
    float4 _CloudColor;

    // 消光系数
    float4 _Extinction;

    // 风方向
    float2 _WindDirection;

    float _CloudMaskUVScale;

    // 恒星半径
    float _PanetRadius;

    // 云层海拔高度
    float _CloudLayerAltitude;

    // 云层厚度
    float _CloudLayerThickness;

    float _ShapeFactor;
    
    // 密度參數
    float _DensityNoiseScale;
    float _DensityMultiplier;

    // 侵蚀參數
    float _ErosionFactor;
    float _ErosionNoiseScale;

    // Lighting
    float _LightIntensity;
    float _MultiScattering;
    float _PowderEffectIntensity;
    float _ErosionOcclusion;

    // HG1的相位参数
    float _PhaseG;

    // HG2的相位参数
    float _PhaseG2;

    // HG方程的混合比例
    float _PhaseBlend;

    float _FadeInStart;
    float _FadeInDistance;

    int _NumPrimarySteps;

    int _UseDownsampleResolution;
CBUFFER_END

Texture2D<float4> _CloudMaskTexture;
SamplerState sampler_CloudMaskTexture;

Texture2D<float4> _CloudLutTexture;
SamplerState sampler_CloudLutTexture;

Texture3D<float4> _DensityNoiseTexture;
SamplerState sampler_DensityNoiseTexture;

uniform Texture3D<float4> _ErosionNoiseTexture;
SamplerState sampler_ErosionNoiseTexture;

#endif
