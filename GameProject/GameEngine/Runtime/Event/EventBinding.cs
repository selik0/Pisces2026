using System;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 保存同一个 EventKey 下的监听委托，并负责委托类型校验与逐个调用。
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
            if (_callbackType != null && _callbackType != typeof(TCallback))
            {
                return false;
            }

            _callbackType = typeof(TCallback);
            _callbacks.Add(callback);
            return true;
        }

        public bool TryRemove<TCallback>(TCallback callback, out bool isEmpty) where TCallback : Delegate
        {
            isEmpty = false;
            if (_callbackType != typeof(TCallback))
            {
                return false;
            }

            _callbacks.Remove(callback);
            isEmpty = _callbacks.Count == 0;
            return true;
        }

        public bool Invoke()
        {
            if (_callbackType != typeof(Action))
            {
                return false;
            }

            foreach (Delegate callback in _callbacks)
            {
                try
                {
                    ((Action)callback)();
                }
                catch (Exception caughtException)
                {
                    Log.Error($"[EventBus] Exception in listener for key={_eventKey}", caughtException);
                }
            }

            return true;
        }

        public bool Invoke<T1>(T1 arg1)
        {
            if (_callbackType != typeof(Action<T1>))
            {
                return false;
            }

            foreach (Delegate callback in _callbacks)
            {
                try
                {
                    ((Action<T1>)callback)(arg1);
                }
                catch (Exception caughtException)
                {
                    Log.Error($"[EventBus] Exception in listener for key={_eventKey}", caughtException);
                }
            }

            return true;
        }

        public bool Invoke<T1, T2>(T1 arg1, T2 arg2)
        {
            if (_callbackType != typeof(Action<T1, T2>))
            {
                return false;
            }

            foreach (Delegate callback in _callbacks)
            {
                try
                {
                    ((Action<T1, T2>)callback)(arg1, arg2);
                }
                catch (Exception caughtException)
                {
                    Log.Error($"[EventBus] Exception in listener for key={_eventKey}", caughtException);
                }
            }

            return true;
        }

        public bool Invoke<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
        {
            if (_callbackType != typeof(Action<T1, T2, T3>))
            {
                return false;
            }

            foreach (Delegate callback in _callbacks)
            {
                try
                {
                    ((Action<T1, T2, T3>)callback)(arg1, arg2, arg3);
                }
                catch (Exception caughtException)
                {
                    Log.Error($"[EventBus] Exception in listener for key={_eventKey}", caughtException);
                }
            }

            return true;
        }
    }
}
