using System;

[Serializable]
public class ObjectTrailsConfig
{
    public enum Resolution
    {
        Low = 256,
        Middle = 512,
        High = 1024
    }

    public Resolution resolution = Resolution.Middle;

    public int cameraHeight = 10;

    public int cameraRange = 20;

    public int cameraNear = 0;

    public int cameraFar = 50;
}
