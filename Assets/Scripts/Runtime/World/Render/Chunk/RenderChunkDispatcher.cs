using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class RenderChunkDispatcher
{
    #region 提交到主线程的数据

    private class PendingUpload : System.IComparable<PendingUpload>
    {
        private readonly RenderChunkCompileTask m_Task;

        private readonly System.Action m_UploadFutureTask;

        private readonly float m_Distance;

        public PendingUpload(RenderChunkCompileTask task, System.Action futureTask, float dist)
        {
            m_Task = task;
            m_UploadFutureTask = futureTask;
            m_Distance = dist;
        }

        public void Run()
        {
            m_UploadFutureTask.Invoke();
        }

        public void ForceFinished()
        {
            m_Task.Finish(false);
        }

        public int CompareTo(PendingUpload other)
        {
            return m_Distance.CompareTo(other.m_Distance);
        }
    }

    #endregion

    private RenderWorld m_RenderWorld;

    private BufferPool m_BufferPool;

    /// <summary>
    /// 主线程中使用rebuild chunk流程
    /// </summary>
    private readonly RenderChunkWorker m_MainRenderWorker;

    private readonly List<System.Threading.Thread> m_ThreadingList = new List<Thread>();
    private readonly List<RenderChunkWorker> m_ThreadingRenderWorkerList = new List<RenderChunkWorker>();

    /// <summary>
    /// 所使用的数据缓冲
    /// </summary>
    private readonly Queue<RenderChunkCacheData> m_RenderChunkCaches;

    /// <summary>
    /// 在子线程中要Rebuild的section(排队中)
    /// </summary>
    private readonly Queue<RenderChunkCompileTask> m_ThreadingTaskQueue = new Queue<RenderChunkCompileTask>();
    private volatile int m_ThreadingTaskQueueCount = 0;//防止竞争

    private readonly Queue<PendingUpload> m_ThreadingUploadQueue = new Queue<PendingUpload>();

    public RenderChunkDispatcher(RenderWorld renderWorld, BufferPool bufferPool)
    {
        m_RenderWorld = renderWorld;
        m_BufferPool = bufferPool;

        int processorCount = UnityEngine.Mathf.Min(UnityEngine.SystemInfo.processorCount, 4);
        int threadCount = UnityEngine.Mathf.Max(1, processorCount);

        // 创建数据缓冲
        m_RenderChunkCaches = new Queue<RenderChunkCacheData>();
        for (int i = 0; i < threadCount; ++i)
            m_RenderChunkCaches.Enqueue(new RenderChunkCacheData(m_BufferPool));

        // 子线程
        for (int i = 0; i < threadCount; ++i)
        {
            // 每个线程都分配避免互相之间锁而挂起
            RenderChunkWorker renderWorker = new RenderChunkWorker(this);
            Thread thread = new Thread(renderWorker.RunThreading);
            thread.IsBackground = true;
            //_thread.Priority = ThreadPriority.Highest;
            thread.Start();
            m_ThreadingRenderWorkerList.Add(renderWorker);
            m_ThreadingList.Add(thread);
        }

        m_MainRenderWorker = new RenderChunkWorker(this);
    }

    public void Destroy()
    {
        StopWorkerThreads();

        m_RenderWorld = null;
        m_BufferPool = null;
    }

    public bool IsNeedToWait()
    {
        return m_ThreadingTaskQueue.Count > 0 || m_ThreadingUploadQueue.Count > 0;
    }

    public void RunUploadInMainThreading(long finishTime)
    {
        while (true)
        {
            bool isContinue = false;

            // 如果没有启动多线程，则在主线程执行任务
            if (m_ThreadingList.Count <= 0)
            {
                RenderChunkCompileTask task = GetNextCompileTask();
                if (task != null)
                {
                    try
                    {
                        m_MainRenderWorker.ProcessTask(task);
                        isContinue = true;
                    }
                    catch (System.Threading.ThreadInterruptedException)
                    {
                    }
                }
            }

            PendingUpload upload = null;
            lock (m_ThreadingUploadQueue)
            {
                if (m_ThreadingUploadQueue.Count > 0)
                    upload = m_ThreadingUploadQueue.Dequeue();
            }
            if (upload != null)
            {
                upload.Run();
                isContinue = true;
            }

            if (RenderUtil.IsRealTimeOut(finishTime) || !isContinue)
                break;
        }
    }

    #region Update RenderChunk

    public bool AddRenderChunk(RenderChunk renderChunk)
    {
        try
        {
            RenderChunkCompileTask compileTask = renderChunk.CreateCompileTask(long.MaxValue);
            if (compileTask == null)
                return false;

            // 添加后续任务
            compileTask.AddFinishRunnable(() =>
            {
                RenderChunkCompileTask task;
                lock (m_ThreadingTaskQueue)
                {
                    task = m_ThreadingTaskQueue.Dequeue();
                    --m_ThreadingTaskQueueCount;
                }

                // 当任务异常的情况下,需要重新提交渲染
                if (task.renderChunk.isNeedReRerender)
                {
                    m_RenderWorld.AddRenderChunkToNextFrame(task.renderChunk);
                    task.renderChunk.isNeedReRerender = false;
                }
            });

            // 添加到任务队列
            lock (m_ThreadingTaskQueue)
            {
                m_ThreadingTaskQueue.Enqueue(compileTask);
                ++m_ThreadingTaskQueueCount;
            }
        }
        catch (System.Threading.ThreadInterruptedException)
        {
        }

        return true;
    }

    #endregion

    #region Worker

    /// <summary>
    /// 停止所有多线程
    /// </summary>
    private void StopWorkerThreads()
    {
        // 清除等待派分的任务
        ClearCompileTask();

        // 通知线程
        for (int i = 0; i < m_ThreadingRenderWorkerList.Count; ++i)
            m_ThreadingRenderWorkerList[i].NotifyToStop();

        // 强制停止子线程
        for (int i = 0; i < m_ThreadingList.Count; ++i)
        {
            try
            {
                m_ThreadingList[i].Interrupt();
                m_ThreadingList[i].Abort();
            }
            catch (System.Threading.ThreadInterruptedException)
            {
            }
        }

        // 清理所有子线程已经完成
        lock (m_ThreadingUploadQueue)
        {
            while (m_ThreadingUploadQueue.Count > 0)
            {
                PendingUpload upload = m_ThreadingUploadQueue.Dequeue();
                if (upload != null)
                    upload.ForceFinished();
            }
        }

        m_RenderChunkCaches.Clear();
    }

    #endregion

    #region 多线程中使用的数据

    /// <summary>
    /// 分配线程中的数据
    /// </summary>
    /// <returns></returns>
    public RenderChunkCacheData AllocateThreadingData()
    {
        RenderChunkCacheData data = null;
        lock (m_RenderChunkCaches)
        {
            if (m_RenderChunkCaches.Count == 0)
                data = new RenderChunkCacheData(m_BufferPool);
            else
                data = m_RenderChunkCaches.Dequeue();
        }
        return data;
    }

    public void FreeThreadingData(RenderChunkCacheData data)
    {
        if (data == null)
            return;

        lock (m_RenderChunkCaches)
        {
            data.Clear();
            m_RenderChunkCaches.Enqueue(data);
        }
    }

    #endregion

    #region Upload

    public void AddUploadTask(System.Action futureTask, RenderChunkCompileTask task, float dist)
    {
        lock (m_ThreadingUploadQueue)
        {
            m_ThreadingUploadQueue.Enqueue(new PendingUpload(task, futureTask, dist));
        }
    }

    public void UploadCompileTaskResult(RenderChunkCompileTask task)
    {
        Debug.Assert(RenderUtil.isMainThread);
        if (!task.isFinished)
        {
            RenderChunk renderChunk = task.renderChunk;
            renderChunk.RebuildMesh(task, this);
            m_RenderWorld.NotifyRenderChunkFinish(renderChunk);
        }
    }

    #endregion

    #region 多线程任务 CompileTask

    private void ClearCompileTask()
    {
        lock (m_ThreadingTaskQueue)
        {
            while (m_ThreadingTaskQueue.Count > 0)
            {
                var task = m_ThreadingTaskQueue.Dequeue();
                --m_ThreadingTaskQueueCount;

                if (task != null)
                    task.Finish(false);
            }
        }
    }

    /// <summary>
    /// 取出正在等待多线程的渲染任务，在多线程中调用
    /// </summary>
    /// <returns></returns>
    public RenderChunkCompileTask GetNextCompileTask()
    {
        RenderChunkCompileTask task = null;
        lock (m_ThreadingTaskQueue)
        {
            if (m_ThreadingTaskQueue.Count > 0)
            {
                task = m_ThreadingTaskQueue.Dequeue();
                --m_ThreadingTaskQueueCount;
            }
        }
        return task;
    }

    #endregion
}
