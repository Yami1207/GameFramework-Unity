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
    
    public ObjectTrailsConfig objectTrails { get { return m_Asset != null ? m_Asset.objectTrails : null; } }

#if UNITY_EDITOR
    private void OnEnable()
    {
        if (m_Asset == null && !Application.isPlaying)
        {
            AssetManagerSetup.Setup();
            m_Asset = AssetManager.instance.LoadAsset<EnvironmentAsset>(8000);
        }
    }
#endif

    private void LateUpdate()
    {
        if (m_Asset == null)
            return;

        SetupWind(m_Asset.wind);
    }

    private void SetupWind(EnvironmentAsset.Wind wind)
    {
        Shader.SetGlobalVector(ShaderConstants.windParameterPropID, new Vector4(wind.speedX, wind.speedZ, wind.intensity));
    }
}
