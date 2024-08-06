using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal.VR;
using UnityEngine;

public class WaterShaderGUI : BaseShaderGUI
{
    private static Material s_CosineGradientMaterial;
    private static Material cosineGradientMaterial
    {
        get
        {
            if (s_CosineGradientMaterial == null)
            {
                s_CosineGradientMaterial = new Material(Shader.Find("Rendering/Other/Cosine Gradient"));
                s_CosineGradientMaterial.hideFlags = HideFlags.DontSave;
            }
            return s_CosineGradientMaterial;
        }
    }

    private MaterialProperty m_WaterDirectionProp;

    private MaterialProperty m_WaterPhaseProp;
    private MaterialProperty m_WaterAmplitudeProp;
    private MaterialProperty m_WaterFrequencyProp;
    private MaterialProperty m_WaterOffsetProp;

    private MaterialProperty m_BumpMapProp;
    private MaterialProperty m_NormalTilingProp;

    private MaterialProperty m_EnableReflectionProp;
    private MaterialProperty m_EnableRefractionProp;

    private MaterialProperty m_EnableFoamProp;
    private MaterialProperty m_FoamMaskMapProp;
    private MaterialProperty m_FoamTilingProp;

    private MaterialProperty m_EnableIntersectionProp;
    private MaterialProperty m_EnableSpecularProp;

    protected override void FindProperties(MaterialProperty[] properties)
    {
        base.FindProperties(properties);

        m_WaterDirectionProp = FindProperty("_WaterDirection", false);

        m_WaterPhaseProp = FindProperty("_WaterPhase", false);
        m_WaterAmplitudeProp = FindProperty("_WaterAmplitude", false);
        m_WaterFrequencyProp = FindProperty("_WaterFrequency", false);
        m_WaterOffsetProp = FindProperty("_WaterOffset", false);

        m_BumpMapProp = FindProperty("_BumpMap", false);
        m_NormalTilingProp = FindProperty("_NormalTiling", false);

        m_EnableReflectionProp = FindProperty("_EnableReflection", false);
        m_EnableRefractionProp = FindProperty("_EnableRefraction", false);

        m_EnableFoamProp = FindProperty("_EnableFoam", false);
        m_FoamMaskMapProp = FindProperty("_FoamMaskMap", false);
        m_FoamTilingProp = FindProperty("_FoamTiling", false);

        m_EnableIntersectionProp = FindProperty("_EnableIntersection", false);
        m_EnableSpecularProp = FindProperty("_EnableSpecular", false);
    }

    protected override void DoGUI()
    {
        DoGUI_General();
        DoGUI_CosineGradient();
        DoGUI_Bump();
        DoGUI_Reflection();
        DoGUI_Refraction();
        DoGUI_Foam();
        DoGUI_Intersection();
        DoGUI_Specular();
        DoGUI_UnityDefaultPart();
    }

    private void DoGUI_General()
    {
        EditorGUILayout.BeginVertical(BaseShaderGUI.Styles.frameBgStyle);
        {
            DoGUI_Title("< General >");
            DrawProperty("_DepthDistance", "深浅度范围", false);
            DrawProperty("_TransparentDistance", "水的透明度", false);

            m_WaterDirectionProp.vectorValue = EditorGUILayout.Vector2Field("Direction", m_WaterDirectionProp.vectorValue);
            DrawProperty("_WaterSpeed", "Speed", false);
        }
        EditorGUILayout.EndVertical();
    }

