using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Reflection;

public static class ShaderVariantCollect
{
    private class ShaderData
    {
        public string name;
        public List<string> keywords;
    }

    private class ShaderVariantCollectionFile
    {
        public string filePath;
        public ShaderVariantCollection svc;
    }

    private static readonly Dictionary<string, PassType> s_PassTypeTable;

    static ShaderVariantCollect()
    {
        s_PassTypeTable = new Dictionary<string, PassType>();
        s_PassTypeTable.Add("UniversalForward".ToLower(), PassType.ScriptableRenderPipeline);
        s_PassTypeTable.Add("PDO".ToLower(), PassType.ScriptableRenderPipeline);
        s_PassTypeTable.Add("ObjectTrails".ToLower(), PassType.ScriptableRenderPipeline);
        s_PassTypeTable.Add("ShadowCaster".ToLower(), PassType.ShadowCaster);
    }

    private static string[] GetAllTypeAssetPaths(string filter)
    {
        string[] guids = AssetDatabase.FindAssets(filter);
        if (guids != null)
        {
            string[] paths = new string[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            }
            return paths;
        }
        return null;
    }

    private static List<ShaderVariantCollectionFile> FindShaderVariantCollections()
    {
        var result = new List<ShaderVariantCollectionFile>();
        var paths = GetAllTypeAssetPaths("t:ShaderVariantCollection");
        if (paths != null)
        {
            foreach (var path in paths)
            {
                var svc = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(path);
                if (svc != null)
                {
                    var item = new ShaderVariantCollectionFile();
                    item.svc = svc;
                    item.filePath = path;
                    result.Add(item);
                }
            }
        }
        return result;
    }

    private static List<ShaderVariantCollection.ShaderVariant> GetShaderVariants(Material material)
    {
        var result = new List<ShaderVariantCollection.ShaderVariant>();

        var shader = material.shader;
        var shaderPath = AssetDatabase.GetAssetPath(shader);
        var assetImporter = AssetImporter.GetAtPath(shaderPath);
        if (assetImporter == null)
            return result;

        for (int i = 0; i < material.passCount; ++i)
        {
            string shaderTag = shader.FindPassTagValue(i, new ShaderTagId("LightMode")).name;
            s_PassTypeTable.TryGetValue(shaderTag.ToLower(), out var passType);
            if(passType == PassType.Normal && UniversalRenderPipeline.asset)
                passType = PassType.ScriptableRenderPipeline;

            PassIdentifier identifier = new PassIdentifier();
            Type passIdentifierType = typeof(PassIdentifier);
            FieldInfo fieldInfo = passIdentifierType.GetField("m_PassIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            object obj = identifier;
            fieldInfo.SetValue(obj, (uint)i);
            identifier = (PassIdentifier)obj;

            List<string> keywords = ListPool<string>.Get();
            {
                var passKeywords = ShaderUtil.GetPassKeywords(shader, identifier);
                for (int j = 0; j < passKeywords.Length; ++j)
                {
                    int index = Array.IndexOf(material.shaderKeywords, passKeywords[j].name);
                    if (index != -1)
                        keywords.Add(material.shaderKeywords[index]);
                }

                var sv = new ShaderVariantCollection.ShaderVariant();
                sv.shader = material.shader;
                sv.keywords = keywords.ToArray();
                sv.passType = passType;
                result.Add(sv);
            }
            ListPool<string>.Release(keywords);
        }

        return result;
    }

    #region 材质

    private static List<Material> FindMaterials()
    {
        List<Material> materials = new List<Material>();
        Dictionary<int, int> keyDict = new Dictionary<int, int>();

        // 搜索asset表
        SearchAssetTable(materials, keyDict);

        // 搜索DummyMaterial目录
        SearchDummyMaterials(materials, keyDict);

        return materials;
    }

    private static void SearchAssetTable(List<Material> list, Dictionary<int, int> keyDict)
    {
        AssetManagerSetup.Setup();

        // 根据Assets表获取材质
        AssetInfo.getAssetTable = () =>
        {
            CSVAssets.Load();
            List<AssetInfo> assetInfoList = new List<AssetInfo>(CSVAssets.GetAllDict(true).Count);

            var iter = CSVAssets.GetAllDict(true).GetEnumerator();
            while (iter.MoveNext())
            {
                CSVAssets assets = iter.Current.Value;
                if (assets.suffix == "prefab" || assets.suffix == "mat")
                {
                    AssetInfo assetInfo = new AssetInfo(assets.id, assets.dir, assets.name, assets.suffix);
                    assetInfoList.Add(assetInfo);
                }
            }
            iter.Dispose();

            CSVAssets.Unload();
            return assetInfoList;
        };

        // 加载资源表
        AssetInfo.Load(true);

        // 搜索材质
        if (AssetInfo.count > 0)
        {
            int index = 0, total = AssetInfo.count;
            Dictionary<int, AssetInfo>.Enumerator iter = AssetInfo.GetEnumerator();
            while (iter.MoveNext())
            {
                EditorUtility.DisplayProgressBar("Collection material", "Please wait...(" + (index + 1) + " | " + total + ")", 1.0f * index / total);
                ++index;

                string filename = "Assets/Res/" + iter.Current.Value.resourcesPath + "." + iter.Current.Value.suffix;
                if (iter.Current.Value.suffix == "mat")
                {
                    var m = AssetDatabase.LoadAssetAtPath<Material>(filename);
                    if (m != null && !keyDict.ContainsKey(m.GetHashCode()))
                    {
                        keyDict.Add(m.GetHashCode(), m.GetHashCode());
                        list.Add(m);
                    }
                }
                else
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(filename);
                    if (prefab == null)
                        continue;
                    Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
                    foreach (var r in renderers)
                    {
                        for (int i = 0; i < r.sharedMaterials.Length; ++i)
                        {
                            Material m = r.sharedMaterials[i];
                            if (m != null && !keyDict.ContainsKey(m.GetHashCode()))
                            {
                                keyDict.Add(m.GetHashCode(), m.GetHashCode());
                                list.Add(m);
                            }
                        }
                    }
                }
            }
            iter.Dispose();
        }
    }

