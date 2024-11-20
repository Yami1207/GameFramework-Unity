using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Vector4Int
{
    private static Vector4Int s_One = new Vector4Int(1, 1, 1, 1);
    public static Vector4Int one { get { return s_One; } }

    private static Vector4Int s_Zero = new Vector4Int(0, 0, 0, 0);
    public static Vector4Int zero { get { return s_Zero; } }

    public int x, y, z, w;

    public Vector4Int(int _x, int _y = 0, int _z = 0, int _w = 0)
    {
        x = _x;
        y = _y;
        z = _z;
        w = _w;
    }

    public void Set(int _x, int _y = 0, int _z = 0, int _w = 0)
    {
        x = _x;
        y = _y;
        z = _z;
        w = _w;
    }
}
