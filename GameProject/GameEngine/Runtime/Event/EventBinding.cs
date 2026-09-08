using System;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 保存同一个 EventKey 下的监听委托，并负责委托类型校验与逐个调用。
    /// 回调执行期间的订阅变更会暂存，并在最外层派发完成后统一提交。
    /// </summary>
    internal sealed class EventBinding
    {
        private readonly HashSet<Delegate> _callbacks = new HashSet<Delegate>();
        private readonly List<PendingOperation> _pendingOperations = new List<PendingOperation>();
        private readonly int _eventKey;
        private Type _callbackType;
        private int _invokeDepth;
        private bool _clearPending;

        private sealed class PendingOperation
        {
            public Delegate Callback;
            public bool IsAdd;
        }

        public EventBinding(int eventKey)
        {
            _eventKey = eventKey;
        }

        public bool TryAdd<TCallback>(TCallback callback) where TCallback : Delegate
        {
            if (!VaildType<TCallback>())
            {
                return false;
            }

            if (_invokeDepth > 0)
            {
                _pendingOperations.Add(new PendingOperation { Callback = callback, IsAdd = true });
                return !_callbacks.Contains(callback);
            }

            return _callbacks.Add(callback);
        }

        public bool TryRemove<TCallback>(TCallback callback) where TCallback : Delegate
        {
            if (!VaildType<TCallback>())
            {
                return false;
            }

            if (_invokeDepth > 0)
            {
                _pendingOperations.Add(new PendingOperation { Callback = callback, IsAdd = false });
                return true;
            }

            _callbacks.Remove(callback);
            return true;
        }

        public void Clear()
        {
            if (_invokeDepth > 0)
            {
                _clearPending = true;
                _pendingOperations.Clear();
                return;
            }

            _callbacks.Clear();
            _pendingOperations.Clear();
            _clearPending = false;
        }

        public bool Invoke()
        {
            if (!VaildType<Action>())
            {
                return false;
            }

            _invokeDepth++;
            try
            {
                foreach (Delegate callback in _callbacks)
                {
                    InvokeCallback(() => ((Action)callback)(), $"{callback.Method.DeclaringType?.Name}.{callback.Method.Name}");
                }
            }
            finally
            {
                EndInvoke();
            }

            return true;
        }

        public bool Invoke<T1>(T1 arg1)
        {
            if (!VaildType<Action<T1>>())
            {
                return false;
            }

            _invokeDepth++;
            try
            {
                foreach (Delegate callback in _callbacks)
                {
                    InvokeCallback(() => ((Action<T1>)callback)(arg1), $"{callback.Method.DeclaringType?.Name}.{callback.Method.Name}");
                }
            }
            finally
            {
                EndInvoke();
            }

            return true;
        }

        public bool Invoke<T1, T2>(T1 arg1, T2 arg2)
        {
            if (!VaildType<Action<T1, T2>>())
            {
                return false;
            }

            // 直接遍历订阅集合，不做快照：
            // 回调执行期间禁止 Subscribe / Unsubscribe 修改本集合，否则枚举器会抛 InvalidOperationException。
            // 需要注销的监听器应延迟到本次派发结束之后处理（例如用标志位收集后统一注销）。
            _invokeDepth++;
            try
            {
                foreach (Delegate callback in _callbacks)
                {
                    InvokeCallback(() => ((Action<T1, T2>)callback)(arg1, arg2), $"{callback.Method.DeclaringType?.Name}.{callback.Method.Name}");
                }
            }
            finally
            {
                EndInvoke();
            }

            return true;
        }

        public bool Invoke<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
        {
            if (!VaildType<Action<T1, T2, T3>>())
            {
                return false;
            }

            // 直接遍历订阅集合，不做快照：
            // 回调执行期间禁止 Subscribe / Unsubscribe 修改本集合，否则枚举器会抛 InvalidOperationException。
            // 需要注销的监听器应延迟到本次派发结束之后处理（例如用标志位收集后统一注销）。
            _invokeDepth++;
            try
            {
                foreach (Delegate callback in _callbacks)
                {
                    InvokeCallback(() => ((Action<T1, T2, T3>)callback)(arg1, arg2, arg3), $"{callback.Method.DeclaringType?.Name}.{callback.Method.Name}");
                }
            }
            finally
            {
                EndInvoke();
            }

            return true;
        }

        private void EndInvoke()
        {
            _invokeDepth--;
            if (_invokeDepth != 0)
            {
                return;
            }

            if (_clearPending)
            {
                _callbacks.Clear();
                _pendingOperations.Clear();
                _clearPending = false;
                return;
            }

            for (int i = 0; i < _pendingOperations.Count; i++)
            {
                PendingOperation operation = _pendingOperations[i];
                if (operation.IsAdd)
                {
                    _callbacks.Add(operation.Callback);
                }
                else
                {
                    _callbacks.Remove(operation.Callback);
                }
            }

            _pendingOperations.Clear();
        }

        private void InvokeCallback(Action callback, string listenerName)
        {
            try
            {
                callback();
            }
            catch (Exception caughtException)
            {
                Log.Error($"[EventManager] Exception in listener for key={_eventKey} Name={listenerName}", caughtException);
            }
        }

        private bool VaildType<TCallback>() where TCallback : Delegate
        {
            Type expectedType = typeof(TCallback);
            if (_callbackType == null)
            {
                _callbackType = expectedType;
                return true;
            }
            if (_callbackType == expectedType)
            {
                return true;
            }

            string registeredTypeName = _callbackType.FullName;
            Log.Error($"[EventBinding] Callback type mismatch: key={_eventKey}, registeredType={registeredTypeName}, requestedType={expectedType.FullName}.");
            return false;
        }
    }
}
