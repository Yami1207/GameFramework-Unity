using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabDataBuffer
{
    private Vector3 m_Position = Vector3.zero;
    public Vector3 position { set { m_Position = value; } get { return m_Position; } }

    private Vector3 m_EulerAngle = Vector3.zero;
    public Vector3 eulerAngle { set { m_EulerAngle = value; } get { return m_EulerAngle; } }

    private Vector3 m_Scale = Vector3.one;
    public Vector3 scale { set { m_Scale = value; } get { return m_Scale; } }
}
