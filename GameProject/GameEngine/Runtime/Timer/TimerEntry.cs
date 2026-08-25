using System;

namespace GameEngine
{
    /// <summary>
    /// 单个定时器条目，封装自身的调度状态与触发逻辑。
    /// 由 <see cref="TimerManager"/> 统一管理并每帧驱动 <see cref="Tick"/>。
    /// </summary>
    public sealed class TimerEntry
    {
        private const int MaxCallbacksPerTick = 8;
        private static int _nextId = 1;

        private readonly Action _callback;
        private readonly bool _repeat;
        private readonly float _interval;
        private readonly int _maxRepeat;
        private readonly bool _useTimeScale;

        private float _remaining;
        private int _repeatCount;
        private bool _isDone;

        /// <summary>全局唯一 ID，用于日志区分。</summary>
        public int Id { get; }

        /// <summary>是否已完成或已取消。</summary>
        public bool IsDone => _isDone;

        /// <summary>
        /// 是否受 <see cref="UnityEngine.Time.timeScale"/> 影响。
        /// 默认 true，即使用受时间缩放影响的时间（scaled time）。
        /// </summary>
        public bool UseTimeScale => _useTimeScale;

        /// <summary>
        /// 创建定时器条目。
        /// </summary>
        /// <param name="delay">首次触发前的延迟（秒），≥ 0</param>
        /// <param name="callback">触发时执行的回调，不可为 null</param>
        /// <param name="repeat">是否重复触发</param>
        /// <param name="interval">重复触发的间隔（秒），≤ 0 时使用 delay 作为间隔</param>
        /// <param name="maxRepeat">最大重复次数，≤ 0 表示无限</param>
        /// <param name="useTimeScale">是否受时间缩放影响，默认 true 表示使用 scaled time</param>
        public TimerEntry(float delay,
                          Action callback,
                          bool repeat = false,
                          float interval = 0f,
                          int maxRepeat = 0,
                          bool useTimeScale = true)
        {
            if (callback == null)
            {
                Log.Error("[Timer] TimerEntry creation failed: callback is null");
            }

            if (delay < 0f)
            {
                delay = 0f;
            }

            _callback = callback;
            _repeat = repeat;
            _interval = (repeat && interval > 0f) ? interval : delay;
            _maxRepeat = maxRepeat;
            _useTimeScale = useTimeScale;
            _remaining = delay;

            Id = _nextId++;
        }

        /// <summary>停止该定时器。若已完成则无效。</summary>
        public void Stop()
        {
            _isDone = true;
        }

        /// <summary>
        /// 推进时间，到期时触发回调并更新自身状态。
        /// 重复定时器会补发本帧跨过的触发点，但单帧最多执行 <see cref="MaxCallbacksPerTick"/> 次，避免长时间卡顿后阻塞主线程。
        /// </summary>
        public void Tick()
        {
            if (_isDone)
            {
                return;
            }

            _remaining -= _useTimeScale ? UnityEngine.Time.deltaTime : UnityEngine.Time.unscaledDeltaTime;
            if (_remaining > 0f)
            {
                return;
            }

            int callbackCount = 0;
            while (!_isDone && _remaining <= 0f && callbackCount < MaxCallbacksPerTick)
            {
                callbackCount++;
                InvokeCallback();

                bool maxReached = _repeat && _maxRepeat > 0 && _repeatCount >= _maxRepeat;
                if (!_repeat || maxReached)
                {
                    _isDone = true;
                    break;
                }

                if (_interval <= 0f)
                {
                    // 零间隔重复定时器每帧最多触发一次，避免同一 Tick 无限循环。
                    _remaining = 0f;
                    break;
                }

                // 保留负的剩余时间，使低帧率下跨过的触发点可在本帧按顺序补发。
                _remaining += _interval;
            }
        }

        private void InvokeCallback()
        {
            try
            {
                _callback();
            }
            catch (Exception ex)
            {
                Log.Error($"[Timer] Exception in timer #{Id}", ex);
            }
            finally
            {
                // 回调抛异常也算一次触发，确保 maxRepeat 能可靠终止。
                _repeatCount++;
            }
        }
    }
}
