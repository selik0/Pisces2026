using System;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// <see cref="GameObject"/> 与 <see cref="EventBus"/> / <see cref="EventSystem"/> 的扩展方法。
    /// 提供将 GameObject 生命周期与事件订阅绑定的便捷 API。
    ///
    /// <code>
    /// // 绑定到全局 EventSystem（推荐）
    /// gameObject.Subscribe&lt;ScoreChangedEvent&gt;(e => RefreshUI(e.Score));
    /// gameObject.Subscribe&lt;PlayerDiedEvent&gt;(e => ShowGameOver(), once: true);
    ///
    /// // 绑定到指定 EventBus
    /// gameObject.Subscribe(myBus, (ScoreChangedEvent e) => RefreshUI(e.Score));
    /// </code>
    ///
    /// <para>
    /// 底层实现：使用 Unity <c>obj != null</c> 在每次 Emit 时自动检测销毁状态，
    /// 无需额外 MonoBehaviour 组件。
    /// </para>
    /// </summary>
    public static class EventSystemExtensions
    {
        // ── 绑定到全局 EventSystem ───────────────────────────────────────────────

        /// <summary>
        /// 以 <paramref name="gameObject"/> 为生命周期将回调订阅到全局 <see cref="EventSystem"/>。
        /// GameObject 销毁后，该订阅在下一次 Emit 时自动清除。
        /// </summary>
        /// <typeparam name="T">事件参数类型</typeparam>
        /// <param name="gameObject">绑定的 GameObject（作为生命周期锚点）</param>
        /// <param name="callback">回调，不可为 null</param>
        /// <param name="once">true 表示只触发一次后自动解绑</param>
        public static void Subscribe<T>(this GameObject gameObject,
                                        Action<T> callback,
                                        bool once = false)
        {
            if (gameObject == null) throw new ArgumentNullException(nameof(gameObject));
            EventSystem.Subscribe(callback, once, gameObject);
        }

        /// <summary>
        /// 取消 <paramref name="gameObject"/> 在全局 <see cref="EventSystem"/> 下
        /// 指定类型事件的某个回调。
        /// </summary>
        public static void Unsubscribe<T>(this GameObject gameObject, Action<T> callback)
        {
            if (gameObject == null) return;
            EventSystem.Unsubscribe(callback);
        }

        /// <summary>
        /// 主动取消 <paramref name="gameObject"/> 绑定到全局 <see cref="EventSystem"/>
        /// 的 <b>所有</b> 订阅（跨所有事件类型）。
        /// <para>通常不需要手动调用——GameObject 销毁时 Emit 会自动清理。
        /// 在需要提前解绑时使用。</para>
        /// </summary>
        public static void UnsubscribeAll(this GameObject gameObject)
        {
            if (gameObject == null) return;
            EventSystem.UnsubscribeAll(gameObject);
        }

        // ── 绑定到指定 EventBus ──────────────────────────────────────────────────

        /// <summary>
        /// 以 <paramref name="gameObject"/> 为生命周期将回调订阅到指定 <paramref name="bus"/>。
        /// </summary>
        /// <typeparam name="T">事件参数类型</typeparam>
        /// <param name="gameObject">绑定的 GameObject</param>
        /// <param name="bus">目标事件总线</param>
        /// <param name="callback">回调</param>
        /// <param name="once">true 表示只触发一次后自动解绑</param>
        public static void Subscribe<T>(this GameObject gameObject,
                                        EventBus bus,
                                        Action<T> callback,
                                        bool once = false)
        {
            if (gameObject == null) throw new ArgumentNullException(nameof(gameObject));
            if (bus == null)        throw new ArgumentNullException(nameof(bus));
            bus.Subscribe(callback, once, gameObject);
        }

        /// <summary>
        /// 主动取消 <paramref name="gameObject"/> 绑定到指定 <paramref name="bus"/>
        /// 的所有订阅（跨所有事件类型）。
        /// </summary>
        public static void UnsubscribeAll(this GameObject gameObject, EventBus bus)
        {
            if (gameObject == null || bus == null) return;
            bus.UnsubscribeAll(gameObject);
        }
    }
}
