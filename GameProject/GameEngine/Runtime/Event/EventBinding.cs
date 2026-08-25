using System;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 保存同一个 EventKey 下的监听委托，并负责委托类型校验与逐个调用。
    /// 注意：回调执行期间禁止 Subscribe / Unsubscribe 修改订阅集合，
    /// 否则遍历时枚举器会抛 InvalidOperationException；需要注销的监听器应延迟到派发结束后处理。
    /// </summary>
    internal sealed class EventBinding
    {
        private readonly HashSet<Delegate> _callbacks = new HashSet<Delegate>();
        private readonly int _eventKey;
        private Type _callbackType;

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

            return _callbacks.Add(callback);
        }

        public bool TryRemove<TCallback>(TCallback callback, out bool isEmpty) where TCallback : Delegate
        {
            isEmpty = false;
            if (!VaildType<TCallback>())
            {
                return false;
            }

            _callbacks.Remove(callback);
            isEmpty = _callbacks.Count == 0;
            return true;
        }

        public bool Invoke()
        {
            if (!VaildType<Action>())
            {
                return false;
            }

            // 直接遍历订阅集合，不做快照：
            // 回调执行期间禁止 Subscribe / Unsubscribe 修改本集合，否则枚举器会抛 InvalidOperationException。
            // 需要注销的监听器应延迟到本次派发结束之后处理（例如用标志位收集后统一注销）。
            foreach (Delegate callback in _callbacks)
            {
                try
                {
                    ((Action)callback)();
                }
                catch (Exception caughtException)
                {
                    Log.Error($"[EventManager] Exception in listener for key={_eventKey} Name={callback.Method.DeclaringType}.{callback.Method.Name}", caughtException);
                }
            }

            return true;
        }

        public bool Invoke<T1>(T1 arg1)
        {
            if (!VaildType<Action<T1>>())
            {
                return false;
            }

            // 直接遍历订阅集合，不做快照：
            // 回调执行期间禁止 Subscribe / Unsubscribe 修改本集合，否则枚举器会抛 InvalidOperationException。
            // 需要注销的监听器应延迟到本次派发结束之后处理（例如用标志位收集后统一注销）。
            foreach (Delegate callback in _callbacks)
            {
                try
                {
                    ((Action<T1>)callback)(arg1);
                }
                catch (Exception caughtException)
                {
                    Log.Error($"[EventManager] Exception in listener for key={_eventKey} Name={callback.Method.DeclaringType}.{callback.Method.Name}", caughtException);
                }
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
            foreach (Delegate callback in _callbacks)
            {
                try
                {
                    ((Action<T1, T2>)callback)(arg1, arg2);
                }
                catch (Exception caughtException)
                {
                    Log.Error($"[EventManager] Exception in listener for key={_eventKey} Name={callback.Method.DeclaringType}.{callback.Method.Name}", caughtException);
                }
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
            foreach (Delegate callback in _callbacks)
            {
                try
                {
                    ((Action<T1, T2, T3>)callback)(arg1, arg2, arg3);
                }
                catch (Exception caughtException)
                {
                    Log.Error($"[EventManager] Exception in listener for key={_eventKey} Name={callback.Method.DeclaringType}.{callback.Method.Name}", caughtException);
                }
            }

            return true;
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
