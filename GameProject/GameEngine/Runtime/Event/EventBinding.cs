using System;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// 单个事件监听绑定，内部持有回调、once 标记与可选的 GameObject 弱引用。
    /// <para>
    /// GameObject 绑定语义：若绑定了 <see cref="BoundObject"/>，则每次派发前
    /// 先检查 GameObject 是否仍然存在（<c>obj != null</c>，Unity 重载了 == null）；
    /// 若已销毁，该绑定自动标记为 <see cref="IsExpired"/>，不再触发回调。
    /// </para>
    /// </summary>
    /// <typeparam name="T">事件参数类型</typeparam>
    internal sealed class EventBinding<T>
    {
        /// <summary>事件回调</summary>
        public Action<T> Callback { get; }

        /// <summary>是否只触发一次</summary>
        public bool Once { get; }

        /// <summary>
        /// 绑定的 Unity GameObject（可为 null，表示不绑定）。
        /// 使用普通引用即可，Unity 的 == null 重载会在对象销毁后返回 true。
        /// </summary>
        private readonly GameObject _boundObject;

        /// <summary>是否绑定了 GameObject</summary>
        public bool HasBoundObject => _boundObject != null || _hasBoundObjectSet;

        // 需要区分"没有绑定"与"绑定了但已被销毁"两种状态
        private readonly bool _hasBoundObjectSet;

        /// <summary>
        /// 绑定是否已过期：once 已触发，或绑定的 GameObject 已被销毁。
        /// </summary>
        public bool IsExpired
        {
            get
            {
                if (_hasBoundObjectSet && _boundObject == null)
                    return true;  // GameObject 已销毁（Unity == null 重载）
                return false;
            }
        }

        /// <summary>
        /// 构造一个事件绑定。
        /// </summary>
        /// <param name="callback">事件回调，不可为 null</param>
        /// <param name="once">true 表示触发一次后自动移除</param>
        /// <param name="boundObject">绑定的 GameObject，销毁后自动解绑；传 null 表示不绑定</param>
        public EventBinding(Action<T> callback, bool once = false, GameObject boundObject = null)
        {
            Callback        = callback ?? throw new ArgumentNullException(nameof(callback));
            Once            = once;
            _boundObject    = boundObject;
            _hasBoundObjectSet = boundObject != null;
        }

        /// <summary>
        /// 判断此绑定是否绑定了指定的 <paramref name="target"/> GameObject。
        /// </summary>
        public bool IsBoundTo(GameObject target) => _hasBoundObjectSet && _boundObject == target;

        /// <summary>
        /// 尝试执行回调。
        /// </summary>
        /// <param name="arg">事件参数</param>
        /// <returns>
        /// <c>true</c>  — 回调已执行（once 绑定执行后应立即从列表中移除）<br/>
        /// <c>false</c> — 绑定已过期，不执行
        /// </returns>
        public bool TryInvoke(T arg)
        {
            if (IsExpired) return false;
            Callback(arg);
            return true;
        }
    }
}
