using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class StarRailCharacterCommonShaderGUI : StarRailCharacterBaseShaderGUI
{
    protected override void DoGUI()
    {
        DoGUI_Main();
        DoGUI_Specular();
        DoGUI_RimLight();
        DoGUI_RimShadow();
        DoGUI_Emission();
        DoGUI_Outline();
        DoGUI_Other();
        DoGUI_UnityDefaultPart();
    }

    private void DoGUI_Main()
    {
        EditorGUILayout.BeginVertical(BaseShaderGUI.Styles.frameBgStyle);
        {
            DoGUI_Title("< Main >");

            m_Editor.TexturePropertySingleLine(StarRailCharacterBaseShaderGUI.Styles.baseMap, m_BaseMapProp, m_BaseColorProp);
            DrawProperty("_BackColor", "Back Color", false);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("R: 边缘光宽度 G: AO B: 高光阈值 A: Ramp", MessageType.Info);
            m_Editor.TexturePropertySingleLine(Styles.kLightMap, m_LightMapProp);

            EditorGUILayout.Space();
            DrawProperty("_CoolRampTex", "Cool Ramp", false);
            DrawProperty("_WarmRampTex", "Warm Ramp", false);

            if (m_MaterialValuesPackLUTProp != null)
            {
                EditorGUILayout.Space();
                m_Editor.TexturePropertySingleLine(EditorDraw.TempContent("Values Pack Map"), m_MaterialValuesPackLUTProp);
                CheckMaterialValuesPackLUT();
            }
        }
        EditorGUILayout.EndVertical();
    }
}
