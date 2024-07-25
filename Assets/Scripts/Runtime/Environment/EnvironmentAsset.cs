using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentAsset : ScriptableObject
{
    public enum WindType
    {
        Off,
        On,
        Wave
    }

    [Serializable]
    public class Wind
    {
        public WindType type = WindType.Off;

        // x方向
        public float directionX = 1.0f;

        // z方向
        public float directionZ = 0.0f;

        // 速度
        public float speed = 1.0f;

        // 强度
        public float intensity = 1.0f;

        [Range(0.01f, 100.0f)]
        public float waveSize = 20.0f;

        [Range(0.0f, 10.0f)]
        public float waveIntensity = 1.0f;

        public Texture2D waveMap;
    }

    public ObjectTrailsConfig objectTrails = new ObjectTrailsConfig();

    public Wind wind = new Wind();

    [SerializeField]
    private bool m_EnablePixelDepthOffset = false;
    public bool enablePixelDepthOffset { get { return m_EnablePixelDepthOffset; } }

    [SerializeField]
    private Vector4 m_ShadowColor = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
    public Vector4 shadowColor { get { return m_ShadowColor; } }
}
