using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// 以 int 类型 EventKey 路由的事件总线，支持零到三个事件参数。
    /// 同一个 EventKey 必须始终使用相同的参数数量和参数类型。
    /// </summary>
    /// <remarks>非线程安全，应仅在 Unity 主线程使用。</remarks>
    public sealed class EventBus
    {
        private readonly Dictionary<int, IBindingList> _bindings = new Dictionary<int, IBindingList>();

        /// <summary>
        /// 开启后，订阅、取消订阅和派发操作会通过 <see cref="Log"/> 输出日志。
        /// </summary>
        public bool DebugMode { get; set; }

        public void Subscribe(int eventKey, Action callback, bool once = false, GameObject boundObject = null)
        {
            SubscribeInternal(eventKey, callback, once, boundObject);
        }

        public void Subscribe<T1>(int eventKey, Action<T1> callback, bool once = false, GameObject boundObject = null)
        {
            SubscribeInternal(eventKey, callback, once, boundObject);
        }

        public void Subscribe<T1, T2>(int eventKey, Action<T1, T2> callback, bool once = false, GameObject boundObject = null)
        {
            SubscribeInternal(eventKey, callback, once, boundObject);
        }

        public void Subscribe<T1, T2, T3>(int eventKey, Action<T1, T2, T3> callback, bool once = false, GameObject boundObject = null)
        {
            SubscribeInternal(eventKey, callback, once, boundObject);
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

        /// <summary>移除指定 GameObject 在此事件总线上的所有绑定。</summary>
        public void UnsubscribeAll(GameObject boundObject)
        {
            if (boundObject == null)
            {
                return;
            }

            int removed = 0;
            foreach (IBindingList list in _bindings.Values)
            {
                int previousCount = list.Count;
                list.RemoveByBoundObject(boundObject);
                removed += previousCount - list.Count;
            }

            if (DebugMode)
            {
                Log.Debug($"[EventBus] UnsubscribeAll bound={BoundName(boundObject)} removed={removed}");
            }
        }

        /// <summary>清除指定 EventKey 下的所有订阅。</summary>
        public void Clear(int eventKey)
        {
            if (!_bindings.TryGetValue(eventKey, out IBindingList list))
            {
                return;
            }

            _bindings.Remove(eventKey);
            if (DebugMode)
            {
                Log.Debug($"[EventBus] Clear key={eventKey} removed={list.Count}");
            }
        }

        /// <summary>清除全部订阅。</summary>
        public void ClearAll()
        {
            int removed = 0;
            foreach (IBindingList list in _bindings.Values)
            {
                removed += list.Count;
            }

            _bindings.Clear();
            if (DebugMode)
            {
                Log.Debug($"[EventBus] ClearAll removed={removed}");
            }
        }

        public void Emit(int eventKey)
        {
            EmitInternal<Action>(eventKey, callback => callback());
        }

        public void Emit<T1>(int eventKey, T1 arg1)
        {
            EmitInternal<Action<T1>>(eventKey, callback => callback(arg1));
        }

        public void Emit<T1, T2>(int eventKey, T1 arg1, T2 arg2)
        {
            EmitInternal<Action<T1, T2>>(eventKey, callback => callback(arg1, arg2));
        }

        public void Emit<T1, T2, T3>(int eventKey, T1 arg1, T2 arg2, T3 arg3)
        {
            EmitInternal<Action<T1, T2, T3>>(eventKey, callback => callback(arg1, arg2, arg3));
        }

        private void SubscribeInternal<TCallback>(int eventKey, TCallback callback, bool once, GameObject boundObject)
            where TCallback : Delegate
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            BindingList<TCallback> list = GetOrCreateList<TCallback>(eventKey);
            list.Add(new EventBinding<TCallback>(callback, once, boundObject));

            if (DebugMode)
            {
                Log.Debug($"[EventBus] Subscribe key={eventKey} signature={typeof(TCallback).Name} once={once} bound={BoundName(boundObject)} listeners={list.Count}");
            }
        }

        private void UnsubscribeInternal<TCallback>(int eventKey, TCallback callback) where TCallback : Delegate
        {
            if (callback == null || !_bindings.TryGetValue(eventKey, out IBindingList rawList))
            {
                return;
            }

            BindingList<TCallback> list = GetList<TCallback>(eventKey, rawList);
            int removed = list.RemoveAll(binding => binding.Callback.Equals(callback));

            if (DebugMode)
            {
                Log.Debug($"[EventBus] Unsubscribe key={eventKey} removed={removed} listeners={list.Count}");
            }
        }

        private void EmitInternal<TCallback>(int eventKey, Action<TCallback> invoke) where TCallback : Delegate
        {
            if (!_bindings.TryGetValue(eventKey, out IBindingList rawList))
            {
                return;
            }

            BindingList<TCallback> list = GetList<TCallback>(eventKey, rawList);
            EventBinding<TCallback>[] snapshot = list.ToArray();
            var toRemove = new List<EventBinding<TCallback>>();

            if (DebugMode)
            {
                Log.Debug($"[EventBus] Emit key={eventKey} signature={typeof(TCallback).Name} listeners={list.Count}");
            }

            foreach (EventBinding<TCallback> binding in snapshot)
            {
                if (binding.IsExpired)
                {
                    toRemove.Add(binding);
                    continue;
                }

                if (binding.Once)
                {
                    list.Remove(binding);
                }

                try
                {
                    invoke(binding.Callback);
                }
                catch (Exception exception)
                {
                    Log.Error($"[EventBus] Exception in listener for key={eventKey}", exception);
                }
            }

            foreach (EventBinding<TCallback> binding in toRemove)
            {
                list.Remove(binding);
            }
        }

        private BindingList<TCallback> GetOrCreateList<TCallback>(int eventKey) where TCallback : Delegate
        {
            if (_bindings.TryGetValue(eventKey, out IBindingList rawList))
            {
                return GetList<TCallback>(eventKey, rawList);
            }

            var list = new BindingList<TCallback>();
            _bindings.Add(eventKey, list);
            return list;
        }

        private static BindingList<TCallback> GetList<TCallback>(int eventKey, IBindingList rawList)
            where TCallback : Delegate
        {
            if (rawList is BindingList<TCallback> list)
            {
                return list;
            }

            throw new InvalidOperationException(
                $"EventKey {eventKey} uses callback type {rawList.CallbackType}, not {typeof(TCallback)}.");
        }

        private static string BoundName(GameObject boundObject)
        {
            return boundObject != null ? boundObject.name : "none";
        }
    }
}
