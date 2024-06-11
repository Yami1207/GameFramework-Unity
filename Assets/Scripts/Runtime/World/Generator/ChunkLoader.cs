using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkLoader
{
    private World m_World;

    public ChunkLoader(World world)
    {
        m_World = world;
    }

    public void Destroy()
    {
        m_World = null;
    }

    public Chunk LoadChunk(int x, int z)
    {
        ChunkPos pos = new ChunkPos(x, z);
        if (m_World.IsOutOfRange(pos))
            return null;

        Chunk newChunk = new Chunk(x, z);
        return newChunk;
    }
}
