using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ObjectNode
{
    public GameObject node { protected set; get; }

    public Transform transform { protected set; get; }

    /// <summary>
    /// GameObject实体是否有效
    /// </summary>
    public bool isValid { get { return node != null; } }

    public bool activeSelf
    {
        set
        {
            if (node != null)
                node.SetActive(value);
        }
        get { return node != null ? node.activeSelf : false; }
    }

    /// <summary>
    /// 创建一个节点
    /// </summary>
    /// <param name="name"></param>
    public void Create(string name)
    {
        if (node != null)
        {
            node.name = name;
        }
        else
        {
            node = new GameObject(name);
            transform = node.transform;
            OnCreate();
        }

        SetLayer(TagsAndLayers.DEFAULT_LAYER);
    }

    public void CreateWithStandardPosition(string name, Vector3 pos)
    {
        Create(name);
        PlaceStandardPosition(pos, Space.World);
    }

    public void PlaceStandardPosition(Vector3 pos, Space space)
    {
        if (transform != null)
        {
            if (space == Space.World)
            {
                transform.position = pos;
                transform.rotation = Quaternion.identity;
                OnPlaceScale();
            }
            else
            {
                transform.localPosition = pos;
                transform.localRotation = Quaternion.identity;
                OnPlaceScale();
            }
        }
    }

    protected virtual void OnPlaceScale()
    {
        transform.localScale = Vector3.one;
    }

    public void Destroy()
    {
        OnDestroy();

        if (node != null)
            GameObject.Destroy(node);

        node = null;
        transform = null;
    }

    public void ChangeParent(Transform _parent)
    {
        if (transform != null)
            transform.SetParent(_parent);
    }

    /// <summary>
    /// 设置layer
    /// </summary>
    /// <param name="layer"></param>
    public void SetLayer(int layer)
    {
        if (node != null)
            node.layer = layer;
    }

    /// <summary>
    /// 获得layer
    /// </summary>
    /// <returns></returns>
    public int GetLayer()
    {
        if (node != null)
            return node.layer;
        return 0;
    }

    /// <summary>
    /// 给子类重写，创建GameObject时调用
    /// </summary>
    protected virtual void OnCreate()
    {
    }

    /// <summary>
    /// 给子类重写，删除GameObject前时调用
    /// </summary>
    protected virtual void OnDestroy()
    {
    }
}
