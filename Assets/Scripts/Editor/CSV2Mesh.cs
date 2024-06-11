using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class CSV2Mesh
{
    private readonly static List<string> s_PositionKey = new List<string> { "POSITION.x", "in_POSITION0.x" };
    private static string s_Position = "";

    private readonly static List<string> s_NormalKey = new List<string> { "NORMAL.x", "in_NORMAL0.x" };
    private static bool s_HasNormal = false;
    private static string s_Normal = "";

    private readonly static List<string> s_TangentKey = new List<string> { "in_TANGENT0.x" };
    private static bool s_HasTangent = false;
    private static string s_Tangent = "";

    private readonly static List<string> s_ColorKey = new List<string> { "COLOR.x", "in_COLOR0.x" };
    private static bool s_HasColor = false;
    private static string s_Color = "";

    private readonly static List<string> s_Texcoord0_Key = new List<string> { "TEXCOORD0.x", "in_TEXCOORD0.x" };
    private static bool s_HasTexcoord0 = false;
    private static string s_Texcoord0 = "";

    private readonly static List<string> s_Texcoord1_Key = new List<string> { "TEXCOORD1.x", "in_TEXCOORD1.x" };
    private static bool s_HasTexcoord1 = false;
    private static string s_Texcoord1 = "";

    private struct CSVVertex
    {
        public int index;

        public Vector3 position;
        public Vector3 normal;
        public Vector4 tangent;
        public Color color;
        public Vector4 uv;
        public Vector4 uv1;

        public CSVVertex(int index, string[] data, Dictionary<string, int> dict)
        {
            this.index = index;

            // 顶点坐标
            position = Vector3.one;
            position.x = float.Parse(data[dict[string.Format("{0}.x", s_Position)]]);
            position.y = float.Parse(data[dict[string.Format("{0}.y", s_Position)]]);
            position.z = float.Parse(data[dict[string.Format("{0}.z", s_Position)]]);

            // 法线
            normal = Vector3.up;
            if (s_HasNormal)
            {
                normal.x = float.Parse(data[dict[string.Format("{0}.x", s_Normal)]]);
                normal.y = float.Parse(data[dict[string.Format("{0}.y", s_Normal)]]);
                normal.z = float.Parse(data[dict[string.Format("{0}.z", s_Normal)]]);
            }

            // 切线
            tangent = Vector4.zero;
            if (s_HasTangent)
            {
                tangent.x = float.Parse(data[dict[string.Format("{0}.x", s_Tangent)]]);
                tangent.y = float.Parse(data[dict[string.Format("{0}.y", s_Tangent)]]);
                tangent.z = float.Parse(data[dict[string.Format("{0}.z", s_Tangent)]]);
                tangent.w = float.Parse(data[dict[string.Format("{0}.w", s_Tangent)]]);
            }

            // 顶点颜色
            color = Color.white;
            if (s_HasColor)
            {
                color.r = float.Parse(data[dict[string.Format("{0}.x", s_Color)]]);
                color.g = float.Parse(data[dict[string.Format("{0}.y", s_Color)]]);
                color.b = float.Parse(data[dict[string.Format("{0}.z", s_Color)]]);
                color.a = float.Parse(data[dict[string.Format("{0}.w", s_Color)]]);
            }

            // uv
            uv = Vector4.zero;
            if (s_HasTexcoord0)
            {
                uv.x = float.Parse(data[dict[string.Format("{0}.x", s_Texcoord0)]]);
                uv.y = float.Parse(data[dict[string.Format("{0}.y", s_Texcoord0)]]);

                string key = string.Format("{0}.z", s_Texcoord0);
                if (dict.ContainsKey(key)) uv.z = float.Parse(data[dict[key]]);

                key = string.Format("{0}.w", s_Texcoord0);
                if (dict.ContainsKey(key)) uv.w = float.Parse(data[dict[key]]);
            }

            // uv1
            uv1 = Vector4.zero;
            if (s_HasTexcoord1)
            {
                uv1.x = float.Parse(data[dict[string.Format("{0}.x", s_Texcoord1)]]);
                uv1.y = float.Parse(data[dict[string.Format("{0}.y", s_Texcoord1)]]);

                string key = string.Format("{0}.z", s_Texcoord1);
                if (dict.ContainsKey(key)) uv1.z = float.Parse(data[dict[key]]);

                key = string.Format("{0}.w", s_Texcoord1);
                if (dict.ContainsKey(key)) uv1.w = float.Parse(data[dict[key]]);
            }
        }
    }

    private static bool ParseProperty(Dictionary<string, int> dict, List<string> list, out string key)
    {
        key = "";
        for (int i = 0; i < list.Count; ++i)
        {
            if (dict.ContainsKey(list[i]))
            {
                key = list[i].Substring(0, list[i].IndexOf('.'));
                return true;
            }
        }
        return false;
    }

    [MenuItem("Assets/CSV To Mesh")]
    private static void ExecCSV2Mesh()
    {
        if (UnityEditor.Selection.activeObject == null)
            return;

        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (path == null)
            return;

        string[] lines = System.IO.File.ReadAllLines(path);
        if (lines.Length == 0)
            return;

        string parameterText = lines[0];
        string[] parmas = parameterText.Split(new char[1] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
        Dictionary<string, int> parameterDict = new Dictionary<string, int>();
        for (int i = 0; i < parmas.Length; ++i)
        {
            string parma = parmas[i];
            if (parma[0] == ' ')
                parma = parma.Substring(1);
            parameterDict.Add(parma, i);
        }

        if (!ParseProperty(parameterDict, s_PositionKey, out s_Position))
        {
            Debug.LogError("Error:no position property");
            return;
        }

        s_HasNormal = ParseProperty(parameterDict, s_NormalKey, out s_Normal);
        s_HasTangent = ParseProperty(parameterDict, s_TangentKey, out s_Tangent);
        s_HasColor = ParseProperty(parameterDict, s_ColorKey, out s_Color);
        s_HasTexcoord0 = ParseProperty(parameterDict, s_Texcoord0_Key, out s_Texcoord0);
        s_HasTexcoord1 = ParseProperty(parameterDict, s_Texcoord1_Key, out s_Texcoord1);

        Dictionary<int, int> vertexDict = new Dictionary<int, int>();
        List<CSVVertex> vertexList = new List<CSVVertex>();
        List<int> indexList = new List<int>();

        for (int i = 1; i < lines.Length; ++i)
        {
            string[] data = lines[i].Split(new char[1] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            int key = int.Parse(data[parameterDict["IDX"]]);
            int vertexIndex = -1;
            if (!vertexDict.ContainsKey(key))
            {
                vertexIndex = vertexList.Count;

                CSVVertex vertex = new CSVVertex(vertexIndex, data, parameterDict);
                vertexList.Add(vertex);
                vertexDict.Add(key, vertexIndex);
            }
            else
            {
                vertexIndex = vertexDict[key];
            }
            indexList.Add(vertexIndex);
        }

        if (vertexList.Count == 0 || indexList.Count == 0)
            return;

        List<Vector3> vertexArray = new List<Vector3>();
        List<Vector3> normalArray = new List<Vector3>();
        List<Vector4> tangentArray = new List<Vector4>();
        List<Color> colorArray = new List<Color>();
        List<Vector4> uv0Array = new List<Vector4>();
        List<Vector4> uv1Array = new List<Vector4>();
        //List<Vector2> uv3Array = new List<Vector2>();

        for (int i = 0; i < vertexList.Count; ++i)
        {
            vertexArray.Add(vertexList[i].position);
            normalArray.Add(vertexList[i].normal);
            tangentArray.Add(vertexList[i].tangent);
            colorArray.Add(vertexList[i].color);
            uv0Array.Add(vertexList[i].uv);
            uv1Array.Add(vertexList[i].uv1);
        }

        int numTriangles = (int)(indexList.Count / 3);
        int[] triangles = new int[indexList.Count];
        for (int i = 0; i < numTriangles; ++i)
        {
            int index = i * 3;
            triangles[index] = indexList[index];
            triangles[index + 1] = indexList[index + 1];
            triangles[index + 2] = indexList[index + 2];
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertexArray);
        if (s_HasNormal) mesh.SetNormals(normalArray);
        if (s_HasTangent) mesh.SetTangents(tangentArray);
        if (s_HasColor) mesh.SetColors(colorArray);
        if (s_HasTexcoord0) mesh.SetUVs(0, uv0Array);
        if (s_HasTexcoord1) mesh.SetUVs(1, uv1Array);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();

        int pos = path.LastIndexOf('/');
        string dir = path.Substring(0, pos);
        string name = path.Substring(pos + 1);
        name = name.Substring(0, name.LastIndexOf('.'));
        AssetDatabase.CreateAsset(mesh, string.Format("{0}/{1}.asset", dir, name));
    }
}
