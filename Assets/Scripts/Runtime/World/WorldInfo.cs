using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct MapData
{
    public int id;

    /// <summary>
    /// 起始位置
    /// </summary>
    public ChunkPos pos;

    public Material terrainStandard;
    public Material terrainAddStandard;
    public Material terrainLow;
}

public class WorldInfo
{
    public string name { private set; get; }

    private ChunkPos m_MinChunkPos;
    public ChunkPos minChunkPos { get { return m_MinChunkPos; } }

    private ChunkPos m_MaxChunkPos;
    public ChunkPos maxChunkPos { get { return m_MaxChunkPos; } }

    private readonly Dictionary<Vector2Int, MapData> m_MapDataDict = new Dictionary<Vector2Int, MapData>();
    public Dictionary<Vector2Int, MapData> mapDataDict { get { return m_MapDataDict; } }

    public void Load(string path)
    {
        var textAsset = AssetManager.instance.LoadAsset<TextAsset>(path);
        if (textAsset == null)
            return;
        var jsonObject = JObject.Parse(textAsset.text);
        if (jsonObject == null)
            return;

        // 名称
        name = (string)jsonObject.SelectToken("name");

        // 获取世界范围
        int minX = int.MaxValue, maxX = int.MinValue;
        int minZ = int.MaxValue, maxZ = int.MinValue;
        var scenesObject = jsonObject.SelectToken("scenes");
        if (scenesObject != null)
        {
            var iter = scenesObject.Children().GetEnumerator();
            while (iter.MoveNext())
            {
                int mapID = iter.Current.GetValue("map_id", false, 0);
                if (mapID == 0)
                    continue;

                Vector2Int scenePos = new Vector2Int(iter.Current.GetValue("x", false, 0), iter.Current.GetValue("z", false, 0));
                int startChunkX = scenePos.x * Define.kSceneSideLength, endChunkX = startChunkX + Define.kSceneSideLengthMinusOne;
                int startChunkZ = scenePos.y * Define.kSceneSideLength, endChunkZ = startChunkZ + Define.kSceneSideLengthMinusOne;

                minX = Mathf.Min(minX, startChunkX);
                maxX = Mathf.Max(maxX, endChunkX);
                minZ = Mathf.Min(minZ, startChunkZ);
                maxZ = Mathf.Max(minZ, endChunkZ);

                Material terrainStandard = null, terrainAddStandard = null, terrainLow = null;
                var terrainToken = iter.Current.SelectToken("terrain");
                if (terrainToken != null)
                {
                    int assetPath = terrainToken.GetValue("base", false, -1);
                    terrainStandard = AssetManager.instance.LoadAsset<Material>(assetPath);

                    assetPath = terrainToken.GetValue("add", false, -1);
                    terrainAddStandard = AssetManager.instance.LoadAsset<Material>(assetPath);

                    assetPath = terrainToken.GetValue("low", false, -1);
                    terrainLow = AssetManager.instance.LoadAsset<Material>(assetPath);
                }

                MapData data = new MapData()
                {
                    id = mapID,
                    pos = new ChunkPos(startChunkX, startChunkZ),
                    terrainStandard = terrainStandard,
                    terrainAddStandard = terrainAddStandard,
                    terrainLow = terrainLow
                };
                m_MapDataDict.Add(scenePos, data);
            }
            iter.Dispose();
        }

        m_MinChunkPos = new ChunkPos(minX, minZ);
        m_MaxChunkPos = new ChunkPos(maxX, maxZ);
    }

    public bool TryGetMapData(Vector2Int key, out MapData data)
    {
        return m_MapDataDict.TryGetValue(key, out data);
    }
}

//namespace Framework
//{
//    public class WorldInfo
//    {
//        private int m_Identity;
//        public int id => m_Identity;

//        /// <summary>
//        /// 东西方向Chunk数 - x轴
//        /// </summary>
//        private int m_ChunkCountEW;
//        public int chunkCountEW => m_ChunkCountEW;

//        /// <summary>
//        /// 南北方向Chunk数 - z轴
//        /// </summary>
//        private int m_ChunkCountSN;
//        public int chunkCountSN => m_ChunkCountSN;

//        private float[] m_HeightMap;
//        public float[] heightMap => m_HeightMap;

//        public WorldInfo(WorldInfoAsset asset)
//        {
//            m_Identity = asset.id;
//            m_ChunkCountEW = asset.chunkColumn;
//            m_ChunkCountSN = asset.chunkRow;

//            m_HeightMap = new float[m_ChunkCountEW * m_ChunkCountSN * Define.CHUNK_SIDE_LENGTH * Define.CHUNK_SIDE_LENGTH];
//            if (asset.heightMap != null && asset.heightMap.isReadable)
//            {
//                Color[] pixels = asset.heightMap.GetPixels();
//                if (pixels.Length == m_HeightMap.Length)
//                {
//                    for (int i = 0; i < pixels.Length; ++i)
//                        m_HeightMap[i] = pixels[i].r;
//                }
//                else
//                {

//                }
//            }
//        }
//    }

//    public class WorldInfoAsset : ScriptableObject
//    {
//        [System.Serializable]
//        public struct TerrainData
//        {
//            public Mesh mesh;
//            public Material material;
//        }

//        [SerializeField]
//        private int m_Identity = 0;
//        public int id => m_Identity;

//        [SerializeField]
//        private Vector3 m_Position = Vector3.zero;
//        public Vector3 position => m_Position;

//        [SerializeField]
//        private int m_ChunkRow = 32;
//        public int chunkRow => m_ChunkRow;

//        [SerializeField]
//        private int m_ChunkColumn = 32;
//        public int chunkColumn => m_ChunkColumn;

//        [SerializeField]
//        private Texture2D m_HeightMap;
//        public Texture2D heightMap => m_HeightMap;

//        [SerializeField]
//        private TerrainData m_Terrain = new TerrainData();
//        public TerrainData terrain => m_Terrain;

//#if UNITY_EDITOR
//        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812")]
//        internal class CreateWorldInfoAsset : EndNameEditAction
//        {
//            public override void Action(int instanceId, string pathName, string resourceFile)
//            {
//                var instance = CreateInstance<WorldInfoAsset>();
//                AssetDatabase.CreateAsset(instance, pathName);
//                Selection.activeObject = instance;
//            }
//        }

//        [MenuItem("Assets/Create/Example/World Info Asset")]
//        private static void CreateWorldInfo()
//        {
//            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateWorldInfoAsset>(), "WorldInfoAsset.asset", null, null);
//        }
//#endif
//    }
//}
