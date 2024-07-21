using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentAsset : ScriptableObject
{
    [Serializable]
    public class Wind
    {
        // x方向
        public float directionX = 1.0f;

        // z方向
        public float directionZ = 0.0f;

        // 速度
        public float speed = 1.0f;

        // 强度
        public float intensity = 1.0f;
    }

    public ObjectTrailsConfig objectTrails = new ObjectTrailsConfig();

    public Wind wind = new Wind();

    [SerializeField]
    private bool m_EnablePixelDepthOffset = false;
    public bool enablePixelDepthOffset { get { return m_EnablePixelDepthOffset; } }
}
