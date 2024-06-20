using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.GZip;
using System.Runtime.CompilerServices;
using ICSharpCode.SharpZipLib.Tar;

public static class ZipUtils
{
    /// <summary>
    /// 压缩包大小
    /// </summary>
    private static long s_AllSize = 0;

    /// <summary>
    /// 当前解压文件的大小
    /// </summary>
    private static long s_CurSize = 0;

    static ZipUtils()
    {
        ZipConstants.DefaultCodePage = System.Text.Encoding.UTF8.CodePage;
    }

    #region 压缩

    /// <summary>
    /// 压缩一个目录
    /// </summary>
    /// <param name="sourcePath">源文件路径</param>
    /// <param name="desPath">压缩后路径 path.</param>
    /// <param name="isContainRoot">是否包含根目录</param>
    /// <param name="isEncrypt">是否加密</param>
    /// <param name="isRootLower">根目录是否小写</param>
    public static void ZipDir(string sourcePath, string desPath, bool isContainRoot = true, bool isRootLower = false)
    {
        if (sourcePath[sourcePath.Length - 1] != Path.DirectorySeparatorChar)
        {
            ZipOutputStream zipStream = new ZipOutputStream(File.Create(desPath));
            zipStream.SetLevel(9);

            string folder = sourcePath.Replace("\\", "/");
            folder = folder.Substring(folder.LastIndexOf("/") + 1);
            folder = folder + "/";

            CreateZipFiles(sourcePath, zipStream, folder, isContainRoot, isRootLower);

            try
            {
                zipStream.Finish();
                zipStream.Close();
            }
            catch (Exception e)
            {
                Debug.LogError(e.StackTrace);
            }
        }
    }

    private static void CreateZipFiles(string sourcePath, ZipOutputStream zipStream, string folder, bool isContainRoot = true, bool isRootLower = false)
    {
        string[] filesArray = Directory.GetFileSystemEntries(sourcePath);
        foreach (string file in filesArray)
        {
            if (Directory.Exists(file)) // 如果是文件夹，递归
            {
                //过滤svn
                if (Path.GetExtension(file).Equals(".svn"))
                    continue;

                CreateZipFiles(file, zipStream, folder, isContainRoot, isRootLower);
            }
            else //如果是文件，开始压缩 
            {
                // 过滤meta
                if (Path.GetExtension(file).Equals(".meta"))
                    continue;

                using (Stream fileStream = File.OpenRead(file))
                {
                    byte[] buffer = new byte[fileStream.Length];
                    fileStream.Read(buffer, 0, buffer.Length);
                    string tempFile = file.Replace("\\", "/");
                    // 修改LastIndexOf为IndexOf，处理目录名结尾包含顶层目录名时裁剪路径错误的bug
                    tempFile = tempFile.Substring(tempFile.IndexOf(folder));
                    if (!isContainRoot) //如果不包含根目录，去除根目录
                    {
                        tempFile = tempFile.Substring(tempFile.IndexOf(folder) + folder.Length);
                    }
                    else
                    {
                        if (isRootLower)
                            tempFile = folder.ToLower() + tempFile.Substring(tempFile.IndexOf(folder) + folder.Length);
                    }

                    ZipEntry entry = new ZipEntry(tempFile);
                    entry.Size = fileStream.Length;
                    zipStream.PutNextEntry(entry);
                    zipStream.Write(buffer, 0, buffer.Length);
                    fileStream.Close();
                }
            }
        }
    }

    #endregion

    #region 解压

    /// <summary>
    /// 解压zip格式的文件
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="unZipDir"></param>
    /// <param name="isSafeSave"></param>
    /// <returns></returns>
    public static string UnZipFileByBytes(byte[] bytes, string unZipDir, bool isSafeSave)
    {
        if (bytes == null)
            return "bytes为空";

        System.IO.Stream stream = new System.IO.MemoryStream(bytes);
        return UnZipFile(stream, unZipDir, isSafeSave);
    }

