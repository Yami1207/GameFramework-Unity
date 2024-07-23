using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GrassShaderGUI : BaseShaderGUI
{
    private new static class Styles
    {
        public static readonly GUIContent baseMap = EditorGUIUtility.TrTextContent("Base Map",
            "Specifies the base Material and/or Color of the surface. If you’ve selected Transparent or Alpha Clipping under Surface Options, your Material uses the Texture’s alpha channel or color.");
    }

    private MaterialProperty m_BaseMapProp;
    private MaterialProperty m_BaseColorProp;

    private MaterialProperty m_EnableInteractiveProp;

    protected override void FindProperties(MaterialProperty[] properties)
    {
        base.FindProperties(properties);

        m_BaseMapProp = FindProperty("_BaseMap", false);
        m_BaseColorProp = FindProperty("_BaseColor", false);

        m_EnableInteractiveProp = FindProperty("_EnableInteractive", false);
    }

    protected override void DoGUI()
    {
        DoGUI_AlphaCutoff();
        DoGUI_Main();
        DoGUI_Interactive();
        DoGUI_Other();
        DoGUI_UnityDefaultPart();
    }

    private void DoGUI_Main()
    {
        EditorGUILayout.BeginVertical(BaseShaderGUI.Styles.frameBgStyle);
        {
            DoGUI_Title("< Main >");

            m_Editor.TexturePropertySingleLine(Styles.baseMap, m_BaseMapProp, m_BaseColorProp);
            DrawProperty("_GrassTipColor", "草尖颜色", false);
            DrawProperty("_GrassShadowColor", "阴影色", false);

            DrawProperty("_Roughness", "粗糙度", false);
            DrawProperty("_ReflectionIntensity", "反射强度", false);
        }
        EditorGUILayout.EndVertical();
    }

    private void DoGUI_AlphaCutoff()
    {
        EditorGUILayout.BeginVertical(BaseShaderGUI.Styles.frameBgStyle);
        {
            DoGUI_Title("< 透明通道裁剪 >");
            GUILayout.Label("错误使用这个功能将会使游戏效率下降！", EditorStyles.centeredGreyMiniLabel);
            DrawProperty("_UseAlphaCutoff", "使用透明通道裁剪", false);
            DrawProperty("_AlphaCutoff", "裁剪Alpha值", false);
        }
        EditorGUILayout.EndVertical();
    }

    private void DoGUI_Interactive()
    {
        EditorGUILayout.BeginVertical(BaseShaderGUI.Styles.frameBgStyle);
        {
            DoGUI_Title("< 与角色互动 >");
            DrawProperty(m_EnableInteractiveProp, "开启互动");
            if (m_EnableInteractiveProp.floatValue > 0.5f)
                DrawProperty("_GrassPushStrength", "推力强度", false);
        }
        EditorGUILayout.EndVertical();
    }

    private void DoGUI_Other()
    {
        EditorGUILayout.BeginVertical(BaseShaderGUI.Styles.frameBgStyle);
        {
            DoGUI_Title("< Other >");
            DrawProperty("_Cull", "三角型正反面裁剪", false);
            DrawProperty("_EnableWind", "风动效果", false);
        }
        EditorGUILayout.EndVertical();
    }
}
