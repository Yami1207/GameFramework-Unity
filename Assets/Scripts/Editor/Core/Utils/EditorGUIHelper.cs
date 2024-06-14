using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class EditorGUIHelper
{
    private static class Styles
    {
        public static readonly GUIStyle muiltLine = new GUIStyle(EditorStyles.label);
        public static readonly GUIStyle frameBgStyle = new GUIStyle("HelpBox");
        public static readonly GUIStyle toolbarStyle = new GUIStyle("preToolbar");
        public static readonly GUIStyle toolbarTitleStyle = new GUIStyle("preToolbar");
        public static readonly GUIStyle foldoutHeaderStyle = new GUIStyle("ShurikenModuleTitle");

        static Styles()
        {
            foldoutHeaderStyle.font = new GUIStyle(EditorStyles.boldLabel).font;
            foldoutHeaderStyle.border = new RectOffset(15, 7, 4, 4);
            foldoutHeaderStyle.fixedHeight = 22;
            foldoutHeaderStyle.contentOffset = new Vector2(20f, -2f);

            muiltLine.wordWrap = true;
        }
    }

    public static void DrawTitleGUI(string title)
    {
        EditorGUILayout.BeginHorizontal(Styles.toolbarStyle);
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label(title, Styles.toolbarTitleStyle);
            GUILayout.FlexibleSpace();
        }
        EditorGUILayout.EndHorizontal();
    }
}
