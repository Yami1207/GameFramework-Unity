using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class JoystickInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    private RectTransform m_RectTransform = null;
    private RectTransform m_CenterPointReactTransform = null;

    /// <summary>
    /// 摇杆内切圆半径
    /// </summary>
    private float m_Radius = 0;

    /// <summary>
    /// 摇杆中心点
    /// </summary>
    private Vector2 m_CenterStartPos;

    /// <summary>
    /// 摇杆中心偏移位置
    /// </summary>
    private Vector2 m_OffsetPos;

    /// <summary>
    /// GetUIPos临时变量
    /// </summary>
    private Vector2 m_LocalPoint;

    /// <summary>
    /// 滑动回调
    /// </summary>
    public Action<Vector2> onDragAction = null;

    /// <summary>
    /// 抬起回调
    /// </summary>
    public Action onPointUpAction = null;

    /// <summary>
    /// 摁下回调
    /// </summary>
    public Action<Vector2> onPointDownAction = null;

    void Awake()
    {
        m_RectTransform = transform as RectTransform;
        m_CenterPointReactTransform = transform.GetChild(0) as RectTransform;

        //joystick是内切圆所以除以2
        m_Radius = 0.5f * GetComponent<Image>().rectTransform.rect.width;
        m_OffsetPos = new Vector2(m_Radius, m_Radius);
        m_CenterStartPos = m_CenterPointReactTransform.anchoredPosition;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        m_CenterPointReactTransform.anchoredPosition = m_CenterStartPos;
        if (onPointUpAction != null)
            onPointUpAction.Invoke();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        var pos = GetUIPos(eventData.position, eventData.enterEventCamera);
        m_CenterPointReactTransform.anchoredPosition = pos;
        if (onPointDownAction != null)
            onPointDownAction.Invoke(pos);
    }

    public void OnDrag(PointerEventData eventData)
    {
        var pos = GetUIPos(eventData.position, eventData.enterEventCamera);
        if (pos.magnitude > m_Radius)
            pos = pos.normalized * m_Radius;

        m_CenterPointReactTransform.anchoredPosition = pos;
        if (onDragAction != null)
            onDragAction.Invoke(pos);
    }

    private Vector2 GetUIPos(Vector2 viewPos, Camera camera)
    {
        m_LocalPoint = Vector2.zero;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(m_RectTransform, viewPos, camera, out m_LocalPoint);
        return m_LocalPoint - m_OffsetPos;
    }
}
