using System.Collections;
using System.Collections.Generic;
using System.Security;
using UnityEngine;

public class SettingManager : Singleton<SettingManager>
{
    /// <summary>
    /// 游戏版本
    /// </summary>
    private int m_GameVersion = 0;
    public int gameVersion { get { return m_GameVersion; } }

    /// <summary>
    /// 资源版本
    /// </summary>
    private int m_ResVersion = 0;
    public int resVersion { get { return m_ResVersion; } }

    /// <summary>
    /// 是否开发者模式
    /// </summary>
    private bool m_DevelopMode = false;
    public bool developMode { get { return m_DevelopMode; } }

    /// <summary>
    /// 是否使用AB
    /// </summary>
    private bool m_EnableAssetBundle = true;
    public bool enableAssetBundle { get { return m_EnableAssetBundle; } }

    /// <summary>
    /// 是否开启Instancing
    /// </summary>
    private bool m_EnableInstancing = true;
    public bool enableInstancing { get { return m_EnableInstancing; } }

    public void Init()
    {
        byte[] setupBytes = GetSetupBytes();
        if (setupBytes == null)
        {
            Debug.LogError("请检查setup.xml是否存在");
            return;
        }

        XMLParser xml = new XMLParser();
        xml.Parse(XMLTool.ToString(setupBytes));

        SecurityElement node = xml.ToXml().SearchForChildByTag("Publish");
        int version = XMLTool.GetIntAttribute(node, "version");
#if UNITY_EDITOR
        m_DevelopMode = XMLTool.GetBoolAttribute(node, "devel_mode");
#endif
        m_EnableInstancing = XMLTool.GetBoolAttribute(node, "instancing");
        m_GameVersion = XMLTool.GetIntAttribute(node, "game_version");
        m_ResVersion = XMLTool.GetIntAttribute(node, "res_version");

        // 只有编辑器下可供选择
#if UNITY_EDITOR
        node = xml.ToXml().SearchForChildByTag("Debug");
        m_EnableAssetBundle = XMLTool.GetBoolAttribute(node, "use_ab");
#else
        m_EnableAssetBundle = true;
#endif

        AppInfo.Setup(m_GameVersion);
    }

    /// <summary>
    /// 从StreamingAssets下读取setup.xml
    /// </summary>
    /// <returns></returns>
    private byte[] GetSetupBytes()
    {
#if (UNITY_EDITOR || UNITY_STANDALONE)
        string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, "setup.xml");
        byte[] setupBytes = null;
        try
        {
            setupBytes = System.IO.File.ReadAllBytes(filePath);
        }
        catch (System.Exception e)
        {
#if UNITY_EDITOR
            Debug.LogError("ReadAllText(setup.xml)出错：" + e.Message);
#endif
            return null;
        }

        return setupBytes;
#else
        // IOS平台:Application.streamingAssetsPath = Application/xxxxx/xxx.app/Data/Raw
        // android平台:Application.streamingAssetsPath = jar:file:///data/app/xxx.xxx.xxx.apk/!/assets

        string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, "setup.xml");
        byte[] setupBytes = null;
        if (filePath.Contains("://"))
        {
            WWW www = new WWW(filePath);
            long stime = Utils.currentTimeMillis;
            while (!www.isDone)
            {
                long etime = Utils.currentTimeMillis;
                if ((etime - stime) >= 5000.0f)
                {
#if UNITY_EDITOR
                    Debug.LogError("读取StreamingAssets/setup.xml超时");
#endif
                    return null;
                }
                System.Threading.Thread.Sleep(1);
            }
            if (string.IsNullOrEmpty(www.error) != true)
            {
#if UNITY_EDITOR
                Debug.LogError("读取StreamingAssets/setup.xml出错：" + www.error);
#endif
                return null;
            }
            setupBytes = www.bytes;
        }
        else
        {
            try
            {
                setupBytes = System.IO.File.ReadAllBytes(filePath);
            }
            catch (System.Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError("ReadAllText(setup.xml)出错：" + e.Message);
#endif
                return null;
            }
        }

        return setupBytes;
#endif
    }
}
