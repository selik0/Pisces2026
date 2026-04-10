using System;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 定时器调度器。
    /// <para>
    /// 管理所有延迟/重复定时器，需在游戏主循环中每帧调用 <see cref="Tick"/>。<br/>
    /// 所有操作均为非线程安全，应仅在 Unity 主线程使用。
    /// </para>
    ///
    /// <para><b>特性</b></para>
    /// <list type="bullet">
    ///   <item>支持一次性延迟回调 (<c>repeat=false</c>)</item>
    ///   <item>支持固定间隔重复回调 (<c>repeat=true</c>)</item>
    ///   <item>支持可选的最大重复次数 (<c>maxRepeat</c>)</item>
    ///   <item>Tick 期间新增/取消的定时器在下一帧生效，不影响当前帧遍历</item>
    /// </list>
    /// </summary>
    public sealed class TimerScheduler
    {
        // ── 内部定时器条目 ───────────────────────────────────────────────────────

        private sealed class TimerEntry
        {
            public TimerHandle Handle;
            public Action      Callback;
            public float       Interval;   // 触发间隔（秒）
            public float       Remaining;  // 距下次触发剩余时间
            public bool        Repeat;
            public int         MaxRepeat;  // <=0 表示无限
            public int         RepeatCount; // 已触发次数
        }

        // ── 状态 ─────────────────────────────────────────────────────────────────

        private readonly List<TimerEntry> _timers    = new List<TimerEntry>();
        private readonly List<TimerEntry> _toAdd     = new List<TimerEntry>(); // 帧内缓冲新增
        private bool _isTicking;

        /// <summary>是否开启 Debug 日志</summary>
        public bool DebugMode { get; set; }

        /// <summary>当前正在管理的定时器数量（包含本帧新增、不含已完成）</summary>
        public int Count => _timers.Count + _toAdd.Count;

        // ── Schedule ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 注册一个定时器。
        /// </summary>
        /// <param name="delay">首次触发前的延迟（秒），≥ 0</param>
        /// <param name="callback">触发时执行的回调，不可为 null</param>
        /// <param name="repeat">是否重复触发。false 表示只触发一次</param>
        /// <param name="interval">
        /// 重复触发的间隔（秒）。仅在 <paramref name="repeat"/> 为 true 时有效。
        /// 若 ≤ 0 则使用 <paramref name="delay"/> 作为间隔。
        /// </param>
        /// <param name="maxRepeat">最大重复次数，≤ 0 表示无限重复。仅在 <paramref name="repeat"/> 为 true 时有效</param>
        /// <returns>定时器句柄，可用于提前取消</returns>
        public TimerHandle Schedule(float delay, Action callback,
                                    bool repeat = false,
                                    float interval = 0f,
                                    int maxRepeat = 0)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            if (delay < 0f) delay = 0f;

            float actualInterval = (repeat && interval > 0f) ? interval : delay;

            var handle = new TimerHandle();
            var entry  = new TimerEntry
            {
                Handle     = handle,
                Callback   = callback,
                Interval   = actualInterval,
                Remaining  = delay,
                Repeat     = repeat,
                MaxRepeat  = maxRepeat,
                RepeatCount = 0
            };

            if (_isTicking)
                _toAdd.Add(entry);
            else
                _timers.Add(entry);

            if (DebugMode)
                Log.Debug($"[Timer] Schedule  #{handle.Id}  delay={delay:F3}s  repeat={repeat}  interval={actualInterval:F3}s  maxRepeat={maxRepeat}");

            return handle;
        }

        // ── Cancel ───────────────────────────────────────────────────────────────

        /// <summary>
        /// 取消指定句柄的定时器。
        /// </summary>
        public void Cancel(TimerHandle handle)
        {
            if (handle == null || handle.IsDone) return;
            handle.Cancel();   // 标记为已完成，Tick 时会自动清理

            if (DebugMode)
                Log.Debug($"[Timer] Cancel  #{handle.Id}");
        }

        /// <summary>
        /// 取消所有定时器。
        /// </summary>
        public void CancelAll()
        {
            foreach (var e in _timers)  e.Handle.Cancel();
            foreach (var e in _toAdd)   e.Handle.Cancel();
            _timers.Clear();
            _toAdd.Clear();

            if (DebugMode)
                Log.Debug("[Timer] CancelAll");
        }

        // ── Tick ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 推进所有定时器的时间，触发到期回调。
        /// 应在游戏主循环（MonoBehaviour.Update）中每帧调用。
        /// </summary>
        /// <param name="deltaTime">帧间隔时间（秒），通常传入 Time.deltaTime</param>
        public void Tick(float deltaTime)
        {
            _isTicking = true;

            for (int i = _timers.Count - 1; i >= 0; i--)
            {
                var entry = _timers[i];

                // 已取消，直接移除
                if (entry.Handle.IsDone)
                {
                    _timers.RemoveAt(i);
                    continue;
                }

                entry.Remaining -= deltaTime;
                if (entry.Remaining > 0f) continue;

                // 触发回调
                try
                {
                    entry.Callback();
                    entry.RepeatCount++;

                    if (DebugMode)
                        Log.Debug($"[Timer] Fired  #{entry.Handle.Id}  repeatCount={entry.RepeatCount}");
                }
                catch (Exception ex)
                {
                    Log.Error($"[Timer] Exception in timer #{entry.Handle.Id}", ex);
                }

                // 判断是否继续
                bool maxReached = entry.Repeat && entry.MaxRepeat > 0 && entry.RepeatCount >= entry.MaxRepeat;
                if (!entry.Repeat || maxReached)
                {
                    entry.Handle.IsDone = true;
                    _timers.RemoveAt(i);
                }
                else
                {
                    // 重置剩余时间（累积误差补偿：用负的 Remaining 补入下一轮）
                    entry.Remaining += entry.Interval;
                    if (entry.Remaining < 0f) entry.Remaining = 0f;
                }
            }

            _isTicking = false;

            // 将帧内新增的定时器并入主列表
            if (_toAdd.Count > 0)
            {
                _timers.AddRange(_toAdd);
                _toAdd.Clear();
            }
        }
    }
}
