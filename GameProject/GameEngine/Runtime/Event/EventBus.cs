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
        private readonly Dictionary<int, Delegate> _bindings = new Dictionary<int, Delegate>();

        public void Subscribe(int eventKey, Action callback)
        {
            if (callback == null)
            {
                Log.Error("[EventBus] Subscribe failed: callback is null.");
                return;
            }

            if (!_bindings.ContainsKey(eventKey))
            {
                _bindings.Add(eventKey, callback);
                return;
            }

            if (!TryGetAction(eventKey, out Action action))
            {
                return;
            }

            action += callback;
            _bindings[eventKey] = action;
        }

        public void Subscribe<T1>(int eventKey, Action<T1> callback)
        {
            if (callback == null)
            {
                Log.Error("[EventBus] Subscribe failed: callback is null.");
                return;
            }

            if (!_bindings.ContainsKey(eventKey))
            {
                _bindings.Add(eventKey, callback);
                return;
            }

            if (!TryGetAction(eventKey, out Action<T1> action))
            {
                return;
            }

            action += callback;
            _bindings[eventKey] = action;
        }

        public void Subscribe<T1, T2>(int eventKey, Action<T1, T2> callback)
        {
            if (callback == null)
            {
                Log.Error("[EventBus] Subscribe failed: callback is null.");
                return;
            }

            if (!_bindings.ContainsKey(eventKey))
            {
                _bindings.Add(eventKey, callback);
                return;
            }

            if (!TryGetAction(eventKey, out Action<T1, T2> action))
            {
                return;
            }

            action += callback;
            _bindings[eventKey] = action;
        }

        public void Subscribe<T1, T2, T3>(int eventKey, Action<T1, T2, T3> callback)
        {
            if (callback == null)
            {
                Log.Error("[EventBus] Subscribe failed: callback is null.");
                return;
            }

            if (!_bindings.ContainsKey(eventKey))
            {
                _bindings.Add(eventKey, callback);
                return;
            }

            if (!TryGetAction(eventKey, out Action<T1, T2, T3> action))
            {
                return;
            }

            action += callback;
            _bindings[eventKey] = action;
        }

        public void Unsubscribe(int eventKey, Action callback)
        {
            if (callback == null || !TryGetAction(eventKey, out Action action))
            {
                return;
            }

            action -= callback;
            if (action == null)
            {
                _bindings.Remove(eventKey);
            }
            else
            {
                _bindings[eventKey] = action;
            }
        }

        public void Unsubscribe<T1>(int eventKey, Action<T1> callback)
        {
            if (callback == null || !TryGetAction(eventKey, out Action<T1> action))
            {
                return;
            }

            action -= callback;
            if (action == null)
            {
                _bindings.Remove(eventKey);
            }
            else
            {
                _bindings[eventKey] = action;
            }
        }

        public void Unsubscribe<T1, T2>(int eventKey, Action<T1, T2> callback)
        {
            if (callback == null || !TryGetAction(eventKey, out Action<T1, T2> action))
            {
                return;
            }

            action -= callback;
            if (action == null)
            {
                _bindings.Remove(eventKey);
            }
            else
            {
                _bindings[eventKey] = action;
            }
        }

        public void Unsubscribe<T1, T2, T3>(int eventKey, Action<T1, T2, T3> callback)
        {
            if (callback == null || !TryGetAction(eventKey, out Action<T1, T2, T3> action))
            {
                return;
            }

            action -= callback;
            if (action == null)
            {
                _bindings.Remove(eventKey);
            }
            else
            {
                _bindings[eventKey] = action;
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
            if (!TryGetAction(eventKey, out Action action))
            {
                return;
            }

            try
            {
                action.Invoke();
            }
            catch (Exception exception)
            {
                Log.Error($"[EventBus] Exception in listener for key={eventKey}", exception);
            }
        }

        public void Emit<T1>(int eventKey, T1 arg1)
        {
            if (!TryGetAction(eventKey, out Action<T1> action))
            {
                return;
            }

            try
            {
                action.Invoke(arg1);
            }
            catch (Exception exception)
            {
                Log.Error($"[EventBus] Exception in listener for key={eventKey}", exception);
            }
        }

        public void Emit<T1, T2>(int eventKey, T1 arg1, T2 arg2)
        {
            if (!TryGetAction(eventKey, out Action<T1, T2> action))
            {
                return;
            }

            try
            {
                action.Invoke(arg1, arg2);
            }
            catch (Exception exception)
            {
                Log.Error($"[EventBus] Exception in listener for key={eventKey}", exception);
            }
        }

        public void Emit<T1, T2, T3>(int eventKey, T1 arg1, T2 arg2, T3 arg3)
        {
            if (!TryGetAction(eventKey, out Action<T1, T2, T3> action))
            {
                return;
            }

            try
            {
                action.Invoke(arg1, arg2, arg3);
            }
            catch (Exception exception)
            {
                Log.Error($"[EventBus] Exception in listener for key={eventKey}", exception);
            }
        }

        private bool TryGetAction<TCall>(int eventKey, out TCall action) where TCall : Delegate
        {
            if (!_bindings.TryGetValue(eventKey, out Delegate existing))
            {
                action = null;
                return false;
            }
            if (existing.GetType() != typeof(TCall))
            {
                Log.Error($"[EventBus] EventKey {eventKey} uses a different callback type.");
                action = null;
                return false;
            }

            action = existing as TCall;
            return action != null;
        }
    }
}
