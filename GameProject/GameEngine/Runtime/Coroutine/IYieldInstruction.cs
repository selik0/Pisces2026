namespace GameEngine
{
    /// <summary>
    /// 协程 yield 指令接口。
    /// 在 <see cref="CoroutineScheduler.Tick"/> 每帧被调用，
    /// 当 <see cref="IsCompleted"/> 返回 true 时协程继续执行下一步。
    /// </summary>
    public interface IYieldInstruction
    {
        /// <summary>
        /// 每帧由调度器调用以推进状态。
        /// </summary>
        /// <param name="deltaTime">帧间隔（秒）</param>
        void Tick(float deltaTime);

        /// <summary>该 yield 条件是否已满足（协程可继续）</summary>
        bool IsCompleted { get; }
    }
}
