using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class CSV2Texture
{
    [MenuItem("Assets/CSV To Texture(RFloat)")]
    private static void ExecCSV2Texture()
    {
        if (UnityEditor.Selection.activeObject == null)
            return;

        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (path == null)
            return;

        string[] lines = System.IO.File.ReadAllLines(path);
        if (lines.Length <= 1)
            return;

        string headText = lines[0];
        string[] texts = headText.Split(new char[1] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
        int width = texts.Length - 1, height = lines.Length - 1;

        List<Color> pixelList = new List<Color>(width * height);
        for (int i = 1; i < lines.Length; ++i)
        {
            string[] data = lines[i].Split(new char[1] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int j = 1; j < data.Length; ++j)
                pixelList.Add(new Color(float.Parse(data[j]), 0.0f, 0.0f));
        }

        Texture2D texture = new Texture2D(width, height, TextureFormat.RFloat, false, true);
        texture.SetPixels(pixelList.ToArray());

        int pos = path.LastIndexOf('/');
        string dir = path.Substring(0, pos);
        string name = path.Substring(pos + 1);
        name = name.Substring(0, name.LastIndexOf('.'));
        AssetDatabase.CreateAsset(texture, string.Format("{0}/{1}.asset", dir, name));
    }
}
