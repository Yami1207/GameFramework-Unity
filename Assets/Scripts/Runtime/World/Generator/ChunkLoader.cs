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

    public Chunk LoadChunk(ChunkPos pos)
    {
        if (m_World.IsOutOfRange(pos))
            return null;

        Chunk newChunk = new Chunk(pos.x, pos.z);
        return newChunk;
    }
}
