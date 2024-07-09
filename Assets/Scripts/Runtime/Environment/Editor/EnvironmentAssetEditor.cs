using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnvironmentAsset))]
public class EnvironmentAssetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.BeginVertical();
        {
            DoGUI_Rendering();
            DoGUI_ObjectTrails();
            DoGUI_Wind();
        }
        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }

    private void DoGUI_Rendering()
    {
        EditorGUIHelper.DrawTitleGUI("Rendering");

        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_EnablePixelDepthOffset"), EditorDraw.TempContent("启动物体与地形融合"));
    }

    #region Character Trajectory

    private void DoGUI_ObjectTrails()
    {
        EditorGUIHelper.DrawTitleGUI("Object Trails");

        var so = serializedObject.FindProperty("objectTrails");
        EditorGUILayout.PropertyField(so.FindPropertyRelative("resolution"), EditorDraw.TempContent("分辨率"));
        EditorGUILayout.PropertyField(so.FindPropertyRelative("cameraHeight"), EditorDraw.TempContent("相机偏移高度"));
        EditorGUILayout.PropertyField(so.FindPropertyRelative("cameraRange"), EditorDraw.TempContent("范围"));
        EditorGUILayout.PropertyField(so.FindPropertyRelative("cameraNear"), EditorDraw.TempContent("近平面"));
        EditorGUILayout.PropertyField(so.FindPropertyRelative("cameraFar"), EditorDraw.TempContent("远平面"));
    }

    #endregion

    #region Wind

    private void DoGUI_Wind()
    {
        EditorGUIHelper.DrawTitleGUI("Wind");

        var so = serializedObject.FindProperty("wind");
        EditorGUILayout.PropertyField(so.FindPropertyRelative("speedX"), EditorDraw.TempContent("Speed X"));
        EditorGUILayout.PropertyField(so.FindPropertyRelative("speedZ"), EditorDraw.TempContent("Speed Z"));
        EditorGUILayout.PropertyField(so.FindPropertyRelative("intensity"), EditorDraw.TempContent("Intensity"));
    }

    #endregion

    [MenuItem("Assets/Create/Game/Environment Asset")]
    public static void CreateEnvironmentAsset()
    {
        EditorHelper.CreateAsset<EnvironmentAsset>(null, false, false);
    }
}
