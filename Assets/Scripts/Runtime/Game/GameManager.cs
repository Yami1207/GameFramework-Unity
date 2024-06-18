using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    private World m_World;

    private Player m_Player;

    private Vector3 m_PlayerStartPos = new Vector3(130.0f, 0.0f, 485.0f);

    public void Init()
    {
        // 解压数据
        //DataExtractor.ExtractAll();

        //GameSetting.enableInstancing = false;

        // 设置镜头
        CameraManager.mainCamera.clearFlags = CameraClearFlags.SolidColor;
        CameraManager.SetCameraRenderer(CameraManager.mainCamera, CameraManager.CameraRenderer.UI);

        // 创建Player
        m_Player = new Player();
        m_Player.SetPosition(m_PlayerStartPos);
        DataBridge.RegisterPlayer(m_Player);

        m_World = new World();
        m_World.enableRenderWorld = false;
        m_World.Load("demo_world.json");
        m_World.LoadChunkNow(m_Player.actor.position, Define.kChunkLoadDistance, OnChunkLoaded);
    }

    public void Destroy()
    {
        if (m_Player != null)
        {
            m_Player.OnExitWorld();
            m_Player.Destroy();
            m_Player = null;
        }

        if (m_World != null)
        {
            m_World.Destroy();
            m_World = null;
        }
    }

    public void Update()
    {
        m_Player.Update();
        m_World.Update();
    }

    public void LateUpdate()
    {
        m_Player.LateUpdate();
        m_World.LateUpdate();
    }

    public void FixedUpdate()
    {
    }

    private void OnChunkLoaded()
    {
        m_World.enableRenderWorld = true;

        // 切换镜头渲染器
        Camera camera = CameraManager.mainCamera;
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.farClipPlane = GameSetting.playerChunkView * Define.kChunkSideLength;
        CameraManager.SetCameraRenderer(camera, CameraManager.CameraRenderer.Default);

        // 角色位置
        {
            ChunkPos chunkPos = Helper.WorldPosToChunkPos(m_PlayerStartPos);
            Chunk chunk = m_World.GetChunk(chunkPos);
            Vector3 pos = m_PlayerStartPos;
            pos.y = chunk.bounds.max.y + 50.0f;

            RaycastHit hit;
            if (Physics.Raycast(pos, Vector3.down, out hit, 500.0f, 1 << TagsAndLayers.kLayerTerrain))
            {
                var newPos = hit.point;
                m_Player.SetPosition(new Vector3(newPos.x, newPos.y + 2.0f, newPos.z));
            }
            m_Player.OnEnterWorld();
        }
    }
}
