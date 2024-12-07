using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class StarRailCharacterFaceShaderGUI : StarRailCharacterBaseShaderGUI
{
    private MaterialProperty m_FaceTexProp;
    private MaterialProperty m_FaceExpressionTexProp;

    protected override void FindProperties(MaterialProperty[] properties)
    {
        base.FindProperties(properties);

        m_FaceTexProp = FindProperty("_FaceTex", false);
        m_FaceExpressionTexProp = FindProperty("_FaceExpressionTex", false);
    }

    protected override void DoGUI()
    {
        DoGUI_Main();
        DoGUI_Expression();
        DoGUI_Emission();
        DoGUI_Outline();
        DoGUI_Other();
        DoGUI_UnityDefaultPart();
    }

    private void DoGUI_Main()
    {
        EditorGUILayout.BeginVertical(BaseShaderGUI.Styles.frameBgStyle);
        {
            DoGUI_Title("< Main >");

            m_Editor.TexturePropertySingleLine(StarRailCharacterBaseShaderGUI.Styles.baseMap, m_BaseMapProp, m_BaseColorProp);

            m_Editor.TexturePropertySingleLine(EditorDraw.TempContent("Face"), m_FaceTexProp);
            m_Editor.TexturePropertySingleLine(EditorDraw.TempContent("Face Expression"), m_FaceExpressionTexProp);

            DrawProperty("_ShadowColor", "Face Shadow Color", false);
            DrawProperty("_EyeShadowColor", "Eye Shadow Color", false);
        }
        EditorGUILayout.EndVertical();
    }

    private void DoGUI_Expression()
    {
        EditorGUILayout.BeginVertical(BaseShaderGUI.Styles.frameBgStyle);
        {
            DoGUI_Title("< Expression >");

            DrawProperty("_ExCheekColor", "Cheek Color", false);
            DrawProperty("_ExShyColor", "Shy Color", false);
            DrawProperty("_ExShadowColor", "Shadow Color", false);
            DrawProperty("_ExEyeShadowColor", "Eye Shadow Color", false);
        }
        EditorGUILayout.EndVertical();
    }
}
