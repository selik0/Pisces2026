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
        private bool _isAdvancing;
        private bool _disposeRequested;

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
            Id = _nextId++;
            _stack = new Stack<IEnumerator>();

            if (routine == null)
            {
                Log.Error($"[Coroutine] #{Id} 创建失败：routine 为 null。");
                _isDone = true;
                return;
            }

            _stack.Push(routine);
        }

        /// <summary>停止该协程。若已完成则无效。</summary>
        public void Stop()
        {
            if (_isDone && _stack.Count == 0)
            {
                return;
            }

            _isDone = true;
            _currentYield = null;
            _waitNextFrame = false;

            if (_isAdvancing)
            {
                _disposeRequested = true;
                return;
            }

            DisposeStack();
        }

        /// <summary>
        /// 推进协程一帧。
        /// </summary>
        public void Tick()
        {
            if (_isDone)
            {
                return;
            }

            _isAdvancing = true;
            try
            {
                if (!Advance())
                {
                    _isDone = true;
                }
            }
            catch (Exception ex)
            {
                _isDone = true;
                Log.Error($"[Coroutine] Exception in #{Id}", ex);
            }
            finally
            {
                _isAdvancing = false;
                if (_disposeRequested || _isDone)
                {
                    _disposeRequested = false;
                    DisposeStack();
                }
            }
        }

        private bool Advance()
        {
            // null、NextFrame 或未知对象在产生它们的当帧返回；下一次 Tick 应继续推进。
            if (_waitNextFrame)
            {
                _waitNextFrame = false;
            }

            // 先推进上一帧产生的等待指令。指令未完成时，协程保持暂停。
            if (_currentYield != null)
            {
                _currentYield.Tick();
                if (!_currentYield.IsCompleted)
                {
                    return true;
                }

                _currentYield = null;
            }

            // 栈顶是当前正在执行的迭代器；嵌套 IEnumerator 会被压入栈顶。
            while (_stack.Count > 0)
            {
                IEnumerator top = _stack.Peek();
                bool hasNext;

                try
                {
                    // MoveNext 执行协程方法，Current 保存本次 yield 的对象。
                    hasNext = top.MoveNext();
                    if (_isDone)
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    // 单个协程异常视为该协程结束，并保留异常堆栈日志。
                    Log.Error($"[Coroutine] Exception in #{Id}", ex);
                    return false;
                }

                if (!hasNext)
                {
                    // 当前迭代器执行完毕，先释放其资源，再返回父级迭代器继续执行。
                    _stack.Pop();
                    DisposeEnumerator(top);
                    continue;
                }

                object current = top.Current;
                if (current == null)
                {
                    // null 表示等待下一帧，避免同一帧继续执行后续代码。
                    _waitNextFrame = true;
                    return true;
                }

                if (ReferenceEquals(current, NextFrame.Instance))
                {
                    // NextFrame 单例是共享状态，由调度器特殊处理为等待下一帧（等价 yield return null），可安全复用。
                    _waitNextFrame = true;
                    return true;
                }

                if (current is IYieldInstruction instruction)
                {
                    if (instruction is WaitForFrames)
                    {
                        // 等待帧数从下一次 Tick 开始消耗，避免 WaitForFrames(1) 在当前帧立即完成。
                        _currentYield = instruction;
                        return true;
                    }

                    // 时间和条件类指令在首次产生时立即检查，零时长或已满足的条件无需额外等待一帧。
                    instruction.Tick();
                    if (!instruction.IsCompleted)
                    {
                        // 指令尚未完成，保存下来，在后续帧继续推进。
                        _currentYield = instruction;
                        return true;
                    }

                    // 指令已完成，继续处理当前迭代器的下一次 yield。
                    continue;
                }

                if (current is IEnumerator nested)
                {
                    // 嵌套协程以内联方式执行，父迭代器暂停，子迭代器成为新的栈顶。
                    _stack.Push(nested);
                    continue;
                }

                // 不支持的 yield 对象按等待一帧处理，避免阻塞整个协程。
                Log.Debug($"[Coroutine] Unknown yield object '{current.GetType().Name}' in #{Id}, treated as next frame");
                _waitNextFrame = true;
                return true;
            }

            // 所有迭代器都已执行完毕，协程结束。
            return false;
        }

        private void DisposeStack()
        {
            while (_stack.Count > 0)
            {
                DisposeEnumerator(_stack.Pop());
            }
        }

        private void DisposeEnumerator(IEnumerator enumerator)
        {
            if (!(enumerator is IDisposable disposable))
            {
                return;
            }

            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error($"[Coroutine] Exception while disposing #{Id}", ex);
            }
        }
    }
}
