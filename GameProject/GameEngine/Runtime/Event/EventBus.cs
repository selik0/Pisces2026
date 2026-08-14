using System;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 以 int 类型 EventKey 路由的事件总线，支持零到三个事件参数。
    /// 同一个 EventKey 必须始终使用相同的参数数量和参数类型。
    /// </summary>
    /// <remarks>非线程安全，应仅在 Unity 主线程使用。</remarks>
    public sealed class EventBus
    {
        private readonly Dictionary<int, EventBinding> _bindings = new Dictionary<int, EventBinding>();

        public void Subscribe(int eventKey, Action callback)
        {
            SubscribeInternal(eventKey, callback);
        }

        public void Subscribe<T1>(int eventKey, Action<T1> callback)
        {
            SubscribeInternal(eventKey, callback);
        }

        public void Subscribe<T1, T2>(int eventKey, Action<T1, T2> callback)
        {
            SubscribeInternal(eventKey, callback);
        }

        public void Subscribe<T1, T2, T3>(int eventKey, Action<T1, T2, T3> callback)
        {
            SubscribeInternal(eventKey, callback);
        }

        public void Unsubscribe(int eventKey, Action callback)
        {
            UnsubscribeInternal(eventKey, callback);
        }

        public void Unsubscribe<T1>(int eventKey, Action<T1> callback)
        {
            UnsubscribeInternal(eventKey, callback);
        }

        public void Unsubscribe<T1, T2>(int eventKey, Action<T1, T2> callback)
        {
            UnsubscribeInternal(eventKey, callback);
        }

        public void Unsubscribe<T1, T2, T3>(int eventKey, Action<T1, T2, T3> callback)
        {
            UnsubscribeInternal(eventKey, callback);
        }

        private void SubscribeInternal<TCallback>(int eventKey, TCallback callback)
            where TCallback : Delegate
        {
            if (callback == null)
            {
                Log.Error("[EventBus] Subscribe failed: callback is null.");
                return;
            }

            if (!_bindings.TryGetValue(eventKey, out EventBinding binding))
            {
                binding = new EventBinding(eventKey);
                _bindings.Add(eventKey, binding);
            }

            if (!binding.TryAdd(callback))
            {
                Log.Error($"[EventBus] EventKey {eventKey} uses a different callback type.");
            }
        }

        private void UnsubscribeInternal<TCallback>(int eventKey, TCallback callback)
            where TCallback : Delegate
        {
            if (callback == null || !_bindings.TryGetValue(eventKey, out EventBinding binding))
            {
                return;
            }

            if (!binding.TryRemove(callback, out bool isEmpty))
            {
                Log.Error($"[EventBus] EventKey {eventKey} uses a different callback type.");
                return;
            }

            if (isEmpty)
            {
                _bindings.Remove(eventKey);
            }
        }

        /// <summary>清除指定 EventKey 下的所有订阅。</summary>
        public void Clear(int eventKey)
        {
            if (!_bindings.Remove(eventKey))
            {
                return;
            }

            Log.Debug($"[EventBus] Clear key={eventKey}");
        }

        /// <summary>清除全部订阅。</summary>
        public void ClearAll()
        {
            int removed = _bindings.Count;
            _bindings.Clear();
            Log.Debug($"[EventBus] ClearAll removed={removed}");
        }

        public void Emit(int eventKey)
        {
            if (!_bindings.TryGetValue(eventKey, out EventBinding binding))
            {
                return;
            }

            binding.Invoke();
        }

        public void Emit<T1>(int eventKey, T1 arg1)
        {
            if (!_bindings.TryGetValue(eventKey, out EventBinding binding))
            {
                return;
            }

            binding.Invoke(arg1);
        }

        public void Emit<T1, T2>(int eventKey, T1 arg1, T2 arg2)
        {
            if (!_bindings.TryGetValue(eventKey, out EventBinding binding))
            {
                return;
            }

            binding.Invoke(arg1, arg2);
        }

        public void Emit<T1, T2, T3>(int eventKey, T1 arg1, T2 arg2, T3 arg3)
        {
            if (!_bindings.TryGetValue(eventKey, out EventBinding binding))
            {
                return;
            }

            binding.Invoke(arg1, arg2, arg3);
        }
    }
}
