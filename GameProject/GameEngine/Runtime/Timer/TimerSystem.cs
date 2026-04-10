using System;

namespace GameEngine
{
    /// <summary>
    /// 全局定时器系统静态入口，内部持有默认的 <see cref="TimerScheduler"/> 单例。
    ///
    /// <para>
    /// 需要在游戏主循环中每帧调用 <see cref="Tick"/>，通常放在 GameBootstrap.Update 中。
    /// </para>
    ///
    /// <code>
    /// // ── 游戏启动 Bootstrap ────────────────────────────────
    /// void Update() => TimerSystem.Tick(Time.deltaTime);
    ///
    /// // ── 延迟一次性回调 ────────────────────────────────────
    /// TimerSystem.Delay(2f, () => Log.Debug("2 秒后触发"));
    ///
    /// // ── 固定间隔重复 ──────────────────────────────────────
    /// var h = TimerSystem.Repeat(interval: 1f, () => Log.Debug("每秒触发"));
    ///
    /// // ── 重复固定次数 ──────────────────────────────────────
    /// TimerSystem.Repeat(interval: 0.5f, () => Log.Debug("每 0.5 秒"), maxRepeat: 5);
    ///
    /// // ── 取消 ──────────────────────────────────────────────
    /// h.Cancel();                 // 通过句柄取消
    /// TimerSystem.Cancel(h);      // 通过系统取消（等价）
    ///
    /// // ── Debug 模式 ────────────────────────────────────────
    /// TimerSystem.DebugMode = true;
    /// </code>
    /// </summary>
    public static class TimerSystem
    {
        private static TimerScheduler _default;

        /// <summary>全局默认 TimerScheduler 实例（懒初始化）</summary>
        public static TimerScheduler Default
        {
            get
            {
                if (_default == null) _default = new TimerScheduler();
                return _default;
            }
        }

        /// <summary>是否开启 Debug 模式（透传给 Default scheduler）</summary>
        public static bool DebugMode
        {
            get => Default.DebugMode;
            set => Default.DebugMode = value;
        }

        // ── Tick ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 推进所有定时器时间。应在 MonoBehaviour.Update 中每帧调用。
        /// </summary>
        /// <param name="deltaTime">帧间隔（秒），传入 <c>Time.deltaTime</c></param>
        public static void Tick(float deltaTime)
            => Default.Tick(deltaTime);

        // ── 快捷注册 ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 延迟 <paramref name="delay"/> 秒后触发一次回调。
        /// </summary>
        /// <param name="delay">延迟时间（秒），≥ 0</param>
        /// <param name="callback">回调，不可为 null</param>
        /// <returns>定时器句柄</returns>
        public static TimerHandle Delay(float delay, Action callback)
            => Default.Schedule(delay, callback, repeat: false);

        /// <summary>
        /// 每隔 <paramref name="interval"/> 秒重复触发回调。
        /// </summary>
        /// <param name="interval">触发间隔（秒）</param>
        /// <param name="callback">回调，不可为 null</param>
        /// <param name="maxRepeat">最大触发次数，≤ 0 表示无限</param>
        /// <returns>定时器句柄</returns>
        public static TimerHandle Repeat(float interval, Action callback, int maxRepeat = 0)
            => Default.Schedule(interval, callback, repeat: true, interval: interval, maxRepeat: maxRepeat);

        /// <summary>
        /// 延迟 <paramref name="delay"/> 秒后首次触发，之后每隔 <paramref name="interval"/> 秒重复。
        /// </summary>
        /// <param name="delay">首次触发延迟（秒）</param>
        /// <param name="interval">后续触发间隔（秒）</param>
        /// <param name="callback">回调，不可为 null</param>
        /// <param name="maxRepeat">最大触发次数，≤ 0 表示无限</param>
        /// <returns>定时器句柄</returns>
        public static TimerHandle DelayRepeat(float delay, float interval, Action callback, int maxRepeat = 0)
            => Default.Schedule(delay, callback, repeat: true, interval: interval, maxRepeat: maxRepeat);

        /// <inheritdoc cref="TimerScheduler.Schedule"/>
        public static TimerHandle Schedule(float delay, Action callback,
                                           bool repeat = false,
                                           float interval = 0f,
                                           int maxRepeat = 0)
            => Default.Schedule(delay, callback, repeat, interval, maxRepeat);

        // ── 取消 ─────────────────────────────────────────────────────────────────

        /// <inheritdoc cref="TimerScheduler.Cancel"/>
        public static void Cancel(TimerHandle handle)
            => Default.Cancel(handle);

        /// <inheritdoc cref="TimerScheduler.CancelAll"/>
        public static void CancelAll()
            => Default.CancelAll();

        // ── 重置 ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 完全重置全局定时器系统（测试用，游戏运行时慎用）。
        /// </summary>
        public static void Reset()
        {
            _default?.CancelAll();
            _default = null;
        }
    }
}