    private static void SearchDummyMaterials(List<Material> list, Dictionary<int, int> keyDict)
    {
        string[] searchPath = new string[] { "Assets/Res/DummyMaterial" };
        string[] guids = AssetDatabase.FindAssets("t:Material", searchPath);
        for (int i = 0; i < guids.Length; ++i)
        {
            EditorUtility.DisplayProgressBar("Collection dummy materials", "Please wait...(" + (i + 1) + " | " + guids.Length + ")", 1.0f * i / guids.Length);

            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null && !keyDict.ContainsKey(m.GetHashCode()))
            {
                keyDict.Add(m.GetHashCode(), m.GetHashCode());
                list.Add(m);
            }
        }
    }

    #endregion

    [MenuItem("Tools/Shader变体工具/搜索Shader变体并更新")]
    private static void Collect()
    {
        string path = "Assets/Res/ShaderVariants";
        string fileName = "/dummy_shadervar.shadervariants";
        string filePath = path + fileName;

        // 删除旧数据
        if (Directory.Exists(path))
        {
            string[] files = Directory.GetFiles(path);
            foreach (var file in files)
                AssetDatabase.DeleteAsset(file);
            AssetDatabase.Refresh();
        }

        var existedSVCFile = false;
        var alwaysContinue = false;

        var shaderVariantCollectionFiles = FindShaderVariantCollections();
        ShaderVariantCollection collection = null;
        if (shaderVariantCollectionFiles.Count > 0)
        {
            existedSVCFile = true;
            collection = shaderVariantCollectionFiles[0].svc;
            filePath = shaderVariantCollectionFiles[0].filePath;
            collection.Clear();
        }
        else
        {
            collection = new ShaderVariantCollection();
            Directory.CreateDirectory(path);
        }

        var materials = FindMaterials();
        for (int i = 0; i < materials.Count; ++i)
        {
            try
            {
                var shaderVariants = GetShaderVariants(materials[i]);
                for (int j = 0; j < shaderVariants.Count; ++j)
                {
                    bool contains = false;
                    var variant = shaderVariants[j];
                    EditorUtility.DisplayProgressBar("完成进度", string.Format("当前材质：{0},着色器:{1}", materials[i].name, variant.shader.name), (1.0f * i / materials.Count));

                    for (int k = 0; k < shaderVariantCollectionFiles.Count; k++)
                    {
                        var svcFile = shaderVariantCollectionFiles[k];
                        if (svcFile.svc.Contains(variant))
                        {
                            contains = true;
                            break;
                        }
                    }

                    if (!contains && !collection.Contains(variant))
                    {
                        collection.Add(variant);
                    }
                }
            }
            catch (Exception ex)
            {
                if (alwaysContinue || EditorUtility.DisplayDialog("异常", string.Format("{0},\n是否继续？", ex.Message), "是", "否"))
                {
                    alwaysContinue = true;
                    continue;
                }
                else
                {
                    EditorUtility.ClearProgressBar();
                    return;
                }
            }
        }

        if (!existedSVCFile)
            AssetDatabase.CreateAsset(collection, filePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();
    }
}
