using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReflectionManager : Singleton<ReflectionManager>
{
    private readonly List<ReflectionPlane> m_Planes = new List<ReflectionPlane>();
    public List<ReflectionPlane> planes { get { return m_Planes; } }

    public void AddPlane(ReflectionPlane plane)
    {
        m_Planes.Add(plane);
    }

    public void RemovePlane(ReflectionPlane plane)
    {
        m_Planes.Remove(plane);
    }
}
