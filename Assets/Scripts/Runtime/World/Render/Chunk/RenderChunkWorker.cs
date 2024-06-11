using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor.Search;
using UnityEngine;

public class RenderChunkWorker
{
    private readonly RenderChunkDispatcher m_Dispatcher;

    /// <summary>
    /// 多线程中是否要中断
    /// </summary>
    private bool m_ShouldRun;

    public RenderChunkWorker(RenderChunkDispatcher dispatcher)
    {
        m_ShouldRun = true;
        m_Dispatcher = dispatcher;
    }

    #region Threading 子线程专用

    public void RunThreading()
    {
        unchecked
        {
            while (m_ShouldRun)
            {
                try
                {
                    ProcessTask(m_Dispatcher.GetNextCompileTask());
                }
                catch (System.Threading.ThreadInterruptedException /*_e*/)
                {
                    // 正常现象，结束渲染就要中止线程
                    return;
                }
                catch (System.Threading.ThreadAbortException /*_e*/)
                {
                    //正常现象 场景切换的时候回强制中断   
                    return;
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e);
                    return;
                }
            }
        }
    }

    public void NotifyToStop()
    {
        m_ShouldRun = false;
    }

    #endregion

    public void ProcessTask(RenderChunkCompileTask task)
    {
        if (task == null || task.status != RenderChunkCompileTask.Status.Pending)
        {
            // 可能在线程执行时，任务被中断了
            return;
        }

        task.status = RenderChunkCompileTask.Status.Compiling;
        {
            task.SetRenderChunkCacheData(m_Dispatcher.AllocateThreadingData());
            task.renderChunk.LoadChunkData(task);
        }

        if (task.status != RenderChunkCompileTask.Status.Compiling)
        {
            // 可能在线程执行时，任务被中断了
            FreeRenderChunkCacheData(task);
            return;
        }

        task.status = RenderChunkCompileTask.Status.TurnToMainThread;
        if (RenderUtil.isMainThread)
        {
            m_Dispatcher.UploadCompileTaskResult(task);
            OnSuccess(task);
        }
        else
        {
            System.Action doTask = () =>
            {
                if (task.isFinished)
                {
                    OnFailure(task, null);
                    return;
                }

                try
                {
                    UnityEngine.Debug.Assert(RenderUtil.isMainThread);

                    // 在主线程中执行
                    m_Dispatcher.UploadCompileTaskResult(task);

                    // 生成中间数据
                    //RenderSectionCompileTaskBuffer _switch_buffer = null;
                    //if (_task.TaskType == RenderSectionCompileTask.Type.RebuildCollider)
                    //{
                    //    _switch_buffer = RenderSectionCompileTask.PullRenderSectionCompileTaskBuffer(_task);
                    //}

                    // 完成任务(释放任务资源)
                    OnSuccess(task);
                }
                catch (System.Exception e)
                {
                    OnFailure(task, e);
                }
            };

            m_Dispatcher.AddUploadTask(doTask, task, task.sortDistance);
        }
    }

    private void FreeRenderChunkCacheData(RenderChunkCompileTask task)
    {
        if (task == null || task.isReleased)
            return;

        task.isReleased = true;
        m_Dispatcher.FreeThreadingData(task.GetRenderChunkCacheData());
    }

    private void OnSuccess(RenderChunkCompileTask task)
    {
        FreeRenderChunkCacheData(task);

        if (task.status == RenderChunkCompileTask.Status.TurnToMainThread)
            task.status = RenderChunkCompileTask.Status.Done;
    }

    private void OnFailure(RenderChunkCompileTask task, System.Exception e)
    {
        m_Dispatcher.FreeThreadingData(task.GetRenderChunkCacheData());

        if (e != null)
            Debug.LogError(e);
    }
}
