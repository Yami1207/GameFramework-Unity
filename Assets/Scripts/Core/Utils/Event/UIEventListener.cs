using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIEventListener : EventTrigger
{
    public delegate void PointerEventDelegate(GameObject go, PointerEventData eventData);

    public PointerEventDelegate onPointerClick;
    public PointerEventDelegate onPointerDown;
    public PointerEventDelegate onPointerUp;
    public PointerEventDelegate onPointerEnter;
    public PointerEventDelegate onPointerExit;

    public PointerEventDelegate onInitializePotentialDrag;
    public PointerEventDelegate onBeginDrag;
    public PointerEventDelegate onDrag;
    public PointerEventDelegate onEndDrag;
    public PointerEventDelegate onDrop;
    public PointerEventDelegate onScroll;

    public System.Action onDisable;

    private void OnDisable()
    {
        if (onDisable != null)
            onDisable();
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        if (onPointerClick != null)
            onPointerClick(gameObject, eventData);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        if (onPointerDown != null)
            onPointerDown(gameObject, eventData);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        if (onPointerEnter != null)
            onPointerEnter(gameObject, eventData);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        if (onPointerExit != null)
            onPointerExit(gameObject, eventData);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        if (onPointerUp != null)
            onPointerUp(gameObject, eventData);
    }

    public override void OnInitializePotentialDrag(PointerEventData eventData)
    {
        base.OnInitializePotentialDrag(eventData);
        if (onInitializePotentialDrag != null)
            onInitializePotentialDrag(gameObject, eventData);
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        base.OnBeginDrag(eventData);
        if (onBeginDrag != null)
            onBeginDrag(gameObject, eventData);
    }

    public override void OnDrag(PointerEventData eventData)
    {
        base.OnDrag(eventData);
        if (onDrag != null)
            onDrag(gameObject, eventData);
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        base.OnEndDrag(eventData);
        if (onEndDrag != null)
            onEndDrag(gameObject, eventData);
    }

    public override void OnDrop(PointerEventData eventData)
    {
        base.OnDrop(eventData);
        if (onDrop != null)
            onDrop(gameObject, eventData);
    }

    public override void OnScroll(PointerEventData eventData)
    {
        base.OnScroll(eventData);
        if (onScroll != null)
            onScroll(gameObject, eventData);
    }

    public static UIEventListener Get(GameObject go)
    {
        UIEventListener listener = go.GetComponent<UIEventListener>();
        if (listener == null)
            listener = go.AddComponent<UIEventListener>();
        return listener;
    }
}
