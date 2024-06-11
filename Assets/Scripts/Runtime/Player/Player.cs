using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player
{
    private PlayerActor m_Actor;
    public PlayerActor actor { get { return m_Actor; } }

    private CharacterMotor m_Motor;

    private CameraFollow m_CameraFollow;

    public Player()
    {
        m_Actor = new PlayerActor();
        m_Actor.Init(1);
    }

    public void Destroy()
    {
        m_CameraFollow = null;
    }

    public void Update()
    {

    }

    public void LateUpdate()
    {
        if (m_CameraFollow != null)
            m_CameraFollow.Update();
    }

    public void SetPosition(Vector3 position)
    {
        m_Actor.position = position;
    }

    public void OnEnterWorld()
    {
        // 注册事件
        //inputManager.onJump += player.OnJump;
        InputManager.instance.onJoystick += OnMove;

        m_Motor = m_Actor.gameObject.AddComponent<CharacterMotor>();
        //m_Motor.jumping.baseHeight = 2.5f;
        m_Motor.movement.maxForwardSpeed = 5;
        m_Motor.movement.maxSidewaysSpeed = 5;
        m_Motor.movement.maxBackwardsSpeed = 5;

        var controller = m_Motor.GetComponent<CharacterController>();
        controller.radius = 0.3f;
        controller.center = new Vector3(0, 1, 0);
        controller.height = 2.0f;
        controller.slopeLimit = 50;

        // 相机控制
        m_CameraFollow = new CameraFollow();
        m_CameraFollow.camera = CameraManager.mainCamera;
        m_CameraFollow.target = m_Actor.transform;
        m_CameraFollow.layerMask = 1 << TagsAndLayers.kLayerTerrain;
        InputManager.instance.onDragTouchPad += m_CameraFollow.MoveCamera;
    }

    public void OnExitWorld()
    {
        InputManager.instance.onJoystick -= OnMove;
    }

    public void OnMove(Vector2 direction)
    {
        //根据输入驱动玩家移动
        float h = direction.x;
        float v = direction.y;

        if (h != 0 || v != 0)
        {
            // 计算相机前方在Y平面的投影
            Vector3 forward = CameraManager.mainCamera.transform.forward;
            forward.y = 0;
            forward = forward.normalized;

            // 根据相机计算人物朝向
            Vector3 right = new Vector3(forward.z, 0, -forward.x);
            Vector3 targetDirection = (h * right + v * forward).normalized;

            m_Motor.moveDirection = targetDirection;
            if (m_Motor.moveDirection != Vector3.zero)
                m_Actor.transform.forward = m_Motor.moveDirection;
        }
        else
        {
            m_Motor.moveDirection = Vector3.zero;
        }
    }
}
