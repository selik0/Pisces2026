using System.Collections;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 协程管理器。
    /// <para>
    /// 基于 C# <see cref="IEnumerator"/> 实现，无需 Unity MonoBehaviour，
    /// 需在游戏主循环中每帧调用 <see cref="Tick"/>。
    /// </para>
    /// </summary>
    public sealed class CoroutineManager : Singleton<CoroutineManager>, ILogin
    {
        private readonly List<CoroutineEntry> _coroutines = new List<CoroutineEntry>();
        private readonly List<CoroutineEntry> _toAdd = new List<CoroutineEntry>();

        /// <summary>当前正在管理的协程数量（包含本帧新增、不含已完成）</summary>
        public int Count => _coroutines.Count + _toAdd.Count;

        /// <summary>
        /// 启动一个协程。
        /// </summary>
        /// <param name="routine">由 <c>IEnumerator</c> 方法生成的迭代器</param>
        /// <returns>协程 ID，可用于 <see cref="Stop"/> 停止</returns>
        public int Start(IEnumerator routine)
        {
            var entry = new CoroutineEntry(routine);
            _toAdd.Add(entry);

            Log.Debug($"[Coroutine] Start  #{entry.Id}");
            return entry.Id;
        }

        /// <summary>根据 ID 停止指定协程。</summary>
        /// <param name="id">由 <see cref="Start"/> 返回的协程 ID</param>
        public void Stop(int id)
        {
            for (int i = _coroutines.Count - 1; i >= 0; i--)
            {
                if (_coroutines[i].Id == id)
                {
                    _coroutines[i].Stop();
                    Log.Debug($"[Coroutine] Stop  #{id}");
                    return;
                }
            }

            for (int i = 0; i < _toAdd.Count; i++)
            {
                if (_toAdd[i].Id == id)
                {
                    _toAdd[i].Stop();
                    Log.Debug($"[Coroutine] Stop  #{id}");
                    return;
                }
            }
        }

        /// <summary>停止所有协程。</summary>
        public void StopAll()
        {
            foreach (var entry in _coroutines)
            {
                entry.Stop();
            }

            foreach (var entry in _toAdd)
            {
                entry.Stop();
            }

            _coroutines.Clear();
            _toAdd.Clear();

            Log.Debug("[Coroutine] StopAll");
        }

        /// <summary>
        /// 推进所有协程一帧。应在 MonoBehaviour.Update 中每帧调用。
        /// </summary>
        public void Tick()
        {
            if (_toAdd.Count > 0)
            {
                _coroutines.AddRange(_toAdd);
                _toAdd.Clear();
            }

            for (int i = _coroutines.Count - 1; i >= 0; i--)
            {
                if (_coroutines[i].IsDone)
                {
                    _coroutines.RemoveAt(i);
                }
            }

            for (int i = 0; i < _coroutines.Count; i++)
            {
                _coroutines[i].Tick();
            }
        }

        public void Login()
        {
            StopAll();
        }

        public void Logout()
        {
            StopAll();
        }
    }
}
