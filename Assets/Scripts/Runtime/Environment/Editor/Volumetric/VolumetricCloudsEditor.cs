using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[CustomEditor(typeof(VolumetricClouds))]
public class VolumetricCloudsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.BeginVertical();
        {
            DoGUI_General();
            DoGUI_CloudLayer();
            DoGUI_Shape();
            DoGUI_Phase();
            DoGUI_Lighting();
            DoGUI_Quality();
        }
        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }

    private void DoGUI_General()
    {
        EditorGUIHelper.DrawTitleGUI("General");

        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_EarthRadiusKm"), EditorDraw.TempContent("地球半径(Km)"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_EarthCurvature"), EditorDraw.TempContent("地球曲率"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_WindDirection"), EditorDraw.TempContent("风方向"));
    }

    private void DoGUI_CloudLayer()
    {
        EditorGUIHelper.DrawTitleGUI("Cloud");

        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_CloudColor"), EditorDraw.TempContent("颜色"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_CloudMaskTexture"), EditorDraw.TempContent("Cloud Map"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_CloudMaskUVScale"), EditorDraw.TempContent("Cloud Map UV Scale"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_CloudLutTexture"), EditorDraw.TempContent("云层分布图"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_CloudLayerAltitude"), EditorDraw.TempContent("云层海拔高度"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_CloudLayerThickness"), EditorDraw.TempContent("云层厚度"));
    }

    private void DoGUI_Shape()
    {
        EditorGUIHelper.DrawTitleGUI("Shape");

        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ShapeFactor"), EditorDraw.TempContent("Shape Factor"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_DensityNoiseTexture"), EditorDraw.TempContent("Density Noise Texture"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_DensityNoiseScale"), EditorDraw.TempContent("Density Noise Scale"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_DensityMultiplier"), EditorDraw.TempContent("Density Multiplier"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ErosionFactor"), EditorDraw.TempContent("Erosion Factor"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ErosionNoiseTexture"), EditorDraw.TempContent("Erosion Noise Texture"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ErosionNoiseScale"), EditorDraw.TempContent("Erosion Noise Scale"));
    }

    private void DoGUI_Lighting()
    {
        EditorGUIHelper.DrawTitleGUI("Lighting");

        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_LightIntensity"), EditorDraw.TempContent("Light Intensity"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_MultiScattering"), EditorDraw.TempContent("Multi Scattering"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_PowderEffectIntensity"), EditorDraw.TempContent("Powder Effect Intensity"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Extinction"), EditorDraw.TempContent("Extinction"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ExtinctionScale"), EditorDraw.TempContent("Extinction Scale"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ErosionOcclusion"), EditorDraw.TempContent("Erosion Occlusion"));
    }

    private void DoGUI_Phase()
    {
        EditorGUIHelper.DrawTitleGUI("Phase");

        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_PhaseG"), EditorDraw.TempContent("PhaseG"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_PhaseG2"), EditorDraw.TempContent("PhaseG2"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_PhaseBlend"), EditorDraw.TempContent("Phase Blend"));
    }

    private void DoGUI_Quality()
    {
        EditorGUIHelper.DrawTitleGUI("Quality");

        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_EnbaleDownsampleResolution"), EditorDraw.TempContent("Downsample Resolution"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_NumPrimarySteps"), EditorDraw.TempContent("Num Primary Steps"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FadeInStart"), EditorDraw.TempContent("Fade In Start"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FadeInDistance"), EditorDraw.TempContent("Fade In Distance"));
    }
}
