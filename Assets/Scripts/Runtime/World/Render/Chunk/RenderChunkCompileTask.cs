using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class RenderChunkCompileTask
{
    /// <summary>
    /// 任务状态
    /// </summary>
    public enum Status
    {
        Pending,            // 准备
        Compiling,          // 编译
        TurnToMainThread,   // 交回主线程完成任务
        Done,               // 完成
    }

    /// <summary>
    /// 当前执行任务的RenderChunk
    /// </summary>
    private readonly RenderChunk m_RenderChunk;
    public RenderChunk renderChunk { get { return m_RenderChunk; } }

    /// <summary>
    /// 所使用的数据缓冲
    /// </summary>
    private RenderChunkCacheData m_RenderChunkCacheData;

    /// <summary>
    /// 任务状态
    /// </summary>
    private Status m_Status = Status.Pending;
    public Status status { set { lock (m_Lock) { m_Status = value; } } get { return m_Status; } }

    private float m_SortDistance;
    public float sortDistance { get { return m_SortDistance; } }

    /// <summary>
    /// 状态锁
    /// </summary>
    private readonly object m_Lock = new object();

    /// <summary>
    /// 任务是否完成
    /// </summary>
    private bool m_Finished = false;
    public bool isFinished { get { return m_Finished; } }

    /// <summary>
    /// 是否已经释放了资源
    /// </summary>
    private bool m_IsReleased = false;
    public bool isReleased { set { m_IsReleased = value; } get { return m_IsReleased; } }

    private System.Action m_FirstFinishRunnable;
    private List<System.Action> m_FinishRunnables;

    public RenderChunkCompileTask(RenderChunk renderChunk, float distance)
    {
        m_RenderChunk = renderChunk;
        m_SortDistance = distance;
    }

    public void SetRenderChunkCacheData(RenderChunkCacheData data)
    {
        m_RenderChunkCacheData = data;
    }

    public RenderChunkCacheData GetRenderChunkCacheData()
    {
        return m_RenderChunkCacheData;
    }

    #region Finish

    /// <summary>
    /// 添加任务完成时执行其它工作
    /// </summary>
    /// <param name="runnable"></param>
    public void AddFinishRunnable(System.Action runnable)
    {
        lock (m_Lock)
        {
            if (isFinished)
            {
                // 任务完成,直接执行其它任务
                runnable.Invoke();
            }
            else
            {
                if (m_FirstFinishRunnable == null)
                {
                    m_FirstFinishRunnable = runnable;
                }
                else
                {
                    if (m_FinishRunnables == null)
                        m_FinishRunnables = new List<System.Action>();
                    m_FinishRunnables.Add(runnable);
                }
            }
        }
    }

    public void Finish(bool isCheckRerender = true)
    {
        lock (m_Lock)
        {
            if (status != Status.Done && isCheckRerender)
            {
                m_RenderChunk.NeedUpdate(false);
                m_RenderChunk.isNeedReRerender = true;
            }

            // 设置状态
            m_Finished = true;
            m_Status = Status.Done;

            if (m_FirstFinishRunnable != null)
                m_FirstFinishRunnable.Invoke();

            if (m_FinishRunnables != null)
            {
                for (int i = 0; i < m_FinishRunnables.Count; ++i)
                    m_FinishRunnables[i].Invoke();
                m_FinishRunnables.Clear();
            }

            m_FirstFinishRunnable = null;
            m_FinishRunnables = null;
            m_RenderChunkCacheData = null;
        }
    }

    #endregion
}
