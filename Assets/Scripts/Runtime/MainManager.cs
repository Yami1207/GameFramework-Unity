using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainManager
{
    public static void Init()
    {
        GameManager.instance.Init();
    }

    public static void Destroy()
    {
        GameManager.instance.Destroy();
    }

    public static void Update()
    {
        GameManager.instance.Update();
    }

    public static void LateUpdate()
    {
        GameManager.instance.LateUpdate();
    }

    public static void FixedUpdate()
    {
        GameManager.instance.FixedUpdate();
    }
}
