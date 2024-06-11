using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameSetting
{
    private static bool s_EnableInstancing = true;
    public static bool enableInstancing { set { s_EnableInstancing = value; } get { return s_EnableInstancing; } }

    /// <summary>
    /// 玩家视野需要渲染的Chunk数量
    /// </summary>
    private static  int s_PlayerChunkView = 12;
    public static int playerChunkView { set { s_PlayerChunkView = value; } get { return s_PlayerChunkView; } }

    private static int s_LowTerrainDistance = 10;
    public static int lowTerrainDistance { set { s_LowTerrainDistance = value; } get { return s_LowTerrainDistance; } }
}
