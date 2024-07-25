using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EnvironmentSetting;

[ExecuteInEditMode]
public class EnvironmentCore : SingletonMono<EnvironmentCore>
{
    private static class ShaderConstants
    {
        public static readonly int SHADOW_COLOR_PROP_ID = Shader.PropertyToID("_G_ShadowColor");

        public static readonly int WIND_PARAMETER_PROP_ID = Shader.PropertyToID("_G_WindParameter");

        public static readonly int WIND_WAVE_PARAMS_PROP_ID = Shader.PropertyToID("_G_WindWavePrams");

        public static readonly int WIND_WAVE_MAP_PROP_ID = Shader.PropertyToID("_G_WindWaveMap");
    }

    [SerializeField]
    private EnvironmentAsset m_Asset;

    public bool enablePixelDepthOffset { get { return m_Asset.enablePixelDepthOffset; } }

    public ObjectTrailsConfig objectTrails { get { return m_Asset != null ? m_Asset.objectTrails : null; } }

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (m_Asset == null && !Application.isPlaying)
        {
            AssetManagerSetup.Setup();
            AssetManager.instance.Init();
            m_Asset = AssetManager.instance.LoadAsset<EnvironmentAsset>(8000);
        }
#endif

        if (m_Asset != null)
            Setup();
    }

#if UNITY_EDITOR
    private void LateUpdate()
    {
        if (m_Asset != null)
            Setup();
    }
#endif

    private void Setup()
    {
        Debug.Assert(m_Asset != null);

        // 物体与地形混合
        if (enablePixelDepthOffset)
            Shader.EnableKeyword("_PIXEL_DEPTH_OFFSET_ON");
        else
            Shader.DisableKeyword("_PIXEL_DEPTH_OFFSET_ON");

        // 阴影色
        Shader.SetGlobalVector(ShaderConstants.SHADOW_COLOR_PROP_ID, m_Asset.shadowColor);

        // Wind
        SetupWind();
    }

    private void SetupWind()
    {
        EnvironmentAsset.Wind wind = m_Asset.wind;
        if (wind.type == EnvironmentAsset.WindType.Off)
        {
            Shader.EnableKeyword("_USE_WIND_OFF");
            Shader.DisableKeyword("_USE_WIND_ON");
            Shader.DisableKeyword("_USE_WIND_WAVE");
        }
        else
        {
            Shader.DisableKeyword("_USE_WIND_OFF");

            float length = Mathf.Abs(wind.directionX) + Mathf.Abs(wind.directionZ);
            Vector2 direction = new Vector2(wind.directionX, wind.directionZ);
            direction.Normalize();
            Shader.SetGlobalVector(ShaderConstants.WIND_PARAMETER_PROP_ID, new Vector4(direction.x, direction.y, wind.speed, 0.02f * wind.intensity));

            if (wind.type == EnvironmentAsset.WindType.On)
            {
                Shader.EnableKeyword("_USE_WIND_ON");
                Shader.DisableKeyword("_USE_WIND_WAVE");
            }
            else
            {
                Shader.DisableKeyword("_USE_WIND_ON");
                Shader.EnableKeyword("_USE_WIND_WAVE");

                Shader.SetGlobalVector(ShaderConstants.WIND_WAVE_PARAMS_PROP_ID, new Vector4(wind.waveSize, 1.0f / wind.waveSize, wind.waveIntensity));
                Texture2D windWaveTexture = wind.waveMap;
                Shader.SetGlobalTexture(ShaderConstants.WIND_WAVE_MAP_PROP_ID, windWaveTexture == null ? Texture2D.blackTexture : windWaveTexture);
            }
        }
    }
}
