namespace GameEngine
{
    /// <summary>
    /// 协程句柄。
    /// 由 <see cref="CoroutineScheduler.Start"/> 返回，可用于查询状态或提前停止协程。
    ///
    /// <code>
    /// var handle = CoroutineSystem.Start(MyRoutine());
    ///
    /// // 查询是否完成
    /// if (handle.IsDone) { ... }
    ///
    /// // 提前停止
    /// handle.Stop();
    /// </code>
    /// </summary>
    public sealed class CoroutineHandle
    {
        private static int _nextId = 1;

        /// <summary>唯一 ID</summary>
        public int Id { get; }

        /// <summary>协程是否已结束（正常完成或被停止）</summary>
        public bool IsDone { get; internal set; }

        /// <summary>协程是否仍在运行</summary>
        public bool IsRunning => !IsDone;

        internal CoroutineHandle()
        {
            Id = _nextId++;
        }

        /// <summary>请求停止该协程。下一帧生效。</summary>
        public void Stop()
        {
            IsDone = true;
        }

        public override string ToString() => $"CoroutineHandle#{Id} IsDone={IsDone}";
    }
}
