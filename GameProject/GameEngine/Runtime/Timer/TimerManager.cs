using System;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 定时器管理器。
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
    public sealed class TimerManager : Singleton<TimerManager>, ILogin
    {
        public TimerManager()
        {
        }

        // ── 状态 ─────────────────────────────────────────────────────────────────

        private readonly List<TimerEntry> _timers = new List<TimerEntry>();
        private readonly List<TimerEntry> _toAdd = new List<TimerEntry>(); // 帧内缓冲新增

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
        /// <param name="useTimeScale">是否受时间缩放影响，默认 true 表示使用 scaled time</param>
        /// <returns>定时器 ID，可用于 <see cref="Stop"/> 停止；失败时返回 -1</returns>
        public int Schedule(float delay, 
                             Action callback,
                             bool repeat = false,
                             float interval = 0f,
                             int maxRepeat = 0,
                             bool useTimeScale = true)
        {
            if (callback == null)
            {
                Log.Error("[Timer] Schedule failed: callback is null");
                return -1;
            }

            var entry = new TimerEntry(delay, callback, repeat, interval, maxRepeat, useTimeScale);

            _toAdd.Add(entry);

            Log.Debug($"[Timer] Schedule  #{entry.Id}  delay={delay:F3}s  repeat={repeat}  maxRepeat={maxRepeat}  useTimeScale={useTimeScale}");
            return entry.Id;
        }

        // ── Stop ─────────────────────────────────────────────────────────────────

        /// <summary>根据 ID 停止指定定时器。</summary>
        /// <param name="id">由 <see cref="Schedule"/> 返回的定时器 ID</param>
        public void Stop(int id)
        {
            for (int i = _timers.Count - 1; i >= 0; i--)
            {
                if (_timers[i].Id == id)
                {
                    _timers[i].Stop();
                    Log.Debug($"[Timer] Stop  #{id}");
                    return;
                }
            }

            for (int i = 0; i < _toAdd.Count; i++)
            {
                if (_toAdd[i].Id == id)
                {
                    _toAdd[i].Stop();
                    Log.Debug($"[Timer] Stop  #{id}");
                    return;
                }
            }
        }

        // ── StopAll ──────────────────────────────────────────────────────────────

        /// <summary>停止所有定时器。</summary>
        public void StopAll()
        {
            foreach (var entry in _timers)
            {
                entry.Stop();
            }

            foreach (var entry in _toAdd)
            {
                entry.Stop();
            }

            _timers.Clear();
            _toAdd.Clear();

            Log.Debug("[Timer] StopAll");
        }

        /// <summary>
        /// 推进所有定时器的时间，触发到期回调。
        /// 先移除已完成的定时器，再顺序执行剩余定时器。
        /// 应在游戏主循环（MonoBehaviour.Update）中每帧调用。
        /// </summary>
        /// <param name="deltaTime">帧间隔时间（秒），通常传入 Time.deltaTime</param>
        public void Tick()
        {
            // 先并入上一帧新增的定时器
            if (_toAdd.Count > 0)
            {
                _timers.AddRange(_toAdd);
                _toAdd.Clear();
            }

            // 移除已完成/已取消的定时器
            for (int i = _timers.Count - 1; i >= 0; i--)
            {
                if (_timers[i].IsDone)
                {
                    _timers.RemoveAt(i);
                }
            }

            // 再顺序执行剩余定时器
            for (int i = 0; i < _timers.Count; i++)
            {
                _timers[i].Tick();
            }
        }

        // ── ILogin ───────────────────────────────────────────────────────────────

        /// <summary>登录时清理残留定时器，保持初始状态。</summary>
        public void Login()
        {
            StopAll();
        }

        /// <summary>登出时停止所有定时器，避免跨会话回调泄漏。</summary>
        public void Logout()
        {
            StopAll();
        }
    }
}
