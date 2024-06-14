using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteInEditMode]
public class EnvironmentSetting : SingletonMono<EnvironmentSetting>
{
    private static class ShaderConstants
    {
        public static readonly int kWindParameterPropID = Shader.PropertyToID("_G_WindParameter");
    }

    [Serializable]
    public struct WindSetting
    {
        public float speedX, speedZ;
        public float intensity;
    }

    [SerializeField]
    private CloudSetting m_CloudSetting;
    public CloudSetting cloud { get { return m_CloudSetting; } }

    [SerializeField]
    private WindSetting m_WindSetting;

    //public EnvironmentSetting()
    //{
    //    m_CloudSetting = new CloudSetting();
    //    m_CloudSetting.enabled = true;
    //    m_CloudSetting.height = 200.0f;
    //    m_CloudSetting.thickness = 10.0f;
    //    m_CloudSetting.setpCount = 20;

    //    m_WindSetting = new WindSetting() { speedX = 1, speedZ = 1, intensity = 1 };
    //}

    private void OnEnable()
    {
        LoadCloudSetting();
    }

    private void LateUpdate()
    {
        Shader.SetGlobalVector(ShaderConstants.kWindParameterPropID, new Vector4(m_WindSetting.speedX, m_WindSetting.speedZ, m_WindSetting.intensity));
    }

    private void LoadCloudSetting()
    {
        m_CloudSetting.enabled = true;
        m_CloudSetting.color = Color.white;
    }
}
