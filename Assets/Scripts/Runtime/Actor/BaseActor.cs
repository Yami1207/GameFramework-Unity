using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseActor
{
    protected GameObject m_GameObject;
    public GameObject gameObject { get { return m_GameObject; } }

    public Transform transform { get { return m_GameObject.transform; } }

    public Vector3 position
    {
        set
        {
            if (m_GameObject != null)
                m_GameObject.transform.position = value;
        }
        get
        {
            if (m_GameObject != null)
                return m_GameObject.transform.position;
            return Vector3.zero;
        }
    }

    public virtual void Init(int assetId)
    {
        m_GameObject = AssetManager.instance.LoadAssetAndInstantiate(assetId);
    }
}
