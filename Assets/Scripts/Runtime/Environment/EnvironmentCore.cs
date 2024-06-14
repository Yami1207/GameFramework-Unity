using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EnvironmentSetting;

[ExecuteInEditMode]
public class EnvironmentCore : SingletonMono<EnvironmentCore>
{
    private static class ShaderConstants
    {
        public static readonly int windParameterPropID = Shader.PropertyToID("_G_WindParameter");
    }

    [SerializeField]
    private EnvironmentAsset m_Asset;
    
    public ObjectTrailsConfig objectTrails { get { return m_Asset != null ? m_Asset.objectTrails : null; } }

#if UNITY_EDITOR
    private void OnEnable()
    {
        if (m_Asset == null && !Application.isPlaying)
        {
            AssetInfo.getAssetTable = () =>
            {
                CSVAssets.Load();
                List<AssetInfo> assetInfoList = new List<AssetInfo>(CSVAssets.GetAllDict(true).Count);

                var iter = CSVAssets.GetAllDict(true).GetEnumerator();
                while (iter.MoveNext())
                {
                    CSVAssets assets = iter.Current.Value;
                    AssetInfo assetInfo = new AssetInfo(assets.id, assets.dir, assets.name, assets.suffix);
                    assetInfoList.Add(assetInfo);
                }
                iter.Dispose();

                CSVAssets.Unload();
                return assetInfoList;
            };

            m_Asset = AssetManager.instance.LoadAsset<EnvironmentAsset>(8000);
        }
    }
#endif

    private void LateUpdate()
    {
        if (m_Asset == null)
            return;

        SetupWind(m_Asset.wind);
    }

    private void SetupWind(EnvironmentAsset.Wind wind)
    {
        Shader.SetGlobalVector(ShaderConstants.windParameterPropID, new Vector4(wind.speedX, wind.speedZ, wind.intensity));
    }
}
