using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabNode : ObjectNode
{
    private GameObject m_GameObject;

    public void Load(PrefabInfo info)
    {

    }

    #region ObjectNode Override

    /// <summary>
    /// 给子类重写，创建GameObject时调用
    /// </summary>
    protected override void OnCreate()
    {
    }

    /// <summary>
    /// 给子类重写，删除GameObject前时调用
    /// </summary>
    protected override void OnDestroy()
    {
    }

    #endregion
}
