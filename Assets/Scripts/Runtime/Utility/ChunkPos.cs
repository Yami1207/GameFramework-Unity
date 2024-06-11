using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ChunkPos : System.IEquatable<ChunkPos>
{
    public int x { private set; get; }
    public int z { private set; get; }

    private int m_HashCode;
    public int hashCode { get { return m_HashCode; } }

    public ChunkPos(int _x, int _z)
    {
        x = _x;
        z = _z;

        m_HashCode = Helper.GetHashCode(x, z);
    }

    public int GetCenterXPos()
    {
        return (this.x << Define.kChunkSideLengthBits) + (Define.kChunkSideLength >> 1);
    }

    public int GetCenterZPos()
    {
        return (this.z << Define.kChunkSideLengthBits) + (Define.kChunkSideLength >> 1);
    }

    public int GetXStart()
    {
        return this.x << Define.kChunkSideLengthBits;
    }

    public int GetZStart()
    {
        return this.z << Define.kChunkSideLengthBits;
    }

    public int GetXEnd()
    {
        return (this.x << Define.kChunkSideLengthBits) + Define.kChunkSideLengthMinusOne;
    }

    public int GetZEnd()
    {
        return (this.z << Define.kChunkSideLengthBits) + Define.kChunkSideLengthMinusOne;
    }

    public override string ToString()
    {
        return string.Format("({0},{1})", x, z);
    }

    public override int GetHashCode()
    {
        return m_HashCode;
    }

    public bool Equals(ChunkPos other)
    {
        return x == other.x && z == other.z;
    }

    public override bool Equals(object other)
    {
        if (!(other is ChunkPos))
            return false;
        ChunkPos otherChunkPos = (ChunkPos)other;
        return x == otherChunkPos.x && z == otherChunkPos.z;
    }

    public static bool operator ==(ChunkPos a, ChunkPos b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(ChunkPos a, ChunkPos b)
    {
        return a.Equals(b) == false;
    }

    public static ChunkPos operator +(ChunkPos a, ChunkPos b)
    {
        return new ChunkPos(a.x + b.x, a.z + b.z);
    }

    public static ChunkPos operator -(ChunkPos a, ChunkPos b)
    {
        return new ChunkPos(a.x - b.x, a.z - b.z);
    }

    public Vector2Int ToVector2Int()
    {
        return new Vector2Int(x, z);
    }
}
