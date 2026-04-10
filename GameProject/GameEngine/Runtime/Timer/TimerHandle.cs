namespace GameEngine
{
    /// <summary>
    /// 定时器句柄。
    /// 由 <see cref="TimerScheduler.Schedule"/> 返回，可用于提前取消定时器。
    ///
    /// <code>
    /// var handle = TimerSystem.Schedule(delay: 2f, () => Log.Debug("2 秒后触发"));
    ///
    /// // 取消
    /// handle.Cancel();
    ///
    /// // 或通过系统取消
    /// TimerSystem.Cancel(handle);
    /// </code>
    /// </summary>
    public sealed class TimerHandle
    {
        /// <summary>全局自增 ID，用于区分不同句柄</summary>
        private static int _nextId = 1;

        /// <summary>唯一 ID</summary>
        public int Id { get; }

        /// <summary>是否已取消或已完成</summary>
        public bool IsDone { get; internal set; }

        /// <summary>是否仍有效（未取消且未完成）</summary>
        public bool IsValid => !IsDone;

        internal TimerHandle()
        {
            Id = _nextId++;
        }

        /// <summary>取消该定时器。若已完成则无效。</summary>
        public void Cancel()
        {
            IsDone = true;
        }

        public override string ToString() => $"TimerHandle#{Id} IsDone={IsDone}";
    }
}
