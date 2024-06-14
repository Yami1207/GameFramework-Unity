using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EditorDraw
{
    private static GUIContent s_Text = new GUIContent();
    public static GUIContent TempContent(string t)
    {
        s_Text.text = t;
        return s_Text;
    }
}
