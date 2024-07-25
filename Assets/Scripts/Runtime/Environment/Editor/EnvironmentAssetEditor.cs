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

        EditorGUIHelper.LinearPropertyField(serializedObject.FindProperty("m_ShadowColor"), "阴影色");
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

        var typeProp = so.FindPropertyRelative("type");
        EditorGUILayout.PropertyField(typeProp, EditorDraw.TempContent("类型"));

        var type = (EnvironmentAsset.WindType)typeProp.intValue;
        if (type == EnvironmentAsset.WindType.Off)
            return;

        EditorGUILayout.PropertyField(so.FindPropertyRelative("directionX"), EditorDraw.TempContent("方向(X)"));
        EditorGUILayout.PropertyField(so.FindPropertyRelative("directionZ"), EditorDraw.TempContent("方向(Z)"));
        EditorGUILayout.PropertyField(so.FindPropertyRelative("speed"), EditorDraw.TempContent("风速"));

        if (type == EnvironmentAsset.WindType.On)
        {
            EditorGUILayout.PropertyField(so.FindPropertyRelative("intensity"), EditorDraw.TempContent("强度"));
        }
        else
        {
            EditorGUILayout.PropertyField(so.FindPropertyRelative("waveMap"), EditorDraw.TempContent("风浪图"));
            EditorGUILayout.PropertyField(so.FindPropertyRelative("waveSize"), EditorDraw.TempContent("风浪范围"));
            EditorGUILayout.PropertyField(so.FindPropertyRelative("waveIntensity"), EditorDraw.TempContent("风浪强度"));
        }
    }

    #endregion

    [MenuItem("Assets/Create/Game/Environment Asset")]
    public static void CreateEnvironmentAsset()
    {
        EditorHelper.CreateAsset<EnvironmentAsset>(null, false, false);
    }
}
