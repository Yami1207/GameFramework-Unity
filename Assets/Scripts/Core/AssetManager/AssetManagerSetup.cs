using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AssetManagerSetup
{
    public static void Setup()
    {
        SettingManager.instance.Init();

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
    }
}
