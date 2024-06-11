namespace Core.Collections
{
    public interface IDataLink<T>
    {
        /// <summary>
        /// 是否为头
        /// </summary>
        bool isHead { get; }

        /// <summary>
        /// 是否为尾
        /// </summary>
        bool isTail { get; }

        /// <summary>
        /// 下一个节点
        /// </summary>
        T nextNode { get; set; }

        /// <summary>
        /// 上一个节点
        /// </summary>
        T prevNode { get; set; }
    }

    public abstract class DataLinkNode<T> : IDataLink<T>
    {
        /// <summary>
        /// 下一个节点
        /// </summary>
        private T m_NextNode;

        /// <summary>
        /// 上一个节点
        /// </summary>
        private T m_PrevNode;

        #region IDataLink<T>

        /// <summary>
        /// 是否为头
        /// </summary>
        bool IDataLink<T>.isHead => m_NextNode == null;

        /// <summary>
        /// 是否为尾
        /// </summary>
        bool IDataLink<T>.isTail => m_PrevNode == null;

        /// <summary>
        /// 下一个节点
        /// </summary>
        T IDataLink<T>.nextNode { set { m_NextNode = value; } get { return m_NextNode; } }

        /// <summary>
        /// 上一个节点
        /// </summary>
        T IDataLink<T>.prevNode { set { m_PrevNode = value; } get { return m_PrevNode; } }

        #endregion
    }
}
