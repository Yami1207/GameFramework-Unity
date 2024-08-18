using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ReflectionRendererFeature))]
public class ReflectionRendererFeatureEditor : Editor
{
    private SerializedProperty m_ReflectionTypeProp;
    private SerializedProperty m_QualityProp;

    public void OnEnable()
    {
        m_ReflectionTypeProp = serializedObject.FindProperty("m_ReflectionType");
        m_QualityProp = serializedObject.FindProperty("m_Quality");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.BeginVertical();
        {
            EditorGUILayout.PropertyField(m_ReflectionTypeProp, EditorDraw.TempContent("选择反射类型"));
            EditorGUILayout.PropertyField(m_QualityProp, EditorDraw.TempContent("Quality"));

            ReflectionRendererFeature.ReflectionType reflectionType = (ReflectionRendererFeature.ReflectionType)m_ReflectionTypeProp.intValue;
            switch(reflectionType)
            {
                case ReflectionRendererFeature.ReflectionType.PlanarReflection:
                    {
                        var settingProp = serializedObject.FindProperty("m_PlanarReflectionSetting");
                        EditorGUILayout.PropertyField(settingProp.FindPropertyRelative("cullingMask"), EditorDraw.TempContent("Culling Mask"));
                        EditorGUILayout.PropertyField(settingProp.FindPropertyRelative("renderSkybox"), EditorDraw.TempContent("Render Skybox"));
                    }
                    break;
                case ReflectionRendererFeature.ReflectionType.ScreenSpaceReflection:
                    {
                        var settingProp = serializedObject.FindProperty("m_ScreenSpaceReflectionSetting");
                        EditorGUILayout.PropertyField(settingProp.FindPropertyRelative("thickness"), EditorDraw.TempContent("Thickness"));
                        EditorGUILayout.PropertyField(settingProp.FindPropertyRelative("stride"), EditorDraw.TempContent("Stride"));
                    }
                    break;
                case ReflectionRendererFeature.ReflectionType.ScreenSpacePlanarReflection:
                    {
                        var settingProp = serializedObject.FindProperty("m_ScreenSpacePlanarReflectionSetting");
                        EditorGUILayout.PropertyField(settingProp.FindPropertyRelative("useDoubleMapping"), EditorDraw.TempContent("修正渲染顺序"));
                        EditorGUILayout.PropertyField(settingProp.FindPropertyRelative("fadeOutToEdge"), EditorDraw.TempContent("边缘淡出"));
                        EditorGUILayout.PropertyField(settingProp.FindPropertyRelative("fillHoles"), EditorDraw.TempContent("填充空洞"));
                    }
                    break;
            }
        }
        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }
}
