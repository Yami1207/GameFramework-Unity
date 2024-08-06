using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class BaseShaderGUI : ShaderGUI
{
    #region Enums

    protected enum SurfaceType
    {
        Opaque,
        Transparent
    }

    protected enum BlendMode
    {
        Alpha,
        Premultiply,
        Additive,
        Multiply
    }

    protected enum RenderFace
    {
        Front = 2,
        Back = 1,
        Both = 0
    }

    protected enum ZWrite
    {
        Off,
        On,
    }

    #endregion

    protected static class Property
    {
        public static readonly string surfaceType = "_Surface";
        public static readonly string blendMode = "_Blend";

        public static readonly string cull = "_Cull";
        public static readonly string zwrite = "_ZWrite";

        public static readonly string alphaClip = "_Cutoff";

        public static readonly string srcBlend = "_SrcBlend";
        public static readonly string dstBlend = "_DstBlend";
    }

    protected static class Styles
    {
        public static readonly GUIStyle muiltLine = new GUIStyle(EditorStyles.label);
        public static readonly GUIStyle frameBgStyle = new GUIStyle("HelpBox");
        public static readonly GUIStyle toolbarStyle = new GUIStyle("preToolbar");
        public static readonly GUIStyle toolbarTitleStyle = new GUIStyle("preToolbar");
        public static readonly GUIStyle foldoutHeaderStyle = new GUIStyle("ShurikenModuleTitle");

        public static readonly GUIContent surfaceType = EditorGUIUtility.TrTextContent("Surface Type",
            "Select a surface type for your texture. Choose between Opaque or Transparent.");
        public static readonly string[] surfaceTypeNames = Enum.GetNames(typeof(SurfaceType));

        public static readonly GUIContent blendingMode = EditorGUIUtility.TrTextContent("Blending Mode",
            "Controls how the color of the Transparent surface blends with the Material color in the background.");
        public static readonly string[] blendModeNames = Enum.GetNames(typeof(BlendMode));

        public static readonly GUIContent cullingText = EditorGUIUtility.TrTextContent("Render Face",
            "Specifies which faces to cull from your geometry. Front culls front faces. Back culls backfaces. None means that both sides are rendered.");
        public static readonly string[] renderFaceNames = Enum.GetNames(typeof(RenderFace));

        public static readonly GUIContent zwriteText = EditorGUIUtility.TrTextContent("Depth Write",
            "Controls whether the shader writes depth.  Auto will write only when the shader is opaque.");
        public static readonly string[] zwriteNames = Enum.GetNames(typeof(ZWrite));

        static Styles()
        {
            foldoutHeaderStyle.font = new GUIStyle(EditorStyles.boldLabel).font;
            foldoutHeaderStyle.border = new RectOffset(15, 7, 4, 4);
            foldoutHeaderStyle.fixedHeight = 22;
            foldoutHeaderStyle.contentOffset = new Vector2(20f, -2f);

            muiltLine.wordWrap = true;
        }
    }

    /// <summary>
    /// 显示原生界面
    /// </summary>
    private static bool s_OriginalInspector = false;

    protected float m_LastLabelWidth;
    protected float m_LastFieldWidth;

    protected Material m_TargetMaterial;
    protected MaterialEditor m_Editor;
    protected MaterialProperty[] m_Properties;

    private MaterialProperty m_SurfaceTypeProp;
    private MaterialProperty m_BlendModeProp;
    private MaterialProperty m_CullingProp;
    private MaterialProperty m_ZWriteProp;

    /// <summary>
    /// 是否检查过没使用的属性
    /// </summary>
    private bool m_CheckUnusedProperity = false;

    /// <summary>
    /// 未使用的属性列表
    /// </summary>
    private string[] m_UnusedProperities = null;

    /// <summary>
    /// 滚动条位置
    /// </summary>
    private Vector2 m_UnusedProperitiesPos;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
    {
        m_TargetMaterial = materialEditor.target as Material;
        m_Editor = materialEditor;
        m_Properties = props;

        m_LastLabelWidth = EditorGUIUtility.labelWidth;
        m_LastFieldWidth = EditorGUIUtility.fieldWidth;

        // UI开始
        //m_Editor.SetDefaultGUIWidths();

        FindProperties(m_Properties);

        // 子类
        DoGUI_Before();

        // 检查冗余
        CheckUnusedProperity();
        DoGUI_UnusedProperties();

        // 子类
        DoGUI_Tool();

        if (s_OriginalInspector)
        {
            if (GUILayout.Button("显示自定义界面"))
                s_OriginalInspector = false;
            base.OnGUI(materialEditor, props);
        }
        else
        {
            if (GUILayout.Button("显示原生界面"))
                s_OriginalInspector = true;

            // 子类
            DoGUI();
        }
    }

    private void SetMaterialSrcDstBlendProperties(UnityEngine.Rendering.BlendMode srcBlend, UnityEngine.Rendering.BlendMode dstBlend)
    {
        if (m_TargetMaterial.HasProperty(Property.srcBlend))
            m_TargetMaterial.SetFloat(Property.srcBlend, (float)srcBlend);

        if (m_TargetMaterial.HasProperty(Property.dstBlend))
            m_TargetMaterial.SetFloat(Property.dstBlend, (float)dstBlend);
    }

    #region 子类函数

    protected virtual void FindProperties(MaterialProperty[] properties)
    {
        m_SurfaceTypeProp = FindProperty(Property.surfaceType, false);
        m_BlendModeProp = FindProperty(Property.blendMode, false);
        m_CullingProp = FindProperty(Property.cull, false);
        m_ZWriteProp = FindProperty(Property.zwrite, false);
    }

    protected virtual void DoChangeShader(Material material, Shader oldShader, Shader newShader)
    {

    }

    protected virtual void DoGUI_Before()
    {

    }

    protected virtual void DoGUI_Tool()
    {

    }

    protected virtual void DoGUI()
    {

    }

    #endregion

    #region 可选择模块

    protected void DoGUI_SurfaceOptions()
    {
        EditorGUILayout.BeginVertical(Styles.frameBgStyle);
        {
            DoGUI_Title("< Surface Options >");

            DoPopup(Styles.surfaceType, m_SurfaceTypeProp, Styles.surfaceTypeNames);

            if (m_SurfaceTypeProp != null && (SurfaceType)m_SurfaceTypeProp.floatValue == SurfaceType.Transparent)
                DoPopup(Styles.blendingMode, m_BlendModeProp, Styles.blendModeNames);

            DoPopup(Styles.cullingText, m_CullingProp, Styles.renderFaceNames);
            DoPopup(Styles.zwriteText, m_ZWriteProp, Styles.zwriteNames);
        }
        EditorGUILayout.EndVertical();
    }

    protected void DoGUI_UnityDefaultPart()
    {
        EditorGUILayout.BeginVertical(Styles.frameBgStyle);
        {
            DoGUI_Title("< Unity内置设置 >");

            m_Editor.DoubleSidedGIField();
            m_Editor.EnableInstancingField();
            m_Editor.RenderQueueField();
        }
        EditorGUILayout.EndVertical();
    }

    protected void Internal_SetupMaterialBlendMode()
    {
        int renderQueue = m_TargetMaterial.shader.renderQueue;

        bool alphaClip = false;
        if (m_TargetMaterial.HasProperty(Property.alphaClip))
            alphaClip = m_TargetMaterial.GetFloat(Property.alphaClip) >= 0.5;

        if (HasProperty(Property.surfaceType))
        {
            bool zwrite = false;
            SurfaceType surfaceType = (SurfaceType)m_TargetMaterial.GetFloat(Property.surfaceType);
            if (surfaceType == SurfaceType.Opaque)
            {
                if (alphaClip)
                {
                    renderQueue = (int)RenderQueue.AlphaTest;
                    m_TargetMaterial.SetOverrideTag("RenderType", "TransparentCutout");
                }
                else
                {
                    renderQueue = (int)RenderQueue.Geometry;
                    m_TargetMaterial.SetOverrideTag("RenderType", "Opaque");
                }

                SetMaterialSrcDstBlendProperties(UnityEngine.Rendering.BlendMode.One, UnityEngine.Rendering.BlendMode.Zero);
                zwrite = true;
            }
            else
            {
                BlendMode blendMode = (BlendMode)m_TargetMaterial.GetFloat(Property.blendMode);
                switch (blendMode)
                {
                    case BlendMode.Alpha:
                        {
                            SetMaterialSrcDstBlendProperties(UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        }
                        break;
                    case BlendMode.Premultiply:
                        {
                            SetMaterialSrcDstBlendProperties(UnityEngine.Rendering.BlendMode.One, UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        }
                        break;
                    case BlendMode.Additive:
                        {
                            SetMaterialSrcDstBlendProperties(UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.One);
                        }
                        break;
                    case BlendMode.Multiply:
                        {
                            SetMaterialSrcDstBlendProperties(UnityEngine.Rendering.BlendMode.DstColor, UnityEngine.Rendering.BlendMode.Zero);
                        }
                        break;
                }

                renderQueue = (int)RenderQueue.Transparent;
                m_TargetMaterial.SetOverrideTag("RenderType", "Transparent");
                zwrite = false;
            }

            if (m_TargetMaterial.HasProperty(Property.zwrite))
                m_TargetMaterial.SetFloat(Property.zwrite, zwrite ? 1.0f : 0.0f);

            m_TargetMaterial.SetShaderPassEnabled("DepthOnly", zwrite);
        }
        else
        {
            m_TargetMaterial.SetShaderPassEnabled("DepthOnly", true);
        }

        if (renderQueue != m_TargetMaterial.renderQueue)
            m_TargetMaterial.renderQueue = renderQueue;
    }

    #endregion

    #region Property Helper

    protected bool HasProperty(string name)
    {
        return FindProperty(name, false) != null;
    }

    protected MaterialProperty FindProperty(string name)
    {
        return ShaderGUI.FindProperty(name, m_Properties);
    }

    protected MaterialProperty FindProperty(string name, bool propertyIsMandatory)
    {
        return ShaderGUI.FindProperty(name, m_Properties, propertyIsMandatory);
    }

    protected void DrawProperty(string name, string text, bool propertyIsMandatory)
    {
        DrawProperty(FindProperty(name, propertyIsMandatory), text);
    }

    protected void DrawProperty(MaterialProperty prop, string text)
    {
        if (prop != null)
            m_Editor.ShaderProperty(prop, text);
    }

    #endregion

    #region GUI Helper

    protected void DoGUI_Title(string title)
    {
        EditorGUILayout.BeginHorizontal(Styles.toolbarStyle);
        GUILayout.FlexibleSpace();
        //GUILayout.Label(title, Styles.toolbarTitleStyle);
        GUILayout.Label(title);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    protected void DoPopup(GUIContent label, MaterialProperty property, string[] options)
    {
        if (property != null)
        {
            if (property.type == MaterialProperty.PropType.Int)
            {
                int newValue = EditorGUILayout.Popup(label, property.intValue, options);
                if (newValue != property.intValue)
                {
                    property.intValue = newValue;
                }
            }
            else if (property.type == MaterialProperty.PropType.Float)
            {
                float newValue = EditorGUILayout.Popup(label, (int)property.floatValue, options);
                if (newValue != property.floatValue)
                {
                    property.floatValue = newValue;
                }
            }
        }
    }

    #endregion

    #region 检查

    /// <summary>
    /// 检查没用的属性
    /// </summary>
    private void CheckUnusedProperity()
    {
        if (m_UnusedProperities == null || m_UnusedProperities.Length <= 0)
            return;

        EditorGUILayout.BeginVertical(Styles.frameBgStyle);
        {
            DoGUI_Title("< 冗余属性检查 >");
            EditorGUILayout.HelpBox("有冗余的属性可以清除", MessageType.Warning);

            EditorGUILayout.LabelField(string.Format("一共冗余属性{0}个！冗余属性列表：", m_UnusedProperities.Length));

            EditorGUILayout.BeginHorizontal();
            {
                var lineCount = Mathf.Min(5, m_UnusedProperities.Length);
                m_UnusedProperitiesPos = EditorGUILayout.BeginScrollView(m_UnusedProperitiesPos, false, false, GUILayout.MinHeight(lineCount * EditorGUIUtility.singleLineHeight));
                {
                    for (int i = 0; i < m_UnusedProperities.Length; ++i)
                    {
                        EditorGUILayout.LabelField(m_UnusedProperities[i]);
                    }
                }
                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("清除", GUILayout.ExpandHeight(true)))
                {
                    OptimalPerformance.CheckMaterial.RemoveUnusedProperties(m_TargetMaterial);
                    m_CheckUnusedProperity = false;
                    CheckUnusedProperity();
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    private void DoGUI_UnusedProperties()
    {
        if (!m_CheckUnusedProperity)
        {
            m_UnusedProperities = OptimalPerformance.CheckMaterial.GetUnusedPropertyNames(m_TargetMaterial);
            m_UnusedProperitiesPos = Vector2.zero;

            m_CheckUnusedProperity = true;
        }
    }

    #endregion
}
