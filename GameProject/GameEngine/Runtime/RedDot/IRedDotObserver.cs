namespace GameEngine
{
    /// <summary>
    /// 红点变更观察者接口。
    /// 实现该接口并注册到 <see cref="RedDotNode"/> 后，
    /// 当节点的 <see cref="RedDotNode.Count"/> 发生变化时会收到回调。
    /// </summary>
    public interface IRedDotObserver
    {
        /// <summary>
        /// 红点数量发生变化时调用。
        /// </summary>
        /// <param name="node">触发变更的节点</param>
        /// <param name="newCount">变更后的数量（0 表示无红点）</param>
        void OnRedDotChanged(RedDotNode node, int newCount);
    }
}
