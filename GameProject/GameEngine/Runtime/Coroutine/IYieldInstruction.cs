namespace GameEngine
{
    /// <summary>
    /// 协程 yield 指令接口。
    /// 在 <see cref="CoroutineManager.Tick"/> 每帧被调用，
    /// 当 <see cref="IsCompleted"/> 返回 true 时协程继续执行下一步。
    /// </summary>
    public interface IYieldInstruction
    {
        /// <summary>
        /// 每帧由调度器调用以推进状态。
        /// </summary>
        void Tick();

        /// <summary>该 yield 条件是否已满足（协程可继续）</summary>
        bool IsCompleted { get; }
    }
}
