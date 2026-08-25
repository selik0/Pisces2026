using System;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 以 int 类型 EventKey 路由的事件总线，支持零到三个事件参数。
    /// 同一个 EventKey 必须始终使用相同的参数数量和参数类型。
    /// 监听器回调中禁止 Subscribe / Unsubscribe 修改同一事件的订阅集合（会破坏遍历并抛异常），
    /// 需要注销的监听器应延迟到 Emit 返回之后处理。
    /// </summary>
    /// <remarks>非线程安全，应仅在 Unity 主线程使用。</remarks>
    public sealed class EventManager : Singleton<EventManager>, ILogin
    {
        private readonly Dictionary<int, EventBinding> _bindings = new Dictionary<int, EventBinding>();

        public EventManager()
        {
        }

        public bool Subscribe(int eventKey, Action callback)
        {
            return SubscribeInternal(eventKey, callback);
        }

        public bool Subscribe<T1>(int eventKey, Action<T1> callback)
        {
            return SubscribeInternal(eventKey, callback);
        }

        public bool Subscribe<T1, T2>(int eventKey, Action<T1, T2> callback)
        {
            return SubscribeInternal(eventKey, callback);
        }

        public bool Subscribe<T1, T2, T3>(int eventKey, Action<T1, T2, T3> callback)
        {
            return SubscribeInternal(eventKey, callback);
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

        private bool SubscribeInternal<TCallback>(int eventKey, TCallback callback)
            where TCallback : Delegate
        {
            if (callback == null)
            {
                Log.Error("[EventManager] Subscribe failed: callback is null.");
                return false;
            }

            if (!_bindings.TryGetValue(eventKey, out EventBinding binding))
            {
                binding = new EventBinding(eventKey);
                _bindings.Add(eventKey, binding);
            }

            return binding.TryAdd(callback);
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

            Log.Debug($"[EventManager] Clear key={eventKey}");
        }

        /// <summary>清除全部订阅。</summary>
        public void ClearAll()
        {
            int removed = _bindings.Count;
            _bindings.Clear();
            Log.Debug($"[EventManager] ClearAll removed={removed}");
        }

        public void Login()
        {
            ClearAll();
        }

        public void Logout()
        {
            ClearAll();
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
