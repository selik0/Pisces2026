using System;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 保存一组已创建的定时器，并支持一次性停止全部定时器。
    /// 便于按模块统一管理定时器，避免逐个停止。
    /// </summary>
    public sealed class TimerGroup : IDisposable
    {
        private readonly TimerManager _timerManager;
        private readonly List<int> _timerIds = new List<int>();

        public TimerGroup()
        {
            _timerManager = TimerManager.Instance;
        }

        /// <summary>记录一个已创建的定时器 ID。ID 无效（&lt; 0）时忽略。</summary>
        public void Add(int timerId)
        {
            if (timerId < 0)
            {
                return;
            }

            _timerIds.Add(timerId);
        }

        /// <summary>延迟 <paramref name="delay"/> 秒后触发一次回调，并加入本组。</summary>
        public int Delay(float delay, Action callback, bool useTimeScale = true)
        {
            int id = _timerManager.Schedule(delay, callback, repeat: false, useTimeScale: useTimeScale);
            Add(id);
            return id;
        }

        /// <summary>每隔 <paramref name="interval"/> 秒重复触发回调，并加入本组。</summary>
        public int Repeat(float interval, Action callback, int maxRepeat = 0, bool useTimeScale = true)
        {
            int id = _timerManager.Schedule(interval, callback, repeat: true, interval: interval, maxRepeat: maxRepeat, useTimeScale: useTimeScale);
            Add(id);
            return id;
        }

        /// <summary>延迟 <paramref name="delay"/> 秒后首次触发，之后每隔 <paramref name="interval"/> 秒重复，并加入本组。</summary>
        public int DelayRepeat(float delay, float interval, Action callback, int maxRepeat = 0, bool useTimeScale = true)
        {
            int id = _timerManager.Schedule(delay, callback, repeat: true, interval: interval, maxRepeat: maxRepeat, useTimeScale: useTimeScale);
            Add(id);
            return id;
        }

        /// <inheritdoc cref="TimerManager.Schedule"/>
        public int Schedule(float delay,
                            Action callback,
                            bool repeat = false,
                            float interval = 0f,
                            int maxRepeat = 0,
                            bool useTimeScale = true)
        {
            int id = _timerManager.Schedule(delay, callback, repeat, interval, maxRepeat, useTimeScale);
            Add(id);
            return id;
        }

        /// <summary>停止本组内全部定时器。</summary>
        public void StopAll()
        {
            for (int i = _timerIds.Count - 1; i >= 0; i--)
            {
                _timerManager.Stop(_timerIds[i]);
            }

            _timerIds.Clear();
        }

        public void Dispose()
        {
            StopAll();
        }
    }
}
