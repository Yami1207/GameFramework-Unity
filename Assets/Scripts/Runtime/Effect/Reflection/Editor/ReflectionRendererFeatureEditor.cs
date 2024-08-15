using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ReflectionRendererFeature))]
public class ReflectionRendererFeatureEditor : Editor
{
    private SerializedProperty m_ReflectionTypeProp;
    private SerializedProperty m_QualityProp;

    private SerializedProperty m_CullingMaskProp;
    private SerializedProperty m_RenderSkyboxProp;

    private SerializedProperty m_UseDoubleMappingProp;
    private SerializedProperty m_FadeOutToEdgeProp;
    private SerializedProperty m_FillHolesProp;

    public void OnEnable()
    {
        m_ReflectionTypeProp = serializedObject.FindProperty("m_ReflectionType");
        m_QualityProp = serializedObject.FindProperty("m_Quality");

        m_CullingMaskProp = serializedObject.FindProperty("m_CullingMask");
        m_RenderSkyboxProp = serializedObject.FindProperty("m_RenderSkybox");

        m_UseDoubleMappingProp = serializedObject.FindProperty("m_UseDoubleMapping");
        m_FadeOutToEdgeProp = serializedObject.FindProperty("m_FadeOutToEdge");
        m_FillHolesProp = serializedObject.FindProperty("m_FillHoles");
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
                        EditorGUILayout.PropertyField(m_CullingMaskProp, EditorDraw.TempContent("Culling Mask"));
                        EditorGUILayout.PropertyField(m_RenderSkyboxProp, EditorDraw.TempContent("Render Skybox"));
                    }
                    break;
                case ReflectionRendererFeature.ReflectionType.ScreenSpaceReflection:
                    {

                    }
                    break;
                case ReflectionRendererFeature.ReflectionType.ScreenSpacePlanarReflection:
                    {
                        EditorGUILayout.PropertyField(m_UseDoubleMappingProp, EditorDraw.TempContent("修正渲染顺序"));
                        EditorGUILayout.PropertyField(m_FadeOutToEdgeProp, EditorDraw.TempContent("边缘淡出"));
                        EditorGUILayout.PropertyField(m_FillHolesProp, EditorDraw.TempContent("填充空洞"));
                    }
                    break;
            }
        }
        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }
}
