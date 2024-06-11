using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : Singleton<CameraManager>
{
    public enum CameraRenderer
    {
        Default = -1,
        UI = 1
    }

    private Camera m_MainCamera;
    public static Camera mainCamera { get { return CameraManager.instance.m_MainCamera; } }

    public void InitCamera(Camera camera)
    {
        m_MainCamera = camera;
        if (m_MainCamera != null)
            GameObject.DontDestroyOnLoad(m_MainCamera);
    }

    public static void SetCameraRenderer(Camera camera, CameraRenderer renderer)
    {
        UnityEngine.Rendering.Universal.UniversalAdditionalCameraData cameraData = camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (cameraData != null)
            cameraData.SetRenderer((int)renderer);
    }
}
