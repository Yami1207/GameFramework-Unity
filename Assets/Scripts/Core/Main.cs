using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    [SerializeField]
    private Camera m_Camera;

    [SerializeField]
    private GameObject m_UIRoot;
    public GameObject uiRoot { get { return m_UIRoot; } }

    private void Awake()
    {
        Globals.SetMainScript(this);

        Application.targetFrameRate = 30;
        DontDestroyOnLoad(this);

        if (m_UIRoot != null)
            DontDestroyOnLoad(m_UIRoot);

        InitManager();
        MainManager.Init();
    }

    private void OnDestroy()
    {
        MainManager.Destroy();
    }

    private void Update()
    {
        InputManager.instance.Update();
        MainManager.Update();
    }

    private void LateUpdate()
    {
        MainManager.LateUpdate();
    }

    private void FixedUpdate()
    {
        MainManager.FixedUpdate();
    }

    private void InitManager()
    {
        AssetManagerSetup.Setup();
        AssetManager.instance.Init();

        CameraManager.instance.InitCamera(m_Camera);
        InputManager.instance.Init();
    }
}
