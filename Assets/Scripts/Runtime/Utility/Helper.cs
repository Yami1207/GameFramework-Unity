using UnityEngine;

public static class Helper
{
    public static ChunkPos WorldPosToChunkPos(Vector3 pos)
    {
        return WorldPosToChunkPos(new Vector3Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z)));
    }

    public static ChunkPos WorldPosToChunkPos(Vector3Int pos)
    {
        return new ChunkPos(pos.x >> Define.kChunkSideLengthBits, pos.z >> Define.kChunkSideLengthBits);
    }

    public static Vector3 ChunkPosToWorld(ChunkPos pos)
    {
        return new Vector3(pos.x * Define.kChunkSideLength, 0.0f, pos.z * Define.kChunkSideLength);
    }

    public static Vector2Int ChunkPosToScenePos(ChunkPos pos)
    {
        return new Vector2Int(Mathf.FloorToInt(1.0f * pos.x / Define.kSceneSideLength), Mathf.FloorToInt(1.0f * pos.z / Define.kSceneSideLength));
    }

    public static int GetHashCode(int x, int z)
    {
        return (x << 16) | (z & 0xffff);
    }
}
