using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class FoliageShaderGUI : BaseShaderGUI
{
    private new static class Styles
    {
        public static readonly GUIContent baseMap = EditorGUIUtility.TrTextContent("Base Map",
            "Specifies the base Material and/or Color of the surface. If you’ve selected Transparent or Alpha Clipping under Surface Options, your Material uses the Texture’s alpha channel or color.");
    }

    private MaterialProperty m_BaseMapProp;
    private MaterialProperty m_BaseColorProp;

    private MaterialProperty m_EnableSubsurfaceScattering;
    private MaterialProperty m_UseGradientColorProp;

    protected override void FindProperties(MaterialProperty[] properties)
    {
        base.FindProperties(properties);

        m_BaseMapProp = FindProperty("_BaseMap", false);
        m_BaseColorProp = FindProperty("_BaseColor", false);

        m_EnableSubsurfaceScattering = FindProperty("_EnableSubsurfaceScattering", false);
        m_UseGradientColorProp = FindProperty("_UseGradientColor", false);
    }

    protected override void DoGUI()
    {
        DoGUI_Main();
        DoGUI_AlphaCutoff();
        DoGUI_SSS();
        DoGUI_Other();
        DoGUI_UnityDefaultPart();
    }

    private void DoGUI_Main()
    {
        EditorGUILayout.BeginVertical(BaseShaderGUI.Styles.frameBgStyle);
        {
            DoGUI_Title("< Main >");

            m_Editor.TexturePropertySingleLine(Styles.baseMap, m_BaseMapProp, m_BaseColorProp);

            DrawProperty(m_UseGradientColorProp, "颜色渐变");
            if (m_UseGradientColorProp.floatValue > 0.5f)
            {
                DrawProperty("_BaseBottomColor", "底部颜色", false);
                DrawProperty("_ColorMaskHeight", "颜色占比", false);
            }
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

    private void DoGUI_SSS()
    {
        if (!HasProperty("_SubsurfaceRadius"))
            return;

        EditorGUILayout.BeginVertical(BaseShaderGUI.Styles.frameBgStyle);
        {
            DoGUI_Title("< SSS >");

            DrawProperty(m_EnableSubsurfaceScattering, "使用次表面散射");
            if (m_EnableSubsurfaceScattering.floatValue > 0.5f)
            {
                DrawProperty("_SubsurfaceRadius", "散射半径", false);
                DrawProperty("_SubsurfaceColor", "散射颜色", false);
                DrawProperty("_SubsurfaceColorIntensity", "散射颜色强度", false);
            }
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
