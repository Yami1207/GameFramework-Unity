using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ReflectionPlane))]
public class ReflectionPlaneEditor : Editor
{
    private SerializedProperty m_IsAloneProperty;
    private SerializedProperty m_QualityProperty;
    private SerializedProperty m_CullingMaskProperty;

    public void OnEnable()
    {
        m_IsAloneProperty = serializedObject.FindProperty("m_IsAlone");
        m_QualityProperty = serializedObject.FindProperty("m_Quality");
        m_CullingMaskProperty = serializedObject.FindProperty("m_CullingMask");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.BeginVertical();
        {
            EditorGUILayout.PropertyField(m_IsAloneProperty, EditorDraw.TempContent("Alone"));
            if (m_IsAloneProperty.boolValue)
            {
                EditorGUILayout.PropertyField(m_QualityProperty, EditorDraw.TempContent("Quality"));
                EditorGUILayout.PropertyField(m_CullingMaskProperty, EditorDraw.TempContent("Culling Mask"));
            }
        }
        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }
}
