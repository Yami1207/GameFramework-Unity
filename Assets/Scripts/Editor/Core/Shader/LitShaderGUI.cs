using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal.VR;
using UnityEngine;

public class LitShaderGUI : BaseShaderGUI
{
    private new static class Styles
    {
        public static readonly GUIContent baseMap = EditorGUIUtility.TrTextContent("Base Map",
            "Specifies the base Material and/or Color of the surface. If you’ve selected Transparent or Alpha Clipping under Surface Options, your Material uses the Texture’s alpha channel or color.");

        public static readonly GUIContent normalMapText = EditorGUIUtility.TrTextContent("Normal Map",
            "Designates a Normal Map to create the illusion of bumps and dents on this Material's surface.");

        public static readonly GUIContent maskTex = EditorGUIUtility.TrTextContent("Mask Tex", "");
    }

    private MaterialProperty m_BaseMapProp;
    private MaterialProperty m_BaseColorProp;

    private MaterialProperty m_BumpMapProp;
    private MaterialProperty m_BumpMapScaleProp;

    protected override void FindProperties(MaterialProperty[] properties)
    {
        base.FindProperties(properties);

        m_BaseMapProp = FindProperty("_BaseMap", false);
        m_BaseColorProp = FindProperty("_BaseColor", false);

        m_BumpMapProp = FindProperty("_BumpMap", false);
        m_BumpMapScaleProp = FindProperty("_BumpScale", false);
    }

    protected override void DoGUI()
    {
        DoGUI_Main();
        DoGUI_PBR();
        DoGUI_Emission();
        DoGUI_PDO();
        DoGUI_UnityDefaultPart();
    }

    private void DoGUI_Main()
    {
        EditorGUILayout.BeginVertical(BaseShaderGUI.Styles.frameBgStyle);
        {
            DoGUI_Title("< Main >");
            m_Editor.TexturePropertySingleLine(Styles.baseMap, m_BaseMapProp, m_BaseColorProp);
            m_Editor.TexturePropertySingleLine(Styles.normalMapText, m_BumpMapProp, m_BumpMapProp.textureValue != null ? m_BumpMapScaleProp : null);
        }
        EditorGUILayout.EndVertical();
    }

    private void DoGUI_PBR()
    {
        EditorGUILayout.BeginVertical(BaseShaderGUI.Styles.frameBgStyle);
        {
            DoGUI_Title("< PBR >");
            DrawProperty("_Metallic", "金属强", false);
            DrawProperty("_Smoothness", "平滑度", false);
        }
        EditorGUILayout.EndVertical();
    }

    private void DoGUI_Emission()
    {
        EditorGUILayout.BeginVertical(BaseShaderGUI.Styles.frameBgStyle);
        {
            DoGUI_Title("< 自发光 >");
            DrawProperty("_EmissionColor", "颜色", false);
            DrawProperty("_EmissionIntensity", "强度", false);
        }
        EditorGUILayout.EndVertical();
    }

    private void DoGUI_PDO()
    {
        EditorGUILayout.BeginVertical(BaseShaderGUI.Styles.frameBgStyle);
        {
            DoGUI_Title("< 地形融合 >");

            bool lastEnablePDO = m_TargetMaterial.IsKeywordEnabled("_USE_PDO");
            bool enablePDO = EditorGUILayout.Toggle("开启地形融合", lastEnablePDO);
            if (lastEnablePDO != enablePDO)
            {
                if (enablePDO)
                    m_TargetMaterial.EnableKeyword("_USE_PDO");
                else
                    m_TargetMaterial.DisableKeyword("_USE_PDO");
            }

            if (enablePDO)
                DrawProperty("_DepthDiffer", "深度差", false);
        }
        EditorGUILayout.EndVertical();
    }
}