    private void DoGUI_CosineGradient()
    {
        if (m_WaterPhaseProp == null || m_WaterAmplitudeProp == null || m_WaterFrequencyProp == null || m_WaterOffsetProp == null)
            return;

        EditorGUILayout.BeginVertical(BaseShaderGUI.Styles.frameBgStyle);
        {
            DoGUI_Title("< Cosine Gradient >");

            var material = cosineGradientMaterial;
            if (material != null)
            {
                material.SetVector("_Phase", m_WaterPhaseProp.vectorValue);
                material.SetVector("_Amplitude", m_WaterAmplitudeProp.vectorValue);
                material.SetVector("_Frequenc", m_WaterFrequencyProp.vectorValue);
                material.SetVector("_Offset", m_WaterOffsetProp.vectorValue);

                var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * 2.0f);
                EditorGUI.DrawPreviewTexture(rect, Texture2D.whiteTexture, material);
            }

            DrawProperty(m_WaterPhaseProp, "Phase");
            DrawProperty(m_WaterAmplitudeProp, "Amplitude");
            DrawProperty(m_WaterFrequencyProp, "Frequency");
            DrawProperty(m_WaterOffsetProp, "Offset");
        }
        EditorGUILayout.EndVertical();
    }

    private void DoGUI_Bump()
    {
        EditorGUILayout.BeginVertical(BaseShaderGUI.Styles.frameBgStyle);
        {
            DoGUI_Title("< Normal >");
            m_Editor.TexturePropertySingleLine(EditorDraw.TempContent("贴图"), m_BumpMapProp);

            m_NormalTilingProp.vectorValue = EditorGUILayout.Vector2Field("平铺", m_NormalTilingProp.vectorValue);
            DrawProperty("_NormalSpeed", "速度", false);

            GUILayout.Label("Sub-layer");
            ++EditorGUI.indentLevel;
            {
                DrawProperty("_NormalSubTiling", "平铺", false);
                DrawProperty("_NormalSubSpeed", "速度", false);
            }
            --EditorGUI.indentLevel;
        }
        EditorGUILayout.EndVertical();
    }

    private void DoGUI_Reflection()
    {
        EditorGUILayout.BeginVertical(BaseShaderGUI.Styles.frameBgStyle);
        {
            DoGUI_Title("< 反射 >");

            DrawProperty(m_EnableReflectionProp, "开启反射");
            if (m_EnableReflectionProp.floatValue > 0.5f)
            {
                DrawProperty("_ReflectionColor", "颜色", false);
                DrawProperty("_ReflectionCubemap", "反射球", false);
                DrawProperty("_ReflectionDistort", "扭曲", false);
                DrawProperty("_ReflectionIntensity", "强度", false);
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DoGUI_Refraction()
    {
        EditorGUILayout.BeginVertical(BaseShaderGUI.Styles.frameBgStyle);
        {
            DoGUI_Title("< 折射 >");

            DrawProperty(m_EnableRefractionProp, "开启折射");
            if (m_EnableRefractionProp.floatValue > 0.5f)
            {
                DrawProperty("_RefractionFactor", "强度", false);
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DoGUI_Foam()
    {
        EditorGUILayout.BeginVertical(BaseShaderGUI.Styles.frameBgStyle);
        {
            DoGUI_Title("< 泡沫 >");

            DrawProperty(m_EnableFoamProp, "开启泡沫");
            if (m_EnableFoamProp.floatValue > 0.5f)
            {
                DrawProperty("_FoamColor", "颜色", false);
                m_Editor.TexturePropertySingleLine(EditorDraw.TempContent("Mask图"), m_FoamMaskMapProp);

                DrawProperty("_FoamAmount", "泡沫量", false);
                DrawProperty("_FoamDistortion", "泡沫扰动", false);

                m_FoamTilingProp.vectorValue = EditorGUILayout.Vector2Field("平铺", m_FoamTilingProp.vectorValue);
                DrawProperty("_FoamSpeed", "速度", false);

                GUILayout.Label("Sub-layer");
                ++EditorGUI.indentLevel;
                {
                    DrawProperty("_FoamSubTiling", "平铺", false);
                    DrawProperty("_FoamSubSpeed", "速度", false);
                }
                --EditorGUI.indentLevel;
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DoGUI_Intersection()
    {
        EditorGUILayout.BeginVertical(BaseShaderGUI.Styles.frameBgStyle);
        {
            DoGUI_Title("< 交界处泡沫 >");

            DrawProperty(m_EnableIntersectionProp, "开启交界处泡沫");
            if (m_EnableIntersectionProp.floatValue > 0.5f)
            {
                DrawProperty("_IntersectionDistance", "距离", false);
                DrawProperty("_IntersectionClipping", "Cutoff", false);

                DrawProperty("_IntersectionColor", "颜色", false);

                DrawProperty("_IntersectionNoiseMap", "扰动图", false);
                DrawProperty("_IntersectionTiling", "Tiling", false);

                DrawProperty("_IntersectionThreshold", "阈值", false);
                DrawProperty("_IntersectionSpeed", "泡沫速度", false);
                DrawProperty("_IntersectionDistortion", "泡沫扰动", false);

                DrawProperty("_IntersectionRippleStrength", "涟漪强弱", false);
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DoGUI_Specular()
    {
        EditorGUILayout.BeginVertical(BaseShaderGUI.Styles.frameBgStyle);
        {
            DoGUI_Title("< 高光 >");

            DrawProperty(m_EnableSpecularProp, "使用高光");
            if (m_EnableSpecularProp.floatValue > 0.5f)
            {
                DrawProperty("_SpecularColor", "颜色", false);
                DrawProperty("_SpecularShinness", "光泽度", false);
                DrawProperty("_SpecularIntensity", "强度", false);
            }
        }
        EditorGUILayout.EndVertical();
    }
}
