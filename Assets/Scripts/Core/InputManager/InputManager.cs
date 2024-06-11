using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InputManager : Singleton<InputManager>
{
    private static readonly float kLongPressTime = 0.5f;

    private GameObject m_InputObject;
    private GameObject m_TouchPad;

    private JoystickInput m_JoystickInput;

    private float m_PressTime = 0.0f;
    private PointerEventData m_PressData = null;

    private Vector2 m_LastDirection = Vector2.zero;

    public Action<Vector2> onJoystick;
    public Action<Vector2> onClickTouchPad;
    public Action<Vector2> onLongPressTouchPad;
    public Action<Vector2> onDragTouchPad;

    public void Init()
    {
        var prefab = Resources.Load<GameObject>("Prefab/InputManager");
        m_InputObject = GameObject.Instantiate(prefab);
        m_InputObject.transform.SetParent(Globals.uiRoot.transform, false);

        m_TouchPad = m_InputObject.transform.Find("TouchPad").gameObject;
        if (m_TouchPad != null)
        {
            var listener = UIEventListener.Get(m_TouchPad);
            listener.onPointerClick += OnClickTouchPad;
            listener.onPointerDown += OnTouchPadDown;
            listener.onPointerUp += OnTouchPadUp;
            listener.onDrag += OnDrag;
        }

        m_JoystickInput = m_InputObject.transform.Find("Joystick").GetComponent<JoystickInput>();
        m_JoystickInput.onDragAction += NotifyJoystick;
        m_JoystickInput.onPointUpAction += () => { NotifyJoystick(Vector2.zero); };
    }

    public void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");

        Vector2 direction = new Vector2(x, y);
        if (direction != m_LastDirection)
        {
            NotifyJoystick(direction);
            m_LastDirection = direction;
        }

        if (m_PressData != null && Time.realtimeSinceStartup - m_PressTime > kLongPressTime)
        {
            if (onLongPressTouchPad != null)
                onLongPressTouchPad(m_PressData.position);
            m_PressData = null;
        }
    }

    public void OnPlayerEnterWorld()
    {

    }

    public void OnPlayerExitWorld()
    {

    }

    private void OnDrag(GameObject go, PointerEventData eventData)
    {
        m_PressTime = Time.realtimeSinceStartup;
        if (onDragTouchPad != null)
            onDragTouchPad(eventData.delta);
    }

    private void OnTouchPadUp(GameObject go, PointerEventData eventData)
    {
        m_PressData = null;
    }

    private void OnTouchPadDown(GameObject go, PointerEventData eventData)
    {
        m_PressTime = Time.realtimeSinceStartup;
        m_PressData = eventData;
    }

    private void OnClickTouchPad(GameObject go, PointerEventData eventData)
    {
        if (Time.realtimeSinceStartup - m_PressTime > kLongPressTime)
            return;
        if ((eventData.position - eventData.pressPosition).sqrMagnitude > 0.1f)
            return;

        if (onClickTouchPad != null)
            onClickTouchPad(eventData.position);
    }

    private void NotifyJoystick(Vector2 pos)
    {
        if (onJoystick != null)
            onJoystick.Invoke(pos);
    }
}
