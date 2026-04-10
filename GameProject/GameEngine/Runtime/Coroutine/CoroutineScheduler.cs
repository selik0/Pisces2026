using System;
using System.Collections;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 协程调度器。
    /// <para>
    /// 基于 C# <see cref="IEnumerator"/> 实现，无需 Unity MonoBehaviour，
    /// 需在游戏主循环中每帧调用 <see cref="Tick"/>。
    /// </para>
    ///
    /// <para><b>支持的 yield 对象</b></para>
    /// <list type="table">
    ///   <item><term><c>yield return null</c></term><description>等待下一帧</description></item>
    ///   <item><term><see cref="IYieldInstruction"/></term><description>自定义 yield 指令（WaitForSeconds、WaitUntil 等）</description></item>
    ///   <item><term><see cref="IEnumerator"/></term><description>嵌套子协程（inline 方式，与父协程共用句柄）</description></item>
    ///   <item><term><see cref="CoroutineHandle"/></term><description>等待另一个已启动的协程完成</description></item>
    /// </list>
    /// </summary>
    public sealed class CoroutineScheduler
    {
        // ── 内部协程条目 ─────────────────────────────────────────────────────────

        private sealed class CoroutineEntry
        {
            public CoroutineHandle   Handle;
            public Stack<IEnumerator> Stack;   // 支持嵌套子协程：栈顶为当前执行层
            public IYieldInstruction CurrentYield; // 当前等待的指令（null = 下一帧继续）
            public bool              WaitNextFrame; // yield return null
        }

        // ── 状态 ─────────────────────────────────────────────────────────────────

        private readonly List<CoroutineEntry> _coroutines = new List<CoroutineEntry>();
        private readonly List<CoroutineEntry> _toAdd      = new List<CoroutineEntry>();
        private bool _isTicking;

        /// <summary>是否开启 Debug 日志</summary>
        public bool DebugMode { get; set; }

        /// <summary>当前正在运行的协程数量</summary>
        public int Count => _coroutines.Count + _toAdd.Count;

        // ── Start ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 启动一个协程。
        /// </summary>
        /// <param name="routine">由 <c>IEnumerator</c> 方法生成的迭代器</param>
        /// <returns>协程句柄，可用于停止或等待</returns>
        public CoroutineHandle Start(IEnumerator routine)
        {
            if (routine == null) throw new ArgumentNullException(nameof(routine));

            var handle = new CoroutineHandle();
            var stack  = new Stack<IEnumerator>();
            stack.Push(routine);

            var entry = new CoroutineEntry
            {
                Handle = handle,
                Stack  = stack
            };

            if (_isTicking)
                _toAdd.Add(entry);
            else
                _coroutines.Add(entry);

            if (DebugMode)
                Log.Debug($"[Coroutine] Start  #{handle.Id}");

            return handle;
        }

        // ── Stop ─────────────────────────────────────────────────────────────────

        /// <summary>停止指定协程。</summary>
        public void Stop(CoroutineHandle handle)
        {
            if (handle == null || handle.IsDone) return;
            handle.Stop();

            if (DebugMode)
                Log.Debug($"[Coroutine] Stop  #{handle.Id}");
        }

        /// <summary>停止所有协程。</summary>
        public void StopAll()
        {
            foreach (var e in _coroutines) e.Handle.Stop();
            foreach (var e in _toAdd)      e.Handle.Stop();
            _coroutines.Clear();
            _toAdd.Clear();

            if (DebugMode)
                Log.Debug("[Coroutine] StopAll");
        }

        // ── Tick ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 推进所有协程一帧。应在 MonoBehaviour.Update 中每帧调用。
        /// </summary>
        /// <param name="deltaTime">帧间隔（秒），传入 <c>Time.deltaTime</c></param>
        public void Tick(float deltaTime)
        {
            _isTicking = true;

            for (int i = _coroutines.Count - 1; i >= 0; i--)
            {
                var entry = _coroutines[i];

                if (entry.Handle.IsDone)
                {
                    _coroutines.RemoveAt(i);
                    continue;
                }

                if (!AdvanceEntry(entry, deltaTime))
                {
                    // 协程已完成
                    entry.Handle.IsDone = true;
                    _coroutines.RemoveAt(i);

                    if (DebugMode)
                        Log.Debug($"[Coroutine] Completed  #{entry.Handle.Id}");
                }
            }

            _isTicking = false;

            if (_toAdd.Count > 0)
            {
                _coroutines.AddRange(_toAdd);
                _toAdd.Clear();
            }
        }

        // ── 核心推进逻辑 ─────────────────────────────────────────────────────────

        /// <returns>true = 协程仍在运行；false = 协程已完成</returns>
        private bool AdvanceEntry(CoroutineEntry entry, float deltaTime)
        {
            // ── 处理当前 yield 等待 ────────────────────────────────────────────

            if (entry.WaitNextFrame)
            {
                entry.WaitNextFrame = false;
                return true; // 本帧跳过，下帧继续
            }

            if (entry.CurrentYield != null)
            {
                entry.CurrentYield.Tick(deltaTime);
                if (!entry.CurrentYield.IsCompleted)
                    return true; // 仍在等待

                entry.CurrentYield = null; // 等待完成，继续执行
            }

            // ── 推进迭代器 ────────────────────────────────────────────────────

            while (entry.Stack.Count > 0)
            {
                var top = entry.Stack.Peek();
                bool hasNext;

                try
                {
                    hasNext = top.MoveNext();
                }
                catch (Exception ex)
                {
                    Log.Error($"[Coroutine] Exception in #{entry.Handle.Id}", ex);
                    return false;
                }

                if (!hasNext)
                {
                    // 当前层完成，弹出
                    entry.Stack.Pop();
                    continue; // 继续执行上层
                }

                var current = top.Current;

                // yield return null → 等待下一帧
                if (current == null)
                {
                    entry.WaitNextFrame = true;
                    return true;
                }

                // yield return IYieldInstruction
                if (current is IYieldInstruction instruction)
                {
                    // 如果指令已立即完成（如 WaitForSeconds(0)），直接继续
                    instruction.Tick(deltaTime);
                    if (!instruction.IsCompleted)
                    {
                        entry.CurrentYield = instruction;
                        return true;
                    }
                    continue; // 已完成，继续下一步
                }

                // yield return IEnumerator → 压栈，inline 执行子协程
                if (current is IEnumerator nested)
                {
                    entry.Stack.Push(nested);
                    continue;
                }

                // yield return CoroutineHandle → 等待另一个协程完成
                if (current is CoroutineHandle waitHandle)
                {
                    if (!waitHandle.IsDone)
                    {
                        entry.CurrentYield = new WaitForCoroutine(waitHandle);
                        return true;
                    }
                    continue;
                }

                // 未知对象，直接跳过（等下一帧）
                if (DebugMode)
                    Log.Debug($"[Coroutine] Unknown yield object '{current.GetType().Name}' in #{entry.Handle.Id}, treated as next frame");

                entry.WaitNextFrame = true;
                return true;
            }

            return false; // 所有层均已完成
        }
    }
}
