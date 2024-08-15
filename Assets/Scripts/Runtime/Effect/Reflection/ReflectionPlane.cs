using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

[ExecuteInEditMode]
public class ReflectionPlane : MonoBehaviour
{
    private static readonly int REFLECTION_TEX_PROP_ID = Shader.PropertyToID("_ReflectionTex");

    private static MaterialPropertyBlock s_MaterialPropertyBlock = null;
    public static MaterialPropertyBlock materialPropertyBlock
    {
        get
        {
            if (s_MaterialPropertyBlock == null)
                s_MaterialPropertyBlock = new MaterialPropertyBlock();
            return s_MaterialPropertyBlock;
        }
    }

    [SerializeField]
    private bool m_IsAlone = false;
    public bool isAlone { get { return m_IsAlone; } }

    [SerializeField]
    private ReflectionQuality m_Quality = ReflectionQuality.High;
    public ReflectionQuality quality { get { return m_Quality; } }

    [SerializeField]
    private LayerMask m_CullingMask = -1;
    public LayerMask cullingMask { get { return m_CullingMask; } }

    /// <summary>
    /// 渲染器对象
    /// </summary>
    private MeshRenderer m_MeshRenderer;
    public MeshRenderer meshRenderer { get { return m_MeshRenderer; } }

    private RenderTexture m_ReflectionTexture;
    public RenderTexture texture { get { return m_ReflectionTexture; } }

    private void OnEnable()
    {
        if (m_MeshRenderer = this.GetComponent<MeshRenderer>())
            ReflectionManager.instance.AddPlane(this);
    }

    private void OnDisable()
    {
        DestroyTexture();
        ReflectionManager.instance.RemovePlane(this);
    }

    private void LateUpdate()
    {
        if (m_IsAlone)
        {
            Vector2Int size = BaseReflectionPass.GetTextureSize(m_Quality, Screen.width, Screen.height);
            if (m_ReflectionTexture != null && (size.x != m_ReflectionTexture.width || size.y != m_ReflectionTexture.height))
                DestroyTexture();

            if (m_ReflectionTexture == null)
            {
                // 创建纹理
                m_ReflectionTexture = RenderTexture.GetTemporary(size.x, size.y, 24, RenderTextureFormat.ARGB32);
                m_ReflectionTexture.autoGenerateMips = false;
                m_ReflectionTexture.useMipMap = false;
                m_ReflectionTexture.filterMode = FilterMode.Bilinear;

                // 纹理设置到材质上
                var propertyBlock = materialPropertyBlock;
                m_MeshRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetTexture(REFLECTION_TEX_PROP_ID, m_ReflectionTexture);
                m_MeshRenderer.SetPropertyBlock(propertyBlock);
            }
        }
    }

    private void DestroyTexture()
    {
        if (m_ReflectionTexture != null)
        {
            RenderTexture.ReleaseTemporary(m_ReflectionTexture);
            m_ReflectionTexture = null;
        }
    }
}
