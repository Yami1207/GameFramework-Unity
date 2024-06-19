using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

/// <summary>
/// 数据解压器
/// </summary>
public class DataExtractor
{
    /// <summary>
    /// 数据解压流程
    /// </summary>
    public static void ExtractAll()
    {
        if (SettingManager.instance.developMode || !IsNeedExtract())
            return;

        ExtractDataZip();
        ExtractMap();

        // 更新数据版本
        PlayerPrefs.SetInt("game_version", SettingManager.instance.gameVersion);
    }

    private static bool IsNeedExtract()
    {
        int gameVersion = PlayerPrefs.GetInt("game_version");
        return gameVersion != SettingManager.instance.gameVersion;
    }

    private static void ExtractDataZip()
    {
        string dataFile = AssetPathDefine.packedDataPath;
#if UNITY_ANDROID && !UNITY_EDITOR
        var www = new WWW(dataFile);
        CoroutineRunner.Wait(www);
        ZipUtils.UnZipFileByBytes(www.bytes, AssetPathDefine.externalFilePath, false);
#else
        ZipUtils.UnZipFile(dataFile, AssetPathDefine.externalFilePath);
#endif
    }

    private static void ExtractMap()
    {
        if (Directory.Exists(SavePath.mapSavePath) == false)
            Directory.CreateDirectory(SavePath.mapSavePath);

        string mapDir = Application.streamingAssetsPath + "/Map/";
#if !UNITY_EDITOR && UNITY_ANDROID
        WWW www = new WWW(mapDir + "files.json");
        CoroutineRunner.Wait(www);
        string json = www.text;
#else
        string json = File.ReadAllText(mapDir + "files.json");
#endif

        using (JsonReader reader = new JsonTextReader(new StringReader(json)))
        {
            while (reader.Read())
            {
                if(reader.Value != null)
                {
                    string file = reader.Value.ToString();
                    Debug.LogWarning("开始解压:" + file);
                    string path = mapDir + file;

                    Stream fileStream = null;
#if !UNITY_EDITOR && UNITY_ANDROID
                    www = new WWW(path);
                    CoroutineRunner.Wait(www);
                    fileStream = new MemoryStream(www.bytes);
#else
                    fileStream = File.OpenRead(path);
#endif

                    string distFile = SavePath.mapSavePath + "/" + file;
                    var inStream = new LZ4.LZ4Stream(fileStream, LZ4.LZ4StreamMode.Decompress);
                    FileStream outStream = new FileStream(distFile, FileMode.Create);

                    CompressionUtils.ConvertStream(inStream, outStream);
                    inStream.Close();
                    outStream.Flush();
                    outStream.Close();

                    Compress.UPK.UPKFolder.UnpackFolder(distFile, Path.GetDirectoryName(distFile) + "/", null);
                    File.Delete(distFile);
                }
            }
        }
    }
}
