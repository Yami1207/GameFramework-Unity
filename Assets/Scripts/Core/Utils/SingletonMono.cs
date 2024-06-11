using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public class SingletonMono<T> : MonoBehaviour where T : Component
{
    private static T s_Instance;
    public static T instance
    {
        get
        {
            if (s_Instance == null)
                CreateInstance();
            return s_Instance;
        }
    }

    private static T CreateInstance()
    {
        if (s_Instance == null)
        {
            string name = typeof(T).Name;
            var go = GameObject.Find(name);
            if (go == null)
                go = new GameObject(name);
            //UnityEngine.Object.DontDestroyOnLoad(go);

            s_Instance = go.GetComponent<T>();
            if (s_Instance == null)
                s_Instance = go.AddComponent<T>();
        }
        return s_Instance;
    }
}
