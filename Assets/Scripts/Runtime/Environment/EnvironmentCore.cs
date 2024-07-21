using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EnvironmentSetting;

[ExecuteInEditMode]
public class EnvironmentCore : SingletonMono<EnvironmentCore>
{
    private static class ShaderConstants
    {
        public static readonly int windParameterPropID = Shader.PropertyToID("_G_WindParameter");
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
        if (m_Asset == null)
            return;

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

        // Wind
        {
            EnvironmentAsset.Wind wind = m_Asset.wind;

            float length = Mathf.Abs(wind.directionX) + Mathf.Abs(wind.directionZ);
            Vector2 direction = new Vector2(wind.directionX, wind.directionZ);
            direction.Normalize();
            Shader.SetGlobalVector(ShaderConstants.windParameterPropID, new Vector4(direction.x, direction.y, 0.3f * wind.speed, 0.02f * wind.intensity));
        }
    }
}