    /// <summary>
    /// 解压zip格式的文件
    /// </summary>
    /// <param name="zipFilePath"></param>
    /// <param name="unZipDir"></param>
    /// <returns></returns>
    public static string UnZipFile(string zipFilePath, string unZipDir = null)
    {
        if (string.IsNullOrEmpty(zipFilePath))
        {
            Debug.LogError("压缩文件不能为空！");
            return "(ZipFilePath is Null)";
        }

        if (File.Exists(zipFilePath) == false)
        {
            Debug.LogError("压缩文件不存在！");
            return "(Not find the file:" + zipFilePath + ")";
        }

        // 解压文件夹为空时默认与压缩文件同一级目录下，跟压缩文件同名的文件夹
        if (string.IsNullOrEmpty(unZipDir))
            unZipDir = zipFilePath.Replace(Path.GetFileName(zipFilePath), Path.GetFileNameWithoutExtension(zipFilePath));

        if (!unZipDir.EndsWith("\\"))
            unZipDir += Path.DirectorySeparatorChar;

        if (!Directory.Exists(unZipDir))
            Directory.CreateDirectory(unZipDir);

        return UnZipFile(File.OpenRead(zipFilePath), unZipDir, false);
    }

    /// <summary>
    /// 解压zip格式的文件
    /// </summary>
    /// <param name="inputStream"></param>
    /// <param name="unZipDir"></param>
    /// <param name="isSafeSave"></param>
    /// <returns></returns>
    public static string UnZipFile(Stream inputStream, string unZipDir, bool isSafeSave)
    {
        ZipInputStream zis = null;
        FileStream streamWriter = null;

        try
        {
            s_AllSize = inputStream.Length;
            s_CurSize = 0;

            zis = new ZipInputStream(inputStream);

            ZipEntry zipEntry;
            while ((zipEntry = zis.GetNextEntry()) != null)
            {
                s_CurSize += zipEntry.CompressedSize;
                string directoryName = Path.GetDirectoryName(zipEntry.Name);
                string fileName = Path.GetFileName(zipEntry.Name);

                //检查目录是否存在
                if (directoryName == null)
                {
#if UNITY_EDITOR
                    Debug.LogError("目录为空，需要过滤");
#endif
                    continue;
                }

                // 安全防范，因为directoryName不能/开头
                if (directoryName.Length > 0)
                {
                    if (directoryName.StartsWith("/"))
                        directoryName = directoryName.Substring(1);
                }

                string directoryPath = Path.Combine(unZipDir, directoryName);

                // 解压目录不存在，则创建
                if (Directory.Exists(directoryPath) == false)
                    Directory.CreateDirectory(directoryPath);

                // 检查文件名
                if (string.IsNullOrEmpty(fileName))
                    continue;

                // 注意：先写入临时文件,成功之后再覆盖原有文件,然后删除临时文件
                string savePath = Path.Combine(directoryPath, fileName);
                string tempPath;
                if (isSafeSave)
                    tempPath = savePath + ".temp";
                else
                    tempPath = savePath;

                // 读取文件数据
                streamWriter = File.Create(tempPath);
                int buffSize = 2048, size = 0;
                byte[] data = new byte[buffSize];
                while (true)
                {
                    size = zis.Read(data, 0, data.Length);
                    if (size <= 0)
                        break;
                    streamWriter.Write(data, 0, size);
                }
                streamWriter.Close();

                if (isSafeSave)
                {
                    if (File.Exists(savePath))
                        File.Delete(savePath);
                    File.Move(tempPath, savePath);
                }
            }
        }
        catch (Exception e)
        {
            s_AllSize = 0;
            s_CurSize = 0;
            Debug.LogError(e.StackTrace);

            string error = e.StackTrace;
            if (string.IsNullOrEmpty(error) == false && error.Length > 100)
                return "(" + e.Message + "\n" + error.Substring(0, 50) + "\n" + error.Substring(error.Length - 16, 16) + ")";
            else
                return "(" + e.Message + "\n" + error + ")";
        }
        finally
        {
            if (streamWriter != null)
            {
                try
                {
                    // 关闭流
                    streamWriter.Close();
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e.StackTrace);
                }
                streamWriter = null;
            }

            if (zis != null)
            {
                try
                {
                    // 关闭流
                    zis.Close(); 
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e.StackTrace);
                }
                zis = null;
            }
        }

        s_AllSize = s_CurSize = 0;
        return null;
    }

    #endregion
}
