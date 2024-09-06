using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterNode : ObjectNode
{
    private ReflectionPlane m_ReflectionPlane;

    #region ObjectNode Override

    /// <summary>
    /// 给子类重写，创建GameObject时调用
    /// </summary>
    protected override void OnCreate()
    {
        var meshFilter = base.node.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = AssetManager.instance.LoadAsset<Mesh>(2000);

        var meshRenderer = base.node.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = AssetManager.instance.LoadAsset<Material>(5100);
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = true;

        m_ReflectionPlane = base.node.AddComponent<ReflectionPlane>();
    }

    /// <summary>
    /// 给子类重写，删除GameObject前时调用
    /// </summary>
    protected override void OnDestroy()
    {
    }

    #endregion
}
