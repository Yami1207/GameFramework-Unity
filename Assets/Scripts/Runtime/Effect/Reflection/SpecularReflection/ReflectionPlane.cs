using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ReflectionPlane : MonoBehaviour
{
    private static readonly int s_ReflectionTexPropID = Shader.PropertyToID("_ReflectionTex");

    private static MaterialPropertyBlock s_PropertyBlock = null;

    [SerializeField]
    private LayerMask m_CullMask = -1;
    public LayerMask cullMask { get { return m_CullMask; } }

    [SerializeField]
    [Range(0.01f, 1.0f)]
    private float m_TextureQuality = 0.5f;

    private bool m_IsDirty = true;

    private Renderer m_Renderer;

    private RenderTexture m_ReflectionTexture;
    public RenderTexture texture { get { return m_ReflectionTexture; } }

    private void OnEnable()
    {
        m_Renderer = this.GetComponent<MeshRenderer>();

        if (m_Renderer)
            ReflectionManager.instance.AddPlane(this);
    }

    private void OnDisable()
    {
        DestroyTexture();

        ReflectionManager.instance.RemovePlane(this);
    }

    private void LateUpdate()
    {
        int width = Mathf.FloorToInt(Screen.width * m_TextureQuality), height = Mathf.FloorToInt(Screen.height * m_TextureQuality);
        if (m_ReflectionTexture != null)
        {
            if (width != m_ReflectionTexture.width || height != m_ReflectionTexture.height)
                DestroyTexture();
        }

        if (m_ReflectionTexture == null)
        {
            CreateTexture(width, height);
            m_IsDirty = true;
        }

        UpdateReflectionTexture();
    }

    private void CreateTexture(int width, int height)
    {
        DestroyTexture();

        m_ReflectionTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
        //m_ReflectionTexture.isPowerOfTwo = true;
        m_ReflectionTexture.autoGenerateMips = false;
        m_ReflectionTexture.useMipMap = false;
        m_ReflectionTexture.filterMode = FilterMode.Bilinear;
    }

    private void DestroyTexture()
    {
        if (m_ReflectionTexture != null)
        {
            RenderTexture.ReleaseTemporary(m_ReflectionTexture);
            m_ReflectionTexture = null;
        }
    }

    private void UpdateReflectionTexture()
    {
        if (m_IsDirty)
        {
            if (s_PropertyBlock == null)
                s_PropertyBlock = new MaterialPropertyBlock();

            m_Renderer.GetPropertyBlock(s_PropertyBlock);
            s_PropertyBlock.SetTexture(s_ReflectionTexPropID, m_ReflectionTexture);
            m_Renderer.SetPropertyBlock(s_PropertyBlock);

            m_IsDirty = false;
        }
    }
}
