using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class StarRailCharacterBaseShaderGUI : BaseShaderGUI
{
    protected new static class Styles
    {
        public static readonly GUIContent baseMap = EditorGUIUtility.TrTextContent("Base Map", "");

        public static readonly GUIContent kLightMap = EditorGUIUtility.TrTextContent("Light Map", "R:边缘光宽度 G:ao B:高光阈值 A:Ramp");
    }

    protected MaterialProperty m_BaseMapProp;
    protected MaterialProperty m_BaseColorProp;

    protected MaterialProperty m_LightMapProp;

    protected MaterialProperty m_MaterialValuesPackLUTProp;

    private MaterialProperty m_UseSpecularProp;
    private MaterialProperty m_UseRimLightProp;
    private MaterialProperty m_UseRimShadowProp;
    private MaterialProperty m_UseEmissionProp;

    protected bool useMaterialValuesPackLUT { get { return m_MaterialValuesPackLUTProp != null && m_MaterialValuesPackLUTProp.textureValue != null; } }

    protected override void FindProperties(MaterialProperty[] properties)
    {
        base.FindProperties(properties);

        m_BaseMapProp = FindProperty("_AlbedoTex", false);
        m_BaseColorProp = FindProperty("_Color", false);

        m_LightMapProp = FindProperty("_LightMap", false);

        m_MaterialValuesPackLUTProp = FindProperty("_MaterialValuesPackLUT", false);

        m_UseSpecularProp = FindProperty("_UseSpecular", false);
        m_UseRimLightProp = FindProperty("_UseRimLight", false);
        m_UseRimShadowProp = FindProperty("_UseRimShadow", false);
        m_UseEmissionProp = FindProperty("_UseEmission", false);

        CheckMaterialValuesPackLUT();
    }

    protected void CheckMaterialValuesPackLUT()
    {
        if (m_MaterialValuesPackLUTProp != null)
        {
            if(m_MaterialValuesPackLUTProp.textureValue != null)
                m_TargetMaterial.EnableKeyword("_USE_MATERIAL_VALUES_PACK_LUT");
            else
                m_TargetMaterial.DisableKeyword("_USE_MATERIAL_VALUES_PACK_LUT");
        }
    }

    protected void DoGUI_Specular()  
    {
        EditorGUILayout.BeginVertical(BaseShaderGUI.Styles.frameBgStyle);
        {
            DoGUI_Title("< Specular >");

            DrawProperty(m_UseSpecularProp, "开启高光");
            if (!useMaterialValuesPackLUT && m_UseSpecularProp.floatValue > 0.5f)
            {
                DrawProperty("_SpecularColor", "Color", false);
                DrawProperty("_SpecularIntensity", "Intensity", false);
                DrawProperty("_SpecularShininess", "Shininess", false);
                DrawProperty("_SpecularRoughness", "Roughness", false);
            }
        }
        EditorGUILayout.EndVertical();
    }

    protected void DoGUI_RimLight()
    {
        EditorGUILayout.BeginVertical(BaseShaderGUI.Styles.frameBgStyle);
        {
            DoGUI_Title("< 边缘光 >");

            DrawProperty(m_UseRimLightProp, "开启边缘光");
            if (!useMaterialValuesPackLUT && m_UseRimLightProp.floatValue > 0.5f)
            {
                DrawProperty("_RimColor", "Color", false);
                DrawProperty("_RimWidth", "Width", false);
                DrawProperty("_RimLightThreshold", "Threshold", false);
                DrawProperty("_RimLightEdgeSoftness", "Edge Softness", false);
            }
        }
        EditorGUILayout.EndVertical();
    }

    protected void DoGUI_RimShadow()
    {
        EditorGUILayout.BeginVertical(BaseShaderGUI.Styles.frameBgStyle);
        {
            DoGUI_Title("< Rim Shadow >");

            DrawProperty(m_UseRimShadowProp, "Use Rim Shadow");
            if (!useMaterialValuesPackLUT && m_UseRimShadowProp.floatValue > 0.5f)
            {
                DrawProperty("_RimShadowColor", "Color", false);
                DrawProperty("_RimShadowIntensity", "Intensity", false);
                DrawProperty("_RimShadowWidth", "Width", false);
                DrawProperty("_RimShadowFeather", "Feather", false);
            }
        }
        EditorGUILayout.EndVertical();
    }

    protected void DoGUI_Emission()
    {
        EditorGUILayout.BeginVertical(BaseShaderGUI.Styles.frameBgStyle);
        {
            DoGUI_Title("< Emission >");

            DrawProperty(m_UseEmissionProp, "开启自发光");
            if (m_UseEmissionProp.floatValue > 0.5f)
            {
                DrawProperty("_EmissionColor", "颜色", false);
                DrawProperty("_EmissionThreshold", "阈值", false);
                DrawProperty("_EmissionIntensity", "强度", false);
            }
        }
        EditorGUILayout.EndVertical();
    }

    protected void DoGUI_Outline()
    {
        EditorGUILayout.BeginVertical(BaseShaderGUI.Styles.frameBgStyle);
        {
            DoGUI_Title("< Outline >");

            DrawProperty("_OutlineColor", "颜色", false);
            DrawProperty("_OutlineWidth", "宽度", false);
            DrawProperty("_OutlineZOffset", "外扩偏移值", false);
        }
        EditorGUILayout.EndVertical();
    }

    protected void DoGUI_Other()
    {
        EditorGUILayout.BeginVertical(BaseShaderGUI.Styles.frameBgStyle);
        {
            DoGUI_Title("< Other >");
            DrawProperty("_ZWrite", "深度写入", false);
            DrawProperty("_SrcBlend", "Source Blend", false);
            DrawProperty("_DstBlend", "Dest Blend", false);
            DrawProperty("_CullMode", "三角型正反面裁剪", false);
        }
        EditorGUILayout.EndVertical();
    }
}
