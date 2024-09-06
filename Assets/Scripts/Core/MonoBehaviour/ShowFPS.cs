using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowFPS : MonoBehaviour
{
    private const float FRAME_INTERNAL = 0.5f;

    private GUIStyle m_FPSStyle;

    private Rect m_DisplayRect;

    private float m_LastFrame = 0f;

    private float m_Framer = 0f;

    private float m_FPS = 0f;

    private string m_FPSString;

    void Start()
    {
        m_LastFrame = Time.realtimeSinceStartup;

        m_FPSStyle = new GUIStyle();
        m_FPSStyle.fontSize = 24;
        m_FPSStyle.fontStyle = FontStyle.Bold;
        m_FPSStyle.padding = new UnityEngine.RectOffset(5, 0, 5, 0);

        m_DisplayRect = new Rect(5, 5, 80, 40);

        GUI.backgroundColor = Color.black;
    }

    private void Update()
    {
        ++m_Framer;
        if (Time.realtimeSinceStartup > m_LastFrame + FRAME_INTERNAL)
        {
            m_FPS = m_Framer / (Time.realtimeSinceStartup - m_LastFrame);
            m_Framer = 0;
            m_LastFrame = Time.realtimeSinceStartup;
            m_FPSString = "FPS : " + m_FPS.ToString("f2");
        }
    }

    private void OnGUI()
    {
        SetGUIColor(m_FPS);
        GUI.Label(m_DisplayRect, m_FPSString, m_FPSStyle);
    }

    private void SetGUIColor(float fps)
    {
        if (fps > 30)
            m_FPSStyle.normal.textColor = Color.green;
        else if (fps > 25 && fps <= 30)
            m_FPSStyle.normal.textColor = Color.yellow;
        else
            m_FPSStyle.normal.textColor = Color.red;
    }
}
