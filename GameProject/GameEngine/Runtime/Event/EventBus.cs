using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// 事件总线，以事件参数的 <see cref="Type"/> 为路由键进行类型安全的发布/订阅。
    /// 每个事件类型对应一组订阅者，无需额外的事件键定义。
    ///
    /// <para><b>核心特性</b></para>
    /// <list type="bullet">
    ///   <item>以参数类型路由，编译期类型安全</item>
    ///   <item>支持 once 模式：触发一次后自动解绑</item>
    ///   <item>支持 GameObject 生命周期绑定：对象销毁后自动解绑</item>
    ///   <item>Debug 模式：每次 Subscribe / Emit 均输出调用信息</item>
    /// </list>
    ///
    /// <para><b>非线程安全</b>，应仅在 Unity 主线程使用。</para>
    ///
    /// <code>
    /// var bus = new EventBus { DebugMode = true };
    ///
    /// // 订阅
    /// bus.Subscribe&lt;ScoreChangedEvent&gt;(e => UpdateUI(e.Score));
    ///
    /// // once 模式
    /// bus.Subscribe&lt;PlayerDiedEvent&gt;(e => ShowGameOver(), once: true);
    ///
    /// // 绑定 GameObject 生命周期
    /// bus.Subscribe&lt;ScoreChangedEvent&gt;(e => UpdateUI(e.Score), boundObject: this.gameObject);
    ///
    /// // 发布
    /// bus.Emit(new ScoreChangedEvent { Score = 100 });
    ///
    /// // 取消订阅
    /// bus.Unsubscribe&lt;ScoreChangedEvent&gt;(myCallback);
    /// </code>
    /// </summary>
    public sealed class EventBus
    {
        // Type → BindingList<T>（存为 object 规避泛型擦除）
        private readonly Dictionary<Type, object> _bindings = new Dictionary<Type, object>();

        /// <summary>
        /// 是否开启 Debug 模式。
        /// 开启后，Subscribe / Unsubscribe / Emit 均通过 <see cref="Log"/> 输出调用信息。
        /// </summary>
        public bool DebugMode { get; set; }

        // ── Subscribe ────────────────────────────────────────────────────────────

        /// <summary>
        /// 订阅事件。
        /// </summary>
        /// <typeparam name="T">事件参数类型，同时作为路由键</typeparam>
        /// <param name="callback">回调，不可为 null</param>
        /// <param name="once">true 表示只触发一次后自动解绑</param>
        /// <param name="boundObject">
        /// 绑定的 Unity GameObject。该对象被销毁后，此订阅自动失效。
        /// 传 null 表示不绑定生命周期。
        /// </param>
        public void Subscribe<T>(Action<T> callback, bool once = false, GameObject boundObject = null)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            var list = GetOrCreateList<T>();
            list.Add(new EventBinding<T>(callback, once, boundObject));

            if (DebugMode)
                Log.Debug($"[EventBus] Subscribe  <{typeof(T).Name}>  once={once}  bound={BoundName(boundObject)}  listeners={list.Count}");
        }

        // ── Unsubscribe ──────────────────────────────────────────────────────────

        /// <summary>
        /// 取消订阅指定回调。若同一回调注册了多次，全部移除。
        /// </summary>
        /// <typeparam name="T">事件参数类型</typeparam>
        /// <param name="callback">要移除的回调</param>
        public void Unsubscribe<T>(Action<T> callback)
        {
            if (callback == null) return;
            if (!_bindings.TryGetValue(typeof(T), out var raw)) return;

            var list = (BindingList<T>)raw;
            int removed = list.RemoveAll(b => b.Callback == callback);

            if (DebugMode)
                Log.Debug($"[EventBus] Unsubscribe  <{typeof(T).Name}>  removed={removed}  listeners={list.Count}");
        }

        /// <summary>
        /// 移除与指定 <paramref name="boundObject"/> 绑定的所有订阅（跨所有事件类型）。
        /// 通常不需要手动调用——Emit 时会自动清理过期绑定。
        /// 若要主动提前解绑某个对象的所有监听，可调用此方法。
        /// </summary>
        /// <param name="boundObject">要解绑的 GameObject</param>
        public void UnsubscribeAll(GameObject boundObject)
        {
            if (boundObject == null) return;
            foreach (var raw in _bindings.Values)
            {
                if (raw is IBindingList bl)
                    bl.RemoveByBoundObject(boundObject);
            }

            if (DebugMode)
                Log.Debug($"[EventBus] UnsubscribeAll  bound={BoundName(boundObject)}");
        }

        /// <summary>清除指定事件类型下的所有订阅。</summary>
        /// <typeparam name="T">事件参数类型</typeparam>
        public void Clear<T>()
        {
            if (_bindings.TryGetValue(typeof(T), out var raw))
            {
                var list = (BindingList<T>)raw;
                int count = list.Count;
                list.Clear();

                if (DebugMode)
                    Log.Debug($"[EventBus] Clear  <{typeof(T).Name}>  removed={count}");
            }
        }

        /// <summary>清除所有事件类型的所有订阅。</summary>
        public void ClearAll()
        {
            int total = 0;
            foreach (var raw in _bindings.Values)
                if (raw is IBindingList bl) total += bl.Count;
            _bindings.Clear();

            if (DebugMode)
                Log.Debug($"[EventBus] ClearAll  removed={total}");
        }

        // ── Emit ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 发布事件，依次调用所有有效订阅者。
        /// <para>
        /// 执行顺序：按订阅先后（FIFO）。<br/>
        /// 安全：过期绑定（GameObject 已销毁 / once 已触发）在此次 Emit 后被清理。<br/>
        /// 稳定：Emit 期间新增的订阅不会在本次派发中触发（快照遍历）。
        /// </para>
        /// </summary>
        /// <typeparam name="T">事件参数类型（由传入实参推断）</typeparam>
        /// <param name="arg">事件参数</param>
        public void Emit<T>(T arg)
        {
            if (!_bindings.TryGetValue(typeof(T), out var raw)) return;

            var list = (BindingList<T>)raw;

            if (DebugMode)
                Log.Debug($"[EventBus] Emit  <{typeof(T).Name}>  arg={arg}  listeners={list.Count}");

            // 快照遍历，避免回调内修改列表时出错
            var snapshot = list.ToArray();
            var toRemove = new List<EventBinding<T>>();

            foreach (var binding in snapshot)
            {
                if (binding.IsExpired)
                {
                    toRemove.Add(binding);
                    if (DebugMode)
                        Log.Debug($"[EventBus]   └─ skip (expired/destroyed)  <{typeof(T).Name}>");
                    continue;
                }

                try
                {
                    binding.TryInvoke(arg);
                    if (DebugMode)
                        Log.Debug($"[EventBus]   └─ invoked  <{typeof(T).Name}>  once={binding.Once}");
                }
                catch (Exception ex)
                {
                    Log.Error($"[EventBus] Exception in listener for <{typeof(T).Name}>", ex);
                }

                if (binding.Once)
                    toRemove.Add(binding);
            }

            // 批量清理过期 + once 绑定
            foreach (var b in toRemove)
                list.Remove(b);
        }

        // ── 辅助 ─────────────────────────────────────────────────────────────────

        private BindingList<T> GetOrCreateList<T>()
        {
            var type = typeof(T);
            if (_bindings.TryGetValue(type, out var raw))
                return (BindingList<T>)raw;

            var list = new BindingList<T>();
            _bindings[type] = list;
            return list;
        }

        private static string BoundName(GameObject obj)
            => obj != null ? obj.name : "none";
    }
}
