using System;

namespace GameEngine
{
    /// <summary>
    /// 全局定时器系统静态入口，内部持有默认的 <see cref="TimerManager"/> 单例。
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
    /// TimerSystem.Repeat(interval: 1f, () => Log.Debug("每秒触发"));
    ///
    /// // ── 重复固定次数 ──────────────────────────────────────
    /// TimerSystem.Repeat(interval: 0.5f, () => Log.Debug("每 0.5 秒"), maxRepeat: 5);
    /// </code>
    /// </summary>
    public static class TimerSystem
    {
        /// <summary>全局默认 TimerManager 实例。</summary>
        public static TimerManager Default => TimerManager.Instance;

        // ── 快捷注册 ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 延迟 <paramref name="delay"/> 秒后触发一次回调。
        /// </summary>
        /// <param name="delay">延迟时间（秒），≥ 0</param>
        /// <param name="callback">回调，不可为 null</param>
        /// <param name="useTimeScale">是否受时间缩放影响，默认 true 表示使用 scaled time</param>
        public static int Delay(float delay, Action callback, bool useTimeScale = true)
            => Default.Schedule(delay, callback, repeat: false, useTimeScale: useTimeScale);

        /// <summary>
        /// 每隔 <paramref name="interval"/> 秒重复触发回调。
        /// </summary>
        /// <param name="interval">触发间隔（秒）</param>
        /// <param name="callback">回调，不可为 null</param>
        /// <param name="maxRepeat">最大触发次数，≤ 0 表示无限</param>
        /// <param name="useTimeScale">是否受时间缩放影响，默认 true 表示使用 scaled time</param>
        public static int Repeat(float interval, Action callback, int maxRepeat = 0, bool useTimeScale = true)
            => Default.Schedule(interval, callback, repeat: true, interval: interval, maxRepeat: maxRepeat, useTimeScale: useTimeScale);

        /// <summary>
        /// 延迟 <paramref name="delay"/> 秒后首次触发，之后每隔 <paramref name="interval"/> 秒重复。
        /// </summary>
        /// <param name="delay">首次触发延迟（秒）</param>
        /// <param name="interval">后续触发间隔（秒）</param>
        /// <param name="callback">回调，不可为 null</param>
        /// <param name="maxRepeat">最大触发次数，≤ 0 表示无限</param>
        /// <param name="useTimeScale">是否受时间缩放影响，默认 true 表示使用 scaled time</param>
        public static int DelayRepeat(float delay, float interval, Action callback, int maxRepeat = 0, bool useTimeScale = true)
            => Default.Schedule(delay, callback, repeat: true, interval: interval, maxRepeat: maxRepeat, useTimeScale: useTimeScale);

        /// <inheritdoc cref="TimerManager.Schedule"/>
        public static int Schedule(float delay,
                                   Action callback,
                                   bool repeat = false,
                                   float interval = 0f,
                                   int maxRepeat = 0,
                                   bool useTimeScale = true)
            => Default.Schedule(delay, callback, repeat, interval, maxRepeat, useTimeScale);
    }
}
