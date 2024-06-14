using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    internal class MaterialLinearDrawer : MaterialPropertyDrawer
    {
        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            if (prop.type == MaterialProperty.PropType.Vector)
            {
                position = EditorGUI.IndentedRect(position);
                // 保存的是线性值
                var v = prop.vectorValue;
                Color linearColor = v;
                Color gammaColor = linearColor.gamma;

                EditorGUI.BeginChangeCheck();
                // 界面显示的是Gamma值
                gammaColor = EditorGUI.ColorField(position, label, gammaColor);
                if (EditorGUI.EndChangeCheck())
                {
                    linearColor = gammaColor.linear;
                    prop.vectorValue = linearColor;
                }
            }
            else
            {
                if (prop.type == MaterialProperty.PropType.Color)
                {
                    EditorGUI.HelpBox(position, "Linear请使用Vector类型，Color类型默认为Gamma", MessageType.Error);
                }
                else
                {
                    editor.DefaultShaderProperty(prop, label.text);
                }
            }
        }
    }
}