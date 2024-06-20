using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class EditorFileUtil
{
    private static string s_ProjectFolderPath = string.Empty;
    private static string s_ProjectTempFolderPath = string.Empty;

    public static string GetProjectFolderPath()
    {
        if (string.IsNullOrEmpty(s_ProjectFolderPath))
            s_ProjectFolderPath = System.IO.Directory.GetParent(Application.dataPath).FullName;
        return s_ProjectFolderPath;
    }

    public static string GetProjectTempFolderPath()
    {
        if (string.IsNullOrEmpty(s_ProjectTempFolderPath))
            s_ProjectTempFolderPath = System.IO.Path.Combine(GetProjectFolderPath(), "Temp");
        return s_ProjectTempFolderPath;
    }

    private readonly static Dictionary<string, string> s_ProjectFolderPathDict = new Dictionary<string, string>();

    private static string GetProectFolderPath(string signatureFile)
    {
        if (s_ProjectFolderPathDict.ContainsKey(signatureFile))
            return s_ProjectFolderPathDict[signatureFile];
        return string.Empty;
    }

    private static bool ValidateSignatureFileRoot(string signatureFile, string dir)
    {
        return !string.IsNullOrEmpty(dir) && File.Exists(Path.Combine(dir, signatureFile));
    }

    public static string GetInstallDirectory(string signatureFile)
    {
        string fullPath = GetProectFolderPath(signatureFile);
        if (ValidateSignatureFileRoot(signatureFile, fullPath))
            return fullPath;

        fullPath = FindInstallFormSignatureFile(signatureFile);
        if (!string.IsNullOrEmpty(fullPath))
        {
            s_ProjectFolderPathDict.Add(signatureFile, fullPath);
            return fullPath;
        }

        fullPath = FindInstallPathFromSourceCodeFilePath(signatureFile);
        if (!string.IsNullOrEmpty(fullPath))
        {
            s_ProjectFolderPathDict.Add(signatureFile, fullPath);
            return fullPath;
        }

        Debug.LogErrorFormat("找不到{0}安装目录...", signatureFile);
        return string.Empty;
    }

    private static string FindInstallFormSignatureFile(string signatureFile)
    {
        string dir = string.Empty;
        string[] matches = Directory.GetFiles("Assets", signatureFile, SearchOption.AllDirectories);
        foreach (var match in matches)
        {
            try
            {
                // 回退目录
                var parent = System.IO.Directory.GetParent(match);
                if (parent == null)
                    continue;
                dir = parent.FullName;
            }
            catch (System.Exception)
            {
            }

            dir = dir.Replace("\\", "/") + "/";
            if (ValidateSignatureFileRoot(signatureFile, dir))
                return GetAssetPathFromFullPath(dir);
        }

        return string.Empty;
    }

    private static string FindInstallPathFromSourceCodeFilePath(string signatureFile)
    {
        string csPath = new System.Diagnostics.StackTrace(true).GetFrame(0).GetFileName();
        if (string.IsNullOrEmpty(csPath))
            return string.Empty;
        string dirPath = System.IO.Path.GetDirectoryName(csPath);
        dirPath = System.IO.Directory.GetParent(dirPath).FullName;
        dirPath = dirPath.Replace("\\", "/");
        if (!dirPath.EndsWith("/"))
            dirPath += '/';
        if (ValidateSignatureFileRoot(signatureFile, dirPath))
            return GetAssetPathFromFullPath(dirPath);
        return string.Empty;
    }

    /// <summary>
    /// 把全路径转为Asset路径
    /// </summary>
    /// <param name="fullPath"></param>
    /// <returns></returns>
    public static string GetAssetPathFromFullPath(string fullPath)
    {
        string assetPath = Application.dataPath;
        int index = assetPath.LastIndexOf("Assets");
        assetPath = assetPath.Substring(0, index);
        if (fullPath.StartsWith(assetPath))
            return fullPath.Substring(assetPath.Length);
        return fullPath;
    }
}
