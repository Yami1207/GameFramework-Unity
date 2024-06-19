using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Compress.UPK
{
    public class UPKFolder
    {
        private class FileChangeInfo
        {
            public string inpath;
            public string outpath;
            public Action<long, long> progressDelegate;
        }

        /// <summary>
        /// 进度
        /// </summary>
        private class CodeProgress
        {
            public Action<long, long> m_ProgressDelegate = null;
            public CodeProgress(Action<long, long> del)
            {
                m_ProgressDelegate = del;
            }

            public void SetProgress(Int64 inSize, Int64 outSize)
            {
            }

            public void SetProgressPercent(Int64 fileSize, Int64 processSize)
            {
                m_ProgressDelegate(fileSize, processSize);
            }
        }

        private class OneFileInfo
        {
            public int id = 0;
            public int startPos = 0;
            public int size = 0;
            public int pathLength = 0;
            public string path = "";
            public byte[] data = null;
        };

        public static void PackFolder(string inpath, string outpath, Action<long, long> progress)
        {
            FileChangeInfo info = new FileChangeInfo();
            info.inpath = inpath;
            info.outpath = outpath;
            info.progressDelegate = progress;

            PackFolder(info);
        }

        private static void PackFolder(object obj)
        {
            FileChangeInfo pathinfo = (FileChangeInfo)obj;
            string inpath = pathinfo.inpath;
            string outpath = pathinfo.outpath;
            CodeProgress progress = null;
            if (pathinfo.progressDelegate != null)
                progress = new CodeProgress(pathinfo.progressDelegate);

            int id = 0, totalSize = 0;
            Dictionary<int, OneFileInfo> allFileInfoDic = new Dictionary<int, OneFileInfo>();
            string sourceDirPath = inpath.Substring(0, inpath.LastIndexOf('/'));

            // 遍历文件夹全部文件
            DirectoryInfo dirInfo = new DirectoryInfo(inpath);
            string parentDir = dirInfo.Parent.FullName.Replace("\\", "/");
            foreach (FileInfo fileinfo in dirInfo.GetFiles("*.*", SearchOption.AllDirectories))
            {
                // 无视拓展名为.meta(unity资源标识文件)
                if (fileinfo.Extension == ".meta")
                    continue;

                // 规范化相对路径
                string filename = fileinfo.FullName.Replace("\\", "/");
                filename = filename.Replace(sourceDirPath + "/", "");
                if (filename.StartsWith(parentDir))
                    filename = filename.Substring(parentDir.Length + 1);
                int filesize = (int)fileinfo.Length;

                OneFileInfo info = new OneFileInfo();
                info.id = id;
                info.size = filesize;
                info.path = filename;
                info.pathLength = new UTF8Encoding().GetBytes(filename).Length;

                // 读取这个文件
                FileStream fileStreamRead = new FileStream(fileinfo.FullName, FileMode.Open, FileAccess.Read);
                if (fileStreamRead == null)
                    return;
                byte[] filedata = new byte[filesize];
                fileStreamRead.Read(filedata, 0, filesize);
                info.data = filedata;
                fileStreamRead.Close();

                allFileInfoDic.Add(id, info);
                totalSize += filesize;
                ++id;
            }

            // UPK中前面是写每个包的ID,StartPos,size,pathLength,path
            {
                // 更新文件在UPK中的起始点
                int firstfilestartpos = 0 + 4;
                for (int index = 0; index < allFileInfoDic.Count; index++)
                    firstfilestartpos += 4 + 4 + 4 + 4 + allFileInfoDic[index].pathLength;

                int start = 0;
                for (int index = 0; index < allFileInfoDic.Count; ++index)
                {
                    if (index == 0)
                    {
                        start = firstfilestartpos;
                    }
                    else
                    {
                        // 上一个文件的开始+文件大小;
                        start = allFileInfoDic[index - 1].startPos + allFileInfoDic[index - 1].size;
                    }
                    allFileInfoDic[index].startPos = start;
                }
            }

            // 写文件
            FileStream fileStream = new FileStream(outpath, FileMode.Create);

            // 文件总数量
            byte[] totalIdBytes = System.BitConverter.GetBytes(id);
            fileStream.Write(totalIdBytes, 0, totalIdBytes.Length);

            // 文件信息
            for (int index = 0; index < allFileInfoDic.Count; ++index)
            {
                // 写入ID
                byte[] idBytes = System.BitConverter.GetBytes(allFileInfoDic[index].id);
                fileStream.Write(idBytes, 0, idBytes.Length);

                // 写入StartPos
                byte[] startPosBytes = System.BitConverter.GetBytes(allFileInfoDic[index].startPos);
                fileStream.Write(startPosBytes, 0, startPosBytes.Length);

                // 写入size
                byte[] sizeBytes = System.BitConverter.GetBytes(allFileInfoDic[index].size);
                fileStream.Write(sizeBytes, 0, sizeBytes.Length);

                // 写入pathLength
                byte[] pathLengthBytes = System.BitConverter.GetBytes(allFileInfoDic[index].pathLength);
                fileStream.Write(pathLengthBytes, 0, pathLengthBytes.Length);

                // 写入path
                byte[] pathBytes = new UTF8Encoding().GetBytes(allFileInfoDic[index].path);
                fileStream.Write(pathBytes, 0, pathBytes.Length);
            }

            // 写入文件数据
            int totalProcessSize = 0;
            foreach (var file in allFileInfoDic)
            {
                OneFileInfo info = file.Value;
                int size = info.size;
                byte[] tmpData = null;
                int processSize = 0;
                while (processSize < size)
                {
                    if (size - processSize < 1024)
                        tmpData = new byte[size - processSize];
                    else
                        tmpData = new byte[1024];
                    fileStream.Write(info.data, processSize, tmpData.Length);

                    processSize += tmpData.Length;
                    totalProcessSize += tmpData.Length;
                    if (progress != null)
                        progress.SetProgressPercent(totalSize, totalProcessSize);
                }
            }

            fileStream.Flush();
            fileStream.Close();
        }

        public static void UnpackFolder(string inpath, string outpath, Action<long, long> progress)
        {
            FileChangeInfo pathInfo = new FileChangeInfo();
            pathInfo.inpath = inpath;
            pathInfo.outpath = outpath;
            pathInfo.progressDelegate = progress;

            UnpackFolder(pathInfo);
        }

        private static void UnpackFolder(object obj)
        {
            FileChangeInfo pathinfo = (FileChangeInfo)obj;
            string inpath = pathinfo.inpath;
            string outpath = pathinfo.outpath;
            CodeProgress progress = null;
            if (pathinfo.progressDelegate != null)
                progress = new CodeProgress(pathinfo.progressDelegate);

            Dictionary<int, OneFileInfo> allFileInfoDic = new Dictionary<int, OneFileInfo>();
            System.Text.UTF8Encoding utf8Encoding = new System.Text.UTF8Encoding();
            int totalsize = 0;

            FileStream upkFileStream = new FileStream(inpath, FileMode.Open);
            upkFileStream.Seek(0, SeekOrigin.Begin);
            int offset = 0;

            //读取文件数量;
            byte[] totaliddata = new byte[4];
            upkFileStream.Read(totaliddata, 0, 4);
            int filecount = BitConverter.ToInt32(totaliddata, 0);
            offset += 4;

            //读取所有文件信息;
            for (int index = 0; index < filecount; ++index)
            {
                // 读取id;
                byte[] idBytes = new byte[4];
                upkFileStream.Seek(offset, SeekOrigin.Begin);
                upkFileStream.Read(idBytes, 0, 4);
                int id = BitConverter.ToInt32(idBytes, 0);
                offset += 4;

                // 读取StartPos;
                byte[] startPosBytes = new byte[4];
                upkFileStream.Seek(offset, SeekOrigin.Begin);
                upkFileStream.Read(startPosBytes, 0, 4);
                int startpos = BitConverter.ToInt32(startPosBytes, 0);
                offset += 4;

                // 读取size;
                byte[] sizeBytes = new byte[4];
                upkFileStream.Seek(offset, SeekOrigin.Begin);
                upkFileStream.Read(sizeBytes, 0, 4);
                int size = BitConverter.ToInt32(sizeBytes, 0);
                offset += 4;

                // 读取pathLength
                byte[] pathLengthBytes = new byte[4];
                upkFileStream.Seek(offset, SeekOrigin.Begin);
                upkFileStream.Read(pathLengthBytes, 0, 4);
                int pathLength = BitConverter.ToInt32(pathLengthBytes, 0);
                offset += 4;

                // 读取path
                byte[] pathBytes = new byte[pathLength];
                upkFileStream.Seek(offset, SeekOrigin.Begin);
                upkFileStream.Read(pathBytes, 0, pathLength);
                string path = utf8Encoding.GetString(pathBytes);
                offset += pathLength;

                // 添加到Dic
                OneFileInfo info = new OneFileInfo();
                info.id = id;
                info.size = size;
                info.pathLength = pathLength;
                info.path = path;
                info.startPos = startpos;
                allFileInfoDic.Add(id, info);

                totalsize += size;
            }

            // 解压文件;
            int totalProcessSize = 0;
            foreach (var file in allFileInfoDic)
            {
                OneFileInfo info = file.Value;

                int startPos = info.startPos;
                int size = info.size;

                string parentDir = "", filename = "";
                int pos = info.path.LastIndexOf("/");
                if (pos != -1)
                {
                    parentDir = info.path.Substring(0, info.path.LastIndexOf("/"));
                    filename = info.path.Substring(info.path.LastIndexOf("/") + 1);
                }
                else
                {
                    filename = info.path;
                }

                // 创建文件
                string dirPath = outpath + parentDir;
                if (!Directory.Exists(dirPath))
                    Directory.CreateDirectory(dirPath);

                // 判断如果已经有文件了就删除，再生成.(相当于替换)
                string filePath = dirPath + "/" + filename;
                if (File.Exists(filePath))
                    File.Delete(filePath);

                FileStream fileStream = new FileStream(filePath, FileMode.Create);
                byte[] tmpFileData;
                int processSize = 0;
                while (processSize < size)
                {
                    if (size - processSize < 1024)
                        tmpFileData = new byte[size - processSize];
                    else
                        tmpFileData = new byte[1024];

                    // 读取
                    upkFileStream.Seek(startPos + processSize, SeekOrigin.Begin);
                    upkFileStream.Read(tmpFileData, 0, tmpFileData.Length);

                    // 写入
                    fileStream.Write(tmpFileData, 0, tmpFileData.Length);

                    processSize += tmpFileData.Length;
                    totalProcessSize += tmpFileData.Length;
                    if (progress != null)
                        progress.SetProgressPercent((long)totalsize, (long)totalProcessSize);
                }
                fileStream.Flush();
                fileStream.Close();
            }
            upkFileStream.Close();
        }
    }
}
