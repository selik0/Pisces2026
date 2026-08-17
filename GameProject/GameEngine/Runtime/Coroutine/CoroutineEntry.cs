using System;
using System.Collections;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 单个协程条目，封装自身的迭代状态与推进逻辑。
    /// 由 <see cref="CoroutineManager"/> 统一管理并每帧驱动 <see cref="Tick"/>。
    /// </summary>
    public sealed class CoroutineEntry
    {
        private static int _nextId = 1;

        private readonly Stack<IEnumerator> _stack;
        private IYieldInstruction _currentYield;
        private bool _waitNextFrame;
        private bool _isDone;

        /// <summary>全局唯一 ID，用于停止和日志区分。</summary>
        public int Id { get; }

        /// <summary>是否已完成或已取消。</summary>
        public bool IsDone => _isDone;

        /// <summary>
        /// 创建一个协程条目。
        /// </summary>
        /// <param name="routine">由 <c>IEnumerator</c> 方法生成的迭代器</param>
        public CoroutineEntry(IEnumerator routine)
        {
            if (routine == null)
            {
                throw new ArgumentNullException(nameof(routine));
            }

            Id = _nextId++;
            _stack = new Stack<IEnumerator>();
            _stack.Push(routine);
        }

        /// <summary>停止该协程。若已完成则无效。</summary>
        public void Stop()
        {
            _isDone = true;
        }

        /// <summary>
        /// 推进协程一帧。
        /// </summary>
        public void Tick()
        {
            if (_isDone || !Advance())
            {
                _isDone = true;
            }
        }

        private bool Advance()
        {
            if (_waitNextFrame)
            {
                _waitNextFrame = false;
                return true;
            }

            if (_currentYield != null)
            {
                _currentYield.Tick();
                if (!_currentYield.IsCompleted)
                {
                    return true;
                }

                _currentYield = null;
            }

            while (_stack.Count > 0)
            {
                IEnumerator top = _stack.Peek();
                bool hasNext;

                try
                {
                    hasNext = top.MoveNext();
                }
                catch (Exception ex)
                {
                    Log.Error($"[Coroutine] Exception in #{Id}", ex);
                    return false;
                }

                if (!hasNext)
                {
                    _stack.Pop();
                    continue;
                }

                object current = top.Current;
                if (current == null)
                {
                    _waitNextFrame = true;
                    return true;
                }

                if (current is IYieldInstruction instruction)
                {
                    instruction.Tick();
                    if (!instruction.IsCompleted)
                    {
                        _currentYield = instruction;
                        return true;
                    }

                    continue;
                }

                if (current is IEnumerator nested)
                {
                    _stack.Push(nested);
                    continue;
                }

                Log.Debug($"[Coroutine] Unknown yield object '{current.GetType().Name}' in #{Id}, treated as next frame");
                _waitNextFrame = true;
                return true;
            }

            return false;
        }
    }
}
