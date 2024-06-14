using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentAsset : ScriptableObject
{
    [Serializable]
    public class Wind
    {
        public float speedX = 0.1f;
        public float speedZ = 0.1f;
        public float intensity = 1;
    }

    public ObjectTrailsConfig objectTrails = new ObjectTrailsConfig();

    public Wind wind = new Wind();
}
