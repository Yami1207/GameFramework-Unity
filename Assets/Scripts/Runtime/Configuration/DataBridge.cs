using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public static class DataBridge
{
    #region Player

    private static Player s_Player;

    /// <summary>
    /// 获取当前玩家
    /// </summary>
    /// <returns></returns>
    public static Player GetPlayer()
    {
        return s_Player;
    }

    /// <summary>
    /// 注册当前玩家
    /// </summary>
    /// <param name="player"></param>
    public static void RegisterPlayer(Player player)
    {
        s_Player = player;
    }

    /// <summary>
    /// 获取当前玩家的位置
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public static bool GetCurrentPlayerPosition(out Vector3 position)
    {
        var player = GetPlayer();
        if (player == null)
        {
            position = Vector3.zero;
            return false;
        }

        position = player.actor.position;
        return true;
    }

    #endregion

    #region Camera
    public static Camera mainCamera { private set; get; }

    /// <summary>
    /// 注册摄像机
    /// </summary>
    /// <param name="camera"></param>
    public static void RegisterMainCamera(Camera camera)
    {
        mainCamera = camera;
    }

    #endregion
}
