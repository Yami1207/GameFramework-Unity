using System;
using UnityEngine;
using UnityEngine.Rendering;

[GenerateHLSL(needAccessors = false, generateCBuffer = true)]
public struct VolumetricCloudsVariables
{
    public Vector2Int cameraColorTextureSize;

    public Vector2Int halfDepthTextureSize;

    public Vector4 cloudsTextureSize;

    public Color cloudColor;

    // 消光系数
    public Vector4 extinction;

    // 风方向
    public Vector2 windDirection;

    public float cloudMaskUVScale;

    // 恒星半径
    public float planetRadius;

    // 云层海拔高度
    public float cloudLayerAltitude;

    // 云层厚度
    public float cloudLayerThickness;

    public float shapeFactor;

    // 密度
    public float densityNoiseScale;
    public float densityMultiplier;

    // 侵蚀
    public float erosionFactor;
    public float erosionNoiseScale;

    // Lighting
    public float lightIntensity;
    public float multiScattering;
    public float powderEffectIntensity;
    public float erosionOcclusion;

    // HG参数
    public float phaseG;
    public float phaseG2;
    public float phaseBlend;

    // 淡出参数
    public float fadeInStart;
    public float fadeInDistance;

    public int numPrimarySteps;

    public int useDownsampleResolution;
}
