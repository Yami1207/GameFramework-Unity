using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScenePos : System.IEquatable<ScenePos>
{
    public int x { private set; get; }
    public int z { private set; get; }

    private int m_HashCode;
    public int hashCode { get { return m_HashCode; } }

    public ScenePos(int _x, int _z)
    {
        x = _x;
        z = _z;

        m_HashCode = Helper.GetHashCode(x, z);
    }

    public override string ToString()
    {
        return string.Format("({0},{1})", x, z);
    }

    public override int GetHashCode()
    {
        return m_HashCode;
    }

    public bool Equals(ScenePos other)
    {
        return x == other.x && z == other.z;
    }

    public override bool Equals(object other)
    {
        if (!(other is ScenePos))
            return false;
        ScenePos pos = (ScenePos)other;
        return x == pos.x && z == pos.z;
    }

    public static bool operator ==(ScenePos a, ScenePos b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(ScenePos a, ScenePos b)
    {
        return a.Equals(b) == false;
    }
}
